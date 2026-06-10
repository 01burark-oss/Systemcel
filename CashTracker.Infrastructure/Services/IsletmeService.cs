using System;
using System.Collections.Concurrent;
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
    public sealed class IsletmeService : IIsletmeService
    {
        private const string VarsayilanIsletmeAdi = "Mevcut İşletme";
        private const string AuthProvider = "clerk";
        private const string UserActiveBusinessKeyPrefix = "WebAktifIsletme:";
        private const string UserAccountantCustomerContextKeyPrefix = "WebMuhasebeciMusteriBaglami:";
        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
        private readonly ICurrentUserContext _currentUserContext;
        private readonly IAccountantApplicationNotifier? _accountantApplicationNotifier;
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> ProvisioningLocks = new(StringComparer.Ordinal);

        public IsletmeService(IDbContextFactory<CashTrackerDbContext> dbFactory)
            : this(dbFactory, AnonymousCurrentUserContext.Instance)
        {
        }

        public IsletmeService(
            IDbContextFactory<CashTrackerDbContext> dbFactory,
            ICurrentUserContext currentUserContext,
            IAccountantApplicationNotifier? accountantApplicationNotifier = null)
        {
            _dbFactory = dbFactory;
            _currentUserContext = currentUserContext;
            _accountantApplicationNotifier = accountantApplicationNotifier;
        }

        public async Task<List<Isletme>> GetAllAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var user = await TryEnsureCurrentUserAsync(db);
            if (user == null)
            {
                await EnsureLegacyActiveIsletmeAsync(db);
                return await db.Isletmeler
                    .AsNoTracking()
                    .OrderByDescending(x => x.IsAktif)
                    .ThenBy(x => x.Ad)
                    .ThenBy(x => x.Id)
                    .ToListAsync();
            }

            await EnsureUserActiveIsletmeAsync(db, user);
            var activeId = await GetUserActiveBusinessIdAsync(db, user.Id);
            var businessIds = await GetActiveMembershipBusinessIdsAsync(db, user.Id);
            var rows = await db.Isletmeler
                .AsNoTracking()
                .Where(x => businessIds.Contains(x.Id))
                .OrderBy(x => x.Ad)
                .ThenBy(x => x.Id)
                .ToListAsync();

            foreach (var row in rows)
                row.IsAktif = row.Id == activeId;

            return rows
                .OrderByDescending(x => x.IsAktif)
                .ThenBy(x => x.Ad)
                .ThenBy(x => x.Id)
                .ToList();
        }

        public async Task<Isletme?> GetByIdAsync(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var user = await TryEnsureCurrentUserAsync(db);
            if (user == null)
                return await db.Isletmeler.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

            if (!await IsUserBusinessMemberAsync(db, user.Id, id))
                return null;

            var activeId = await GetUserActiveBusinessIdAsync(db, user.Id);
            var row = await db.Isletmeler.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (row != null)
                row.IsAktif = row.Id == activeId;

            return row;
        }

        public async Task<Isletme> GetActiveAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var user = await TryEnsureCurrentUserAsync(db);
            var isletme = user == null
                ? await EnsureLegacyActiveIsletmeAsync(db)
                : (await TryGetActiveCustomerContextAsync(db, user))?.Customer
                    ?? await EnsureUserActiveIsletmeAsync(db, user);

            await EnsureDefaultKalemlerAsync(db, isletme.Id);
            isletme.IsAktif = true;
            return isletme;
        }

        public async Task<int> GetActiveIdAsync()
        {
            var isletme = await GetActiveAsync();
            return isletme.Id;
        }

        public async Task<int> CreateAsync(string ad, bool makeActive = false)
        {
            var normalizedName = NormalizeBusinessName(ad);

            await using var db = await _dbFactory.CreateDbContextAsync();
            var user = await TryEnsureCurrentUserAsync(db);
            if (user == null)
                return await CreateLegacyAsync(db, normalizedName, makeActive);

            await AdoptLegacyBusinessesForFirstUserAsync(db, user);
            var hasBusiness = await db.IsletmeUyelikleri.AnyAsync(x =>
                x.KullaniciId == user.Id &&
                x.Durum == "Aktif");

            var entity = new Isletme
            {
                Ad = normalizedName,
                IsletmeTuru = "Genel",
                Konum = string.Empty,
                KolayKurulumTamamlandi = false,
                TenantTipi = "Isletme",
                SahipKullaniciId = user.Id,
                IsAktif = !hasBusiness,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            db.Isletmeler.Add(entity);
            await db.SaveChangesAsync();

            db.IsletmeUyelikleri.Add(new IsletmeUyelik
            {
                IsletmeId = entity.Id,
                KullaniciId = user.Id,
                Rol = "isletme_sahibi",
                Durum = "Aktif",
                DavetEposta = user.Eposta,
                KabulAt = DateTime.Now,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
            await db.SaveChangesAsync();

            await EnsureDefaultKalemlerAsync(db, entity.Id);
            if (makeActive || !hasBusiness)
                await SetUserActiveBusinessAsync(db, user.Id, entity.Id);

            return entity.Id;
        }

        public async Task RenameAsync(int id, string ad)
        {
            var normalizedName = NormalizeBusinessName(ad);

            await using var db = await _dbFactory.CreateDbContextAsync();
            var user = await TryEnsureCurrentUserAsync(db);
            var isletme = user == null
                ? await db.Isletmeler.FirstOrDefaultAsync(x => x.Id == id)
                : await GetUserBusinessForEditAsync(db, user.Id, id);

            if (isletme == null)
                return;

            isletme.Ad = normalizedName;
            isletme.UpdatedAt = DateTime.Now;
            await db.SaveChangesAsync();
        }

        public async Task UpdateSetupAsync(int id, string ad, string isletmeTuru, string konum, bool tamamlandi, string? hesapTipi = null, bool? muhasebeciVarMi = null, MuhasebeciProfilKaydetRequest? muhasebeciProfil = null)
        {
            var normalizedName = NormalizeBusinessName(ad);
            var normalizedType = NormalizeSetupText(isletmeTuru, "Genel");
            var normalizedLocation = NormalizeSetupText(konum, string.Empty);
            var normalizedAccountType = NormalizeAccountType(hesapTipi);
            var accountantSetup = string.Equals(normalizedAccountType, HesapTipleri.Muhasebeci, StringComparison.OrdinalIgnoreCase);

            await using var db = await _dbFactory.CreateDbContextAsync();
            var user = await TryEnsureCurrentUserAsync(db);
            var isletme = user == null
                ? await db.Isletmeler.FirstOrDefaultAsync(x => x.Id == id)
                : await GetUserBusinessForEditAsync(db, user.Id, id);

            if (isletme == null)
                return;

            isletme.Ad = normalizedName;
            isletme.IsletmeTuru = normalizedType;
            isletme.Konum = normalizedLocation;
            isletme.KolayKurulumTamamlandi = tamamlandi;
            if (muhasebeciVarMi.HasValue)
                isletme.MuhasebeciVarMi = muhasebeciVarMi.Value;
            if (!string.IsNullOrWhiteSpace(normalizedAccountType))
                isletme.TenantTipi = normalizedAccountType;
            isletme.UpdatedAt = DateTime.Now;

            if (accountantSetup)
                await UpsertAccountantApplicationProfileAsync(db, isletme, muhasebeciProfil);

            if (user != null && !string.IsNullOrWhiteSpace(normalizedAccountType))
            {
                var previousStatus = user.Durum;
                var wasApprovedAccountant =
                    string.Equals(user.HesapTipi, HesapTipleri.Muhasebeci, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(user.Durum, KullaniciDurumlari.Aktif, StringComparison.OrdinalIgnoreCase);
                user.HesapTipi = normalizedAccountType;
                if (string.Equals(normalizedAccountType, HesapTipleri.Muhasebeci, StringComparison.OrdinalIgnoreCase))
                    user.Durum = wasApprovedAccountant ? KullaniciDurumlari.Aktif : KullaniciDurumlari.MuhasebeciOnayBekliyor;
                else
                    user.Durum = KullaniciDurumlari.Aktif;
                user.UpdatedAt = DateTime.Now;

                if (!wasApprovedAccountant &&
                    accountantSetup &&
                    !string.Equals(previousStatus, KullaniciDurumlari.MuhasebeciOnayBekliyor, StringComparison.OrdinalIgnoreCase))
                {
                    await NotifyAccountantApplicationAsync(user, isletme);
                }
            }

            await db.SaveChangesAsync();
        }

        private static async Task UpsertAccountantApplicationProfileAsync(CashTrackerDbContext db, Isletme isletme, MuhasebeciProfilKaydetRequest? request)
        {
            var now = DateTime.Now;
            var profile = await db.MuhasebeciProfilleri.FirstOrDefaultAsync(x => x.MuhasebeciIsletmeId == isletme.Id);

            var telefon = NormalizeRequiredProfileText(request?.Telefon ?? profile?.Telefon, "Telefon numarası");
            var profilResmiUrl = NormalizeRequiredProfileText(request?.ProfilResmiUrl ?? profile?.ProfilResmiUrl, "Profil resmi");
            var ucretBilgisi = NormalizeRequiredProfileText(request?.UcretBilgisi ?? profile?.UcretBilgisi, "Ücret bilgisi");
            var deneyimYili = request?.DeneyimYili ?? profile?.DeneyimYili ?? 0;
            if (deneyimYili < 0)
                throw new InvalidOperationException("Deneyim yılı negatif olamaz.");

            if (profile == null)
            {
                profile = new MuhasebeciProfil
                {
                    MuhasebeciIsletmeId = isletme.Id,
                    CreatedAt = now
                };
                db.MuhasebeciProfilleri.Add(profile);
            }

            profile.Yayinda = false;
            profile.Unvan = NormalizeSetupText(request?.Unvan, isletme.Ad);
            profile.Konum = NormalizeSetupText(request?.Konum, isletme.Konum);
            profile.Telefon = telefon;
            profile.DeneyimYili = deneyimYili;
            profile.ProfilResmiUrl = profilResmiUrl;
            profile.UcretBilgisi = ucretBilgisi;
            profile.Uzmanliklar = NormalizeSetupText(request?.Uzmanliklar, "Genel muhasebe");
            profile.MusteriTipleri = NormalizeSetupText(request?.MusteriTipleri, "KOBİ ve küçük işletmeler");
            profile.KisaAciklama = NormalizeSetupText(request?.KisaAciklama, "Gelir, gider, fatura ve dönem takibinde destek olur.");
            profile.UpdatedAt = now;
        }

        private static string NormalizeRequiredProfileText(string? value, string fieldName)
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
                throw new InvalidOperationException($"{fieldName} zorunludur.");
            return normalized;
        }

        private async Task NotifyAccountantApplicationAsync(Kullanici user, Isletme isletme)
        {
            if (_accountantApplicationNotifier == null)
                return;

            try
            {
                await _accountantApplicationNotifier.NotifyApplicationCreatedAsync(new AccountantApplicationNotification(
                    user.AdSoyad,
                    user.Eposta,
                    isletme.Ad,
                    isletme.IsletmeTuru,
                    isletme.Konum));
            }
            catch
            {
                // Telegram bildirimi kurulumun tamamlanmasını engellememeli.
            }
        }

        public async Task SetActiveAsync(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var user = await TryEnsureCurrentUserAsync(db);
            if (user == null)
            {
                var target = await db.Isletmeler.FirstOrDefaultAsync(x => x.Id == id);
                if (target == null)
                    return;

                var activeRows = await db.Isletmeler.Where(x => x.IsAktif && x.Id != id).ToListAsync();
                foreach (var row in activeRows)
                    row.IsAktif = false;

                target.IsAktif = true;
                target.UpdatedAt = DateTime.Now;
                await db.SaveChangesAsync();
                await EnsureDefaultKalemlerAsync(db, id);
                return;
            }

            if (!await IsUserBusinessMemberAsync(db, user.Id, id))
                return;

            await ClearUserAccountantCustomerContextAsync(db, user.Id);
            await SetUserActiveBusinessAsync(db, user.Id, id);
            await EnsureDefaultKalemlerAsync(db, id);
        }

        public async Task SetActiveCustomerContextAsync(int musteriIsletmeId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var user = await TryEnsureCurrentUserAsync(db);
            if (user == null)
                throw new InvalidOperationException("MÃ¼ÅŸteri baÄŸlamÄ± iÃ§in oturum gerekir.");

            var relation = await FindUsableAccountantRelationAsync(db, user.Id, musteriIsletmeId);
            if (relation == null)
                throw new InvalidOperationException("Bu mÃ¼ÅŸteri iÃ§in aktif muhasebeci baÄŸlantÄ±sÄ± bulunamadÄ±.");

            await SetUserAccountantCustomerContextAsync(db, user.Id, relation.MuhasebeciIsletmeId, musteriIsletmeId);
        }

        public async Task ClearActiveCustomerContextAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var user = await TryEnsureCurrentUserAsync(db);
            if (user == null)
                return;

            await ClearUserAccountantCustomerContextAsync(db, user.Id);
        }

        public async Task<ActiveBusinessAccess> GetActiveAccessAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var user = await TryEnsureCurrentUserAsync(db);
            if (user == null)
            {
                var active = await EnsureLegacyActiveIsletmeAsync(db);
                return new ActiveBusinessAccess
                {
                    IsletmeId = active.Id,
                    MuhasebeciMusteriBaglami = false,
                    YetkiSeviyesi = MuhasebeciYetkiSeviyeleri.TamIslem
                };
            }

            var context = await TryGetActiveCustomerContextAsync(db, user);
            if (context != null)
            {
                return new ActiveBusinessAccess
                {
                    IsletmeId = context.Customer.Id,
                    MuhasebeciMusteriBaglami = true,
                    MuhasebeciIsletmeId = context.Relation.MuhasebeciIsletmeId,
                    YetkiSeviyesi = context.Relation.YetkiSeviyesi
                };
            }

            var activeBusiness = await EnsureUserActiveIsletmeAsync(db, user);
            return new ActiveBusinessAccess
            {
                IsletmeId = activeBusiness.Id,
                MuhasebeciMusteriBaglami = false,
                YetkiSeviyesi = MuhasebeciYetkiSeviyeleri.TamIslem
            };
        }

        public async Task DeleteAsync(int id)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var user = await TryEnsureCurrentUserAsync(db);
            if (user == null)
            {
                await DeleteLegacyAsync(db, id);
                return;
            }

            var businessIds = await GetActiveMembershipBusinessIdsAsync(db, user.Id);
            if (!businessIds.Contains(id))
                return;

            if (businessIds.Count <= 1)
                throw new InvalidOperationException("En az bir işletme kalmalı.");

            var target = await db.Isletmeler.FirstOrDefaultAsync(x => x.Id == id);
            if (target == null)
                return;

            if (target.SahipKullaniciId.HasValue && target.SahipKullaniciId.Value != user.Id)
                throw new InvalidOperationException("Bu işletmeyi sadece sahibi silebilir.");

            var activeId = await GetUserActiveBusinessIdAsync(db, user.Id);
            var replacementId = businessIds
                .Where(x => x != id)
                .OrderBy(x => x)
                .First();

            RemoveBusinessGraph(db, target);
            await db.SaveChangesAsync();

            if (activeId == id || activeId == null)
            {
                await SetUserActiveBusinessAsync(db, user.Id, replacementId);
                await EnsureDefaultKalemlerAsync(db, replacementId);
            }
        }

        private async Task<int> CreateLegacyAsync(CashTrackerDbContext db, string normalizedName, bool makeActive)
        {
            var hasActive = await db.Isletmeler.AnyAsync(x => x.IsAktif);

            if (makeActive)
            {
                var activeRows = await db.Isletmeler.Where(x => x.IsAktif).ToListAsync();
                foreach (var row in activeRows)
                    row.IsAktif = false;
            }

            var entity = new Isletme
            {
                Ad = normalizedName,
                IsletmeTuru = "Genel",
                Konum = string.Empty,
                KolayKurulumTamamlandi = false,
                TenantTipi = "Isletme",
                IsAktif = makeActive || !hasActive,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            db.Isletmeler.Add(entity);
            await db.SaveChangesAsync();
            await EnsureDefaultKalemlerAsync(db, entity.Id);

            return entity.Id;
        }

        private async Task DeleteLegacyAsync(CashTrackerDbContext db, int id)
        {
            var target = await db.Isletmeler.FirstOrDefaultAsync(x => x.Id == id);
            if (target == null)
                return;

            var total = await db.Isletmeler.CountAsync();
            if (total <= 1)
                throw new InvalidOperationException("En az bir işletme kalmalı.");

            var wasActive = target.IsAktif;
            RemoveBusinessGraph(db, target);

            int? newActiveId = null;
            if (wasActive)
            {
                var newActive = await db.Isletmeler
                    .Where(x => x.Id != id)
                    .OrderBy(x => x.Id)
                    .FirstOrDefaultAsync();

                if (newActive != null)
                {
                    newActive.IsAktif = true;
                    newActiveId = newActive.Id;
                }
            }

            await db.SaveChangesAsync();

            if (newActiveId.HasValue)
                await EnsureDefaultKalemlerAsync(db, newActiveId.Value);
        }

        private async Task<Kullanici?> TryEnsureCurrentUserAsync(CashTrackerDbContext db)
        {
            var identity = _currentUserContext.GetCurrentUser();
            if (identity == null)
                return null;

            using var _ = await AcquireProvisioningLockAsync($"user:{identity.ProviderUserId}");
            var now = DateTime.Now;
            var user = await db.Kullanicilar.FirstOrDefaultAsync(x =>
                x.AuthProvider == AuthProvider &&
                x.AuthProviderUserId == identity.ProviderUserId);

            if (user == null)
            {
                user = new Kullanici
                {
                    AuthProvider = AuthProvider,
                    AuthProviderUserId = identity.ProviderUserId,
                    Eposta = NormalizeOptional(identity.Email),
                    AdSoyad = NormalizeOptional(identity.FullName),
                    HesapTipi = "Isletme",
                    Durum = "Aktif",
                    SonGirisAt = now,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.Kullanicilar.Add(user);
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    db.ChangeTracker.Clear();
                    user = await db.Kullanicilar.FirstOrDefaultAsync(x =>
                        x.AuthProvider == AuthProvider &&
                        x.AuthProviderUserId == identity.ProviderUserId);
                    if (user == null)
                        throw;
                }

                return user;
            }

            var changed = false;
            var email = NormalizeOptional(identity.Email);
            if (!string.IsNullOrWhiteSpace(email) && !string.Equals(user.Eposta, email, StringComparison.OrdinalIgnoreCase))
            {
                user.Eposta = email;
                changed = true;
            }

            var fullName = NormalizeOptional(identity.FullName);
            if (!string.IsNullOrWhiteSpace(fullName) && !string.Equals(user.AdSoyad, fullName, StringComparison.Ordinal))
            {
                user.AdSoyad = fullName;
                changed = true;
            }

            if (!user.SonGirisAt.HasValue || user.SonGirisAt.Value < now.AddMinutes(-5))
            {
                user.SonGirisAt = now;
                changed = true;
            }

            if (changed)
            {
                user.UpdatedAt = now;
                await db.SaveChangesAsync();
            }

            return user;
        }

        private async Task<Isletme> EnsureUserActiveIsletmeAsync(CashTrackerDbContext db, Kullanici user)
        {
            using var _ = await AcquireProvisioningLockAsync($"business:{user.Id}");
            await AdoptLegacyBusinessesForFirstUserAsync(db, user);

            var businessIds = await GetActiveMembershipBusinessIdsAsync(db, user.Id);
            if (businessIds.Count == 0)
            {
                var created = new Isletme
                {
                    Ad = VarsayilanIsletmeAdi,
                    IsletmeTuru = "Genel",
                    Konum = string.Empty,
                    KolayKurulumTamamlandi = false,
                    TenantTipi = "Isletme",
                    SahipKullaniciId = user.Id,
                    IsAktif = false,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                db.Isletmeler.Add(created);
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    db.ChangeTracker.Clear();
                    businessIds = await GetActiveMembershipBusinessIdsAsync(db, user.Id);
                    if (businessIds.Count > 0)
                        return await LoadUserActiveBusinessAsync(db, user.Id, businessIds);

                    throw;
                }

                db.IsletmeUyelikleri.Add(new IsletmeUyelik
                {
                    IsletmeId = created.Id,
                    KullaniciId = user.Id,
                    Rol = "isletme_sahibi",
                    Durum = "Aktif",
                    DavetEposta = user.Eposta,
                    KabulAt = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    db.ChangeTracker.Clear();
                    businessIds = await GetActiveMembershipBusinessIdsAsync(db, user.Id);
                    if (businessIds.Count > 0)
                        return await LoadUserActiveBusinessAsync(db, user.Id, businessIds);

                    throw;
                }

                await SetUserActiveBusinessAsync(db, user.Id, created.Id);
                created.IsAktif = true;
                return created;
            }

            return await LoadUserActiveBusinessAsync(db, user.Id, businessIds);
        }

        private static async Task<Isletme> EnsureLegacyActiveIsletmeAsync(CashTrackerDbContext db)
        {
            var active = await db.Isletmeler.FirstOrDefaultAsync(x => x.IsAktif);
            if (active != null)
                return active;

            var first = await db.Isletmeler.OrderBy(x => x.Id).FirstOrDefaultAsync();
            if (first != null)
            {
                first.IsAktif = true;
                await db.SaveChangesAsync();
                return first;
            }

            var created = new Isletme
            {
                Ad = VarsayilanIsletmeAdi,
                IsletmeTuru = "Genel",
                Konum = string.Empty,
                KolayKurulumTamamlandi = false,
                TenantTipi = "Isletme",
                IsAktif = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            db.Isletmeler.Add(created);
            await db.SaveChangesAsync();
            return created;
        }

        private static async Task<List<int>> GetActiveMembershipBusinessIdsAsync(CashTrackerDbContext db, int userId)
        {
            return await db.IsletmeUyelikleri
                .Where(x =>
                    x.KullaniciId == userId &&
                    x.Durum == "Aktif")
                .Select(x => x.IsletmeId)
                .Distinct()
                .ToListAsync();
        }

        private static async Task<bool> IsUserBusinessMemberAsync(CashTrackerDbContext db, int userId, int businessId)
        {
            return await db.IsletmeUyelikleri.AnyAsync(x =>
                x.KullaniciId == userId &&
                x.IsletmeId == businessId &&
                x.Durum == "Aktif");
        }

        private static async Task<Isletme?> GetUserBusinessForEditAsync(CashTrackerDbContext db, int userId, int businessId)
        {
            if (!await IsUserBusinessMemberAsync(db, userId, businessId))
                return null;

            return await db.Isletmeler.FirstOrDefaultAsync(x => x.Id == businessId);
        }

        private static async Task<int?> GetUserActiveBusinessIdAsync(CashTrackerDbContext db, int userId)
        {
            var key = BuildUserActiveBusinessKey(userId);
            var value = await db.AppSettings
                .AsNoTracking()
                .Where(x => x.Key == key)
                .Select(x => x.Value)
                .FirstOrDefaultAsync();

            return int.TryParse(value, out var id) ? id : null;
        }

        private static async Task SetUserActiveBusinessAsync(CashTrackerDbContext db, int userId, int businessId)
        {
            var key = BuildUserActiveBusinessKey(userId);
            var setting = await db.AppSettings.FirstOrDefaultAsync(x => x.Key == key);
            if (setting == null)
            {
                db.AppSettings.Add(new AppSetting
                {
                    Key = key,
                    Value = businessId.ToString(),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
            }
            else if (!string.Equals(setting.Value, businessId.ToString(), StringComparison.Ordinal))
            {
                setting.Value = businessId.ToString();
                setting.UpdatedAt = DateTime.Now;
            }
            else
            {
                return;
            }

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                db.ChangeTracker.Clear();
                setting = await db.AppSettings.FirstOrDefaultAsync(x => x.Key == key);
                if (setting == null)
                    throw;

                if (!string.Equals(setting.Value, businessId.ToString(), StringComparison.Ordinal))
                {
                    setting.Value = businessId.ToString();
                    setting.UpdatedAt = DateTime.Now;
                    await db.SaveChangesAsync();
                }
            }
        }

        private static string BuildUserActiveBusinessKey(int userId)
        {
            return $"{UserActiveBusinessKeyPrefix}{userId}";
        }

        private static async Task AdoptLegacyBusinessesForFirstUserAsync(CashTrackerDbContext db, Kullanici user)
        {
            var userHasMembership = await db.IsletmeUyelikleri.AnyAsync(x => x.KullaniciId == user.Id);
            if (userHasMembership)
                return;

            var tenantizedExists =
                await db.IsletmeUyelikleri.AnyAsync(x => x.KullaniciId != null) ||
                await db.Isletmeler.AnyAsync(x => x.SahipKullaniciId != null);
            if (tenantizedExists)
                return;

            var legacyBusinesses = await db.Isletmeler
                .Where(x => x.SahipKullaniciId == null)
                .OrderBy(x => x.Id)
                .ToListAsync();
            if (legacyBusinesses.Count == 0)
                return;

            foreach (var business in legacyBusinesses)
            {
                business.SahipKullaniciId = user.Id;
                business.UpdatedAt = DateTime.Now;
                db.IsletmeUyelikleri.Add(new IsletmeUyelik
                {
                    IsletmeId = business.Id,
                    KullaniciId = user.Id,
                    Rol = "isletme_sahibi",
                    Durum = "Aktif",
                    DavetEposta = user.Eposta,
                    KabulAt = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
            }

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                db.ChangeTracker.Clear();
                if (!await db.IsletmeUyelikleri.AnyAsync(x => x.KullaniciId == user.Id && x.Durum == "Aktif"))
                    throw;
            }

            var active = legacyBusinesses.FirstOrDefault(x => x.IsAktif) ?? legacyBusinesses[0];
            await SetUserActiveBusinessAsync(db, user.Id, active.Id);
        }

        private static void RemoveBusinessGraph(CashTrackerDbContext db, Isletme target)
        {
            var id = target.Id;
            db.Kasalar.RemoveRange(db.Kasalar.Where(x => x.IsletmeId == id));
            db.KalemTanimlari.RemoveRange(db.KalemTanimlari.Where(x => x.IsletmeId == id));
            db.CariHareketleri.RemoveRange(db.CariHareketleri.Where(x => x.IsletmeId == id));
            db.CariKartlari.RemoveRange(db.CariKartlari.Where(x => x.IsletmeId == id));
            db.StokHareketleri.RemoveRange(db.StokHareketleri.Where(x => x.IsletmeId == id));
            db.UrunHizmetleri.RemoveRange(db.UrunHizmetleri.Where(x => x.IsletmeId == id));
            db.FaturaSatirlari.RemoveRange(db.FaturaSatirlari.Where(x => x.IsletmeId == id));
            db.Faturalar.RemoveRange(db.Faturalar.Where(x => x.IsletmeId == id));
            db.TahsilatOdemeleri.RemoveRange(db.TahsilatOdemeleri.Where(x => x.IsletmeId == id));
            db.BelgeDosyalari.RemoveRange(db.BelgeDosyalari.Where(x => x.IsletmeId == id));
            db.GibPortalAyarlari.RemoveRange(db.GibPortalAyarlari.Where(x => x.IsletmeId == id));
            db.GibPortalIslemLoglari.RemoveRange(db.GibPortalIslemLoglari.Where(x => x.IsletmeId == id));
            db.Abonelikler.RemoveRange(db.Abonelikler.Where(x => x.IsletmeId == id));
            db.IsletmeDenemeleri.RemoveRange(db.IsletmeDenemeleri.Where(x => x.IsletmeId == id));
            db.IsletmeEntitlementlari.RemoveRange(db.IsletmeEntitlementlari.Where(x => x.IsletmeId == id));
            db.AiKullanimDonemleri.RemoveRange(db.AiKullanimDonemleri.Where(x => x.IsletmeId == id));
            db.MuhasebeciProfilleri.RemoveRange(db.MuhasebeciProfilleri.Where(x => x.MuhasebeciIsletmeId == id));
            db.MuhasebeciMusteriTalepleri.RemoveRange(db.MuhasebeciMusteriTalepleri.Where(x =>
                x.MuhasebeciIsletmeId == id ||
                x.MusteriIsletmeId == id ||
                x.TalepEdenIsletmeId == id));
            db.MuhasebeciMusterileri.RemoveRange(db.MuhasebeciMusterileri.Where(x =>
                x.MuhasebeciIsletmeId == id ||
                x.MusteriIsletmeId == id));
            db.IsletmeUyelikleri.RemoveRange(db.IsletmeUyelikleri.Where(x => x.IsletmeId == id));
            db.Isletmeler.Remove(target);
        }

        private static string NormalizeBusinessName(string value)
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
                throw new ArgumentException("İşletme adı boş olamaz.", nameof(value));
            return normalized;
        }

        private static async Task EnsureDefaultKalemlerAsync(CashTrackerDbContext db, int isletmeId)
        {
            using var _ = await AcquireProvisioningLockAsync($"categories:{isletmeId}");
            var gelirVar = await db.KalemTanimlari.AnyAsync(x => x.IsletmeId == isletmeId && x.Tip == "Gelir");
            var existingExpenseCategories = await db.KalemTanimlari
                .Where(x => x.IsletmeId == isletmeId && x.Tip == "Gider")
                .Select(x => x.Ad)
                .ToListAsync();
            var existingExpenseSet = new HashSet<string>(existingExpenseCategories, StringComparer.OrdinalIgnoreCase);
            var changed = false;

            if (!gelirVar)
            {
                db.KalemTanimlari.Add(new KalemTanimi
                {
                    IsletmeId = isletmeId,
                    Tip = "Gelir",
                    Ad = "Genel Gelir",
                    CreatedAt = DateTime.Now
                });
                changed = true;
            }

            foreach (var category in DefaultKalemCatalog.DefaultExpenseCategories)
            {
                if (existingExpenseSet.Contains(category))
                    continue;

                existingExpenseSet.Add(category);
                db.KalemTanimlari.Add(new KalemTanimi
                {
                    IsletmeId = isletmeId,
                    Tip = "Gider",
                    Ad = category,
                    CreatedAt = DateTime.Now
                });
                changed = true;
            }

            if (changed)
            {
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    db.ChangeTracker.Clear();
                    if (!await HasRequiredDefaultKalemlerAsync(db, isletmeId))
                        throw;
                }
            }
        }

        private static async Task<Isletme> LoadUserActiveBusinessAsync(
            CashTrackerDbContext db,
            int userId,
            List<int> businessIds)
        {
            var activeId = await GetUserActiveBusinessIdAsync(db, userId);
            Isletme? active = null;
            if (activeId.HasValue && businessIds.Contains(activeId.Value))
                active = await db.Isletmeler.FirstOrDefaultAsync(x => x.Id == activeId.Value);

            if (active == null)
            {
                active = await db.Isletmeler
                    .Where(x => businessIds.Contains(x.Id))
                    .OrderByDescending(x => x.IsAktif)
                    .ThenBy(x => x.Id)
                    .FirstAsync();

                await SetUserActiveBusinessAsync(db, userId, active.Id);
            }

            active.IsAktif = true;
            return active;
        }

        private static async Task<bool> HasRequiredDefaultKalemlerAsync(CashTrackerDbContext db, int isletmeId)
        {
            var rows = await db.KalemTanimlari
                .AsNoTracking()
                .Where(x => x.IsletmeId == isletmeId)
                .Select(x => new { x.Tip, x.Ad })
                .ToListAsync();
            var hasIncome = rows.Any(x => x.Tip == "Gelir");
            var expenseSet = rows
                .Where(x => x.Tip == "Gider")
                .Select(x => x.Ad)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return hasIncome && DefaultKalemCatalog.DefaultExpenseCategories.All(expenseSet.Contains);
        }

        private static async Task<IDisposable> AcquireProvisioningLockAsync(string key)
        {
            var semaphore = ProvisioningLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();
            return new ProvisioningLockLease(semaphore);
        }

        private sealed class ProvisioningLockLease : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;
            private bool _disposed;

            public ProvisioningLockLease(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                _semaphore.Release();
            }
        }

        private static string NormalizeSetupText(string? value, string fallback)
        {
            var normalized = value?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
        }

        private static string NormalizeAccountType(string? value)
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
                return string.Empty;

            return string.Equals(normalized, HesapTipleri.Muhasebeci, StringComparison.OrdinalIgnoreCase)
                ? HesapTipleri.Muhasebeci
                : HesapTipleri.Isletme;
        }

        private static string NormalizeOptional(string? value)
        {
            return value?.Trim() ?? string.Empty;
        }

        private static string BuildUserAccountantCustomerContextKey(int userId)
        {
            return $"{UserAccountantCustomerContextKeyPrefix}{userId}";
        }

        private static async Task SetUserAccountantCustomerContextAsync(
            CashTrackerDbContext db,
            int userId,
            int muhasebeciIsletmeId,
            int musteriIsletmeId)
        {
            var key = BuildUserAccountantCustomerContextKey(userId);
            var value = $"{muhasebeciIsletmeId}:{musteriIsletmeId}";
            var setting = await db.AppSettings.FirstOrDefaultAsync(x => x.Key == key);
            if (setting == null)
            {
                db.AppSettings.Add(new AppSetting
                {
                    Key = key,
                    Value = value,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
            }
            else
            {
                setting.Value = value;
                setting.UpdatedAt = DateTime.Now;
            }

            await db.SaveChangesAsync();
        }

        private static async Task ClearUserAccountantCustomerContextAsync(CashTrackerDbContext db, int userId)
        {
            var key = BuildUserAccountantCustomerContextKey(userId);
            var setting = await db.AppSettings.FirstOrDefaultAsync(x => x.Key == key);
            if (setting == null)
                return;

            db.AppSettings.Remove(setting);
            await db.SaveChangesAsync();
        }

        private static async Task<AccountantCustomerContext?> TryGetActiveCustomerContextAsync(
            CashTrackerDbContext db,
            Kullanici user)
        {
            var key = BuildUserAccountantCustomerContextKey(user.Id);
            var value = await db.AppSettings
                .AsNoTracking()
                .Where(x => x.Key == key)
                .Select(x => x.Value)
                .FirstOrDefaultAsync();

            if (!TryParseContextValue(value, out var muhasebeciIsletmeId, out var musteriIsletmeId))
                return null;

            var relation = await FindUsableAccountantRelationAsync(db, user.Id, musteriIsletmeId, muhasebeciIsletmeId);
            if (relation == null)
                return null;

            var customer = await db.Isletmeler.FirstOrDefaultAsync(x => x.Id == musteriIsletmeId);
            return customer == null ? null : new AccountantCustomerContext(customer, relation);
        }

        private static async Task<MuhasebeciMusteri?> FindUsableAccountantRelationAsync(
            CashTrackerDbContext db,
            int userId,
            int musteriIsletmeId,
            int? muhasebeciIsletmeId = null)
        {
            var now = DateTime.Now;
            var accountantBusinessIds = await db.IsletmeUyelikleri
                .Where(x => x.KullaniciId == userId && x.Durum == "Aktif")
                .Join(
                    db.Isletmeler.Where(x => x.TenantTipi == HesapTipleri.Muhasebeci),
                    membership => membership.IsletmeId,
                    business => business.Id,
                    (membership, business) => business.Id)
                .ToListAsync();

            if (muhasebeciIsletmeId.HasValue && !accountantBusinessIds.Contains(muhasebeciIsletmeId.Value))
                return null;

            return await db.MuhasebeciMusterileri
                .Where(x => x.MusteriIsletmeId == musteriIsletmeId)
                .Where(x => muhasebeciIsletmeId == null || x.MuhasebeciIsletmeId == muhasebeciIsletmeId.Value)
                .Where(x => accountantBusinessIds.Contains(x.MuhasebeciIsletmeId))
                .Where(x => x.Durum == "Aktif" && x.BaslangicAt <= now && (x.BitisAt == null || x.BitisAt >= now))
                .OrderByDescending(x => x.BaslangicAt)
                .FirstOrDefaultAsync();
        }

        private static bool TryParseContextValue(string? value, out int muhasebeciIsletmeId, out int musteriIsletmeId)
        {
            muhasebeciIsletmeId = 0;
            musteriIsletmeId = 0;
            var parts = (value ?? string.Empty).Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length == 2 &&
                int.TryParse(parts[0], out muhasebeciIsletmeId) &&
                int.TryParse(parts[1], out musteriIsletmeId);
        }

        private sealed record AccountantCustomerContext(Isletme Customer, MuhasebeciMusteri Relation);

        private sealed class AnonymousCurrentUserContext : ICurrentUserContext
        {
            public static readonly AnonymousCurrentUserContext Instance = new();

            public CurrentUserIdentity? GetCurrentUser()
            {
                return null;
            }
        }
    }
}
