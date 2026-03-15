using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using i3p_server.Models;

namespace i3p_server.Controllers;

// ─── Request / Response DTOs ──────────────────────────────────────────────────

public record LinkAppItemsRequest(int SipItemId, List<int> AppItemIds);
public record VerifyPhotoRequest(string? VerifiedBy);
public record AddAppItemRequest(
    string? ItemDescription,
    string? Specification,
    string? UnitOfMeasure,
    double? TotalQuantity,
    double? Price
);

public record ArAppItemDto(
    int      Id,
    string?  ArCode,
    string?  ItemDescription,
    string?  Specification,
    string?  UnitOfMeasure,
    double?  TotalQuantity,
    double?  Price,
    double?  TotalAmount,
    string?  PhotoPath,
    bool     IsPhotoVerified,
    DateTime? VerifiedAt,
    string?  VerifiedBy
);

public record ArDetailDto(
    string          ArCode,
    int             SipItemId,
    string?         Activity,
    string?         Kra,
    string?         Category,
    double?         EstimatedCost,
    bool            SipIsVerified,
    List<ArAppItemDto> AppItems,
    double          TotalAppCost,
    int             VerifiedCount,
    int             TotalCount
);

// ─── Controller ───────────────────────────────────────────────────────────────

[Route("api/[controller]")]
[ApiController]
public class ArController : ControllerBase
{
    private readonly AppDbContext      _db;
    private readonly IWebHostEnvironment _env;

    public ArController(AppDbContext db, IWebHostEnvironment env)
    {
        _db  = db;
        _env = env;
    }

    // ── GET /api/Ar/{arCode} ──────────────────────────────────────────────────
    [HttpGet("{arCode}")]
    public async Task<ActionResult<ArDetailDto>> GetDetail(string arCode)
    {
        var sip = await _db.ImplementationItems
            .FirstOrDefaultAsync(i => i.ArCode == arCode);

        if (sip is null)
            return NotFound($"No SIP item found with AR code '{arCode}'.");

        var apps = await _db.AppItems
            .Where(a => a.ArCode == arCode)
            .OrderBy(a => a.Id)
            .ToListAsync();

        return Ok(ToDto(arCode, sip, apps));
    }

    // ── POST /api/Ar/link ─────────────────────────────────────────────────────
    [HttpPost("link")]
    public async Task<IActionResult> Link([FromBody] LinkAppItemsRequest req)
    {
        var sip = await _db.ImplementationItems.FindAsync(req.SipItemId);
        if (sip is null) return NotFound("SIP item not found.");
        if (req.AppItemIds is not { Count: > 0 }) return BadRequest("Provide at least one AppItemId.");

        sip.ArCode ??= GenerateArCode(sip);

        var apps = await _db.AppItems.Where(a => req.AppItemIds.Contains(a.Id)).ToListAsync();
        foreach (var a in apps) a.ArCode = sip.ArCode;

        await RecomputeVerification(sip);
        await _db.SaveChangesAsync();

        return Ok(new { sip.ArCode, LinkedCount = apps.Count });
    }

