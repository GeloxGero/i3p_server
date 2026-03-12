using Microsoft.EntityFrameworkCore;

namespace i3p_server.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
            
    }
        
    public DbSet<Users> Users { get; set; }
    public DbSet<AnnualProcurementPlan>  AnnualProcurementPlan { get; set; }
    public DbSet<AppItem> AppItems { get; set; }
    public DbSet<ExpenditureData>  ExpenditureData { get; set; }
    public DbSet<ExpenditureItem>  ExpenditureItem { get; set; }
    public DbSet<PPMP>  PPMPs { get; set; }
    public DbSet<PpmpItem>   PpmpItem { get; set; }
    public DbSet<ProcurementPlanB>  ProcurementPlanBs { get; set; }
    public DbSet<ProcurementItemB>   ProcurementItemBs { get; set; }
    public DbSet<SchoolImplementation>   SchoolImplementations { get; set; }
    public DbSet<ImplementationItem>   ImplementationItems { get; set; }
    public DbSet<PlanCrossReference> PlanCrossReferences { get; set; }
}