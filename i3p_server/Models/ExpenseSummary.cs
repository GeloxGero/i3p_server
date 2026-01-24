using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace i3p_server.Models;

[Table("expense_summaries")]
public class ExpenseSummary
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("expense_class")] // Maps to uid: "class"
    public required string ExpenseClass { get; set; }

    [Column("dbm_grouping")] // Maps to uid: "grouping"
    public required string DbmGrouping { get; set; }

    [Column("total_amount")] // Maps to uid: "total"
    public decimal TotalAmount { get; set; }

    [Column("manner_of_release")] // Maps to uid: "release"
    public string MannerOfRelease { get; set; } = "Direct Payment";

    // Navigation: One Summary -> Many Details
    public virtual ICollection<ProcurementDetail> Details { get; set; } = new List<ProcurementDetail>();
}