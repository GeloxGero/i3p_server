using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace i3p_server.Models;

// This is the "Parent" table that EF Core will create
public class PPMP
{
    [Key]
    public int Id { get; set; }

    public string SheetName { get; set; } = string.Empty;

    // We store these complex types as JSON strings in the DB
    public string? AuxilliaryJson { get; set; }
    public string? HeadersJson { get; set; }

    // Navigation property: One PPMP has many Items
    public List<PpmpItem> Items { get; set; } = new();
}

// This is the "Child" table for individual rows
public class PpmpItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // Foreign Key back to the Parent PPMP
    public int PPMPId { get; set; }

    public string? Code { get; set; }
    public string? GeneralDescription { get; set; }
    public string? Units { get; set; }
    public double? UnitPrice { get; set; }
    public double? Quantity { get; set; }
    public double? EstimatedBudget { get; set; }
    public string? ModeOfProcurement { get; set; }

    // Schedule is a list; store as JSON string or handle via mapping
    public string? ScheduleJson { get; set; }
}