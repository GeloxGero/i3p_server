using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


using i3p_server.Models;

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
                @class = s.ExpenseClass,
                grouping = s.DbmGrouping,
                total = s.TotalAmount,
                release = s.MannerOfRelease
            })
            .ToListAsync();

        return Ok(summaries);
    }
    
    [HttpPost("seed-nested")]
    public async Task<IActionResult> SeedNestedData()
    {
        // 1. Safety check
        if (await _context.ExpenseSummaries.AnyAsync())
            return BadRequest("Database already has data.");

        var random = new Random();
        var summariesToAdd = new List<ExpenseSummary>();

        string[] classes = { "PS", "MOOE", "CO" };
        string[] groupings = { "Training & Scholarship", "Supplies & Materials", "General Services", "Utilities", "Repairs" };
        string[] items = { "Paper Reams", "Ballpens", "Printer Ink", "Snacks", "Meals", "Chairs", "Desks", "Internet", "Water", "Electricity" };
        string[] units = { "lot", "piece", "box", "ream", "pax" };

        // --- Outer Loop: Create 50 Summaries ---
        for (int s = 1; s <= 50; s++)
        {
            var expenseClass = classes[random.Next(classes.Length)];
            var grouping = groupings[random.Next(groupings.Length)];
            
            // We will collect the 50 details here first
            var currentDetails = new List<ProcurementDetail>();
            decimal runningTotalAmount = 0;

            // --- Inner Loop: Create 50 Details for THIS Summary ---
            for (int d = 1; d <= 50; d++)
            {
                decimal unitPrice = random.Next(100, 2000); 
                int qty = random.Next(5, 50);
                decimal totalLine = unitPrice * qty;

                // Generate realistic monthly spread (simplified)
                int monthlyAvg = qty / 12;
                var timelineObj = new 
                {
                    jan = monthlyAvg, feb = monthlyAvg, mar = monthlyAvg, q1 = monthlyAvg * 3,
                    apr = monthlyAvg, may = monthlyAvg, jun = monthlyAvg, q2 = monthlyAvg * 3,
                    jul = monthlyAvg, aug = monthlyAvg, sep = monthlyAvg, q3 = monthlyAvg * 3,
                    oct = monthlyAvg, nov = monthlyAvg, dec = monthlyAvg, q4 = monthlyAvg * 3
                };

                var detail = new ProcurementDetail
                {
                    Description = $"{items[random.Next(items.Length)]} - Batch {s} Item {d}",
                    Unit = units[random.Next(units.Length)],
                    UnitPrice = unitPrice,
                    TotalQty = qty,
                    TotalAmount = totalLine,
                    // TimelineData = System.Text.Json.JsonSerializer.Serialize(timelineObj)
                };

                currentDetails.Add(detail);
                runningTotalAmount += totalLine; // Add to parent's running total
            }

            // --- Create Parent Summary ---
            var summary = new ExpenseSummary
            {
                ExpenseClass = expenseClass,
                DbmGrouping = grouping,
                // We use the calculated sum so the data is consistent
                TotalAmount = runningTotalAmount, 
                MannerOfRelease = "Direct Payment",
                
                // EF Core Magic: Adding the details to this collection automatically 
                // sets up the Foreign Keys and saves them when the parent is saved.
                Details = currentDetails 
            };

            summariesToAdd.Add(summary);
        }

        try 
        {
            // One massive save for performance
            _context.ExpenseSummaries.AddRange(summariesToAdd);
            await _context.SaveChangesAsync();
            
            return Ok(new { 
                message = "Database populated!", 
                summariesCreated = 50, 
                detailsCreated = 2500 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, detail = ex.InnerException?.Message });
        }
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