using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using i3p_server.Models;
using System.Text.Json;
using ClosedXML.Excel;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace i3p_server.Controllers;

// ─── Template column layout (1-based) ────────────────────────────────────────
// Row 1 : "ANNUAL PROCUREMENT PLAN" title (merged A1:G1)
// Row 2 : Column headers
//   A = UNSPSC
//   B = Item Description
//   C = Specification
//   D = Unit of Measure
//   E = Total Quantity for the Year
//   F = Price (₱)
//   G = Total Amount for the Year (₱)   ← label col; H has the =F*G formula
// Row 3 : Instruction text (merged)
// Row 4 : Blank spacer
// Row 5+ : Data rows

[Route("api/[controller]")]
[ApiController]
public class AnnualProcurementPlanController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly Cloudinary _cloudinary;
    private const int DataStartRow = 5;

    public AnnualProcurementPlanController(AppDbContext context)
    {
        _context = context;
    }

    private double ParseDoubleSafe(IXLCell cell)
    {
        if (cell.Value.IsNumber) return cell.Value.GetNumber();
        var val = cell.GetString().Replace("₱", "").Replace(",", "").Trim();
        if (string.IsNullOrEmpty(val) || val == "####") return 0;
        return double.TryParse(val, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var r) ? r : 0;
    }

    private void RecalculateTotals(AnnualProcurementPlan plan)
    {
        plan.Items.ForEach(i => i.TotalAmount = (i.TotalQuantity ?? 0) * (i.Price ?? 0));
        plan.YearTotal = (decimal)plan.Items.Sum(i => i.TotalAmount ?? 0);
    }

    // GET: api/AnnualProcurementPlan
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AnnualProcurementPlan>>> GetPlans()
    {
        return await _context.AnnualProcurementPlan
            .Include(p => p.Items)
            .ToListAsync();
    }

    [HttpPost("items/{id}/upload-photo")]
    public async Task<IActionResult> UploadPhoto(int id, IFormFile file) // Ensure 'file' matches the FormData key
    {
        var item = await _context.AppItems.FindAsync(id);
        if (item == null) return NotFound("AppItem not found");

        if (file == null || file.Length == 0) return BadRequest("Invalid file");

        try 
        {
            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "app-items/verification",
                // This ensures the image is optimized for viewing on the AR page
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null) return BadRequest(uploadResult.Error.Message);

            // This saves the full URL: https://res.cloudinary.com/dlzobzben/image/upload/...
            item.PhotoPath = uploadResult.SecureUrl.ToString();
        
            await _context.SaveChangesAsync();

            return Ok(new { photoPath = item.PhotoPath });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
    
    // GET: api/AnnualProcurementPlan/5
    [HttpGet("{id}")]
    public async Task<ActionResult<AnnualProcurementPlan>> GetPlan(int id)
    {
        var plan = await _context.AnnualProcurementPlan
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (plan == null) return NotFound();
        return plan;
    }

    // ── POST /api/AnnualProcurementPlan/import ────────────────────────────────
    // Accepts ONLY files matching the official APP template.
    // Template layout:
    //   Row 1  = title
    //   Row 2  = headers (A=UNSPSC, B=Description, C=Spec, D=UOM, E=Qty, F=Price, G=Total)
    //   Row 3  = instruction text
    //   Row 4  = blank
    //   Row 5+ = data rows (stop when row is blank or first cell contains "total")
    [HttpPost("import")]
    public async Task<IActionResult> Import([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
            !file.FileName.EndsWith(".xls",  StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .xlsx / .xls files are accepted.");

        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            using var workbook = new XLWorkbook(stream);

            var worksheet = workbook.Worksheet(1);

            // ── Validate template ────────────────────────────────────────────
            var headerRow = worksheet.Row(2);
            var h1 = headerRow.Cell(1).GetString().Trim().ToLower();
            var h2 = headerRow.Cell(2).GetString().Trim().ToLower();
            if (!h1.Contains("unspsc") && !h2.Contains("item description"))
                return BadRequest(
                    "The uploaded file does not match the official APP template. " +
                    "Expected 'UNSPSC' in column A and 'Item Description' in column B of row 2. " +
                    "Please use the Annual Procurement Plan template.");

            // ── Detect year from title row ───────────────────────────────────
            var titleText = worksheet.Cell(1, 1).GetString();
            int detectedYear = DateTime.Now.Year;
            var yearMatch = System.Text.RegularExpressions.Regex.Match(titleText, @"\b(20\d{2})\b");
            if (yearMatch.Success && int.TryParse(yearMatch.Value, out int y))
                detectedYear = y;

            var plan = new AnnualProcurementPlan
            {
                Year     = detectedYear,
                FileName = file.FileName,
                Items    = new List<AppItem>(),
                YearTotal = 0
            };

            var rangeUsed = worksheet.RangeUsed();
            int lastRow   = rangeUsed?.LastRowUsed()?.RowNumber() ?? DataStartRow;

            for (int rowNum = DataStartRow; rowNum <= lastRow; rowNum++)
            {
                var row = worksheet.Row(rowNum);

                // Stop at blank rows or total/footer rows
                var cellB = row.Cell(2).GetString().Trim();
                var cellA = row.Cell(1).GetString().Trim();
                if (string.IsNullOrWhiteSpace(cellB) && string.IsNullOrWhiteSpace(cellA))
                    continue;
                if (cellA.ToLower().Contains("total") || cellB.ToLower().Contains("total"))
                    break;

                double qty   = ParseDoubleSafe(row.Cell(5));
                double price = ParseDoubleSafe(row.Cell(6));

                // Skip rows with no meaningful data
                if (string.IsNullOrWhiteSpace(cellB) && qty == 0 && price == 0)
                    continue;

                plan.Items.Add(new AppItem
                {
                    Unspsc          = row.Cell(1).GetString().Trim().NullIfEmpty(),
                    ItemDescription = row.Cell(2).GetString().Trim().NullIfEmpty(),
                    Specification   = row.Cell(3).GetString().Trim().NullIfEmpty(),
                    UnitOfMeasure   = row.Cell(4).GetString().Trim().NullIfEmpty(),
                    TotalQuantity   = qty   > 0 ? qty   : null,
                    Price           = price > 0 ? price : null,
                    TotalAmount     = qty * price,
                });
            }

            if (plan.Items.Count == 0)
                return BadRequest("No data rows found. Make sure the file uses the official APP template with data starting at row 5.");

            RecalculateTotals(plan);
            _context.AnnualProcurementPlan.Add(plan);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message   = $"Import successful — {plan.Items.Count} items imported.",
                PlanId    = plan.Id,
                ItemCount = plan.Items.Count,
                YearTotal = plan.YearTotal
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, $"Error processing file: {ex.Message}");
        }
    }

    [HttpPost("seed-bulk")]
    public async Task<IActionResult> SeedBulkAPP()
    {
        if (await _context.AnnualProcurementPlan.AnyAsync())
            return BadRequest("Database already contains APP data.");

        var random = new Random();
        var allPlans = new List<AnnualProcurementPlan>();
        string[] units  = { "piece", "ream", "unit", "box", "pack" };
        string[] descs  = { "Ballpen", "A4 Paper", "Laptop", "Chair", "Alcohol", "Folder" };

        for (int p = 1; p <= 10; p++)
        {
            var plan = new AnnualProcurementPlan
            {
                Year = 2000 + p, FileName = $"seed-{2000+p}.xlsx",
                Items = new List<AppItem>(), YearTotal = 0
            };
            for (int i = 1; i <= 100; i++)
            {
                double price = Math.Round(random.NextDouble() * 2000 + 20, 2);
                double qty   = random.Next(1, 500);
                plan.Items.Add(new AppItem
                {
                    No = i.ToString(), Unspsc = $"4412170{random.Next(10,99)}",
                    ItemDescription = $"{descs[random.Next(descs.Length)]} (Batch {p})",
                    Specification = "Standard Technical Specification",
                    UnitOfMeasure = units[random.Next(units.Length)],
                    TotalQuantity = qty, Price = price, TotalAmount = price * qty
                });
            }
            RecalculateTotals(plan);
            allPlans.Add(plan);
        }

        _context.Database.SetCommandTimeout(180);
        _context.AnnualProcurementPlan.AddRange(allPlans);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Bulk seed successful", PlansCreated = allPlans.Count });
    }
    
    [HttpPatch("items/{id}/verify")]
    public async Task<IActionResult> VerifyPhoto(int id, [FromBody] string adminUsername)
    {
        var item = await _context.AppItems.FindAsync(id);
        if (item == null || string.IsNullOrEmpty(item.PhotoPath)) 
            return BadRequest("No photo to verify.");

        item.IsPhotoVerified = true;
        item.VerifiedAt = DateTime.UtcNow;
        item.VerifiedBy = adminUsername;

        await _context.SaveChangesAsync();
        return Ok(item);
    }
}

// ── Extension helper ──────────────────────────────────────────────────────────
public static class StringExtensions
{
    public static string? NullIfEmpty(this string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s;
}