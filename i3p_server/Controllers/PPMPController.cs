using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using i3p_server.Models;
using System.Text.Json;

namespace i3p_server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PPMPController : ControllerBase
{
    private readonly AppDbContext _context;

    public PPMPController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/PPMP
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PPMP>>> GetPPMPs()
    {
        // We exclude Items by default to keep the list view light
        return await _context.PPMPs.ToListAsync();
    }

    // GET: api/PPMP/5
    [HttpGet("{id}")]
    public async Task<ActionResult<PPMP>> GetPPMP(int id)
    {
        var ppmp = await _context.PPMPs
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (ppmp == null) return NotFound();

        return ppmp;
    }

    [HttpPost("seed-bulk")]
    public async Task<IActionResult> SeedBulkPPMP()
    {
        // Guard: Prevent double seeding
        if (await _context.PPMPs.AnyAsync())
        {
            return BadRequest("The PPMP table already contains data.");
        }

        var random = new Random();
        var allPpmpReports = new List<PPMP>();

        // Mock Data Pools
        string[] offices = { "Accounting", "HR", "IT Services", "Science Dept", "Library" };
        string[] items = { "A4 Paper", "Printer Ink", "External Hard Drive", "Ballpoint Pen", "Cleaning Fluid", "Uniforms" };
        string[] units = { "ream", "cartridge", "unit", "box", "bottle", "piece" };
        string[] modes = { "Shopping", "Public Bidding", "Small Value Procurement", "Direct Contracting" };

        for (int p = 1; p <= 50; p++)
        {
            var ppmp = new PPMP
            {
                SheetName = $"PPMP FY 2026 - {offices[p % offices.Length]} (Batch {p})",
                AuxilliaryJson = JsonSerializer.Serialize(new { 
                    Office = offices[p % offices.Length], 
                    FiscalYear = 2026,
                    Status = "Draft"
                }),
                HeadersJson = JsonSerializer.Serialize(new[] { 
                    "Code", "Description", "Units", "UnitPrice", "Quantity", "Budget", "Mode" 
                }),
                Items = new List<PpmpItem>()
            };

            for (int i = 1; i <= 100; i++)
            {
                double unitPrice = Math.Round(random.NextDouble() * 1500 + 10, 2);
                double qty = random.Next(1, 50);

                ppmp.Items.Add(new PpmpItem
                {
                    Code = $"2026-{p:D3}-{i:D3}",
                    GeneralDescription = $"{items[random.Next(items.Length)]} - Model {i}",
                    Units = units[random.Next(units.Length)],
                    UnitPrice = unitPrice,
                    Quantity = qty,
                    EstimatedBudget = unitPrice * qty,
                    ModeOfProcurement = modes[random.Next(modes.Length)],
                    // Mocking 12 months of quantity distribution
                    ScheduleJson = JsonSerializer.Serialize(Enumerable.Range(1, 12).Select(_ => random.Next(0, 5)).ToList())
                });
            }

            allPpmpReports.Add(ppmp);
        }

        try
        {
            // Set longer timeout for large batch operations
            _context.Database.SetCommandTimeout(180);

            _context.PPMPs.AddRange(allPpmpReports);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Bulk seeding successful",
                TablesCreated = allPpmpReports.Count,
                TotalItemsCreated = allPpmpReports.Sum(x => x.Items.Count)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred during seeding: {ex.Message}");
        }
    }
}