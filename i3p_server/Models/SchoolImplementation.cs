using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace i3p_server.Models;

// ─── SchoolImplementation (parent / yearly plan) ──────────────────────────────

public class SchoolImplementation
{
    [Key] public int Id { get; set; }
    public int Year { get; set; }
    public string SheetName { get; set; } = string.Empty;
    public double TotalEstimatedCost { get; set; }
    public string? AuxilliaryJson { get; set; }
    public string? HeadersJson { get; set; }
    public List<ImplementationItem> Items { get; set; } = new();
}
 
// ─── ImplementationItem ───────────────────────────────────────────────────────
 
public class ImplementationItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
 
    public int SchoolImplementationId { get; set; }
 
    public string? Date            { get; set; }  // "yyyy-MM-dd"
    public string? Kra             { get; set; }
    public string? SipProgram      { get; set; }
    public string? Activity        { get; set; }
    public string? Purpose         { get; set; }
    public string? Indicator       { get; set; }
    public string? Resources       { get; set; }
    public string? Quantity        { get; set; }
    public double? EstimatedCost   { get; set; }
    public string? AccountTitle    { get; set; }
    public string? AccountCode     { get; set; }
    public string? ExpenditureType { get; set; }
 
    // ── AR Code ───────────────────────────────────────────────────────────────
    /// <summary>
    /// Accountability Reference code. Format: "AR-{Year}-{6-char hex}".
    /// Generated when the first AppItem is linked to this row.
    /// Shared with every AppItem connected to this row.
    /// </summary>
    public string? ArCode { get; set; }
 
    // ── Verification ──────────────────────────────────────────────────────────
    /// <summary>
    /// True only when ALL AppItems sharing this ArCode are individually
    /// photo-verified. Recomputed by ArController on each state change.
    /// </summary>
    public bool IsVerified { get; set; } = false;
 
    // Navigation
    [ForeignKey(nameof(SchoolImplementationId))]
    public SchoolImplementation? SchoolImplementation { get; set; }
 
    public List<PlanCrossReference> CrossReferences { get; set; } = new();
 
    [NotMapped] public bool IsSubtotal =>
        Kra?.Contains("Subtotal", StringComparison.OrdinalIgnoreCase) ?? false;
}