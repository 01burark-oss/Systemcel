using System.Data;
using System.Security.Cryptography;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services;

public sealed class IsletmeUyelikService : IIsletmeUyelikService
{
    private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
    private readonly ICurrentUserContext _currentUser;
    private readonly IIsletmeService _isletmeService;
    private readonly IEntitlementGuard _entitlementGuard;

    public IsletmeUyelikService(IDbContextFactory<CashTrackerDbContext> dbFactory, ICurrentUserContext currentUser, IIsletmeService isletmeService, IEntitlementGuard entitlementGuard)
    {
        _dbFactory = dbFactory;
        _currentUser = currentUser;
        _isletmeService = isletmeService;
        _entitlementGuard = entitlementGuard;
    }

    public async Task<IsletmeUyelikDavetDto> CreateInviteAsync(IsletmeUyelikDavetRequest request, CancellationToken ct = default)
    {
        var email = request.Eposta.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) throw new ArgumentException("Gecerli bir davet e-postasi gerekli.");
        var role = NormalizeRole(request.Rol);
        var identity = _currentUser.GetCurrentUser() ?? throw new UnauthorizedAccessException("Davet icin oturum acmalisiniz.");
        var business = await _isletmeService.GetActiveAsync();
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        var actor = await db.Kullanicilar.SingleOrDefaultAsync(x => x.AuthProviderUserId == identity.ProviderUserId, ct)
            ?? throw new UnauthorizedAccessException("Kullanici kaydi bulunamadi.");
        var owner = await db.IsletmeUyelikleri.AnyAsync(x => x.IsletmeId == business.Id && x.KullaniciId == actor.Id && x.Durum == "Aktif" && x.Rol == "isletme_sahibi", ct);
        if (!owner) throw new UnauthorizedAccessException("Yalniz isletme sahibi kullanici davet edebilir.");

        var existing = await db.IsletmeUyelikleri.FirstOrDefaultAsync(x => x.IsletmeId == business.Id && x.DavetEposta == email && x.Durum != "Iptal", ct);
        if (existing is not null)
            return ToDto(existing, true);

        var entitlement = await _entitlementGuard.GetAsync(business.Id, business.TenantTipi, ct);
        _entitlementGuard.EnsureWritable(entitlement);
        var occupied = await db.IsletmeUyelikleri.CountAsync(x => x.IsletmeId == business.Id && (x.Durum == "Aktif" || x.Durum == "DavetBekliyor"), ct);
        _entitlementGuard.EnsureLimit(entitlement, EntitlementLimits.User, occupied);
        var now = DateTime.UtcNow;
        var entity = new IsletmeUyelik
        {
            IsletmeId = business.Id,
            Rol = role,
            Durum = "DavetBekliyor",
            DavetEposta = email,
            DavetKodu = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant(),
            DavetEdenKullaniciId = actor.Id,
            DavetAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.IsletmeUyelikleri.Add(entity);
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return ToDto(entity, false);
    }

    private static string NormalizeRole(string? role) => role?.Trim().ToLowerInvariant() switch
    {
        "yonetici" => "yonetici",
        "personel" or "" or null => "personel",
        _ => throw new ArgumentException("Davet rolu yonetici veya personel olmalidir.")
    };

    private static IsletmeUyelikDavetDto ToDto(IsletmeUyelik row, bool reused) => new()
    {
        Id = row.Id,
        IsletmeId = row.IsletmeId,
        Eposta = row.DavetEposta,
        Rol = row.Rol,
        Durum = row.Durum,
        DavetKodu = row.DavetKodu ?? string.Empty,
        DavetAt = row.DavetAt ?? row.CreatedAt,
        TekrarKullanildi = reused
    };
}
