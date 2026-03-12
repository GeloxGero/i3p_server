using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using i3p_server.Models;
using System.Text.Json;
using ClosedXML.Excel;

namespace i3p_server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AnnualProcurementPlanController : ControllerBase
{
    private readonly AppDbContext _context;

    public AnnualProcurementPlanController(AppDbContext context)
    {
        _context = context;
    }
    
    private string GetFormattedValue(IXLCell cell)
    {
        if (cell.Value.IsBlank) return null;
        if (cell.Value.IsBoolean) return cell.Value.GetBoolean().ToString();
        if (cell.Value.IsNumber) return cell.Value.GetNumber().ToString();
        if (cell.Value.IsDateTime) return cell.Value.GetDateTime().ToString();

        // Default to string for everything else (text, errors, etc.)
        return cell.Value.ToString();
    }
    
    private double ParseDoubleSafe(IXLCell cell)
    {
        var val = cell.GetString().Trim();

        // 1. Handle empty cells or "####" errors
        if (string.IsNullOrEmpty(val) || val == "####")
            return 0;

        // 2. Remove common formatting chars (currency symbols, thousands separators)
        var cleanVal = val.Replace("₱", "").Replace(",", "").Trim();


        Console.Write("NormalValue = " + val);
        Console.WriteLine("  CleanValue = " + cleanVal);
        // 3. Attempt to parse
        if (double.TryParse(cleanVal, out double result))
        {
            Console.WriteLine("Error here");
            Console.WriteLine(result is double);
            return result;
            
            
        }

        
        Console.WriteLine("Error getting double from value" + val);
        return 0; // Return 0 or log a warning if parsing fails
    }

    // GET: api/AnnualProcurementPlan
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AnnualProcurementPlan>>> GetPlans()
    {
        return await _context.AnnualProcurementPlan
            .Include(p => p.Items) // This tells EF to join the Items table
            .ToListAsync();
    }

    // GET: api/AnnualProcurementPlan/5
    [HttpGet("{id}")]
    public async Task<ActionResult<AnnualProcurementPlan>> GetPlan(int id)
    {
        var plan = await _context.AnnualProcurementPlan
            .Include(p => p.Items) // Ensure Items are loaded from the AppItems table
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null) return NotFound();
        return plan;
    }

    // Helper to call before SaveChangesAsync
    private void RecalculateTotals(AnnualProcurementPlan plan)
    {
        // Ensure all items have valid TotalAmount before summing
        plan.Items.ForEach(i => i.TotalAmount = (i.TotalQuantity ?? 0) * (i.Price ?? 0));
    
        // Sum it up
        plan.YearTotal = (decimal)plan.Items.Sum(i => i.TotalAmount ?? 0);
    }
    
    [HttpPost("import")]
    public async Task<IActionResult> Import([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        try
        {
            var plan = new AnnualProcurementPlan
            {
                Year = DateTime.Now.Year, // You can modify this to accept a Year from a form field if needed
                Items = new List<AppItem>(),
                YearTotal = 0,
                FileName = file.FileName
            };

            // Load the Excel file using ClosedXML
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var workbook = new XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheet(1); // Assumes data is in the first sheet
                    var rangedWorksheet =  worksheet.RangeUsed();
                    var lastRow = rangedWorksheet.LastRowUsed().RowNumber();
                    var lastColumn = rangedWorksheet.LastColumnUsed().ColumnNumber();
                    
                    
                    // RowsUsed skips empty rows automatically. Skip(1) ignores the header row.
                    var rows = worksheet.RowsUsed().Skip(1);

                    for (int rowNum = 18; rowNum <= lastRow; rowNum++)
                        
                    {
                        var row = rangedWorksheet.Row(rowNum);
                        
                        if (row.IsEmpty()) continue;
                        var firstColumn = row.FirstCell();
                        if (!firstColumn.IsEmpty() &&
                            GetFormattedValue(row.FirstCell()).ToLower().Contains("total")) break;

                        var currentRowList = new List<object>();

                        for (int colNum = 1; colNum <= lastColumn; colNum++)
                        {
                            var cell = GetFormattedValue(row.Cell(colNum));
                            currentRowList.Add(cell);
                        }
                        // Map your Excel columns to your model properties.
                        // Adjust the Cell(X) index to match your specific Excel file layout.



                        
                        Console.Write(ParseDoubleSafe(row.Cell(33)));
                        try
                        {
                            var item = new AppItem
                            {
                                No = row.Cell(1).GetString(),
                                Unspsc = row.Cell(2).GetString(),
                                ItemDescription = row.Cell(4).GetString(),
                                Specification = row.Cell(7).GetString(),
                                UnitOfMeasure = row.Cell(9).GetString(),
                                TotalQuantity = ParseDoubleSafe(row.Cell(30)),
                                Price = ParseDoubleSafe(row.Cell(31))
                            };

                            plan.Items.Add(item);
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine($"Error processing row {row.RowNumber()}: {ex.Message}");
                            throw;
                        }
                            
                        
                    }
                }
            }

            // Use your existing helper to calculate the total amount
            RecalculateTotals(plan);

            _context.AnnualProcurementPlan.Add(plan);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Import successful", planId = plan.Id });
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
        {
            return BadRequest("Database already contains APP data.");
        }

        var random = new Random();
        var allPlans = new List<AnnualProcurementPlan>();

        // Mock data pools
        string[] categories = { "Office Supplies", "IT Equipment", "Janitorial", "Furniture", "Medical" };
        string[] units = { "piece", "ream", "unit", "box", "pack" };
        string[] commonItems = { "Ballpen", "A4 Paper", "Laptop", "Chair", "Alcohol", "Folder" };

        
   
        for (int p = 1; p <= 10; p++)
        {
            int currentYear = 2000 + p;
            var plan = new AnnualProcurementPlan
            {
                Year = currentYear,
                AuxilliaryJson = JsonSerializer.Serialize(new { Office = "District Office " + p, Year = 2026 }),
                HeadersJson = JsonSerializer.Serialize(new[] { "No.", "UNSPSC", "Description", "Spec", "UOM", "Qty", "Price", "Total" }),
                // We populate the Items property (Note: Remove [NotMapped] in your model for this to save to DB)
                Items = new List<AppItem>(),
                YearTotal = 0
            };

            for (int i = 1; i <= 100; i++)
            {
                double price = Math.Round(random.NextDouble() * 2000 + 20, 2);
                double qty = random.Next(1, 500);

                plan.Items.Add(new AppItem
                {
                    No = i.ToString(),
                    Unspsc = $"4412170{random.Next(10, 99)}",
                    ItemDescription = $"{commonItems[random.Next(commonItems.Length)]} (Batch {p})",
                    Specification = "Standard Technical Specification",
                    UnitOfMeasure = units[random.Next(units.Length)],
                    TotalQuantity = qty,
                    Price = price,
                    TotalAmount = price * qty
                });
                
                
            }
            
            RecalculateTotals(plan); 
            allPlans.Add(plan);
        }

        try
        {
            // Set high timeout for large data volume
            _context.Database.SetCommandTimeout(180);

            _context.AnnualProcurementPlan.AddRange(allPlans);
            
            // If you kept [NotMapped] on Items in the model, 
            // the following SaveChanges will ONLY save the 50 plan headers.
            // You must also add: public DbSet<AppItem> AppItems { get; set; } in AppDbContext.
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Bulk seed successful",
                PlansCreated = allPlans.Count,
                ItemsCreated = allPlans.Sum(x => x.Items.Count)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Seeding failed: {ex.Message}");
        }
    }
}