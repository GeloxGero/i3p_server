using Microsoft.AspNetCore.Mvc;
using i3p_server.Models;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace i3p_server.Controllers;

// ─── Response DTOs ────────────────────────────────────────────────────────────

public record SchoolImplementationHeaderDto(int Id, int Year, string School, double TotalEstimatedCost, double? AnnualBudget);

public record SchoolImplementationDetailDto(
    int Id,
    int Year,
    string School,
    double TotalEstimatedCost,
    double? AnnualBudget,
    List<MonthSheetDto> Months
);

/// <summary>Body for PUT /api/SchoolImplementation/{id}/budget</summary>
public record SetBudgetRequest(double? AnnualBudget);

public record MonthSheetDto(
    string Month,
    bool HasSip,
    List<SchoolPlanItemDto> Items,
    Dictionary<string, double> SubTotals,
    double GrandTotal
);

public record SchoolPlanItemDto(
    int       Id,
    string    KraArea,
    string    SpecificProgram,
    string    ProgramActivity,
    string    Purpose,
    string    PerformanceIndicator,
    string    ResourceDescription,
    string    Quantity,
    double    EstimatedCost,
    string    AccountTitle,
    string    AccountCode,
    string    Category,
    string?   ArCode,
    bool      IsVerified,
    SipStatus Status
);

public record CreateItemRequest(
    string  Date,
    string  Kra,
    string  SipProgram,
    string  Activity,
    string? Purpose,
    string? Indicator,
    string? Resources,
    string? Quantity,
    double  EstimatedCost,
    string? AccountTitle,
    string? AccountCode,
    string  ExpenditureType,
    SipStatus Status
);

// ─── Template Column Layout ───────────────────────────────────────────────────
// The official SIP template has 4 side-by-side category sections on every month sheet:
//
//   Section 1 — Regular Expenditure        : cols A–J  (1–10)
//   [gap col K = 11]
//   Section 2 — Project Related Expenditure: cols L–U  (12–21)
//   [gap col V = 22]
//   Section 3 — Repair and Maintenance     : cols W–AF (23–32)
//   [gap col AG = 33]
//   Section 4 — Others                     : cols AH–AQ(34–43)
//
//   Within each 10-column section (0-indexed offset from section start):
//     +0  KRA
//     +1  Specific Program (SIP)
//     +2  Programs/Projects/Activities
//     +3  Purpose / Objectives
//     +4  Performance Indicator
//     +5  Resources Needed Description
//     +6  Resources Needed Quantity
//     +7  Estimated Cost
//     +8  Account Title
//     +9  Account Code
//
//   Header row: row 4  (1-based)
//   Data rows : row 5 onward

[Route("api/[controller]")]
[ApiController]
public class SchoolImplementationController : ControllerBase
{
    private readonly AppDbContext _context;

    private static readonly string[] MonthOrder =
    [
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    ];

    private static readonly string[] CategoryOrder =
    [
        "Regular Expenditure",
        "Project Related Expenditure",
        "Repair and Maintenance",
        "Others"
    ];

    // Each tuple: (category name, 1-based starting column of that section)
    private static readonly (string Category, int StartCol)[] TemplateSections =
    [
        ("Regular Expenditure",         1),
        ("Project Related Expenditure", 12),
        ("Repair and Maintenance",      23),
        ("Others",                      34),
    ];

    // Within a section, offset from StartCol (0-based)
    private const int OffKra      = 0;
    private const int OffSip      = 1;
    private const int OffPpa      = 2;
    private const int OffPurpose  = 3;
    private const int OffPerfInd  = 4;
    private const int OffResDesc  = 5;
    private const int OffQty      = 6;
    private const int OffCost     = 7;
    private const int OffAccTitle = 8;
    private const int OffAccCode  = 9;

    private const int HeaderRow = 4; // 1-based row containing column labels
    private const int DataStart = 5; // first data row

    // Required header keywords (lowercased) used for template validation
    private static readonly string[] RequiredHeaderKeywords =
    [
        "key result area",
        "specific program",
        "programs/projects",
        "estimated",
        "account"
    ];

    public SchoolImplementationController(AppDbContext context)
    {
        _context = context;
    }

    // ── GET /api/SchoolImplementation ─────────────────────────────────────────
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SchoolImplementationHeaderDto>>> GetPlans()
    {
        var plans = await _context.SchoolImplementations
            .OrderByDescending(s => s.Year)
            .Select(s => new SchoolImplementationHeaderDto(
                s.Id, s.Year, s.SheetName, s.TotalEstimatedCost, s.AnnualBudget))
            .ToListAsync();

        return Ok(plans);
    }

