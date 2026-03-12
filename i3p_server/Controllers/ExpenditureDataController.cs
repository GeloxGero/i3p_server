using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using i3p_server.Models;
using System.Text.Json;

namespace i3p_server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ExpenditureDataController : ControllerBase
{
    private readonly AppDbContext _context;

    public ExpenditureDataController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/ExpenditureData
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpenditureData>>> GetAllReports()
    {
        // Returns the list of reports without the thousands of line items
        return await _context.ExpenditureData.ToListAsync();
    }

    // GET: api/ExpenditureData/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ExpenditureData>> GetReport(int id)
    {
        var report = await _context.ExpenditureData
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null) return NotFound();
        return report;
    }

    [HttpPost("seed-bulk")]
    public async Task<IActionResult> SeedBulkExpenditure()
    {
        if (await _context.ExpenditureData.AnyAsync())
        {
            return BadRequest("Database already contains Expenditure data.");
        }

        var random = new Random();
        var allReports = new List<ExpenditureData>();

        // Mock Pools for Variety
        string[] programs = { "Pillar 1: Quality", "Pillar 2: Governance", "Pillar 3: Equity", "Pillar 4: Resilience" };
        string[] expenseClasses = { "MOOE", "CO", "PS" };
        string[] releaseManners = { "Direct Payment", "Cash Advance", "Downloading" };

        for (int r = 1; r <= 50; r++)
        {
            var report = new ExpenditureData
            {
                SheetName = $"Expenditure Matrix FY 2026 - Unit {r}",
                AuxilliaryJson = JsonSerializer.Serialize(new { OfficeCode = $"OFF-{r:D3}", Year = 2026 }),
                HeadersJson = JsonSerializer.Serialize(new[] { "Program", "Output", "Activity", "Expense Item", "Cost" }),
                Items = new List<ExpenditureItem>()
            };

            // Generate 100 items per report
            for (int i = 1; i <= 100; i++)
            {
                double unitCost = Math.Round(random.NextDouble() * 10000 + 100, 2);
                double qty = random.Next(1, 100);

                report.Items.Add(new ExpenditureItem
                {
                    SpecificProgram = programs[random.Next(programs.Length)],
                    Output = $"Strategic Output {i % 10}", // Cycles every 10 items
                    Activities = $"Capacity Building Activity {i}",
                    PerformanceIndicator = $"Indicator {i} met 100%",
                    ExpenseClass = expenseClasses[random.Next(expenseClasses.Length)],
                    ExpenseObject = "Training and Scholarship Expenses",
                    ExpenseItem = $"Workshop Materials Set {i}",
                    UnitCost = unitCost,
                    Quantity = qty,
                    TotalCost = unitCost * qty,
                    IsPpmp = random.Next(2) == 0 ? "Y" : "N",
                    IsAppSupplies = "N",
                    MannerOfRelease = releaseManners[random.Next(releaseManners.Length)],
                    PhysicalTarget2026 = "12 Months",
                    FinancialObligation = unitCost * qty
                });
            }

            allReports.Add(report);
        }

        try
        {
            // Increase timeout for massive 5,000+ record insertion
            _context.Database.SetCommandTimeout(240); 

            _context.ExpenditureData.AddRange(allReports);
            await _context.SaveChangesAsync();

            return Ok(new { 
                Status = "Bulk Seed Successful", 
                ReportsCreated = allReports.Count, 
                ItemsCreated = allReports.Sum(x => x.Items.Count) 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal error: {ex.Message}");
        }
    }
}