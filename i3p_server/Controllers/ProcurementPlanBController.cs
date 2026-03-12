using Microsoft.AspNetCore.Mvc;
using i3p_server.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace i3p_server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProcurementPlanBController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProcurementPlanBController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProcurementPlanB>>> GetProcurementPlans()
    {
        // Include items so the frontend can map over them immediately
        return await _context.ProcurementPlanBs
            .Include(p => p.Items)
            .ToListAsync();
    }
    
    [HttpPost("seed-bulk")]
    public async Task<IActionResult> SeedBulkProcurementPlanB()
    {
        // 1. Guard: Avoid duplicates
        if (_context.ProcurementPlanBs.Any())
        {
            return BadRequest("Database already contains Procurement Plan B data.");
        }

        var random = new Random();
        var allPlans = new List<ProcurementPlanB>();

        // Mock Data Pools
        string[] types = { "Non-PS", "PS" };
        string[] categories = { "Office Supplies", "Medical Supplies", "IT Equipment", "Janitorial", "Construction" };
        string[] units = { "piece", "box", "ream", "roll", "unit", "bottle" };

        for (int p = 1; p <= 50; p++)
        {
            var plan = new ProcurementPlanB
            {
                SheetName = $"APP-B {2025 + (p % 2)} - Batch {p}",
                AuxilliaryJson = JsonSerializer.Serialize(new { Office = "DepEd District " + p, FiscalYear = 2026 }),
                HeadersJson = JsonSerializer.Serialize(new[] { "Type", "ID", "Code", "Description", "Unit", "Price" }),
                Items = new List<ProcurementItemB>()
            };

            // Create 100 items for this specific plan
            for (int i = 1; i <= 100; i++)
            {
                // Every 20th item, we create a Category Header (Type is null)
                bool isHeader = (i % 20 == 1);
                
                if (isHeader)
                {
                    plan.Items.Add(new ProcurementItemB
                    {
                        Type = null,
                        CategoryId = null,
                        Code = null,
                        Description = categories[random.Next(categories.Length)].ToUpper(),
                        Unit = null,
                        UnitPrice = null
                    });
                }
                else
                {
                    plan.Items.Add(new ProcurementItemB
                    {
                        Type = types[random.Next(types.Length)],
                        CategoryId = random.Next(1, 999).ToString(),
                        Code = $"LHNHS-ITM-{p}-{i}",
                        Description = $"Item Description {i} for Plan {p}",
                        Unit = units[random.Next(units.Length)],
                        UnitPrice = Math.Round(random.NextDouble() * 5000, 2)
                    });
                }
            }

            allPlans.Add(plan);
        }

        try
        {
            // Increase timeout for large data insertion if necessary
            _context.Database.SetCommandTimeout(120); 
            
            _context.ProcurementPlanBs.AddRange(allPlans);
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Bulk seed complete", 
                plansCreated = allPlans.Count, 
                totalItemsCreated = allPlans.Sum(x => x.Items.Count) 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error during seeding: {ex.Message}");
        }
    }
}