    // ── POST /api/Ar/seed-fake-links/{sipItemId} ──────────────────────────────
    // DEV ONLY — generates 3 fake AppItems linked to a SIP row.
    [HttpPost("seed-fake-links/{sipItemId:int}")]
    public async Task<IActionResult> SeedFakeLinks(int sipItemId)
    {
        var sip = await _db.ImplementationItems
            .Include(i => i.SchoolImplementation)
            .FirstOrDefaultAsync(i => i.Id == sipItemId);

        if (sip is null) return NotFound("SIP item not found.");

        int year    = sip.SchoolImplementation?.Year ?? DateTime.Now.Year;
        var appPlan = await GetOrCreateAppPlan(year);

        sip.ArCode ??= GenerateArCode(sip);

        var rng   = new Random();
        string[] units = ["piece", "ream", "box", "unit", "pack"];
        string[] descs = ["Office Supplies", "Training Materials", "Janitorial Supplies",
                          "IT Equipment", "Medical Supplies"];

        var fakes = Enumerable.Range(1, 3).Select(i =>
        {
            double price = Math.Round(rng.NextDouble() * 500 + 500, 2);
            double qty   = rng.Next(1, 20);
            return new AppItem
            {
                AnnualProcurementPlanId = appPlan.Id,
                ArCode          = sip.ArCode,
                No              = $"FAKE-{i}",
                ItemDescription = descs[rng.Next(descs.Length)] + $" #{rng.Next(100, 999)}",
                Specification   = "Standard specification",
                UnitOfMeasure   = units[rng.Next(units.Length)],
                TotalQuantity   = qty,
                Price           = price,
                TotalAmount     = price * qty,
                IsPhotoVerified = false,
            };
        }).ToList();

        _db.AppItems.AddRange(fakes);
        sip.IsVerified = false; // nothing verified yet
        await _db.SaveChangesAsync();

        return Ok(new
        {
            sip.ArCode,
            SipItemId  = sip.Id,
            AppItemIds = fakes.Select(f => f.Id).ToList(),
            Message    = "3 fake AppItems created and linked.",
        });
    }

    // ── POST /api/Ar/add-item/{sipItemId} ─────────────────────────────────────
    // Creates a real AppItem manually and links it to a SIP row.
    [HttpPost("add-item/{sipItemId:int}")]
    public async Task<IActionResult> AddItem(int sipItemId, [FromBody] AddAppItemRequest req)
    {
        var sip = await _db.ImplementationItems
            .Include(i => i.SchoolImplementation)
            .FirstOrDefaultAsync(i => i.Id == sipItemId);

        if (sip is null) return NotFound("SIP item not found.");

        int year    = sip.SchoolImplementation?.Year ?? DateTime.Now.Year;
        var appPlan = await GetOrCreateAppPlan(year);

        sip.ArCode ??= GenerateArCode(sip);

        var item = new AppItem
        {
            AnnualProcurementPlanId = appPlan.Id,
            ArCode          = sip.ArCode,
            ItemDescription = req.ItemDescription,
            Specification   = req.Specification,
            UnitOfMeasure   = req.UnitOfMeasure,
            TotalQuantity   = req.TotalQuantity,
            Price           = req.Price,
            TotalAmount     = (req.TotalQuantity ?? 0) * (req.Price ?? 0),
            IsPhotoVerified = false,
        };

        _db.AppItems.Add(item);
        await RecomputeVerification(sip);
        await _db.SaveChangesAsync();

        return Ok(new { ItemId = item.Id, sip.ArCode });
    }

