using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace i3p_server.Models;

// 1. This is the main entity that will become a table
// ─── AnnualProcurementPlan ────────────────────────────────────────────────────
 
// ─── AnnualProcurementPlan ────────────────────────────────────────────────────
 
public class AnnualProcurementPlan
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
 
    public int Year { get; set; }
    public string FileName { get; set; } = string.Empty;
    public decimal YearTotal { get; set; }
    public string? AuxilliaryJson { get; set; }
    public string? HeadersJson    { get; set; }
 
    public List<AppItem> Items { get; set; } = new();
}

public class AppItem
{
    [Key] public int Id { get; set; }
 
    public int AnnualProcurementPlanId { get; set; }
    
    [JsonIgnore]
    public AnnualProcurementPlan AnnualProcurementPlan { get; set; }
 
    public string? No              { get; set; }
    public string? Unspsc          { get; set; }
    public string? ItemDescription { get; set; }
    public string? Specification   { get; set; }
    public string? UnitOfMeasure   { get; set; }
    public double? TotalQuantity   { get; set; }
    public double? Price           { get; set; }
    public double? TotalAmount     { get; set; }
 
    // ── AR Code ───────────────────────────────────────────────────────────────
    /// <summary>
    /// Set to the same value as the linked ImplementationItem.ArCode when
    /// this item is connected to a SIP row. Null until linked.
    /// </summary>
    public string? ArCode { get; set; }
 
    // ── Photo verification ────────────────────────────────────────────────────
    /// <summary>
    /// Server-relative path of the uploaded proof photo, e.g.
    /// "uploads/app-items/42/receipt_2026.jpg". Null until uploaded.
    /// </summary>
    public string? PhotoPath { get; set; }
 
    /// <summary>UTC time the admin confirmed this item's photo.</summary>
    public DateTime? VerifiedAt { get; set; }
 
    /// <summary>Username of the admin who verified this item.</summary>
    public string? VerifiedBy { get; set; }
 
    /// <summary>True when a photo has been uploaded AND an admin has confirmed it.</summary>
    public bool IsPhotoVerified { get; set; } = false;
    
    public string? SecurePhotoUrl { get; set; } = string.Empty;
 
    // Navigation
    public List<PlanCrossReference> CrossReferences { get; set; } = new();
}