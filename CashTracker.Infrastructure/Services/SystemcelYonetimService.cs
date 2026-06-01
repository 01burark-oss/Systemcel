using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services
{
    public sealed class SystemcelYonetimService : ISystemcelYonetimService
    {
        private const string AuthProvider = "clerk";
        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
        private readonly ICurrentUserContext _currentUserContext;
        private readonly SystemcelYonetimOptions _options;

        public SystemcelYonetimService(
            IDbContextFactory<CashTrackerDbContext> dbFactory,
            ICurrentUserContext currentUserContext,
            SystemcelYonetimOptions options)
        {
            _dbFactory = dbFactory;
            _currentUserContext = currentUserContext;
            _options = options;
        }

        public Task<bool> IsCurrentUserAdminAsync(CancellationToken ct = default)
        {
            var identity = _currentUserContext.GetCurrentUser();
            return Task.FromResult(IsAdmin(identity));
        }

        public async Task<MuhasebeciBasvuruListeDto> GetMuhasebeciBasvurulariAsync(string? durum = null, CancellationToken ct = default)
        {
            RequireAdmin();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var normalizedFilter = NormalizeFilter(durum);
            var users = await db.Kullanicilar.AsNoTracking()
                .Where(x =>
                    x.HesapTipi == HesapTipleri.Muhasebeci ||
                    x.Durum == KullaniciDurumlari.MuhasebeciOnayBekliyor ||
                    x.Durum == KullaniciDurumlari.MuhasebeciReddedildi)
                .OrderByDescending(x => x.UpdatedAt)
                .ThenByDescending(x => x.Id)
                .ToListAsync(ct);

            var counts = BuildCounts(users);
            if (!string.IsNullOrWhiteSpace(normalizedFilter))
                users = users.Where(x => string.Equals(x.Durum, normalizedFilter, StringComparison.OrdinalIgnoreCase)).ToList();

            var businesses = await LoadAccountantBusinessesAsync(db, users.Select(x => x.Id).ToList(), ct);
            var profiles = await LoadAccountantProfilesAsync(db, businesses.Values.Select(x => x.Id).ToList(), ct);

            return new MuhasebeciBasvuruListeDto
            {
                YoneticiMi = true,
                DurumFiltresi = normalizedFilter,
                BekleyenSayisi = counts.Pending,
                OnayliSayisi = counts.Approved,
                ReddedilenSayisi = counts.Rejected,
                Basvurular = users.Select(x => BuildDto(x, businesses, profiles)).ToList()
            };
        }

        public async Task<MuhasebeciBasvuruDto> ApproveMuhasebeciBasvurusuAsync(int kullaniciId, CancellationToken ct = default)
        {
            RequireAdmin();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var user = await FindAccountantApplicantAsync(db, kullaniciId, ct);
            var business = await EnsureAccountantWorkspaceAsync(db, user, ct);
            var profile = await db.MuhasebeciProfilleri.FirstOrDefaultAsync(x => x.MuhasebeciIsletmeId == business.Id, ct);
            if (!IsProfileComplete(profile))
                throw new InvalidOperationException("Muhasebeci profili tamamlanmadan başvuru onaylanamaz.");

            var now = DateTime.Now;
            user.HesapTipi = HesapTipleri.Muhasebeci;
            user.Durum = KullaniciDurumlari.Aktif;
            user.UpdatedAt = now;
            business.TenantTipi = HesapTipleri.Muhasebeci;
            business.UpdatedAt = now;
            profile!.Yayinda = true;
            profile.UpdatedAt = now;
            await EnsureActiveOwnerMembershipAsync(db, business, user, now, ct);

            await db.SaveChangesAsync(ct);

            var businesses = new Dictionary<int, Isletme> { [user.Id] = business };
            var profiles = new Dictionary<int, MuhasebeciProfil> { [business.Id] = profile! };
            return BuildDto(user, businesses, profiles);
        }

        private static async Task EnsureActiveOwnerMembershipAsync(
            CashTrackerDbContext db,
            Isletme business,
            Kullanici user,
            DateTime now,
            CancellationToken ct)
        {
            var membership = await db.IsletmeUyelikleri.FirstOrDefaultAsync(x =>
                x.IsletmeId == business.Id &&
                x.KullaniciId == user.Id, ct);

            if (membership == null)
            {
                db.IsletmeUyelikleri.Add(new IsletmeUyelik
                {
                    IsletmeId = business.Id,
                    KullaniciId = user.Id,
                    Rol = "isletme_sahibi",
                    Durum = "Aktif",
                    DavetEposta = user.Eposta,
                    KabulAt = now,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                return;
            }

            membership.Rol = string.IsNullOrWhiteSpace(membership.Rol) ? "isletme_sahibi" : membership.Rol;
            membership.Durum = "Aktif";
            membership.DavetEposta = string.IsNullOrWhiteSpace(membership.DavetEposta) ? user.Eposta : membership.DavetEposta;
            membership.KabulAt ??= now;
            membership.UpdatedAt = now;
        }

        public async Task<MuhasebeciBasvuruDto> RejectMuhasebeciBasvurusuAsync(int kullaniciId, MuhasebeciBasvuruRedRequest request, CancellationToken ct = default)
        {
            RequireAdmin();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var user = await FindAccountantApplicantAsync(db, kullaniciId, ct);
            var business = await FindPrimaryAccountantBusinessAsync(db, user.Id, ct);

            user.HesapTipi = HesapTipleri.Muhasebeci;
            user.Durum = KullaniciDurumlari.MuhasebeciReddedildi;
            user.UpdatedAt = DateTime.Now;

            if (business != null)
            {
                var profile = await db.MuhasebeciProfilleri.FirstOrDefaultAsync(x => x.MuhasebeciIsletmeId == business.Id, ct);
                if (profile != null)
                {
                    profile.Yayinda = false;
                    profile.UpdatedAt = DateTime.Now;
                }
            }

            await db.SaveChangesAsync(ct);

            var businesses = business == null
                ? new Dictionary<int, Isletme>()
                : new Dictionary<int, Isletme> { [user.Id] = business };
            var profiles = business == null
                ? new Dictionary<int, MuhasebeciProfil>()
                : await LoadAccountantProfilesAsync(db, new List<int> { business.Id }, ct);
            return BuildDto(user, businesses, profiles);
        }

        private bool IsAdmin(CurrentUserIdentity? identity)
        {
            if (identity == null)
                return false;

            var allowedUserIds = Split(_options.AdminClerkUserIds);
            var allowedEmails = Split(_options.AdminEmails);
            if (allowedUserIds.Count == 0 && allowedEmails.Count == 0)
                return false;

            if (allowedUserIds.Any(x => string.Equals(x, identity.ProviderUserId, StringComparison.Ordinal)))
                return true;

            return !string.IsNullOrWhiteSpace(identity.Email) &&
                allowedEmails.Any(x => string.Equals(x, identity.Email, StringComparison.OrdinalIgnoreCase));
        }

        private void RequireAdmin()
        {
            if (IsAdmin(_currentUserContext.GetCurrentUser()))
                return;

            throw new UnauthorizedAccessException(
                "Bu ekran için yönetici hesabı gerekir. SYSTEMCEL_ADMIN_CLERK_USER_IDS ile admin Clerk kullanıcı ID'si tanımlayın.");
        }

        private static IReadOnlyList<string> Split(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<string>();

            return value
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        private static string NormalizeFilter(string? value)
        {
            var normalized = value?.Trim().ToLowerInvariant();
            return normalized switch
            {
                "bekleyen" or "onaybekliyor" or "muhasebecionaybekliyor" => KullaniciDurumlari.MuhasebeciOnayBekliyor,
                "onayli" or "onaylı" or "aktif" => KullaniciDurumlari.Aktif,
                "red" or "reddedildi" or "muhasebecireddedildi" => KullaniciDurumlari.MuhasebeciReddedildi,
                _ => string.Empty
            };
        }

        private static (int Pending, int Approved, int Rejected) BuildCounts(IEnumerable<Kullanici> users)
        {
            var rows = users.ToList();
            return (
                rows.Count(x => x.Durum == KullaniciDurumlari.MuhasebeciOnayBekliyor),
                rows.Count(x => x.Durum == KullaniciDurumlari.Aktif),
                rows.Count(x => x.Durum == KullaniciDurumlari.MuhasebeciReddedildi));
        }

        private static async Task<Dictionary<int, Isletme>> LoadAccountantBusinessesAsync(
            CashTrackerDbContext db,
            List<int> userIds,
            CancellationToken ct)
        {
            if (userIds.Count == 0)
                return new Dictionary<int, Isletme>();

            var rows = await db.Isletmeler.AsNoTracking()
                .Where(x => x.SahipKullaniciId.HasValue && userIds.Contains(x.SahipKullaniciId.Value))
                .OrderByDescending(x => x.TenantTipi == HesapTipleri.Muhasebeci)
                .ThenByDescending(x => x.UpdatedAt)
                .ThenByDescending(x => x.Id)
                .ToListAsync(ct);

            return rows
                .GroupBy(x => x.SahipKullaniciId!.Value)
                .ToDictionary(x => x.Key, x => x.First());
        }

        private static async Task<Dictionary<int, MuhasebeciProfil>> LoadAccountantProfilesAsync(
            CashTrackerDbContext db,
            List<int> businessIds,
            CancellationToken ct)
        {
            if (businessIds.Count == 0)
                return new Dictionary<int, MuhasebeciProfil>();

            return await db.MuhasebeciProfilleri.AsNoTracking()
                .Where(x => businessIds.Contains(x.MuhasebeciIsletmeId))
                .ToDictionaryAsync(x => x.MuhasebeciIsletmeId, ct);
        }

        private static MuhasebeciBasvuruDto BuildDto(
            Kullanici user,
            IReadOnlyDictionary<int, Isletme> businesses,
            IReadOnlyDictionary<int, MuhasebeciProfil> profiles)
        {
            businesses.TryGetValue(user.Id, out var business);
            MuhasebeciProfil? profile = null;
            if (business != null)
                profiles.TryGetValue(business.Id, out profile);

            return new MuhasebeciBasvuruDto
            {
                KullaniciId = user.Id,
                ClerkUserId = user.AuthProviderUserId,
                Eposta = user.Eposta,
                AdSoyad = user.AdSoyad,
                Durum = user.Durum,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                SonGirisAt = user.SonGirisAt,
                IsletmeId = business?.Id,
                IsletmeAdi = business?.Ad ?? string.Empty,
                IsletmeTuru = business?.IsletmeTuru ?? string.Empty,
                Konum = business?.Konum ?? string.Empty,
                Telefon = profile?.Telefon ?? string.Empty,
                DeneyimYili = profile?.DeneyimYili ?? 0,
                ProfilResmiUrl = profile?.ProfilResmiUrl ?? string.Empty,
                UcretBilgisi = profile?.UcretBilgisi ?? string.Empty,
                Uzmanliklar = profile?.Uzmanliklar ?? string.Empty,
                MusteriTipleri = profile?.MusteriTipleri ?? string.Empty,
                KisaAciklama = profile?.KisaAciklama ?? string.Empty,
                ProfilTamam = IsProfileComplete(profile)
            };
        }

        private static bool IsProfileComplete(MuhasebeciProfil? profile)
        {
            return profile != null &&
                !string.IsNullOrWhiteSpace(profile.Telefon) &&
                !string.IsNullOrWhiteSpace(profile.ProfilResmiUrl) &&
                !string.IsNullOrWhiteSpace(profile.UcretBilgisi);
        }

        private static async Task<Kullanici> FindAccountantApplicantAsync(CashTrackerDbContext db, int kullaniciId, CancellationToken ct)
        {
            var user = await db.Kullanicilar.FirstOrDefaultAsync(x => x.Id == kullaniciId, ct)
                ?? throw new InvalidOperationException("Muhasebeci başvurusu bulunamadı.");

            if (user.HesapTipi != HesapTipleri.Muhasebeci &&
                user.Durum != KullaniciDurumlari.MuhasebeciOnayBekliyor &&
                user.Durum != KullaniciDurumlari.MuhasebeciReddedildi)
            {
                throw new InvalidOperationException("Bu kullanıcı muhasebeci başvurusu değil.");
            }

            return user;
        }

        private static async Task<Isletme?> FindPrimaryAccountantBusinessAsync(CashTrackerDbContext db, int userId, CancellationToken ct)
        {
            return await db.Isletmeler
                .Where(x => x.SahipKullaniciId == userId)
                .OrderByDescending(x => x.TenantTipi == HesapTipleri.Muhasebeci)
                .ThenByDescending(x => x.UpdatedAt)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync(ct);
        }

        private static async Task<Isletme> EnsureAccountantWorkspaceAsync(CashTrackerDbContext db, Kullanici user, CancellationToken ct)
        {
            var business = await FindPrimaryAccountantBusinessAsync(db, user.Id, ct);
            if (business != null)
                return business;

            var now = DateTime.Now;
            business = new Isletme
            {
                Ad = string.IsNullOrWhiteSpace(user.AdSoyad) ? "Muhasebeci Çalışma Alanı" : user.AdSoyad.Trim(),
                IsletmeTuru = "MuhasebeOfisi",
                Konum = string.Empty,
                KolayKurulumTamamlandi = true,
                TenantTipi = HesapTipleri.Muhasebeci,
                SahipKullaniciId = user.Id,
                IsAktif = false,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Isletmeler.Add(business);
            await db.SaveChangesAsync(ct);

            db.IsletmeUyelikleri.Add(new IsletmeUyelik
            {
                IsletmeId = business.Id,
                KullaniciId = user.Id,
                Rol = "isletme_sahibi",
                Durum = "Aktif",
                DavetEposta = user.Eposta,
                KabulAt = now,
                CreatedAt = now,
                UpdatedAt = now
            });

            return business;
        }
    }
}
