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
    public sealed class SubscriptionEntitlementService : ISubscriptionEntitlementService
    {
        private static readonly IReadOnlyDictionary<string, int> IsletmePlanSirasi =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                [PlanKodlari.IsletmeUcretsiz] = 0,
                [PlanKodlari.IsletmeBaslangic] = 10,
                [PlanKodlari.IsletmeBuyume] = 20,
                [PlanKodlari.IsletmeIsletme] = 20,
                [PlanKodlari.IsletmeKurumsal] = 30
            };

        private static readonly IReadOnlyDictionary<string, int> MuhasebeciPlanSirasi =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                [PlanKodlari.MuhasebeciUcretsiz] = 0,
                [PlanKodlari.MuhasebeciSaltOkunur] = 0,
                [PlanKodlari.MuhasebeciStandart] = 10,
                [PlanKodlari.MuhasebeciPro] = 20
            };

        private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;

        public SubscriptionEntitlementService(IDbContextFactory<CashTrackerDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<SubscriptionEntitlementStatus> GetIsletmeEntitlementAsync(
            int isletmeId,
            DateTime? now = null,
            CancellationToken ct = default)
        {
            var current = now ?? DateTime.UtcNow;

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var abonelikler = await db.Abonelikler
                .AsNoTracking()
                .Where(x => x.IsletmeId == isletmeId)
                .ToListAsync(ct);

            var kendiUcretliPlani = abonelikler
                .Where(x => IsActiveSubscription(x, current))
                .Where(x => StringEquals(x.HesapTipi, HesapTipleri.Isletme))
                .Where(x => GetPlanRank(x.PlanKodu, IsletmePlanSirasi) > 0)
                .OrderByDescending(x => GetPlanRank(x.PlanKodu, IsletmePlanSirasi))
                .ThenByDescending(x => x.DonemBaslangicAt)
                .FirstOrDefault();

            if (kendiUcretliPlani != null)
            {
                return BuildIsletmeStatus(
                    isletmeId,
                    kendiUcretliPlani.PlanKodu,
                    EntitlementKaynaklari.KendiAboneligi,
                    kendiUcretliPlani.DonemBaslangicAt,
                    GetEffectiveEndAt(kendiUcretliPlani),
                    sponsorMuhasebeciIsletmeId: null,
                    paraBirimi: kendiUcretliPlani.ParaBirimi,
                    faturalamaDonemi: kendiUcretliPlani.FaturalamaDonemi,
                    donemTutari: kendiUcretliPlani.DonemTutari);
            }

            var denemeler = await db.IsletmeDenemeleri
                .AsNoTracking()
                .Where(x => x.IsletmeId == isletmeId)
                .ToListAsync(ct);

            var aktifDeneme = denemeler
                .Where(x => StringEquals(x.HesapTipi, HesapTipleri.Isletme))
                .Where(x => IsActiveTrial(x, current))
                .OrderByDescending(x => x.BaslangicAt)
                .FirstOrDefault();

            if (aktifDeneme != null)
            {
                return BuildIsletmeStatus(
                    isletmeId,
                    aktifDeneme.PlanKodu,
                    EntitlementKaynaklari.IsletmeDenemesi,
                    aktifDeneme.BaslangicAt,
                    aktifDeneme.BitisAt,
                    sponsorMuhasebeciIsletmeId: null,
                    faturalamaDonemi: aktifDeneme.FaturalamaDonemi);
            }

            var sponsor = await FindActiveSponsorAsync(db, isletmeId, current, ct);
            if (sponsor != null)
            {
                return BuildIsletmeStatus(
                    isletmeId,
                    PlanKodlari.IsletmeBaslangic,
                    EntitlementKaynaklari.MuhasebeciProSponsor,
                    sponsor.BaslangicAt,
                    sponsor.BitisAt,
                    sponsor.MuhasebeciIsletmeId);
            }

            return BuildIsletmeStatus(
                isletmeId,
                PlanKodlari.IsletmeUcretsiz,
                EntitlementKaynaklari.Ucretsiz,
                current,
                gecerliBitisAt: null,
                sponsorMuhasebeciIsletmeId: null);
        }

        public async Task<SubscriptionEntitlementStatus> GetMuhasebeciEntitlementAsync(
            int muhasebeciIsletmeId,
            DateTime? now = null,
            CancellationToken ct = default)
        {
            var current = now ?? DateTime.UtcNow;

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var abonelikler = await db.Abonelikler
                .AsNoTracking()
                .Where(x => x.IsletmeId == muhasebeciIsletmeId)
                .ToListAsync(ct);

            var aktifMusteriSayisi = (await db.MuhasebeciMusterileri
                    .AsNoTracking()
                    .Where(x => x.MuhasebeciIsletmeId == muhasebeciIsletmeId)
                    .ToListAsync(ct))
                .Count(x => IsActiveMuhasebeciMusteri(x, current));

            var aktifPlan = abonelikler
                .Where(x => IsActiveSubscription(x, current))
                .Where(x => StringEquals(x.HesapTipi, HesapTipleri.Muhasebeci))
                .Where(x => GetPlanRank(x.PlanKodu, MuhasebeciPlanSirasi) > 0)
                .OrderByDescending(x => GetPlanRank(x.PlanKodu, MuhasebeciPlanSirasi))
                .ThenByDescending(x => x.DonemBaslangicAt)
                .FirstOrDefault();

            var denemeler = await db.IsletmeDenemeleri
                .AsNoTracking()
                .Where(x => x.IsletmeId == muhasebeciIsletmeId && x.HesapTipi == HesapTipleri.Muhasebeci)
                .ToListAsync(ct);
            var aktifDeneme = denemeler
                .Where(x => IsActiveTrial(x, current))
                .OrderByDescending(x => x.BaslangicAt)
                .FirstOrDefault();

            var planKodu = aktifPlan?.PlanKodu
                ?? aktifDeneme?.PlanKodu
                ?? PlanKodlari.MuhasebeciSaltOkunur;
            var kaynak = aktifPlan is not null
                ? EntitlementKaynaklari.KendiAboneligi
                : aktifDeneme is not null
                    ? EntitlementKaynaklari.IsletmeDenemesi
                    : EntitlementKaynaklari.Ucretsiz;
            var saltOkunur = aktifPlan is null && aktifDeneme is null;
            var gecerliBaslangicAt = aktifPlan?.DonemBaslangicAt ?? aktifDeneme?.BaslangicAt ?? current;
            var gecerliBitisAt = aktifPlan is not null ? GetEffectiveEndAt(aktifPlan) : aktifDeneme?.BitisAt;
            var paraBirimi = aktifPlan?.ParaBirimi ?? "TRY";
            var ekMusteriKredisi = StringEquals(planKodu, PlanKodlari.MuhasebeciStandart)
                ? aktifPlan?.EkMusteriKredisi ?? aktifDeneme?.EkMusteriKredisi ?? 0
                : 0;
            var standartAylikTutar = SubscriptionPlanCatalog.CalculateMuhasebeciStandartAylikTutar(ekMusteriKredisi);
            var aylikTutar = StringEquals(planKodu, PlanKodlari.MuhasebeciStandart)
                ? standartAylikTutar
                : GetPlanDefinition(planKodu).AylikTutar;
            var selectedBillingPeriod = aktifPlan?.FaturalamaDonemi ?? aktifDeneme?.FaturalamaDonemi;
            var faturalamaDonemi = string.Equals(selectedBillingPeriod, "Yillik", StringComparison.OrdinalIgnoreCase) ? "Yillik" : "Aylik";
            var donemTutari = aktifPlan?.DonemTutari > 0
                ? aktifPlan.DonemTutari
                : StringEquals(faturalamaDonemi, "Yillik")
                    ? GetPlanDefinition(planKodu).YillikTutar
                    : aylikTutar;

            return BuildMuhasebeciStatus(
                muhasebeciIsletmeId,
                planKodu,
                gecerliBaslangicAt,
                gecerliBitisAt,
                aktifMusteriSayisi,
                ekMusteriKredisi,
                standartAylikTutar,
                SubscriptionPlanCatalog.ShouldRecommendMuhasebeciPro(ekMusteriKredisi)
                    && !StringEquals(planKodu, PlanKodlari.MuhasebeciPro),
                aylikTutar,
                faturalamaDonemi,
                donemTutari,
                paraBirimi,
                kaynak,
                saltOkunur);
        }

        private static async Task<MuhasebeciMusteri?> FindActiveSponsorAsync(
            CashTrackerDbContext db,
            int musteriIsletmeId,
            DateTime current,
            CancellationToken ct)
        {
            var iliskiler = await db.MuhasebeciMusterileri
                .AsNoTracking()
                .Where(x => x.MusteriIsletmeId == musteriIsletmeId)
                .ToListAsync(ct);

            var aktifIliskiler = iliskiler
                .Where(x => IsActiveMuhasebeciMusteri(x, current))
                .OrderByDescending(x => x.BaslangicAt)
                .ToList();

            if (aktifIliskiler.Count == 0)
                return null;

            var muhasebeciIds = aktifIliskiler
                .Select(x => x.MuhasebeciIsletmeId)
                .Distinct()
                .ToList();

            var proAbonelikler = await db.Abonelikler
                .AsNoTracking()
                .Where(x => muhasebeciIds.Contains(x.IsletmeId))
                .ToListAsync(ct);

            var proSponsorIds = proAbonelikler
                .Where(x => IsActiveSubscription(x, current))
                .Where(x => StringEquals(x.HesapTipi, HesapTipleri.Muhasebeci))
                .Where(x => StringEquals(x.PlanKodu, PlanKodlari.MuhasebeciPro))
                .Select(x => x.IsletmeId)
                .ToHashSet();

            return aktifIliskiler.FirstOrDefault(x => proSponsorIds.Contains(x.MuhasebeciIsletmeId));
        }

        private static SubscriptionEntitlementStatus BuildIsletmeStatus(
            int isletmeId,
            string planKodu,
            string kaynak,
            DateTime gecerliBaslangicAt,
            DateTime? gecerliBitisAt,
            int? sponsorMuhasebeciIsletmeId,
            string paraBirimi = "TRY",
            string faturalamaDonemi = "Aylik",
            decimal donemTutari = 0)
        {
            var plan = GetPlanDefinition(planKodu);
            var paidBusinessFeatures = !StringEquals(plan.Kod, PlanKodlari.IsletmeUcretsiz);

            return new SubscriptionEntitlementStatus
            {
                IsletmeId = isletmeId,
                HesapTipi = HesapTipleri.Isletme,
                PlanKodu = plan.Kod,
                PlanAdi = plan.Ad,
                Kaynak = kaynak,
                AylikTutar = plan.AylikTutar,
                YillikTutar = plan.YillikTutar,
                FaturalamaDonemi = faturalamaDonemi,
                DonemTutari = donemTutari > 0 ? donemTutari : StringEquals(faturalamaDonemi, "Yillik") ? plan.YillikTutar : plan.AylikTutar,
                ParaBirimi = paraBirimi,
                OcrAktif = paidBusinessFeatures,
                GibAktif = paidBusinessFeatures,
                TelegramAktif = paidBusinessFeatures,
                AiAktif = paidBusinessFeatures,
                AiMesajLimiti = plan.AiMesajLimiti,
                KullaniciLimiti = plan.KullaniciLimiti,
                FaturaLimiti = plan.FaturaLimiti,
                IsletmeLimiti = plan.IsletmeLimiti,
                GelirGiderIslemLimiti = plan.GelirGiderIslemLimiti,
                CariKartLimiti = plan.CariKartLimiti,
                UrunHizmetLimiti = plan.UrunHizmetLimiti,
                MusteriLimiti = plan.MusteriLimiti,
                BankaMutabakatiAktif = plan.BankaMutabakatiAktif,
                StokRaporAktif = plan.StokRaporAktif,
                MuhasebeciErisimiAktif = plan.MuhasebeciErisimiAktif,
                CokluSubeAktif = plan.CokluSubeAktif,
                CokluParaBirimiAktif = plan.CokluParaBirimiAktif,
                ApiErisimiAktif = plan.ApiErisimiAktif,
                OncelikliDestekAktif = plan.OncelikliDestekAktif,
                GelismisDisaAktarimAktif = paidBusinessFeatures,
                SponsorMuhasebeciIsletmeId = sponsorMuhasebeciIsletmeId,
                GecerliBaslangicAt = gecerliBaslangicAt,
                GecerliBitisAt = gecerliBitisAt
            };
        }

        private static SubscriptionEntitlementStatus BuildMuhasebeciStatus(
            int muhasebeciIsletmeId,
            string planKodu,
            DateTime gecerliBaslangicAt,
            DateTime? gecerliBitisAt,
            int aktifMusteriSayisi,
            int ekMusteriKredisi,
            decimal standartAylikTutar,
            bool proOnerilir,
            decimal aylikTutar,
            string faturalamaDonemi,
            decimal donemTutari,
            string paraBirimi,
            string kaynak,
            bool saltOkunur)
        {
            var plan = GetPlanDefinition(planKodu);
            var isPro = StringEquals(plan.Kod, PlanKodlari.MuhasebeciPro);
            var aiAktif = !saltOkunur && !StringEquals(plan.Kod, PlanKodlari.MuhasebeciUcretsiz);
            var musteriLimiti = StringEquals(plan.Kod, PlanKodlari.MuhasebeciStandart)
                ? SubscriptionPlanCatalog.MuhasebeciStandartDahilMusteriSayisi + ekMusteriKredisi
                : plan.MusteriLimiti;

            return new SubscriptionEntitlementStatus
            {
                IsletmeId = muhasebeciIsletmeId,
                HesapTipi = HesapTipleri.Muhasebeci,
                PlanKodu = plan.Kod,
                PlanAdi = plan.Ad,
                Kaynak = kaynak,
                AylikTutar = aylikTutar,
                YillikTutar = plan.YillikTutar,
                FaturalamaDonemi = faturalamaDonemi,
                DonemTutari = donemTutari,
                ParaBirimi = paraBirimi,
                AiAktif = aiAktif,
                AiMesajLimiti = plan.AiMesajLimiti,
                KullaniciLimiti = plan.KullaniciLimiti,
                MusteriLimiti = musteriLimiti,
                MuhasebeciPaneliAktif = true,
                SaltOkunur = saltOkunur,
                OneCikmaAktif = isPro,
                DonemOtomasyonuAktif = isPro,
                MusteriSaglikSkoruAktif = isPro,
                GecerliBaslangicAt = gecerliBaslangicAt,
                GecerliBitisAt = gecerliBitisAt,
                AktifMusteriSayisi = aktifMusteriSayisi,
                EkMusteriKredisi = ekMusteriKredisi,
                MuhasebeciStandartAylikTutar = standartAylikTutar,
                MuhasebeciProOnerilir = proOnerilir
            };
        }

        private static bool IsActiveSubscription(Abonelik abonelik, DateTime current)
        {
            var normalPeriodActive = abonelik.DonemBitisAt is null || abonelik.DonemBitisAt >= current;
            var gracePeriodActive = abonelik.ToleransBitisAt is not null && abonelik.ToleransBitisAt >= current;
            return IsActiveStatus(abonelik.Durum)
                && abonelik.DonemBaslangicAt <= current
                && (normalPeriodActive || gracePeriodActive)
                && (abonelik.DonemSonundaIptal || abonelik.IptalAt is null || abonelik.IptalAt > current);
        }

        private static DateTime? GetEffectiveEndAt(Abonelik abonelik)
        {
            if (abonelik.ToleransBitisAt is null)
                return abonelik.DonemBitisAt;
            if (abonelik.DonemBitisAt is null)
                return abonelik.ToleransBitisAt;
            return abonelik.ToleransBitisAt > abonelik.DonemBitisAt
                ? abonelik.ToleransBitisAt
                : abonelik.DonemBitisAt;
        }

        private static bool IsActiveTrial(IsletmeDeneme deneme, DateTime current)
        {
            return IsActiveStatus(deneme.Durum)
                && deneme.BaslangicAt <= current
                && deneme.BitisAt >= current;
        }

        private static bool IsActiveMuhasebeciMusteri(MuhasebeciMusteri iliski, DateTime current)
        {
            return IsActiveStatus(iliski.Durum)
                && iliski.BaslangicAt <= current
                && (iliski.BitisAt is null || iliski.BitisAt >= current);
        }

        private static bool IsActiveStatus(string value)
        {
            return StringEquals(value, "Aktif");
        }

        private static int GetPlanRank(string planKodu, IReadOnlyDictionary<string, int> planSirasi)
        {
            return planSirasi.TryGetValue(planKodu, out var rank) ? rank : -1;
        }

        private static SubscriptionPlanDefinition GetPlanDefinition(string planKodu)
        {
            return SubscriptionPlanCatalog.Plans.Single(x => StringEquals(x.Kod, planKodu));
        }

        private static bool StringEquals(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }
}
