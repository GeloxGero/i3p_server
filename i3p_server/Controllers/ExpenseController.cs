using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


using i3p_server.Models;

namespace i3p_server.Controllers;


[Route("api/expense")]
[ApiController]
public class ExpenseController : ControllerBase
{
    
    private readonly AppDbContext _context;
    
    public ExpenseController(AppDbContext context) => _context = context;
    
    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAllExpenses()
    {
        // Use .Include to join the related details table
        var expenses = await _context.Expenses.Select(x => new

            {
                id = x.Id,
                @class = x.ExpenseClass, // 'class' is a keyword in C#, use @ to use it as a name
                grouping = x.DbmGrouping,
                item = x.ExpenseItem,
                qty = x.Quantity,
                unitCost = x.UnitCost,
                total = x.TotalAmount,
                release = x.MannerOfRelease,
                // Include details if they exist
                details = x.Detail != null ? new {
                    x.Detail.TechSpecs,
                    x.Detail.VendorName
                } : null
            })
            .ToListAsync();

        return Ok(expenses);
    }
}