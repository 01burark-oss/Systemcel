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

    public async Task<IsletmeUyelikListeDto> GetMembershipsAsync(CancellationToken ct = default)
    {
        await using var access = await GetAccessAsync(ct);
        return await BuildListAsync(access.Db, access.BusinessId, access.ActorId, ct);
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

    public async Task<IsletmeUyelikListeDto> AcceptInviteAsync(string inviteCode, CancellationToken ct = default)
    {
        var normalizedCode = inviteCode?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalizedCode.Length < 16)
            throw new ArgumentException("Geçerli bir davet kodu gerekli.");

        var identity = _currentUser.GetCurrentUser() ?? throw new UnauthorizedAccessException("Daveti kabul etmek için oturum açmalısınız.");
        if (string.IsNullOrWhiteSpace(identity.Email))
            throw new InvalidOperationException("Hesabınızda doğrulanmış bir e-posta adresi bulunamadı.");
        if (!identity.EmailVerified)
            throw new UnauthorizedAccessException("Daveti kabul etmek için hesabınızdaki e-posta adresini doğrulayın.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        var actor = await RequireActorAsync(db, identity.ProviderUserId, ct);
        var membership = await db.IsletmeUyelikleri.SingleOrDefaultAsync(x => x.DavetKodu == normalizedCode, ct)
            ?? throw new KeyNotFoundException("Davet bulunamadı veya artık geçerli değil.");
        if (membership.Durum != "DavetBekliyor")
            throw new InvalidOperationException("Bu davet daha önce kullanılmış veya iptal edilmiş.");
        if (!string.Equals(membership.DavetEposta, identity.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Bu davet başka bir e-posta adresine gönderilmiş.");

        var previousMembership = await db.IsletmeUyelikleri.SingleOrDefaultAsync(x =>
            x.IsletmeId == membership.IsletmeId && x.KullaniciId == actor.Id && x.Id != membership.Id, ct);
        if (previousMembership?.Durum == "Aktif")
            throw new InvalidOperationException("Bu işletmede zaten aktif bir üyeliğiniz var.");

        var now = DateTime.UtcNow;
        if (previousMembership is not null)
        {
            previousMembership.Rol = membership.Rol;
            previousMembership.Durum = "Aktif";
            previousMembership.DavetEposta = membership.DavetEposta;
            previousMembership.DavetKodu = null;
            previousMembership.KabulAt = now;
            previousMembership.UpdatedAt = now;
            db.IsletmeUyelikleri.Remove(membership);
        }
        else
        {
            membership.KullaniciId = actor.Id;
            membership.Durum = "Aktif";
            membership.KabulAt = now;
            membership.DavetKodu = null;
            membership.UpdatedAt = now;
        }
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return await BuildListAsync(db, membership.IsletmeId, actor.Id, ct);
    }

    public async Task<IsletmeUyelikListeDto> UpdateRoleAsync(int membershipId, string role, CancellationToken ct = default)
    {
        var normalizedRole = NormalizeRole(role);
        await using var access = await GetOwnerAccessAsync(ct);
        var membership = await RequireMutableMembershipAsync(access.Db, access.BusinessId, membershipId, ct);
        membership.Rol = normalizedRole;
        membership.UpdatedAt = DateTime.UtcNow;
        await access.Db.SaveChangesAsync(ct);
        return await BuildListAsync(access.Db, access.BusinessId, access.ActorId, ct);
    }

    public async Task<IsletmeUyelikListeDto> RemoveAsync(int membershipId, CancellationToken ct = default)
    {
        await using var access = await GetOwnerAccessAsync(ct);
        var membership = await RequireMutableMembershipAsync(access.Db, access.BusinessId, membershipId, ct);
        membership.Durum = "Iptal";
        membership.DavetKodu = null;
        membership.UpdatedAt = DateTime.UtcNow;
        await access.Db.SaveChangesAsync(ct);
        return await BuildListAsync(access.Db, access.BusinessId, access.ActorId, ct);
    }

    public async Task<IsletmeUyelikListeDto> TransferOwnershipAsync(int membershipId, CancellationToken ct = default)
    {
        await using var access = await GetOwnerAccessAsync(ct);
        await using var tx = access.Db.Database.IsRelational()
            ? await access.Db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;
        var target = await RequireMutableMembershipAsync(access.Db, access.BusinessId, membershipId, ct);
        if (target.Durum != "Aktif" || !target.KullaniciId.HasValue)
            throw new InvalidOperationException("Sahiplik yalnız aktif bir ekip üyesine devredilebilir.");

        var currentOwner = await access.Db.IsletmeUyelikleri.SingleAsync(x =>
            x.IsletmeId == access.BusinessId && x.KullaniciId == access.ActorId && x.Durum == "Aktif" && x.Rol == "isletme_sahibi", ct);
        var business = await access.Db.Isletmeler.SingleAsync(x => x.Id == access.BusinessId, ct);
        var now = DateTime.UtcNow;
        currentOwner.Rol = "yonetici";
        currentOwner.UpdatedAt = now;
        target.Rol = "isletme_sahibi";
        target.UpdatedAt = now;
        business.SahipKullaniciId = target.KullaniciId;
        await access.Db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return await BuildListAsync(access.Db, access.BusinessId, access.ActorId, ct);
    }

    private static string NormalizeRole(string? role) => role?.Trim().ToLowerInvariant() switch
    {
        "yonetici" => "yonetici",
        "personel" or "" or null => "personel",
        _ => throw new ArgumentException("Davet rolu yonetici veya personel olmalidir.")
    };

    private async Task<MembershipAccess> GetAccessAsync(CancellationToken ct)
    {
        var identity = _currentUser.GetCurrentUser() ?? throw new UnauthorizedAccessException("Üyelikler için oturum açmalısınız.");
        var business = await _isletmeService.GetActiveAsync();
        var db = await _dbFactory.CreateDbContextAsync(ct);
        try
        {
            var actor = await RequireActorAsync(db, identity.ProviderUserId, ct);
            var isMember = await db.IsletmeUyelikleri.AnyAsync(x =>
                x.IsletmeId == business.Id && x.KullaniciId == actor.Id && x.Durum == "Aktif", ct);
            if (!isMember)
                throw new UnauthorizedAccessException("Bu işletmenin ekip bilgilerine erişemezsiniz.");
            return new MembershipAccess(db, business.Id, actor.Id);
        }
        catch
        {
            await db.DisposeAsync();
            throw;
        }
    }

    private async Task<MembershipAccess> GetOwnerAccessAsync(CancellationToken ct)
    {
        var access = await GetAccessAsync(ct);
        var owner = await access.Db.IsletmeUyelikleri.AnyAsync(x =>
            x.IsletmeId == access.BusinessId && x.KullaniciId == access.ActorId && x.Durum == "Aktif" && x.Rol == "isletme_sahibi", ct);
        if (owner)
            return access;
        await access.Db.DisposeAsync();
        throw new UnauthorizedAccessException("Yalnız işletme sahibi ekip üyelerini yönetebilir.");
    }

    private static async Task<Kullanici> RequireActorAsync(CashTrackerDbContext db, string providerUserId, CancellationToken ct) =>
        await db.Kullanicilar.SingleOrDefaultAsync(x => x.AuthProviderUserId == providerUserId, ct)
        ?? throw new UnauthorizedAccessException("Kullanıcı kaydı bulunamadı.");

    private static async Task<IsletmeUyelik> RequireMutableMembershipAsync(CashTrackerDbContext db, int businessId, int membershipId, CancellationToken ct)
    {
        var membership = await db.IsletmeUyelikleri.SingleOrDefaultAsync(x => x.Id == membershipId && x.IsletmeId == businessId, ct)
            ?? throw new KeyNotFoundException("Ekip üyesi bulunamadı.");
        if (membership.Rol == "isletme_sahibi")
            throw new InvalidOperationException("İşletme sahibi bu işlemle değiştirilemez.");
        return membership;
    }

    private static async Task<IsletmeUyelikListeDto> BuildListAsync(CashTrackerDbContext db, int businessId, int actorId, CancellationToken ct)
    {
        var business = await db.Isletmeler.AsNoTracking().SingleAsync(x => x.Id == businessId, ct);
        var isOwner = await db.IsletmeUyelikleri.AsNoTracking().AnyAsync(x =>
            x.IsletmeId == businessId && x.KullaniciId == actorId && x.Rol == "isletme_sahibi" && x.Durum == "Aktif", ct);
        var rows = await (
            from membership in db.IsletmeUyelikleri.AsNoTracking()
            join user in db.Kullanicilar.AsNoTracking() on membership.KullaniciId equals user.Id into users
            from user in users.DefaultIfEmpty()
            where membership.IsletmeId == businessId && membership.Durum != "Iptal"
            orderby membership.Rol == "isletme_sahibi" descending, membership.CreatedAt
            select new IsletmeUyelikDto
            {
                Id = membership.Id,
                KullaniciId = membership.KullaniciId,
                Eposta = user != null && user.Eposta != string.Empty ? user.Eposta : membership.DavetEposta,
                AdSoyad = user != null ? user.AdSoyad : string.Empty,
                Rol = membership.Rol,
                Durum = membership.Durum,
                DavetKodu = isOwner ? membership.DavetKodu ?? string.Empty : string.Empty,
                DavetAt = membership.DavetAt,
                KabulAt = membership.KabulAt
            }).ToListAsync(ct);
        return new IsletmeUyelikListeDto
        {
            SahibiMi = isOwner,
            IsletmeId = business.Id,
            IsletmeAdi = business.Ad,
            Uyelikler = rows
        };
    }

    private sealed record MembershipAccess(CashTrackerDbContext Db, int BusinessId, int ActorId) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

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
