using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace i3p_server.Models;

[Table("expense_records")]
public class ExpenseRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("expense_class")] // e.g., PS, MOOE
    public required string ExpenseClass { get; set; }

    [Column("dbm_grouping")] // e.g., TRAINING & SCHOLARSHIP
    public required string DbmGrouping { get; set; }

    [Column("expense_item")] // e.g., Snacks for Execom Meeting
    public required string ExpenseItem { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("unit_cost")]
    public decimal UnitCost { get; set; }

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("manner_of_release")]
    public string MannerOfRelease { get; set; } = "Direct Payment";

    // --- FOREIGN KEY SECTION ---
    
    [Column("detail_id")]
    public int? DetailId { get; set; } // The actual ID column in PostgreSQL

    [ForeignKey("DetailId")]
    public virtual ExpenseDetail? Detail { get; set; } // Navigation property for C#
}