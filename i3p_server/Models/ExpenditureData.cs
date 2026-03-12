using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace i3p_server.Models;

// This represents the Header/File info (The Table EF is looking for)
public class ExpenditureData
{
    [Key]
    public int Id { get; set; }
    
    public string SheetName { get; set; } = string.Empty;

    // We store complex lists as JSON strings in the DB for simplicity
    public string? AuxilliaryJson { get; set; } 
    public string? HeadersJson { get; set; }

    // Relationship: One Report has many Items
    public List<ExpenditureItem> Items { get; set; } = new();
}

// This represents the individual rows
public class ExpenditureItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // Foreign Key linking back to ExpenditureData
    public int ExpenditureDataId { get; set; }

    public string? SpecificProgram { get; set; } 
    public string? Output { get; set; }          
    public string? Activities { get; set; }      
    public string? PerformanceIndicator { get; set; } 

    public string? ExpenseClass { get; set; }    
    public string? ExpenseObject { get; set; }   
    public string? ExpenseItem { get; set; }     
    public double? UnitCost { get; set; }       
    public double? Quantity { get; set; }       
    public double? TotalCost { get; set; }      

    public string? IsPpmp { get; set; }          
    public string? IsAppSupplies { get; set; }   
    public string? MannerOfRelease { get; set; } 

    public string? PhysicalTarget2026 { get; set; } 
    public double? FinancialObligation { get; set; } 
}