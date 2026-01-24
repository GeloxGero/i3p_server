using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using i3p_server.Models;

namespace i3p_server.Models;
[Table("procurement_details")]
public class ProcurementDetail
{
    [Key]
    public int Id { get; set; }

    // Foreign Key: Links this item to the specific Summary Row
    [Column("summary_id")]
    public int SummaryId { get; set; }
    
    [ForeignKey("SummaryId")]
    [System.Text.Json.Serialization.JsonIgnore] // Prevent loops when serializing
    public virtual ExpenseSummary? Summary { get; set; }
    
    [Column("item_description")]
    public required string Description { get; set; }

    [Column("unit_measure")]
    public string? Unit { get; set; }

    [Column("unit_price")]
    public decimal UnitPrice { get; set; }

    [Column("total_qty")]
    public double TotalQty { get; set; }

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    // // Stores the Jan-Dec quantities dynamically
    // [Column(TypeName = "jsonb")]
    // public string TimelineData { get; set; } = "{}"; 
}