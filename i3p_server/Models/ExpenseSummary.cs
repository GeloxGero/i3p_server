using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using i3p_server.Models.Enums;

namespace i3p_server.Models;

[Table("expense_summaries")]
public class ExpenseSummary
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("key_result_area")] 
    public required string KeyResultArea { get; set; } // e.g., KRA 1: LEADING STRATEGICALLY
    
    [Column("expense_type")]
    public required ExpenseType ExpenseType { get; set; } // REGULAR, PROJECT, etc.

    [Column("programs_projects_activities")] 
    public required string PPA { get; set; } // e.g., Pay of monthly electricity bill

    [Column("objectives")]
    public string? Objectives { get; set; }

    [Column("performance_indicator")] 
    public required string PerformanceIndicator { get; set; }
    
    [Column("description")] 
    public string? Description { get; set; } // General resource description

    [Column("quantity")] 
    public required double Quantity { get; set; }

    [Column("estimated_cost")] 
    public required decimal EstimatedCost { get; set; } // Matches Total Amount of children
    
    [Column("account_title")] 
    public required string AccountTitle { get; set; }
    
    [Column("account_code")]
    public string? AccountCode { get; set; } // Usually stored as string to keep leading zeros
    
    [Column("date_created")]
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    [Column("date_updated")]
    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;
    
    public virtual ICollection<ProcurementDetail> Details { get; set; } = new List<ProcurementDetail>();
}