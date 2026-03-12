using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace i3p_server.Models;

// 1. This is the MAIN table that represents the Excel File/Report
public class ProcurementPlanB
{
    [Key]
    public int Id { get; set; }

    public string SheetName { get; set; } = string.Empty;

    // Use [NotMapped] if you don't want to save these to the DB, 
    // or store them as JSON strings.
    public string? AuxilliaryJson { get; set; }
    public string? HeadersJson { get; set; }

    // Relationship: One Report has many Items
    public List<ProcurementItemB> Items { get; set; } = new();
}

// 2. This is the ITEM table that represents the rows in that file
public class ProcurementItemB
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // Foreign Key linking back to the Header table
    public int ProcurementPlanBId { get; set; }

    public string? Type { get; set; }           
    public string? CategoryId { get; set; }     
    public string? Code { get; set; }           
    public string? Description { get; set; }    
    public string? Unit { get; set; }          
    public double? UnitPrice { get; set; }     

    [NotMapped] // Calculated properties should not be saved as columns
    public bool IsCategoryHeader => string.IsNullOrEmpty(Type) && !string.IsNullOrEmpty(Description);
}