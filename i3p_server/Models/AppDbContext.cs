using Microsoft.EntityFrameworkCore;

namespace i3p_server.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
            
    }
        
    public DbSet<Users> Users { get; set; }
    public DbSet<ExpenseRecord> Expenses { get; set; }
    public DbSet<ExpenseDetail> ExpenseDetails { get; set; }
}