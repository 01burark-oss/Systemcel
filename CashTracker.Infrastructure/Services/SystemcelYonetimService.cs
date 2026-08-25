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
using System.Text.Json;
using System.Data;

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
            return IsAdminAsync(ct);
        }

        public async Task<MuhasebeciBasvuruListeDto> GetMuhasebeciBasvurulariAsync(string? durum = null, CancellationToken ct = default)
        {
            await RequireAdminAsync(ct);
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
            await RequireAdminAsync(ct);
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
            await RequireAdminAsync(ct);
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

        public async Task<YonetimOdemeIncelemeDto> GetOdemeIncelemeAsync(
            string? durum = null,
            bool sadeceHatalar = false,
            int limit = 100,
            CancellationToken ct = default)
        {
            await RequireAdminAsync(ct);
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var normalized = durum?.Trim() ?? string.Empty;
            var safeLimit = Math.Clamp(limit, 1, 250);
            var paymentsQuery = db.OdemeIslemleri.AsNoTracking();

            var toplam = await paymentsQuery.CountAsync(ct);
            var basarili = await paymentsQuery.CountAsync(x => x.Durum == "Basarili" || x.Durum == "Tamamlandi", ct);
            var hatali = await paymentsQuery.CountAsync(x => x.HataKodu != "" || x.HataMesaji != "" || x.Durum == "Basarisiz", ct);
            var islenemeyenOlay = await db.OdemeOlaylari.AsNoTracking()
                .CountAsync(x => x.HataMesaji != "" || x.IslenmeDurumu == "Hata", ct);

            if (!string.IsNullOrWhiteSpace(normalized))
                paymentsQuery = paymentsQuery.Where(x => x.Durum == normalized);
            if (sadeceHatalar)
                paymentsQuery = paymentsQuery.Where(x => x.HataKodu != "" || x.HataMesaji != "" || x.Durum == "Basarisiz");

            var payments = await paymentsQuery
                .OrderByDescending(x => x.UpdatedAt)
                .ThenByDescending(x => x.Id)
                .Take(safeLimit)
                .ToListAsync(ct);
            var businessIds = payments.Select(x => x.IsletmeId).Distinct().ToList();
            var businesses = await db.Isletmeler.AsNoTracking()
                .Where(x => businessIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Ad, ct);
            var checkoutKeys = payments.Select(x => x.CheckoutAnahtari).Where(x => x != "").Distinct().ToList();
            var events = await db.OdemeOlaylari.AsNoTracking()
                .Where(x => checkoutKeys.Contains(x.CheckoutAnahtari))
                .OrderByDescending(x => x.AlindiAt)
                .ToListAsync(ct);
            var eventsByCheckout = events.GroupBy(x => x.CheckoutAnahtari).ToDictionary(x => x.Key, x => x.ToList());

            return new YonetimOdemeIncelemeDto
            {
                YoneticiMi = true,
                ToplamSayisi = toplam,
                BasariliSayisi = basarili,
                HataSayisi = hatali,
                IslenemeyenOlaySayisi = islenemeyenOlay,
                Islemler = payments.Select(payment =>
                {
                    eventsByCheckout.TryGetValue(payment.CheckoutAnahtari, out var paymentEvents);
                    return new YonetimOdemeIslemiDto
                    {
                        Id = payment.Id,
                        IsletmeId = payment.IsletmeId,
                        IsletmeAdi = businesses.GetValueOrDefault(payment.IsletmeId, $"Isletme #{payment.IsletmeId}"),
                        PlanKodu = payment.PlanKodu,
                        HesapTipi = payment.HesapTipi,
                        IslemTipi = payment.IslemTipi,
                        Durum = payment.Durum,
                        OdemeSaglayici = payment.OdemeSaglayici,
                        SaglayiciOturumReferansi = MaskReference(payment.SaglayiciOturumId),
                        SaglayiciIslemReferansi = MaskReference(payment.SaglayiciIslemId),
                        ToplamTutar = payment.ToplamTutar,
                        ParaBirimi = payment.ParaBirimi,
                        HataKodu = payment.HataKodu,
                        HataMesaji = payment.HataMesaji,
                        CreatedAt = payment.CreatedAt,
                        UpdatedAt = payment.UpdatedAt,
                        SonOlayAt = payment.SonOlayAt,
                        Olaylar = (paymentEvents ?? new List<OdemeOlayi>()).Select(olay => new YonetimOdemeOlayiDto
                        {
                            Id = olay.Id,
                            OlayId = MaskReference(olay.OlayId),
                            OlayTipi = olay.OlayTipi,
                            IslenmeDurumu = olay.IslenmeDurumu,
                            SaglayiciIslemReferansi = MaskReference(olay.SaglayiciIslemId),
                            PayloadHash = MaskHash(olay.PayloadHash),
                            HataMesaji = olay.HataMesaji,
                            SaglayiciAt = olay.SaglayiciAt,
                            AlindiAt = olay.AlindiAt,
                            IslendiAt = olay.IslendiAt
                        }).ToList()
                    };
                }).ToList()
            };
        }

        public async Task<MuhasebeciAktarimListeDto> GetMuhasebeciAktarimlariAsync(
            string aktarimDonemi,
            int? muhasebeciIsletmeId = null,
            CancellationToken ct = default)
        {
            await RequireAdminAsync(ct);
            var period = NormalizeTransferPeriod(aktarimDonemi);
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var query = db.MuhasebeciAktarimAlacaklari.AsNoTracking()
                .Where(x => x.AktarimDonemi == period || x.Durum == MuhasebeciAktarimDurumlari.Bekliyor);
            if (muhasebeciIsletmeId.HasValue)
                query = query.Where(x => x.MuhasebeciIsletmeId == muhasebeciIsletmeId.Value);

            var rows = (await query.OrderBy(x => x.MuhasebeciIsletmeId).ThenBy(x => x.Id).ToListAsync(ct))
                .Where(x => x.AktarimDonemi == period ||
                            (x.Durum == MuhasebeciAktarimDurumlari.Bekliyor &&
                             string.CompareOrdinal(x.AktarimDonemi, period) < 0))
                .ToList();
            var ids = rows.Select(x => x.MuhasebeciIsletmeId).Distinct().ToList();
            var names = await db.Isletmeler.AsNoTracking()
                .Where(x => ids.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Ad, ct);

            return new MuhasebeciAktarimListeDto
            {
                YoneticiMi = true,
                AktarimDonemi = period,
                Aktarimlar = rows
                    .GroupBy(x => new
                    {
                        x.MuhasebeciIsletmeId,
                        x.ParaBirimi,
                        x.Durum,
                        AktarimReferansi = x.Durum == MuhasebeciAktarimDurumlari.Aktarildi
                            ? x.AktarimReferansi
                            : string.Empty
                    })
                    .Select(group => BuildTransferSummary(
                        group.ToList(),
                        names.GetValueOrDefault(group.Key.MuhasebeciIsletmeId, $"Muhasebeci #{group.Key.MuhasebeciIsletmeId}"),
                        period))
                    .ToList()
            };
        }

        public async Task<MuhasebeciAktarimOzetDto> CompleteMuhasebeciAktarimiAsync(
            int muhasebeciIsletmeId,
            MuhasebeciAktarimTamamlaRequest request,
            CancellationToken ct = default)
        {
            await RequireAdminAsync(ct);
            if (muhasebeciIsletmeId <= 0)
                throw new ArgumentOutOfRangeException(nameof(muhasebeciIsletmeId));
            var period = NormalizeTransferPeriod(request.AktarimDonemi);
            var reference = (request.AktarimReferansi ?? string.Empty).Trim();
            if (reference.Length is < 6 or > 120)
                throw new ArgumentException("Aktarım referansı 6-120 karakter olmalıdır.");

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var rows = await db.MuhasebeciAktarimAlacaklari
                .Where(x => x.MuhasebeciIsletmeId == muhasebeciIsletmeId &&
                            (x.AktarimDonemi == period || x.Durum == MuhasebeciAktarimDurumlari.Bekliyor))
                .OrderBy(x => x.Id)
                .ToListAsync(ct);
            rows = rows.Where(x => x.AktarimDonemi == period ||
                                   (x.Durum == MuhasebeciAktarimDurumlari.Bekliyor &&
                                    string.CompareOrdinal(x.AktarimDonemi, period) < 0))
                .ToList();

            var pending = rows.Where(x => x.Durum == MuhasebeciAktarimDurumlari.Bekliyor).ToList();
            if (pending.Count == 0)
            {
                var completed = rows.Where(x =>
                    x.Durum == MuhasebeciAktarimDurumlari.Aktarildi &&
                    x.AktarimReferansi == reference).ToList();
                if (completed.Count > 0)
                {
                    var completedName = await db.Isletmeler.AsNoTracking()
                        .Where(x => x.Id == muhasebeciIsletmeId)
                        .Select(x => x.Ad)
                        .SingleOrDefaultAsync(ct) ?? $"Muhasebeci #{muhasebeciIsletmeId}";
                    await transaction.CommitAsync(ct);
                    return BuildTransferSummary(completed, completedName, period);
                }
                throw new InvalidOperationException("Aktarılacak muhasebeci bakiyesi bulunamadı.");
            }

            if (pending.Select(x => x.ParaBirimi).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 1)
                throw new InvalidOperationException("Farklı para birimleri ayrı aktarılmalıdır.");
            if (pending.Sum(x => x.AktarilacakTutar) <= 0m)
                throw new InvalidOperationException("Net bakiye pozitif değil; iade mahsubu sonraki hakedişe devredilecek.");
            if (await db.MuhasebeciAktarimAlacaklari.AsNoTracking().AnyAsync(x =>
                    x.AktarimReferansi == reference, ct))
                throw new InvalidOperationException("Aktarım referansı daha önce kullanılmış.");

            var now = DateTime.UtcNow;
            foreach (var row in pending)
            {
                row.Durum = MuhasebeciAktarimDurumlari.Aktarildi;
                // The monthly field represents the settlement batch after a carry-forward
                // is paid, so list and idempotent replay include every positive/negative row.
                row.AktarimDonemi = period;
                row.AktarimReferansi = reference;
                row.AktarildiAt = now;
                row.UpdatedAt = now;
            }
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            var name = await db.Isletmeler.AsNoTracking()
                .Where(x => x.Id == muhasebeciIsletmeId)
                .Select(x => x.Ad)
                .SingleOrDefaultAsync(ct) ?? $"Muhasebeci #{muhasebeciIsletmeId}";
            return BuildTransferSummary(pending, name, period);
        }

        private static MuhasebeciAktarimOzetDto BuildTransferSummary(
            IReadOnlyCollection<MuhasebeciAktarimAlacagi> rows,
            string accountantName,
            string? settlementPeriod = null)
        {
            var first = rows.First();
            return new MuhasebeciAktarimOzetDto
            {
                MuhasebeciIsletmeId = first.MuhasebeciIsletmeId,
                MuhasebeciAdi = accountantName,
                AktarimDonemi = settlementPeriod ?? first.AktarimDonemi,
                ParaBirimi = first.ParaBirimi,
                AlacakSayisi = rows.Count,
                TahsilEdilenTutar = rows.Sum(x => x.TahsilEdilenTutar),
                PlatformKomisyonTutari = rows.Sum(x => x.PlatformKomisyonTutari),
                AktarilacakTutar = rows.Sum(x => x.AktarilacakTutar),
                Durum = first.Durum,
                AktarimReferansi = first.Durum == MuhasebeciAktarimDurumlari.Aktarildi ? first.AktarimReferansi : string.Empty,
                AktarildiAt = rows.Max(x => x.AktarildiAt)
            };
        }

        private static string NormalizeTransferPeriod(string value)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length != 7 || normalized[4] != '-' ||
                !int.TryParse(normalized[..4], out var year) ||
                !int.TryParse(normalized[5..], out var month) ||
                year is < 2020 or > 2200 || month is < 1 or > 12)
                throw new ArgumentException("Aktarım dönemi YYYY-MM biçiminde olmalıdır.");
            return $"{year:0000}-{month:00}";
        }

        public async Task<DestekTalebiListeDto> GetDestekTalepleriAsync(CancellationToken ct = default)
        {
            await RequireAdminAsync(ct);
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var rows = await db.DestekTalepleri.AsNoTracking()
                .OrderBy(x => x.Oncelik == DestekOncelikleri.Oncelikli ? 0 : 1)
                .ThenBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .ToListAsync(ct);
            var ids = rows.Select(x => x.IsletmeId).Distinct().ToList();
            var names = await db.Isletmeler.AsNoTracking()
                .Where(x => ids.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Ad, ct);
            return new DestekTalebiListeDto
            {
                Talepler = rows.Select(x => DestekTalebiService.BuildDto(
                    x,
                    names.GetValueOrDefault(x.IsletmeId, $"İşletme #{x.IsletmeId}"))).ToList()
            };
        }

        public async Task<DestekTalebiDto> UpdateDestekTalebiAsync(
            int destekTalebiId,
            DestekTalebiGuncelleRequest request,
            CancellationToken ct = default)
        {
            await RequireAdminAsync(ct);
            if (destekTalebiId <= 0)
                throw new ArgumentOutOfRangeException(nameof(destekTalebiId));
            var status = (request.Durum ?? string.Empty).Trim();
            var reply = (request.YoneticiYaniti ?? string.Empty).Trim();
            if (!DestekTalebiDurumlari.TumDurumlar.Contains(status))
                throw new ArgumentException("Geçerli bir destek durumu seçin.");
            if (reply.Length > 1000)
                throw new ArgumentException("Yönetici yanıtı en fazla 1000 karakter olabilir.");

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var row = await db.DestekTalepleri.SingleOrDefaultAsync(x => x.Id == destekTalebiId, ct)
                ?? throw new InvalidOperationException("Destek talebi bulunamadı.");
            var businessName = await db.Isletmeler.AsNoTracking()
                .Where(x => x.Id == row.IsletmeId)
                .Select(x => x.Ad)
                .SingleOrDefaultAsync(ct) ?? $"İşletme #{row.IsletmeId}";
            if (row.Durum == status && row.YoneticiYaniti == reply)
                return DestekTalebiService.BuildDto(row, businessName);
            var before = new { row.Durum, row.YoneticiYaniti };
            var now = DateTime.UtcNow;
            row.Durum = status;
            row.YoneticiYaniti = reply;
            row.CozulduAt = status == DestekTalebiDurumlari.Cozuldu ? row.CozulduAt ?? now : null;
            row.UpdatedAt = now;
            db.YonetimDenetimKayitlari.Add(new YonetimDenetimKaydi
            {
                IsletmeId = row.IsletmeId,
                AktorProviderKullaniciId = _currentUserContext.GetCurrentUser()!.ProviderUserId,
                Islem = "DestekTalebiGuncelle",
                KaynakTuru = nameof(DestekTalebi),
                OncekiDeger = JsonSerializer.Serialize(before),
                YeniDeger = JsonSerializer.Serialize(new { row.Durum, row.YoneticiYaniti }),
                Gerekce = "Destek talebi durumu ve yanıtı güncellendi.",
                CreatedAt = now
            });
            await db.SaveChangesAsync(ct);
            return DestekTalebiService.BuildDto(row, businessName);
        }

        public async Task<EntitlementOverrideResult> ApplyEntitlementOverrideAsync(int isletmeId, EntitlementOverrideRequest request, CancellationToken ct = default)
        {
            await RequireAdminAsync(ct);
            if (string.IsNullOrWhiteSpace(request.Gerekce) || request.Gerekce.Trim().Length < 8)
                throw new ArgumentException("Manuel hak degisikligi icin en az 8 karakterlik gerekce zorunludur.");
            var requestedPlan = SubscriptionPlanCatalog.Plans.SingleOrDefault(x => string.Equals(x.Kod, request.PlanKodu, StringComparison.OrdinalIgnoreCase));
            if (requestedPlan is null)
                throw new ArgumentException("Gecersiz plan kodu.");
            if (request.KullaniciLimiti < 1 || request.MusteriLimiti < 0 || request.AiMesajLimiti < 0)
                throw new ArgumentException("Hak limitleri negatif olamaz; kullanici limiti en az 1 olmalidir.");

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var business = await db.Isletmeler.SingleOrDefaultAsync(x => x.Id == isletmeId, ct)
                ?? throw new InvalidOperationException("Isletme bulunamadi.");
            if (!string.Equals(requestedPlan.HesapTipi, business.TenantTipi, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Plan kodu isletmenin hesap tipiyle uyumlu degildir.");
            var row = await db.IsletmeEntitlementlari.SingleOrDefaultAsync(x => x.IsletmeId == isletmeId, ct);
            var before = row is null ? null : AuditValue(row);
            row ??= new IsletmeEntitlement { IsletmeId = isletmeId, GecerliBaslangicAt = DateTime.UtcNow };
            if (row.Id == 0) db.IsletmeEntitlementlari.Add(row);
            row.PlanKodu = request.PlanKodu.Trim();
            row.Kaynak = "YoneticiOverride";
            row.AiAktif = request.AiAktif;
            row.AiMesajLimiti = request.AiMesajLimiti;
            row.KullaniciLimiti = request.KullaniciLimiti;
            row.MusteriLimiti = request.MusteriLimiti;
            row.UpdatedAt = DateTime.UtcNow;
            var identity = _currentUserContext.GetCurrentUser()!;
            var audit = new YonetimDenetimKaydi
            {
                IsletmeId = isletmeId,
                AktorProviderKullaniciId = identity.ProviderUserId,
                Islem = "EntitlementOverride",
                KaynakTuru = nameof(IsletmeEntitlement),
                OncekiDeger = JsonSerializer.Serialize(before),
                YeniDeger = JsonSerializer.Serialize(AuditValue(row)),
                Gerekce = request.Gerekce.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            db.YonetimDenetimKayitlari.Add(audit);
            await db.SaveChangesAsync(ct);
            return new EntitlementOverrideResult { IsletmeId = isletmeId, DenetimKaydiId = audit.Id, PlanKodu = row.PlanKodu, KullaniciLimiti = row.KullaniciLimiti, MusteriLimiti = row.MusteriLimiti };
        }

        private static object AuditValue(IsletmeEntitlement row) => new { row.PlanKodu, row.Kaynak, row.AiAktif, row.AiMesajLimiti, row.KullaniciLimiti, row.MusteriLimiti, row.GecerliBaslangicAt, row.GecerliBitisAt };

        private static string MaskReference(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return value.Length <= 8 ? "***" : $"{value[..4]}...{value[^4..]}";
        }

        private static string MaskHash(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return value.Length <= 12 ? "***" : $"{value[..12]}...";
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

        private async Task<bool> IsAdminAsync(CancellationToken ct)
        {
            var identity = _currentUserContext.GetCurrentUser();
            if (IsAdmin(identity))
                return true;

            var allowedEmails = Split(_options.AdminEmails);
            if (identity == null || allowedEmails.Count == 0)
                return false;

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var registeredEmail = await db.Kullanicilar.AsNoTracking()
                .Where(x => x.AuthProvider == AuthProvider && x.AuthProviderUserId == identity.ProviderUserId)
                .Select(x => x.Eposta)
                .SingleOrDefaultAsync(ct);

            return !string.IsNullOrWhiteSpace(registeredEmail) &&
                allowedEmails.Any(x => string.Equals(x, registeredEmail, StringComparison.OrdinalIgnoreCase));
        }

        private async Task RequireAdminAsync(CancellationToken ct)
        {
            if (await IsAdminAsync(ct))
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
