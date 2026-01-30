using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


using i3p_server.Models;
using i3p_server.Models.Enums;

namespace i3p_server.Controllers;


[Route("api/expenses")]
[ApiController]
public class ExpenseController : ControllerBase
{
    
    private readonly AppDbContext _context;
    
    public ExpenseController(AppDbContext context) => _context = context;
    
    [HttpGet("GetSummaries")]
    public async Task<IActionResult> GetSummaries()
    {
        var summaries = await _context.ExpenseSummaries
            .Select(s => new 
            {
                id = s.Id, // Important: This ID is the key to opening the next view
                key_result_area = s.KeyResultArea,
                programs_projects_activities = s.PPA,
                expense_type = s.ExpenseType,
                objectives = s.Objectives,
                performance_indicator = s.PerformanceIndicator,
                description = s.Description,
                quantity = s.Quantity,
                estimated_cost = s.EstimatedCost,
                account_title = s.AccountTitle,
                account_code = s.AccountCode,
                date_created = DateTime.UtcNow,
                date_updated = DateTime.UtcNow,
            })
            .ToListAsync();

        return Ok(summaries);
    }
    
    [HttpPost("seed-nested")]
    public async Task<IActionResult> SeedNestedData()
    {
        if (await _context.ExpenseSummaries.AnyAsync())
            return BadRequest("Database already has data.");

        var random = new Random();
        var summariesToAdd = new List<ExpenseSummary>();

        // Reference data from your image context
        ExpenseType[] expenseTypes = { ExpenseType.REGULAR, ExpenseType.PROJECT, ExpenseType.REPAIR_AND_MAINTENANCE };
        string[] kraList = { "KRA 1: LEADING STRATEGICALLY", "KRA 2: MANAGING SCHOOL OPERATIONS", "KRA 3: FOCUSING ON TEACHING" };
        string[] ppaList = { "Pay of monthly electricity bill", "Procure Janitorial Supplies", "Repair of Printing Equipment" };
        string[] accountTitles = { "Electricity Expenses", "Office Supplies Expenses", "Repair and Maintenance" };
        string[] units = { "unit", "piece", "lot", "ream" };

        for (int s = 1; s <= 50; s++)
        {
            var currentDetails = new List<ProcurementDetail>();
            decimal runningTotalAmount = 0;

            // Inner loop: Generate 50 items for the "Orange Table" view
            for (int d = 1; d <= 50; d++)
            {
                decimal unitPrice = random.Next(100, 5000);
                int qty = random.Next(1, 20);
                decimal totalLine = unitPrice * qty;

                currentDetails.Add(new ProcurementDetail
                {
                    Description = $"Detailed Item {d} for Summary {s}",
                    Unit = units[random.Next(units.Length)],
                    UnitPrice = unitPrice,
                    TotalQty = qty,
                    TotalAmount = totalLine
                });
                runningTotalAmount += totalLine;
            }

            // Parent: Maps to the Summary Table (First Image)
            var summary = new ExpenseSummary
            {
                KeyResultArea = kraList[random.Next(kraList.Length)],
                ExpenseType = expenseTypes[random.Next(expenseTypes.Length)],
                PPA = ppaList[random.Next(ppaList.Length)],
                Objectives = "To provide essential resources for school operations.",
                PerformanceIndicator = "# of procurements completed",
                Description = "General Procurement Batch",
                Quantity = 1,
                EstimatedCost = runningTotalAmount, // Total of all 50 items
                AccountTitle = accountTitles[random.Next(accountTitles.Length)],
                AccountCode = "5020301000",
                Details = currentDetails // Automatically handles Foreign Keys
            };

            summariesToAdd.Add(summary);
        }

        _context.ExpenseSummaries.AddRange(summariesToAdd);
        await _context.SaveChangesAsync();
        
        return Ok(new { message = "Seeded 50 summaries and 2,500 details successfully." });
    }

    
    
    
    
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSummaryWithItems(int id)
    {
        var summary = await _context.ExpenseSummaries
            .Include(s => s.Details) // 👈 This is the critical part
            .FirstOrDefaultAsync(s => s.Id == id);

        if (summary == null) return NotFound();

        return Ok(summary);
    }
}