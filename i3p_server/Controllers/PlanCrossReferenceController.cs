using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using i3p_server.Models;

namespace i3p_server.Controllers;

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public record CrossRefSummaryDto(
    int Year,
    int TotalAppItems,
    int Unmatched,
    int PendingReview,
    int Verified,
    int Rejected,
    int Orphaned
);

public record CrossRefRowDto(
    int Id,
    int Year,
    CrossReferenceStatus Status,
    bool IsOrphaned,
    double MatchScore,
    // APP side
    int? AppItemId,
    string? AppItemDescription,
    double AppItemPrice,
    // SIP side
    int? ImplementationItemId,
    string? SipItemActivity,
    double? SipItemCost,
    // Review
    string? AdminNote,
    DateTime DetectedAt,
    DateTime? ReviewedAt,
    string? ReviewedBy
);

public record ReviewActionDto(
    /// <summary>"verify" or "reject"</summary>
    string Action,
    string? AdminNote,
    string? ReviewedBy
);

// ─── Controller ───────────────────────────────────────────────────────────────

[Route("api/[controller]")]
[ApiController]
public class PlanCrossReferenceController : ControllerBase
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Two costs are considered a match when they are within this fractional
    /// tolerance of each other. 0.0 = exact only. Raise to e.g. 0.05 for ±5 %.
    /// </summary>
    private const double MatchTolerance = 0.0;

    public PlanCrossReferenceController(AppDbContext context)
    {
        _context = context;
    }

    // ── GET /api/PlanCrossReference/summary/{year} ────────────────────────────
    // Dashboard card: how many APP items are unmatched / pending / verified
    // for a given fiscal year.
    [HttpGet("summary/{year:int}")]
    public async Task<ActionResult<CrossRefSummaryDto>> GetSummary(int year)
    {
        // Total APP items for this year (need the plan's year, not the item's date)
        var totalApp = await _context.AppItems
            .CountAsync(a => a.AnnualProcurementPlan.Year == year);

        var counts = await _context.PlanCrossReferences
            .Where(x => x.Year == year)
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        int Get(CrossReferenceStatus s) => counts.FirstOrDefault(c => c.Status == s)?.Count ?? 0;

        return Ok(new CrossRefSummaryDto(
            year,
            totalApp,
            Get(CrossReferenceStatus.Unmatched),
            Get(CrossReferenceStatus.PendingReview),
            Get(CrossReferenceStatus.Verified),
            Get(CrossReferenceStatus.Rejected),
            Get(CrossReferenceStatus.Orphaned)
        ));
    }

    // ── GET /api/PlanCrossReference/{year} ────────────────────────────────────
    // Full list of cross-reference rows for a year, optionally filtered by status.
    [HttpGet("{year:int}")]
    public async Task<ActionResult<IEnumerable<CrossRefRowDto>>> GetByYear(
        int year,
        [FromQuery] CrossReferenceStatus? status = null)
    {
        var query = _context.PlanCrossReferences
            .Where(x => x.Year == year);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        var rows = await query
            .OrderByDescending(x => x.MatchScore)
            .ThenBy(x => x.Status)
            .Select(x => ToDto(x))
            .ToListAsync();

        return Ok(rows);
    }

    // ── GET /api/PlanCrossReference/pending ───────────────────────────────────
    // Admin review queue: all PendingReview rows across every year,
    // sorted by year desc then match score desc.
    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<CrossRefRowDto>>> GetPendingQueue()
    {
        var rows = await _context.PlanCrossReferences
            .Where(x => x.Status == CrossReferenceStatus.PendingReview)
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.MatchScore)
            .Select(x => ToDto(x))
            .ToListAsync();

        return Ok(rows);
    }

    // ── POST /api/PlanCrossReference/match/{year} ─────────────────────────────
    // Runs the matching algorithm for a fiscal year.
    //
    // Algorithm:
    //   1. Load all AppItems for the year and all ImplementationItems for the year.
    //   2. For each AppItem, find every ImplementationItem whose EstimatedCost is
    //      within MatchTolerance of the AppItem's Price.
    //   3. Skip AppItems that already have a Verified or PendingReview row so
    //      re-running this endpoint is safe (idempotent for confirmed rows).
    //   4. Delete any previous Unmatched / Rejected rows for that AppItem, then
    //      insert fresh PendingReview rows (one per SIP candidate) or one
    //      Unmatched row if no candidates were found.
    //
    // Returns a summary of what was created / updated.
    [HttpPost("match/{year:int}")]
    public async Task<IActionResult> RunMatching(int year)
    {
        // Load APP items for the year
        var appItems = await _context.AppItems
            .Where(a => a.AnnualProcurementPlan.Year == year && a.Price != null)
            .ToListAsync();

        if (appItems.Count == 0)
            return BadRequest($"No APP items found for year {year}.");

        // Load SIP items for the year (Date stored as "yyyy-MM-dd")
        var yearPrefix = $"{year}-";
        var sipItems = await _context.ImplementationItems
            .Where(i => i.Date != null &&
                        i.Date.StartsWith(yearPrefix) &&
                        i.EstimatedCost != null)
            .ToListAsync();

        // Load existing cross-references for this year so we can skip locked rows
        var existingRefs = await _context.PlanCrossReferences
            .Where(x => x.Year == year)
            .ToListAsync();

        // AppItemIds that already have a Verified or PendingReview row — skip these
        var lockedAppIds = existingRefs
            .Where(x => x.Status is CrossReferenceStatus.Verified
                                 or CrossReferenceStatus.PendingReview)
            .Where(x => x.AppItemId.HasValue)
            .Select(x => x.AppItemId!.Value)
            .ToHashSet();

        // Rows to delete (stale Unmatched / Rejected for non-locked app items)
        var toDelete = existingRefs
            .Where(x => x.AppItemId.HasValue &&
                        !lockedAppIds.Contains(x.AppItemId.Value) &&
                        x.Status is CrossReferenceStatus.Unmatched
                                 or CrossReferenceStatus.Rejected)
            .ToList();

        _context.PlanCrossReferences.RemoveRange(toDelete);

        int pendingCreated  = 0;
        int unmatchedCreated = 0;

        foreach (var appItem in appItems)
        {
            // Skip items that already have a confirmed or queued link
            if (lockedAppIds.Contains(appItem.Id)) continue;

            double appPrice = appItem.Price!.Value;

            // Find SIP candidates within tolerance
            var candidates = sipItems
                .Where(sip =>
                {
                    double sipCost = sip.EstimatedCost!.Value;
                    if (MatchTolerance == 0.0)
                        return sipCost == appPrice; // exact match
                    double delta = Math.Abs(sipCost - appPrice);
                    double threshold = appPrice * MatchTolerance;
                    return delta <= threshold;
                })
                .OrderBy(sip => Math.Abs(sip.EstimatedCost!.Value - appPrice))
                .ToList();

            if (candidates.Count == 0)
            {
                // No match found — create an Unmatched sentinel row
                _context.PlanCrossReferences.Add(new PlanCrossReference
                {
                    Year                 = year,
                    AppItemId            = appItem.Id,
                    AppItemPrice         = appPrice,
                    AppItemDescription   = appItem.ItemDescription,
                    ImplementationItemId = null,
                    SipItemCost          = null,
                    SipItemActivity      = null,
                    Status               = CrossReferenceStatus.Unmatched,
                    MatchScore           = 0,
                    DetectedAt           = DateTime.UtcNow,
                });
                unmatchedCreated++;
            }
            else
            {
                // One PendingReview row per candidate
                foreach (var sip in candidates)
                {
                    double sipCost = sip.EstimatedCost!.Value;
                    double score   = appPrice == 0 ? 0
                        : 1.0 - Math.Abs(sipCost - appPrice) / appPrice;

                    _context.PlanCrossReferences.Add(new PlanCrossReference
                    {
                        Year                 = year,
                        AppItemId            = appItem.Id,
                        AppItemPrice         = appPrice,
                        AppItemDescription   = appItem.ItemDescription,
                        ImplementationItemId = sip.Id,
                        SipItemCost          = sipCost,
                        SipItemActivity      = sip.Activity,
                        Status               = CrossReferenceStatus.PendingReview,
                        MatchScore           = Math.Round(score, 4),
                        DetectedAt           = DateTime.UtcNow,
                    });
                    pendingCreated++;
                }
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Year            = year,
            AppItemsScanned = appItems.Count(a => !lockedAppIds.Contains(a.Id)),
            PendingCreated  = pendingCreated,
            UnmatchedCreated = unmatchedCreated,
            SkippedLocked   = lockedAppIds.Count,
            StaleDeleted    = toDelete.Count,
        });
    }

    // ── POST /api/PlanCrossReference/review/{id} ──────────────────────────────
    // Admin action: verify or reject a PendingReview row.
    //
    // On "verify":
    //   - This row → Verified.
    //   - All *other* PendingReview rows for the same AppItem → Rejected,
    //     so only one verified link exists per AppItem.
    //
    // On "reject":
    //   - This row → Rejected.
    //   - Other PendingReview rows for the same AppItem are untouched
    //     (admin can still verify one of the remaining candidates).
    [HttpPost("review/{id:int}")]
    public async Task<IActionResult> ReviewRow(int id, [FromBody] ReviewActionDto dto)
    {
        var action = dto.Action?.ToLower();
        if (action is not ("verify" or "reject"))
            return BadRequest("Action must be 'verify' or 'reject'.");

        var row = await _context.PlanCrossReferences.FindAsync(id);
        if (row is null) return NotFound();

        if (row.Status is CrossReferenceStatus.Orphaned)
            return BadRequest("Cannot review an orphaned cross-reference.");

        if (row.Status is CrossReferenceStatus.Verified && action == "verify")
            return BadRequest("Row is already verified.");

        row.ReviewedAt  = DateTime.UtcNow;
        row.ReviewedBy  = dto.ReviewedBy;
        row.AdminNote   = dto.AdminNote;

        if (action == "verify")
        {
            row.Status = CrossReferenceStatus.Verified;

            // Reject all other pending candidates for this AppItem
            if (row.AppItemId.HasValue)
            {
                var siblings = await _context.PlanCrossReferences
                    .Where(x => x.AppItemId == row.AppItemId &&
                                x.Id != row.Id &&
                                x.Status == CrossReferenceStatus.PendingReview)
                    .ToListAsync();

                foreach (var sibling in siblings)
                {
                    sibling.Status     = CrossReferenceStatus.Rejected;
                    sibling.ReviewedAt = DateTime.UtcNow;
                    sibling.ReviewedBy = dto.ReviewedBy;
                    sibling.AdminNote  = "Auto-rejected: another candidate was verified.";
                }
            }
        }
        else
        {
            row.Status = CrossReferenceStatus.Rejected;
        }

        await _context.SaveChangesAsync();
        return Ok(ToDto(row));
    }

    // ── POST /api/PlanCrossReference/mark-orphans ─────────────────────────────
    // Maintenance endpoint: scans all cross-reference rows and marks any whose
    // AppItem or ImplementationItem no longer exists in the DB as Orphaned.
    // Called automatically by the delete endpoints in the other controllers,
    // but can also be triggered manually after direct DB edits.
    [HttpPost("mark-orphans")]
    public async Task<IActionResult> MarkOrphans()
    {
        var refs = await _context.PlanCrossReferences
            .Where(x => !x.IsOrphaned)
            .ToListAsync();

        var allAppIds = await _context.AppItems.Select(a => a.Id).ToHashSetAsync();
        var allSipIds = await _context.ImplementationItems.Select(i => i.Id).ToHashSetAsync();

        int marked = 0;

        foreach (var row in refs)
        {
            bool appMissing = row.AppItemId.HasValue && !allAppIds.Contains(row.AppItemId.Value);
            bool sipMissing = row.ImplementationItemId.HasValue && !allSipIds.Contains(row.ImplementationItemId.Value);

            if (appMissing || sipMissing)
            {
                if (appMissing)  row.AppItemId            = null;
                if (sipMissing)  row.ImplementationItemId = null;
                row.IsOrphaned = true;
                row.Status     = CrossReferenceStatus.Orphaned;
                marked++;
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { MarkedOrphaned = marked });
    }

    // ── DELETE /api/PlanCrossReference/{id} ───────────────────────────────────
    // Permanently removes a single cross-reference row.
    // Only allowed for Unmatched, Rejected, and Orphaned rows.
    // Verified rows must be un-verified first (re-review as reject).
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var row = await _context.PlanCrossReferences.FindAsync(id);
        if (row is null) return NotFound();

        if (row.Status == CrossReferenceStatus.Verified)
            return BadRequest("Cannot delete a Verified cross-reference. Reject it first.");

        _context.PlanCrossReferences.Remove(row);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Cross-reference deleted." });
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static CrossRefRowDto ToDto(PlanCrossReference x) => new(
        x.Id,
        x.Year,
        x.Status,
        x.IsOrphaned,
        x.MatchScore,
        x.AppItemId,
        x.AppItemDescription,
        x.AppItemPrice,
        x.ImplementationItemId,
        x.SipItemActivity,
        x.SipItemCost,
        x.AdminNote,
        x.DetectedAt,
        x.ReviewedAt,
        x.ReviewedBy
    );
}

// ─── Extension helper ─────────────────────────────────────────────────────────

file static class QueryableExtensions
{
    public static async Task<HashSet<T>> ToHashSetAsync<T>(
        this IQueryable<T> source,
        CancellationToken ct = default)
    {
        var list = await source.ToListAsync(ct);
        return list.ToHashSet();
    }
}