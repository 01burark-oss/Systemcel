using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services;

public sealed class DestekTalebiService : IDestekTalebiService
{
    private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
    private readonly IIsletmeService _isletmeService;
    private readonly IEntitlementGuard _entitlementGuard;
    private readonly ICurrentUserContext _currentUserContext;

    public DestekTalebiService(
        IDbContextFactory<CashTrackerDbContext> dbFactory,
        IIsletmeService isletmeService,
        IEntitlementGuard entitlementGuard,
        ICurrentUserContext currentUserContext)
    {
        _dbFactory = dbFactory;
        _isletmeService = isletmeService;
        _entitlementGuard = entitlementGuard;
        _currentUserContext = currentUserContext;
    }

    public async Task<DestekTalebiListeDto> GetMineAsync(CancellationToken ct = default)
    {
        var business = await RequireOwnedBusinessAsync();
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var rows = await db.DestekTalepleri.AsNoTracking()
            .Where(x => x.IsletmeId == business.Id)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync(ct);
        return new DestekTalebiListeDto
        {
            Talepler = rows.Select(x => BuildDto(x, business.Ad)).ToList()
        };
    }

    public async Task<DestekTalebiDto> CreateAsync(
        DestekTalebiOlusturRequest request,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        var normalized = ValidateAndNormalize(request, idempotencyKey);
        var business = await RequireOwnedBusinessAsync();
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var existing = await db.DestekTalepleri.AsNoTracking().SingleOrDefaultAsync(x =>
            x.IsletmeId == business.Id && x.OlusturmaAnahtari == normalized.Key, ct);
        if (existing is not null)
            return ReplayOrConflict(existing, business.Ad, normalized);

        var entitlement = await _entitlementGuard.GetAsync(business.Id, business.TenantTipi, ct);
        var now = DateTime.UtcNow;
        var row = new DestekTalebi
        {
            IsletmeId = business.Id,
            OlusturanKullaniciReferansi = _currentUserContext.GetCurrentUser()?.ProviderUserId ?? string.Empty,
            OlusturmaAnahtari = normalized.Key,
            Konu = normalized.Konu,
            Kategori = normalized.Kategori,
            Aciklama = normalized.Aciklama,
            Oncelik = entitlement.OncelikliDestekAktif ? DestekOncelikleri.Oncelikli : DestekOncelikleri.Standart,
            Durum = DestekTalebiDurumlari.Acik,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.DestekTalepleri.Add(row);
        try
        {
            await db.SaveChangesAsync(ct);
            return BuildDto(row, business.Ad);
        }
        catch (DbUpdateException)
        {
            db.Entry(row).State = EntityState.Detached;
            var concurrent = await db.DestekTalepleri.AsNoTracking().SingleOrDefaultAsync(x =>
                x.IsletmeId == business.Id && x.OlusturmaAnahtari == normalized.Key, ct);
            if (concurrent is null)
                throw;
            return ReplayOrConflict(concurrent, business.Ad, normalized);
        }
    }

    internal static DestekTalebiDto BuildDto(DestekTalebi row, string businessName) => new()
    {
        Id = row.Id,
        IsletmeId = row.IsletmeId,
        IsletmeAdi = businessName,
        Konu = row.Konu,
        Kategori = row.Kategori,
        Aciklama = row.Aciklama,
        Oncelik = row.Oncelik,
        Durum = row.Durum,
        YoneticiYaniti = row.YoneticiYaniti,
        CreatedAt = row.CreatedAt,
        UpdatedAt = row.UpdatedAt
    };

    private async Task<Isletme> RequireOwnedBusinessAsync()
    {
        var access = await _isletmeService.GetActiveAccessAsync();
        if (access.MuhasebeciMusteriBaglami)
            throw new UnauthorizedAccessException("Müşteri çalışma alanından destek taleplerine erişilemez.");
        return await _isletmeService.GetActiveAsync();
    }

    private static DestekTalebiDto ReplayOrConflict(DestekTalebi row, string businessName, NormalizedRequest request)
    {
        if (row.Konu != request.Konu || row.Kategori != request.Kategori || row.Aciklama != request.Aciklama)
            throw new InvalidOperationException("Idempotency-Key daha önce farklı bir destek talebi için kullanılmış.");
        return BuildDto(row, businessName);
    }

    private static NormalizedRequest ValidateAndNormalize(DestekTalebiOlusturRequest request, string idempotencyKey)
    {
        var key = (idempotencyKey ?? string.Empty).Trim();
        var konu = (request.Konu ?? string.Empty).Trim();
        var kategori = (request.Kategori ?? string.Empty).Trim();
        var aciklama = (request.Aciklama ?? string.Empty).Trim();
        if (key.Length is < 8 or > 100)
            throw new ArgumentException("Idempotency-Key 8-100 karakter olmalıdır.");
        if (konu.Length is < 3 or > 120)
            throw new ArgumentException("Konu 3-120 karakter olmalıdır.");
        if (!DestekKategorileri.TumKategoriler.Contains(kategori))
            throw new ArgumentException("Geçerli bir destek kategorisi seçin.");
        if (aciklama.Length is < 10 or > 4000)
            throw new ArgumentException("Açıklama 10-4000 karakter olmalıdır.");
        return new NormalizedRequest(key, konu, kategori, aciklama);
    }

    private sealed record NormalizedRequest(string Key, string Konu, string Kategori, string Aciklama);
}
