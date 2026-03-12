using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace i3p_server.Models;

// ─── Match status ─────────────────────────────────────────────────────────────

public enum CrossReferenceStatus
{
    /// <summary>
    /// APP item was scanned but no SIP item with the same cost was found.
    /// Stored so the frontend can show a clear "no match" state.
    /// </summary>
    Unmatched = 0,

    /// <summary>
    /// A cost match was found automatically. Waiting for an admin to
    /// confirm or reject the link.
    /// </summary>
    PendingReview = 1,

    /// <summary>Admin has manually confirmed this link is correct.</summary>
    Verified = 2,

    /// <summary>Admin has manually rejected this proposed link.</summary>
    Rejected = 3,

    /// <summary>
    /// One or both sides of the link were deleted after the row was created.
    /// The row is kept for audit purposes; IsOrphaned is also set to true.
    /// </summary>
    Orphaned = 4,
}

// ─── PlanCrossReference ───────────────────────────────────────────────────────

/// <summary>
/// Join table that tracks every potential or confirmed link between an
/// AnnualProcurementPlan item (AppItem) and a SchoolImplementation item
/// (ImplementationItem) for the same fiscal year.
///
/// One AppItem can have at most one active cross-reference row (the best cost
/// match). If multiple SIP items share the same cost, all candidates are stored
/// as separate PendingReview rows and the admin picks the right one.
/// </summary>
public class PlanCrossReference
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>Fiscal year both items belong to, e.g. 2026.</summary>
    public int Year { get; set; }

    // ── APP side ──────────────────────────────────────────────────────────────
    // Nullable so the row survives if the AppItem is later deleted.

    public int? AppItemId { get; set; }

    [ForeignKey(nameof(AppItemId))]
    public AppItem? AppItem { get; set; }

    /// <summary>
    /// Snapshot of the APP item's price at the time the match was created.
    /// Preserved so the row stays meaningful even after the AppItem is deleted.
    /// </summary>
    public double AppItemPrice { get; set; }

    /// <summary>Snapshot of AppItem.ItemDescription at match time.</summary>
    public string? AppItemDescription { get; set; }

    // ── SIP side ──────────────────────────────────────────────────────────────
    // Nullable for the same reason — orphan-safe.

    /// <summary>
    /// Null when Status == Unmatched (no SIP candidate was found).
    /// </summary>
    public int? ImplementationItemId { get; set; }

    [ForeignKey(nameof(ImplementationItemId))]
    public ImplementationItem? ImplementationItem { get; set; }

    /// <summary>
    /// Snapshot of ImplementationItem.EstimatedCost at match time.
    /// </summary>
    public double? SipItemCost { get; set; }

    /// <summary>Snapshot of ImplementationItem.Activity at match time.</summary>
    public string? SipItemActivity { get; set; }

    // ── Match metadata ────────────────────────────────────────────────────────

    public CrossReferenceStatus Status { get; set; } = CrossReferenceStatus.Unmatched;

    /// <summary>
    /// True when either AppItemId or ImplementationItemId has been set to null
    /// because the referenced row was deleted. Status is also set to Orphaned.
    /// </summary>
    public bool IsOrphaned { get; set; } = false;

    /// <summary>
    /// How closely the two costs matched (0.0 – 1.0).
    /// 1.0 = exact match. Used to rank candidates for the admin review UI.
    /// </summary>
    public double MatchScore { get; set; }

    /// <summary>Free-text note left by the admin when verifying or rejecting.</summary>
    public string? AdminNote { get; set; }

    /// <summary>UTC timestamp when the match was first detected by the system.</summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp of the most recent admin action (verify / reject).</summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>Username / ID of the admin who last acted on this row.</summary>
    public string? ReviewedBy { get; set; }
}