    // ── GET /api/SchoolImplementation/{id} ────────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SchoolImplementationDetailDto>> GetPlanById(int id)
    {
        var plan = await _context.SchoolImplementations
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (plan is null)
            return NotFound($"Plan with id {id} not found.");

        var dto = new SchoolImplementationDetailDto(
            plan.Id, plan.Year, plan.SheetName, plan.TotalEstimatedCost, plan.AnnualBudget,
            BuildMonthDtos(plan.Items));

        return Ok(dto);
    }

    // ── POST /api/SchoolImplementation/import ─────────────────────────────────
    // Accepts ONLY files matching the official SIP template layout.
    //
    // Template structure (per month sheet):
    //   Row 1 : "SCHOOL IMPLEMENTATION PLAN — <MONTH>" title
    //   Row 2 : Instruction text
    //   Row 3 : Category section labels
    //   Row 4 : Column headers (KRA / SIP / PPA / … repeated 4×)
    //   Row 5+: Data rows (one row = one item; same row number = same item across all 4 sections)
    //
    // The 4 sections are laid out horizontally:
    //   Regular Expenditure (A–J) | gap | Project Related (L–U) | gap |
    //   Repair and Maintenance (W–AF) | gap | Others (AH–AQ)
    //
    // Validation rejects files that do not match this header signature.
    [HttpPost("import")]
    public async Task<IActionResult> ImportExcel(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
            !file.FileName.EndsWith(".xls",  StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .xlsx / .xls files are accepted.");

        using var stream   = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);

        var parsedItems     = new List<ImplementationItem>();
        int? detectedYear   = null;
        string? schoolName  = null;

        foreach (var worksheet in workbook.Worksheets)
        {
            var sheetName = worksheet.Name.Trim();
            if (!IsMonthSheet(sheetName)) continue;

            var normalizedMonth = NormalizeMonthName(sheetName)!;

            if (worksheet.RangeUsed() is null) continue;

            // ── Template validation ───────────────────────────────────────────
            // Verify the header row (row 4) matches the expected column pattern.
            var validationError = ValidateTemplateHeaders(worksheet, sheetName);
            if (validationError is not null)
                return BadRequest(validationError);

            // ── Detect year & school name from row 1 ──────────────────────────
            if (!detectedYear.HasValue)
            {
                var titleCell = worksheet.Cell(1, 1).GetString();
                schoolName = titleCell.Trim();

                var yearMatch = System.Text.RegularExpressions.Regex
                    .Match(titleCell, @"\b(20\d{2})\b");
                if (yearMatch.Success && int.TryParse(yearMatch.Value, out int yr))
                    detectedYear = yr;

                // Also scan rows 1-3 for a year number
                if (!detectedYear.HasValue)
                {
                    for (int r = 1; r <= 3 && !detectedYear.HasValue; r++)
                    {
                        int lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 1;
                        for (int c = 1; c <= lastCol; c++)
                        {
                            var txt   = worksheet.Cell(r, c).GetString();
                            var match = System.Text.RegularExpressions.Regex.Match(txt, @"\b(20\d{2})\b");
                            if (match.Success && int.TryParse(match.Value, out int y2))
                            {
                                detectedYear = y2;
                                break;
                            }
                        }
                    }
                }

                detectedYear  ??= DateTime.Now.Year;
                schoolName    ??= $"School Implementation Plan {detectedYear}";
            }

            // ── Parse data rows ───────────────────────────────────────────────
            int monthIndex = Array.IndexOf(MonthOrder, normalizedMonth) + 1;
            var dateString = $"{detectedYear}-{monthIndex:D2}-01";
            int lastRow    = worksheet.LastRowUsed()!.RowNumber();

            for (int r = DataStart; r <= lastRow; r++)
            {
                // Parse items from each of the 4 sections in this row
                foreach (var (category, startCol) in TemplateSections)
                {
                    var item = ParseRowSection(worksheet, r, startCol, category, dateString);
                    if (item is not null)
                        parsedItems.Add(item);
                }
            }
        }

        if (parsedItems.Count == 0)
            return BadRequest(
                "No data rows could be parsed. Make sure the file matches the " +
                "official School Implementation Plan template and contains at least one data row.");

        int    year         = detectedYear ?? DateTime.Now.Year;
        string name         = schoolName   ?? $"School Implementation Plan {year}";
        double importedTotal = parsedItems.Sum(i => i.EstimatedCost ?? 0);

        // ── Append or create ──────────────────────────────────────────────────
        var existing = await _context.SchoolImplementations
            .FirstOrDefaultAsync(s => s.Year == year);

        if (existing is not null)
        {
            foreach (var item in parsedItems)
            {
                item.SchoolImplementationId = existing.Id;
                _context.ImplementationItems.Add(item);
            }
            existing.TotalEstimatedCost += importedTotal;
            if (existing.SheetName.StartsWith("School Implementation Plan "))
                existing.SheetName = name;
        }
        else
        {
            _context.SchoolImplementations.Add(new SchoolImplementation
            {
                Year               = year,
                SheetName          = name,
                TotalEstimatedCost = importedTotal,
                Items              = parsedItems
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = existing is not null
                ? $"Appended {parsedItems.Count} items to existing {year} plan."
                : $"Created new plan for {year} with {parsedItems.Count} items.",
            Year          = year,
            ItemCount     = parsedItems.Count,
            ImportedTotal = importedTotal
        });
    }

    // ── PUT /api/SchoolImplementation/{id}/budget ─────────────────────────────
    // Admin sets (or clears) the annual budget ceiling for a plan.
    // Send { "annualBudget": 1500000 } to set, { "annualBudget": null } to clear.
    [HttpPut("{id:int}/budget")]
    public async Task<IActionResult> SetBudget(int id, [FromBody] SetBudgetRequest req)
    {
        var plan = await _context.SchoolImplementations.FirstOrDefaultAsync(s => s.Id == id);
        if (plan is null) return NotFound($"Plan {id} not found.");

        plan.AnnualBudget = req.AnnualBudget;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message      = req.AnnualBudget.HasValue
                ? $"Annual budget set to ₱{req.AnnualBudget:N2}."
                : "Annual budget cleared.",
            AnnualBudget = plan.AnnualBudget
        });
    }

    // ── POST /api/SchoolImplementation/item ───────────────────────────────────
    [HttpPost("item")]
    public async Task<IActionResult> AddImplementationItem([FromBody] CreateItemRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Date))
            return BadRequest("Item must have a valid Date.");

        if (!DateTime.TryParse(req.Date, out DateTime itemDate))
            return BadRequest("Invalid Date format.");

        int targetYear = itemDate.Year;

        var yearlyPlan = await _context.SchoolImplementations
            .FirstOrDefaultAsync(s => s.Year == targetYear);

        if (yearlyPlan is null)
        {
            yearlyPlan = new SchoolImplementation
            {
                Year               = targetYear,
                SheetName          = $"School Implementation Plan {targetYear}",
                TotalEstimatedCost = 0,
                Items              = new List<ImplementationItem>()
            };
            _context.SchoolImplementations.Add(yearlyPlan);
            await _context.SaveChangesAsync();
        }

        var hex    = Convert.ToHexString(Guid.NewGuid().ToByteArray())[..6];
        var arCode = $"AR-{targetYear}-{hex}";

        var newItem = new ImplementationItem
        {
            SchoolImplementationId = yearlyPlan.Id,
            Date            = req.Date,
            Kra             = req.Kra,
            SipProgram      = req.SipProgram,
            Activity        = req.Activity,
            Purpose         = req.Purpose,
            Indicator       = req.Indicator,
            Resources       = req.Resources,
            Quantity        = req.Quantity,
            EstimatedCost   = req.EstimatedCost,
            AccountTitle    = req.AccountTitle,
            AccountCode     = req.AccountCode,
            ExpenditureType = req.ExpenditureType,
            Status          = req.Status,
            ArCode          = arCode,
            IsVerified      = false,
        };

        _context.ImplementationItems.Add(newItem);
        yearlyPlan.TotalEstimatedCost += req.EstimatedCost;
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Item added successfully", PlanId = yearlyPlan.Id, ItemId = newItem.Id, ArCode = arCode });
    }

    // ── DELETE /api/SchoolImplementation/item/{itemId} ────────────────────────
    [HttpDelete("item/{itemId:int}")]
    public async Task<IActionResult> RemoveImplementationItem(int itemId)
    {
        var item = await _context.ImplementationItems.FirstOrDefaultAsync(i => i.Id == itemId);
        if (item is null) return NotFound("Item not found.");

        var plan = await _context.SchoolImplementations
            .FirstOrDefaultAsync(s => s.Id == item.SchoolImplementationId);
        if (plan is not null)
        {
            plan.TotalEstimatedCost -= item.EstimatedCost ?? 0;
            if (plan.TotalEstimatedCost < 0) plan.TotalEstimatedCost = 0;
        }

        _context.ImplementationItems.Remove(item);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Item removed and plan total updated." });
    }

    // ── POST /api/SchoolImplementation/recalculate/{id} ───────────────────────
    [HttpPost("recalculate/{id:int}")]
    public async Task<IActionResult> RecalculateTotal(int id)
    {
        var plan = await _context.SchoolImplementations
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (plan is null) return NotFound();

        plan.TotalEstimatedCost = plan.Items.Sum(i => i.EstimatedCost ?? 0);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Total recalculated.", NewTotal = plan.TotalEstimatedCost });
    }

    // ── POST /api/SchoolImplementation/seed-bulk ──────────────────────────────
    [HttpPost("seed-bulk")]
    public async Task<IActionResult> SeedBulkDatabase()
    {
        if (_context.SchoolImplementations.Any())
            return BadRequest("Database already contains data. Clear it first to re-seed.");

        var random       = new Random();
        var allReports   = new List<SchoolImplementation>();
        string[] kras    = ["KRA 1: Strategic Leadership", "KRA 2: Operations Management", "KRA 3: Teaching & Learning", "KRA 4: HR Development"];
        string[] titles  = ["Electricity Expenses", "Internet Subscription", "Office Supplies", "Security Services", "Training Expenses"];
        string[] programs = ["Overhead", "ADM", "Senior High School Program", "SBM Initiatives", "Health & Nutrition"];

        for (int i = 1; i <= 50; i++)
        {
            var report = new SchoolImplementation
            {
                SheetName = $"School Implementation Plan {2000 + i}",
                Year      = 2000 + i,
                Items     = new List<ImplementationItem>()
            };
            double total = 0;
            int daysInYear  = DateTime.IsLeapYear(report.Year) ? 366 : 365;
            var startOfYear = new DateTime(report.Year, 1, 1);
            for (int j = 1; j <= 100; j++)
            {
                double cost = random.Next(500, 50000);
                total += cost;
                report.Items.Add(new ImplementationItem
                {
                    Date            = startOfYear.AddDays(random.Next(0, daysInYear)).ToString("yyyy-MM-dd"),
                    Kra             = kras[random.Next(kras.Length)],
                    SipProgram      = programs[random.Next(programs.Length)],
                    ExpenditureType = CategoryOrder[random.Next(CategoryOrder.Length)],
                    Activity        = $"Activity {j} for {report.Year}",
                    Purpose         = "Support school operations and learner development",
                    Indicator       = $"Target met for item {j}",
                    Resources       = "Standard Operating Supplies",
                    Quantity        = random.Next(1, 10).ToString(),
                    EstimatedCost   = cost,
                    AccountTitle    = titles[random.Next(titles.Length)],
                    AccountCode     = (5020000000 + random.Next(1000, 9999)).ToString()
                });
            }
            report.TotalEstimatedCost = total;
            allReports.Add(report);
        }

        _context.SchoolImplementations.AddRange(allReports);
        await _context.SaveChangesAsync();
        return Ok($"Seeded 50 plans with {allReports.Sum(r => r.Items.Count)} total items.");
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Validates that row 4 of the worksheet contains the expected column headers
    /// for all 4 category sections. Returns an error message string on failure,
    /// or null when the sheet passes validation.
    /// </summary>
    private static string? ValidateTemplateHeaders(IXLWorksheet ws, string sheetName)
    {
        // Build a combined string of all header-row content and check for required keywords
        var headerCells = Enumerable.Range(1, 43)
            .Select(c => ws.Cell(HeaderRow, c).GetString().ToLowerInvariant())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var joined = string.Join(" ", headerCells);

        foreach (var keyword in RequiredHeaderKeywords)
        {
            if (!joined.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return $"Sheet \"{sheetName}\" does not match the official SIP template. " +
                       $"Missing expected header: \"{keyword}\". " +
                       "Please use the School Implementation Plan template available via the Templates button.";
        }

        // Verify each of the 4 section start columns has "key result area"
        foreach (var (category, startCol) in TemplateSections)
        {
            var kraHeader = ws.Cell(HeaderRow, startCol).GetString().ToLowerInvariant();
            if (!kraHeader.Contains("key result area"))
                return $"Sheet \"{sheetName}\" does not match the official SIP template. " +
                       $"Expected 'Key Result Area' header at column {startCol} " +
                       $"(section: {category}). " +
                       "Please use the School Implementation Plan template.";
        }

        return null; // passed
    }

    /// <summary>
    /// Reads one row for one category section. Returns null when the row is
    /// blank / not a real data entry (NONE placeholder, empty, or sub-total).
    /// </summary>
    private static ImplementationItem? ParseRowSection(
        IXLWorksheet ws, int row, int startCol,
        string category, string dateString)
    {
        var kra  = ws.Cell(row, startCol + OffKra).GetString().Trim();
        var sip  = ws.Cell(row, startCol + OffSip).GetString().Trim();
        var ppa  = ws.Cell(row, startCol + OffPpa).GetString().Trim();

        // Skip blank rows
        if (string.IsNullOrWhiteSpace(kra) && string.IsNullOrWhiteSpace(ppa)) return null;

        // Skip NONE placeholder rows
        if (kra.Equals("NONE", StringComparison.OrdinalIgnoreCase) ||
            ppa.Equals("NONE", StringComparison.OrdinalIgnoreCase)) return null;

        // Skip sub-total or total rows
        if (ppa.Contains("sub-total",   StringComparison.OrdinalIgnoreCase) ||
            kra.Contains("sub-total",   StringComparison.OrdinalIgnoreCase) ||
            ppa.Contains("total budget",StringComparison.OrdinalIgnoreCase) ||
            kra.Contains("total budget",StringComparison.OrdinalIgnoreCase)) return null;

        // Parse cost — strip ₱, commas, whitespace
        var costText  = ws.Cell(row, startCol + OffCost).GetString().Trim();
        var cleanCost = System.Text.RegularExpressions.Regex.Replace(costText, @"[₱,\s]", "");
        double.TryParse(cleanCost,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out double estimatedCost);

        // Skip rows with no activity and no cost
        if (string.IsNullOrWhiteSpace(ppa) && estimatedCost == 0) return null;

        return new ImplementationItem
        {
            Date            = dateString,
            Kra             = kra,
            SipProgram      = string.IsNullOrWhiteSpace(sip) ? "Unimplemented" : sip,
            ExpenditureType = category,
            Activity        = ppa,
            Purpose         = ws.Cell(row, startCol + OffPurpose ).GetString().Trim(),
            Indicator       = ws.Cell(row, startCol + OffPerfInd ).GetString().Trim(),
            Resources       = ws.Cell(row, startCol + OffResDesc ).GetString().Trim(),
            Quantity        = ws.Cell(row, startCol + OffQty     ).GetString().Trim(),
            EstimatedCost   = estimatedCost,
            AccountTitle    = ws.Cell(row, startCol + OffAccTitle).GetString().Trim(),
            AccountCode     = ws.Cell(row, startCol + OffAccCode ).GetString().Trim(),
        };
    }

    private static bool IsMonthSheet(string name) =>
        MonthOrder.Any(m => string.Equals(name.Trim(), m, StringComparison.OrdinalIgnoreCase));

    private static string? NormalizeMonthName(string name)
    {
        var t = name.Trim();
        return MonthOrder.FirstOrDefault(m => string.Equals(t, m, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ParseMonth(string dateStr) =>
        DateTime.TryParse(dateStr, out var dt) ? dt.ToString("MMMM") : null;

    private List<MonthSheetDto> BuildMonthDtos(IEnumerable<ImplementationItem> allItems)
    {
        return allItems
            .Where(i => !string.IsNullOrWhiteSpace(i.Date))
            .GroupBy(i => ParseMonth(i.Date!))
            .Where(g => g.Key != null)
            .OrderBy(g => Array.IndexOf(MonthOrder, g.Key))
            .Select(monthGroup =>
            {
                var month  = monthGroup.Key!;
                var hasSip = monthGroup.Any(i =>
                    !string.IsNullOrWhiteSpace(i.SipProgram) && i.SipProgram != "Unimplemented");

                var items = monthGroup
                    .OrderBy(i =>
                    {
                        var idx = Array.IndexOf(CategoryOrder, i.ExpenditureType ?? "Others");
                        return idx < 0 ? 99 : idx;
                    })
                    .Select(i => new SchoolPlanItemDto(
                        i.Id,
                        i.Kra             ?? "",
                        i.SipProgram      ?? "Unimplemented",
                        i.Activity        ?? "",
                        i.Purpose         ?? "",
                        i.Indicator       ?? "",
                        i.Resources       ?? "",
                        i.Quantity        ?? "",
                        i.EstimatedCost   ?? 0,
                        i.AccountTitle    ?? "",
                        i.AccountCode     ?? "",
                        i.ExpenditureType ?? "Regular Expenditure",
                        i.ArCode,
                        i.IsVerified,
                        i.Status
                    ))
                    .ToList();

                var subTotals  = monthGroup
                    .GroupBy(i => i.ExpenditureType ?? "Regular Expenditure")
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.EstimatedCost ?? 0));

                double grandTotal = monthGroup.Sum(i => i.EstimatedCost ?? 0);

                return new MonthSheetDto(month, hasSip, items, subTotals, grandTotal);
            })
            .ToList();
    }
}