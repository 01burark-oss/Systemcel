using System.Security.Cryptography;
using System.Text.Json;
using CashTracker.Core.Entities;
using CashTracker.Core.Import;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Systemcel.Api.Import;

internal sealed class DesktopImportCodeStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;

    public DesktopImportCodeStore(IDbContextFactory<CashTrackerDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<DesktopImportCodeRecord> CreateAsync(
        int targetIsletmeId,
        string requestedBy,
        CancellationToken ct = default)
    {
        requestedBy = RequireIdentity(requestedBy);
        for (var attempt = 0; attempt < 20; attempt++)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var now = DateTime.UtcNow;
            var entity = new DesktopImportCode
            {
                Code = $"MIG-{RandomNumberGenerator.GetInt32(0, 1_000_000):D6}",
                Status = DesktopImportCodeStatus.Active,
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddMinutes(30),
                TargetIsletmeId = targetIsletmeId,
                RequestedBy = requestedBy
            };
            db.DesktopImportKodlari.Add(entity);
            try
            {
                await db.SaveChangesAsync(ct);
                return ToRecord(entity);
            }
            catch (DbUpdateException) when (attempt < 19)
            {
                // Extremely rare random-code collision; retry with a fresh context.
            }
        }

        throw new InvalidOperationException("Aktarim kodu uretilemedi.");
    }

    public async Task<DesktopImportCodeRecord?> FindAsync(
        string code,
        string requestedBy,
        CancellationToken ct = default)
    {
        var normalized = NormalizeCode(code);
        requestedBy = RequireIdentity(requestedBy);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var entity = await db.DesktopImportKodlari
            .FirstOrDefaultAsync(x => x.Code == normalized && x.RequestedBy == requestedBy, ct);
        if (entity is null)
            return null;

        if (entity.Status == DesktopImportCodeStatus.Active && entity.ExpiresAtUtc <= DateTime.UtcNow)
        {
            entity.Status = DesktopImportCodeStatus.Expired;
            await db.SaveChangesAsync(ct);
        }
        return ToRecord(entity);
    }

    public async Task<DesktopImportCodeRecord> ClaimAsync(
        string code,
        string requestedBy,
        CancellationToken ct = default)
    {
        var normalized = NormalizeCode(code);
        requestedBy = RequireIdentity(requestedBy);
        var now = DateTime.UtcNow;
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var updated = await db.DesktopImportKodlari
            .Where(x => x.Code == normalized &&
                        x.RequestedBy == requestedBy &&
                        x.Status == DesktopImportCodeStatus.Active &&
                        x.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, DesktopImportCodeStatus.Processing)
                .SetProperty(x => x.ClaimedAtUtc, now), ct);
        if (updated == 1)
        {
            var claimed = await db.DesktopImportKodlari.AsNoTracking()
                .SingleAsync(x => x.Code == normalized && x.RequestedBy == requestedBy, ct);
            return ToRecord(claimed);
        }

        var existing = await db.DesktopImportKodlari.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == normalized && x.RequestedBy == requestedBy, ct);
        if (existing is null)
            throw new DesktopImportValidationException("Aktarim kodu bulunamadi veya bu kullaniciya ait degil.");
        if (existing.ExpiresAtUtc <= now)
            throw new DesktopImportValidationException("Aktarim kodunun suresi dolmus.");
        throw new DesktopImportValidationException($"Aktarim kodu aktif degil: {existing.Status}.");
    }

    public async Task MarkUsedAsync(
        DesktopImportCodeRecord record,
        string packageId,
        DesktopImportTotals importedTotals,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var now = DateTime.UtcNow;
        var totalsJson = JsonSerializer.Serialize(importedTotals, JsonOptions);
        var updated = await db.DesktopImportKodlari
            .Where(x => x.Id == record.Id &&
                        x.RequestedBy == record.RequestedBy &&
                        x.Status == DesktopImportCodeStatus.Processing)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, DesktopImportCodeStatus.Used)
                .SetProperty(x => x.UsedAtUtc, now)
                .SetProperty(x => x.PackageId, packageId)
                .SetProperty(x => x.ImportedTotalsJson, totalsJson), ct);
        if (updated != 1)
            throw new DesktopImportValidationException("Aktarim kodu tek kullanim sozlesmesini kaybetti.");
    }

    public async Task ReleaseClaimAsync(DesktopImportCodeRecord record, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var now = DateTime.UtcNow;
        await db.DesktopImportKodlari
            .Where(x => x.Id == record.Id &&
                        x.RequestedBy == record.RequestedBy &&
                        x.Status == DesktopImportCodeStatus.Processing)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, x => x.ExpiresAtUtc <= now
                    ? DesktopImportCodeStatus.Expired
                    : DesktopImportCodeStatus.Active)
                .SetProperty(x => x.ClaimedAtUtc, (DateTime?)null), ct);
    }

    private static DesktopImportCodeRecord ToRecord(DesktopImportCode entity)
    {
        DesktopImportTotals totals;
        try
        {
            totals = JsonSerializer.Deserialize<DesktopImportTotals>(entity.ImportedTotalsJson, JsonOptions) ?? new();
        }
        catch (JsonException)
        {
            totals = new DesktopImportTotals();
        }

        return new DesktopImportCodeRecord
        {
            Id = entity.Id,
            Code = entity.Code,
            Status = entity.Status,
            CreatedAtUtc = entity.CreatedAtUtc,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            ClaimedAtUtc = entity.ClaimedAtUtc,
            UsedAtUtc = entity.UsedAtUtc,
            TargetIsletmeId = entity.TargetIsletmeId,
            RequestedBy = entity.RequestedBy,
            PackageId = entity.PackageId,
            ImportedTotals = totals
        };
    }

    private static string RequireIdentity(string requestedBy)
    {
        var value = (requestedBy ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 200)
            throw new DesktopImportValidationException("Aktarim kodu icin gecerli kullanici kimligi gerekli.");
        return value;
    }

    private static string NormalizeCode(string code)
    {
        var value = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (value.Length is < 5 or > 32)
            throw new DesktopImportValidationException("Aktarim kodu bicimi gecersiz.");
        return value;
    }
}

internal static class DesktopImportCodeStatus
{
    public const string Active = "Active";
    public const string Processing = "Processing";
    public const string Used = "Used";
    public const string Expired = "Expired";
}

internal sealed class DesktopImportCodeRecord
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Status { get; set; } = DesktopImportCodeStatus.Active;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddMinutes(30);
    public DateTime? ClaimedAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public int? TargetIsletmeId { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public string PackageId { get; set; } = string.Empty;
    public DesktopImportTotals ImportedTotals { get; set; } = new();
}

internal sealed class DesktopImportValidationException : Exception
{
    public DesktopImportValidationException(string message) : base(message) { }
}
