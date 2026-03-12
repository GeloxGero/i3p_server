using Microsoft.AspNetCore.Mvc;
using i3p_server.Models;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace i3p_server.Controllers;

// ─── Response DTOs ────────────────────────────────────────────────────────────

/// <summary>Lightweight header returned by GET /api/SchoolImplementation</summary>
public record SchoolImplementationHeaderDto(int Id, int Year, string School, double TotalEstimatedCost);

/// <summary>Full plan returned by GET /api/SchoolImplementation/{id}</summary>
public record SchoolImplementationDetailDto(
    int Id,
    int Year,
    string School,
    double TotalEstimatedCost,
    List<MonthSheetDto> Months
);

public record MonthSheetDto(
    string Month,
    bool HasSip,
    List<SchoolPlanItemDto> Items,
    Dictionary<string, double> SubTotals,
    double GrandTotal
);

public record SchoolPlanItemDto(
    int Id,
    string KraArea,
    string SpecificProgram,
    string ProgramActivity,
    string Purpose,
    string PerformanceIndicator,
    string ResourceDescription,
    string Quantity,
    double EstimatedCost,
    string AccountTitle,
    string AccountCode,
    string Category
);

// ─── Controller ───────────────────────────────────────────────────────────────

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

    public SchoolImplementationController(AppDbContext context)
    {
        _context = context;
    }

    // ── GET /api/SchoolImplementation ─────────────────────────────────────────
    // Lightweight list for the year dropdown. Includes the running total so the
    // frontend can display it without fetching the full plan.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SchoolImplementationHeaderDto>>> GetPlans()
    {
        var plans = await _context.SchoolImplementations
            .OrderByDescending(s => s.Year)
            .Select(s => new SchoolImplementationHeaderDto(
                s.Id,
                s.Year,
                s.SheetName,
                s.TotalEstimatedCost
            ))
            .ToListAsync();

        return Ok(plans);
    }

    // ── GET /api/SchoolImplementation/{id} ────────────────────────────────────
    // Full plan with items grouped by month → category, subtotals, grand totals.
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SchoolImplementationDetailDto>> GetPlanById(int id)
    {
        var plan = await _context.SchoolImplementations
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (plan is null)
            return NotFound($"Plan with id {id} not found.");

        var monthDtos = BuildMonthDtos(plan.Items);

        var dto = new SchoolImplementationDetailDto(
            plan.Id,
            plan.Year,
            plan.SheetName,
            plan.TotalEstimatedCost,
            monthDtos
        );

        return Ok(dto);
    }

    // ── POST /api/SchoolImplementation/import ─────────────────────────────────
    // Parses every month sheet (skips "Total" and unrecognised sheets).
    //
    // APPEND behaviour:
    //   • If a plan for the detected year already exists → new items are APPENDED
    //     to the existing ones and TotalEstimatedCost is increased accordingly.
    //   • If no plan exists yet → a new one is created.
    //
    // This means importing the same file twice will duplicate rows; the intent is
    // that each import file covers months not yet in the database.
    //
    // Column layouts supported (1-based, ClosedXML):
    //   No SiP, no gap : KRA=1  PPA=2  Purpose=3  PerfInd=4  ResDesc=5  Qty=6  Cost=7  AccTitle=8  AccCode=9
    //   SiP, no gap    : KRA=1  SiP=2  PPA=3  Purpose=4  PerfInd=5  ResDesc=6  Qty=7  Cost=8  AccTitle=9  AccCode=10
    //   SiP + gap      : KRA=1  SiP=2  PPA=3  [blank]=4  Purpose=5  PerfInd=6  ResDesc=7  Qty=8  Cost=9  AccTitle=10  AccCode=11
    [HttpPost("import")]
    public async Task<IActionResult> ImportExcel(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
            !file.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .xlsx / .xls files are accepted.");

        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);

        var parsedItems = new List<ImplementationItem>();
        int? detectedYear = null;
        string? detectedSchoolName = null;

        foreach (var worksheet in workbook.Worksheets)
        {
            var sheetName = worksheet.Name.Trim();
            if (!IsMonthSheet(sheetName)) continue;

            var normalizedMonth = NormalizeMonthName(sheetName)!;

            if (worksheet.RangeUsed() is null) continue;

            int lastRow = worksheet.LastRowUsed()!.RowNumber();
            int lastCol = worksheet.LastColumnUsed()!.ColumnNumber();

            // ── Find header row ───────────────────────────────────────────────
            int headerRow = -1;
            bool hasSip = false;
            bool hasGap = false;

            for (int r = 1; r <= Math.Min(lastRow, 20); r++)
            {
                var joined = string.Join(" ",
                    Enumerable.Range(1, lastCol).Select(c => worksheet.Cell(r, c).GetString())
                ).ToLower();

                if (joined.Contains("key result area") || joined.Contains("programs/projects"))
                {
                    headerRow = r;
                    hasSip = joined.Contains("specific program") || joined.Contains("sip");

                    if (hasSip)
                    {
                        // Phantom blank column: cell at col 4 empty, col 5 contains "purpose"
                        var afterPpa   = worksheet.Cell(r, 4).GetString().Trim();
                        var twoAfter   = worksheet.Cell(r, 5).GetString().Trim().ToLower();
                        hasGap = afterPpa == "" && twoAfter.Contains("purpose");
                    }
                    break;
                }
            }

            if (headerRow < 0) continue;

            // ── Detect year & school name from title rows above the header ────
            // We only need to do this once; subsequent sheets use the same values.
            if (!detectedYear.HasValue)
            {
                for (int r = 1; r < headerRow; r++)
                {
                    for (int c = 1; c <= lastCol; c++)
                    {
                        var cellText = worksheet.Cell(r, c).GetString();
                        if (string.IsNullOrWhiteSpace(cellText)) continue;

                        // First non-empty cell in the title area → treat as school name
                        detectedSchoolName ??= cellText.Trim();

                        // Look for a 4-digit year starting with "20"
                        var match = System.Text.RegularExpressions.Regex
                            .Match(cellText, @"\b(20\d{2})\b");
                        if (match.Success && int.TryParse(match.Value, out int yr))
                            detectedYear = yr;
                    }
                    if (detectedYear.HasValue) break;
                }

                detectedYear ??= DateTime.Now.Year;
                detectedSchoolName ??= $"School Implementation Plan {detectedYear}";
            }

            // ── Build column index map (1-based) ──────────────────────────────
            int gap = hasGap ? 1 : 0;
            var col = hasSip
                ? new ColMap(Kra:1, Sip:2, Ppa:3, Purpose:4+gap, PerfInd:5+gap,
                             ResDesc:6+gap, Qty:7+gap, Cost:8+gap, AccTitle:9+gap, AccCode:10+gap)
                : new ColMap(Kra:1, Sip:-1, Ppa:2, Purpose:3, PerfInd:4,
                             ResDesc:5, Qty:6, Cost:7, AccTitle:8, AccCode:9);

            // ── Parse data rows; stop after the grand-total line ──────────────
            string currentCategory = "Regular Expenditure";
            string[] categoryKeywords =
            [
                "Regular Expenditure",
                "Project Related Expenditure",
                "Repair and Maintenance",
                "Others"
            ];

            for (int r = headerRow + 1; r <= lastRow; r++)
            {
                var kra      = worksheet.Cell(r, col.Kra).GetString().Trim();
                var ppa      = worksheet.Cell(r, col.Ppa).GetString().Trim();
                var costText = worksheet.Cell(r, col.Cost).GetString().Trim();

                // Stop at grand-total line (mirrors frontend parser)
                if (ppa.Contains("total budget", StringComparison.OrdinalIgnoreCase) ||
                    kra.Contains("total budget", StringComparison.OrdinalIgnoreCase))
                    break;

                // Category header row
                var matchedCat = categoryKeywords.FirstOrDefault(c =>
                    ppa.Contains(c, StringComparison.OrdinalIgnoreCase) ||
                    kra.Contains(c, StringComparison.OrdinalIgnoreCase));
                if (matchedCat is not null) { currentCategory = matchedCat; continue; }

                // Sub-total row — skip (we recalculate server-side)
                if (ppa.Contains("SUB-TOTAL", StringComparison.OrdinalIgnoreCase) ||
                    kra.Contains("SUB-TOTAL", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Blank / NONE rows
                if (string.IsNullOrWhiteSpace(kra) && string.IsNullOrWhiteSpace(ppa)) continue;
                if (ppa.Equals("NONE", StringComparison.OrdinalIgnoreCase) ||
                    kra.Equals("NONE", StringComparison.OrdinalIgnoreCase)) continue;

                // Parse cost — strip currency symbols, commas, spaces
                var cleanCost = System.Text.RegularExpressions.Regex.Replace(costText, @"[₱,\s]", "");
                double.TryParse(cleanCost,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double estimatedCost);

                if (string.IsNullOrWhiteSpace(ppa) && estimatedCost == 0) continue;

                var sip = col.Sip > 0
                    ? worksheet.Cell(r, col.Sip).GetString().Trim()
                    : "Unimplemented";

                // Encode month as first day of that month in the detected year
                int monthIndex = Array.IndexOf(MonthOrder, normalizedMonth) + 1;
                var dateString = $"{detectedYear}-{monthIndex:D2}-01";

                parsedItems.Add(new ImplementationItem
                {
                    Date            = dateString,
                    Kra             = kra,
                    SipProgram      = string.IsNullOrWhiteSpace(sip) ? "Unimplemented" : sip,
                    ExpenditureType = currentCategory,
                    Activity        = ppa,
                    Purpose         = worksheet.Cell(r, col.Purpose).GetString().Trim(),
                    Indicator       = worksheet.Cell(r, col.PerfInd).GetString().Trim(),
                    Resources       = worksheet.Cell(r, col.ResDesc).GetString().Trim(),
                    Quantity        = worksheet.Cell(r, col.Qty).GetString().Trim(),
                    EstimatedCost   = estimatedCost,
                    AccountTitle    = worksheet.Cell(r, col.AccTitle).GetString().Trim(),
                    AccountCode     = worksheet.Cell(r, col.AccCode).GetString().Trim(),
                });
            }
        }

        if (parsedItems.Count == 0)
            return BadRequest("No data rows could be parsed from the uploaded file.");

        int year             = detectedYear ?? DateTime.Now.Year;
        string schoolName    = detectedSchoolName ?? $"School Implementation Plan {year}";
        double importedTotal = parsedItems.Sum(i => i.EstimatedCost ?? 0);

        // ── Append or create ──────────────────────────────────────────────────
        var existing = await _context.SchoolImplementations
            .FirstOrDefaultAsync(s => s.Year == year);

        if (existing is not null)
        {
            // Append new items and add their cost to the running total
            foreach (var item in parsedItems)
            {
                item.SchoolImplementationId = existing.Id;
                _context.ImplementationItems.Add(item);
            }
            existing.TotalEstimatedCost += importedTotal;
            // Optionally update school name if the existing one is the placeholder
            if (existing.SheetName.StartsWith("School Implementation Plan "))
                existing.SheetName = schoolName;
        }
        else
        {
            _context.SchoolImplementations.Add(new SchoolImplementation
            {
                Year               = year,
                SheetName          = schoolName,
                TotalEstimatedCost = importedTotal,
                Items              = parsedItems
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message     = existing is not null
                ? $"Appended {parsedItems.Count} items to existing {year} plan."
                : $"Created new plan for {year} with {parsedItems.Count} items.",
            Year        = year,
            ItemCount   = parsedItems.Count,
            ImportedTotal = importedTotal
        });
    }

    // ── POST /api/SchoolImplementation/item ───────────────────────────────────
    // Add a single item. TotalEstimatedCost is updated immediately.
    [HttpPost("item")]
    public async Task<IActionResult> AddImplementationItem([FromBody] ImplementationItem newItem)
    {
        if (string.IsNullOrEmpty(newItem.Date))
            return BadRequest("Item must have a valid Date.");

        if (!DateTime.TryParse(newItem.Date, out DateTime itemDate))
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
            await _context.SaveChangesAsync(); // get the new Id before adding child
        }

        newItem.SchoolImplementationId = yearlyPlan.Id;
        _context.ImplementationItems.Add(newItem);

        // Keep the running total in sync
        yearlyPlan.TotalEstimatedCost += newItem.EstimatedCost ?? 0;

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Item added successfully", PlanId = yearlyPlan.Id, ItemId = newItem.Id });
    }

    // ── DELETE /api/SchoolImplementation/item/{itemId} ────────────────────────
    // Remove a single item and subtract its cost from the parent plan's total.
    [HttpDelete("item/{itemId:int}")]
    public async Task<IActionResult> RemoveImplementationItem(int itemId)
    {
        var item = await _context.ImplementationItems
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item is null)
            return NotFound("Item not found.");

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
    // Utility: recompute TotalEstimatedCost from scratch by summing all child items.
    // Useful if the total ever drifts due to direct DB edits or partial imports.
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
        string[] kras          = ["KRA 1: Strategic Leadership", "KRA 2: Operations Management", "KRA 3: Teaching & Learning", "KRA 4: HR Development"];
        string[] accountTitles = ["Electricity Expenses", "Internet Subscription", "Office Supplies", "Security Services", "Training Expenses", "Repair & Maintenance"];
        string[] programs      = ["Overhead", "ADM", "Senior High School Program", "SBM Initiatives", "Health & Nutrition"];
        string[] expenseTypes  = ["Regular Expenditure", "Project Related Expenditure", "Repair and Maintenance"];

        for (int i = 1; i <= 50; i++)
        {
            var report = new SchoolImplementation
            {
                SheetName = $"School Implementation Plan {2000 + i}",
                Year      = 2000 + i,
                Items     = new List<ImplementationItem>()
            };

            double total = 0;
            int daysInYear   = DateTime.IsLeapYear(report.Year) ? 366 : 365;
            var startOfYear  = new DateTime(report.Year, 1, 1);

            for (int j = 1; j <= 100; j++)
            {
                double cost = random.Next(500, 50000);
                total += cost;
                report.Items.Add(new ImplementationItem
                {
                    Date            = startOfYear.AddDays(random.Next(0, daysInYear)).ToString("yyyy-MM-dd"),
                    Kra             = kras[random.Next(kras.Length)],
                    SipProgram      = programs[random.Next(programs.Length)],
                    ExpenditureType = expenseTypes[random.Next(expenseTypes.Length)],
                    Activity        = $"Activity {j} for {report.Year}",
                    Purpose         = "Support school operations and learner development",
                    Indicator       = $"Target met for item {j}",
                    Resources       = "Standard Operating Supplies",
                    Quantity        = random.Next(1, 10).ToString(),
                    EstimatedCost   = cost,
                    AccountTitle    = accountTitles[random.Next(accountTitles.Length)],
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

    private static bool IsMonthSheet(string name) =>
        MonthOrder.Any(m => string.Equals(name.Trim(), m, StringComparison.OrdinalIgnoreCase));

    private static string? NormalizeMonthName(string name)
    {
        var t = name.Trim();
        return MonthOrder.FirstOrDefault(m => string.Equals(t, m, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ParseMonth(string dateStr) =>
        DateTime.TryParse(dateStr, out var dt) ? dt.ToString("MMMM") : null;

    /// <summary>
    /// Groups a flat item list into MonthSheetDtos with per-category subtotals
    /// and monthly grand totals. Reused by GetPlanById.
    /// </summary>
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
                    !string.IsNullOrWhiteSpace(i.SipProgram) &&
                    i.SipProgram != "Unimplemented");

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
                        i.ExpenditureType ?? "Regular Expenditure"
                    ))
                    .ToList();

                var subTotals = monthGroup
                    .GroupBy(i => i.ExpenditureType ?? "Regular Expenditure")
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.EstimatedCost ?? 0));

                double grandTotal = monthGroup.Sum(i => i.EstimatedCost ?? 0);

                return new MonthSheetDto(month, hasSip, items, subTotals, grandTotal);
            })
            .ToList();
    }

    private record ColMap(
        int Kra, int Sip, int Ppa, int Purpose,
        int PerfInd, int ResDesc, int Qty,
        int Cost, int AccTitle, int AccCode
    );
}