    // ── POST /api/Ar/photo/{appItemId} ────────────────────────────────────────
    // Upload a proof photo.
    [HttpPost("photo/{appItemId:int}")]
    public async Task<IActionResult> UploadPhoto(int appItemId, IFormFile photo)
    {
        if (photo is null || photo.Length == 0)
            return BadRequest("No file provided.");

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
        var ext     = Path.GetExtension(photo.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            return BadRequest("Only JPG, PNG, WEBP, or PDF files are accepted.");

        var item = await _db.AppItems.FindAsync(appItemId);
        if (item is null) return NotFound();

        var folder   = Path.Combine(_env.WebRootPath, "uploads", "app-items", appItemId.ToString());
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(folder, fileName);

        await using (var stream = System.IO.File.Create(filePath))
            await photo.CopyToAsync(stream);

        item.PhotoPath = $"uploads/app-items/{appItemId}/{fileName}";
        await _db.SaveChangesAsync();

        return Ok(new { item.PhotoPath, Message = "Uploaded. Awaiting admin verification." });
    }

    // ── POST /api/Ar/verify-photo/{appItemId} ─────────────────────────────────
    // Admin confirms a photo — recomputes parent SIP row's IsVerified.
    [HttpPost("verify-photo/{appItemId:int}")]
    public async Task<IActionResult> VerifyPhoto(int appItemId, [FromBody] VerifyPhotoRequest req)
    {
        var item = await _db.AppItems.FindAsync(appItemId);
        if (item is null) return NotFound();

        // if (string.IsNullOrWhiteSpace(item.PhotoPath))
        //     return BadRequest("No photo uploaded yet.");

        item.IsPhotoVerified = true;
        item.VerifiedAt      = DateTime.UtcNow;
        item.VerifiedBy      = req.VerifiedBy;

        if (!string.IsNullOrWhiteSpace(item.ArCode))
        {
            var sip = await _db.ImplementationItems
                .FirstOrDefaultAsync(i => i.ArCode == item.ArCode);
            if (sip is not null)
                await RecomputeVerification(sip, overrideItemId: appItemId, overrideValue: true);
        }

        await _db.SaveChangesAsync();
        return Ok(new { Message = "Photo verified." });
    }

    // ── DELETE /api/Ar/unlink/{appItemId} ─────────────────────────────────────
    [HttpDelete("unlink/{appItemId:int}")]
    public async Task<IActionResult> Unlink(int appItemId)
    {
        var item = await _db.AppItems.FindAsync(appItemId);
        if (item is null) return NotFound();

        var oldCode = item.ArCode;
        item.ArCode          = null;
        item.IsPhotoVerified = false;
        item.VerifiedAt      = null;
        item.VerifiedBy      = null;

        if (!string.IsNullOrWhiteSpace(oldCode))
        {
            var sip = await _db.ImplementationItems
                .FirstOrDefaultAsync(i => i.ArCode == oldCode);
            if (sip is not null)
                await RecomputeVerification(sip, excludeItemId: appItemId);
        }

        await _db.SaveChangesAsync();
        return Ok(new { Message = "AppItem unlinked." });
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private static string GenerateArCode(ImplementationItem sip)
    {
        int year = DateTime.TryParse(sip.Date, out var dt) ? dt.Year : DateTime.Now.Year;
        var hex  = Convert.ToHexString(Guid.NewGuid().ToByteArray())[..6];
        return $"AR-{year}-{hex}";
    }

    private async Task RecomputeVerification(
        ImplementationItem sip,
        int? overrideItemId = null,
        bool overrideValue  = false,
        int? excludeItemId  = null)
    {
        var siblings = await _db.AppItems
            .Where(a => a.ArCode == sip.ArCode)
            .ToListAsync();

        // Apply in-memory overrides before checking
        if (overrideItemId.HasValue)
        {
            var t = siblings.FirstOrDefault(a => a.Id == overrideItemId.Value);
            if (t is not null) t.IsPhotoVerified = overrideValue;
        }

        var active = excludeItemId.HasValue
            ? siblings.Where(a => a.Id != excludeItemId.Value).ToList()
            : siblings;

        sip.IsVerified = active.Count > 0 && active.All(a => a.IsPhotoVerified);
    }

    private async Task<AnnualProcurementPlan> GetOrCreateAppPlan(int year)
    {
        var plan = await _db.AnnualProcurementPlan.FirstOrDefaultAsync(p => p.Year == year);
        if (plan is not null) return plan;

        plan = new AnnualProcurementPlan
        {
            Year     = year,
            FileName = $"auto-{year}.xlsx",
        };
        _db.AnnualProcurementPlan.Add(plan);
        await _db.SaveChangesAsync();
        return plan;
    }

    private static ArDetailDto ToDto(string arCode, ImplementationItem sip, List<AppItem> apps)
    {
        var appDtos = apps.Select(a => new ArAppItemDto(
            a.Id, a.ArCode, a.ItemDescription, a.Specification, a.UnitOfMeasure,
            a.TotalQuantity, a.Price, a.TotalAmount, a.PhotoPath,
            a.IsPhotoVerified, a.VerifiedAt, a.VerifiedBy
        )).ToList();

        return new ArDetailDto(
            arCode,
            sip.Id,
            sip.Activity,
            sip.Kra,
            sip.ExpenditureType,
            sip.EstimatedCost,
            sip.IsVerified,
            appDtos,
            TotalAppCost:   apps.Sum(a => a.TotalAmount  ?? 0),
            VerifiedCount:  apps.Count(a => a.IsPhotoVerified),
            TotalCount:     apps.Count
        );
    }
}