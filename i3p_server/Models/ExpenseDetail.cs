using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace i3p_server.Models;

[Table("expense_details")]
public class ExpenseDetail
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("technical_specification")]
    public string? TechSpecs { get; set; }

    [Column("vendor_name")]
    public string? VendorName { get; set; }

    [Column("justification")]
    public string? Justification { get; set; }
}