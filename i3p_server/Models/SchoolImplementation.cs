using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace i3p_server.Models;

// ─── SchoolImplementation (parent / yearly plan) ──────────────────────────────
public enum SipStatus
{
    Implemented = 0,
    Approved    = 1,
}

public class SchoolImplementation
{
    [Key] public int Id { get; set; }
    public int Year { get; set; }
    public string SheetName { get; set; } = string.Empty;
    public double TotalEstimatedCost { get; set; }

    /// <summary>
    /// Annual budget ceiling set by the admin. When non-null the frontend
    /// shows a budget vs expenditure comparison. Null = not yet configured.
    /// </summary>
    public double? AnnualBudget { get; set; }

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

    public string? ArCode { get; set; }
    public bool IsVerified { get; set; } = false;
    public SipStatus Status { get; set; } = SipStatus.Implemented;

    [ForeignKey(nameof(SchoolImplementationId))]
    public SchoolImplementation? SchoolImplementation { get; set; }

    public List<PlanCrossReference> CrossReferences { get; set; } = new();

    [NotMapped] public bool IsSubtotal =>
        Kra?.Contains("Subtotal", StringComparison.OrdinalIgnoreCase) ?? false;
}