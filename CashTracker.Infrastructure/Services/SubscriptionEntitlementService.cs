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
                [PlanKodlari.IsletmeIsletme] = 20
            };

        private static readonly IReadOnlyDictionary<string, int> MuhasebeciPlanSirasi =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                [PlanKodlari.MuhasebeciUcretsiz] = 0,
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
            var current = now ?? DateTime.Now;

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
                    kendiUcretliPlani.DonemBitisAt,
                    sponsorMuhasebeciIsletmeId: null,
                    paraBirimi: kendiUcretliPlani.ParaBirimi);
            }

            var denemeler = await db.IsletmeDenemeleri
                .AsNoTracking()
                .Where(x => x.IsletmeId == isletmeId)
                .ToListAsync(ct);

            var aktifDeneme = denemeler
                .Where(x => IsActiveTrial(x, current))
                .OrderByDescending(x => x.BaslangicAt)
                .FirstOrDefault();

            if (aktifDeneme != null)
            {
                return BuildIsletmeStatus(
                    isletmeId,
                    PlanKodlari.IsletmeBaslangic,
                    EntitlementKaynaklari.IsletmeDenemesi,
                    aktifDeneme.BaslangicAt,
                    aktifDeneme.BitisAt,
                    sponsorMuhasebeciIsletmeId: null);
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
            var current = now ?? DateTime.Now;

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
                .Where(x => GetPlanRank(x.PlanKodu, MuhasebeciPlanSirasi) >= 0)
                .OrderByDescending(x => GetPlanRank(x.PlanKodu, MuhasebeciPlanSirasi))
                .ThenByDescending(x => x.DonemBaslangicAt)
                .FirstOrDefault();

            var planKodu = aktifPlan?.PlanKodu ?? PlanKodlari.MuhasebeciUcretsiz;
            var gecerliBaslangicAt = aktifPlan?.DonemBaslangicAt ?? current;
            var gecerliBitisAt = aktifPlan?.DonemBitisAt;
            var paraBirimi = aktifPlan?.ParaBirimi ?? "TRY";
            var standartAylikTutar = SubscriptionPlanCatalog.CalculateMuhasebeciStandartAylikTutar(aktifMusteriSayisi);
            var aylikTutar = StringEquals(planKodu, PlanKodlari.MuhasebeciStandart)
                ? standartAylikTutar
                : GetPlanDefinition(planKodu).AylikTutar;

            return BuildMuhasebeciStatus(
                muhasebeciIsletmeId,
                planKodu,
                gecerliBaslangicAt,
                gecerliBitisAt,
                aktifMusteriSayisi,
                standartAylikTutar,
                SubscriptionPlanCatalog.ShouldRecommendMuhasebeciPro(aktifMusteriSayisi)
                    && !StringEquals(planKodu, PlanKodlari.MuhasebeciPro),
                aylikTutar,
                paraBirimi);
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
            string paraBirimi = "TRY")
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
                ParaBirimi = paraBirimi,
                OcrAktif = paidBusinessFeatures,
                GibAktif = paidBusinessFeatures,
                TelegramAktif = paidBusinessFeatures,
                AiAktif = paidBusinessFeatures,
                AiMesajLimiti = plan.AiMesajLimiti,
                KullaniciLimiti = plan.KullaniciLimiti,
                MusteriLimiti = plan.MusteriLimiti,
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
            decimal standartAylikTutar,
            bool proOnerilir,
            decimal aylikTutar,
            string paraBirimi)
        {
            var plan = GetPlanDefinition(planKodu);
            var isPro = StringEquals(plan.Kod, PlanKodlari.MuhasebeciPro);
            var aiAktif = !StringEquals(plan.Kod, PlanKodlari.MuhasebeciUcretsiz);

            return new SubscriptionEntitlementStatus
            {
                IsletmeId = muhasebeciIsletmeId,
                HesapTipi = HesapTipleri.Muhasebeci,
                PlanKodu = plan.Kod,
                PlanAdi = plan.Ad,
                Kaynak = StringEquals(plan.Kod, PlanKodlari.MuhasebeciUcretsiz)
                    ? EntitlementKaynaklari.Ucretsiz
                    : EntitlementKaynaklari.KendiAboneligi,
                AylikTutar = aylikTutar,
                ParaBirimi = paraBirimi,
                AiAktif = aiAktif,
                AiMesajLimiti = plan.AiMesajLimiti,
                KullaniciLimiti = plan.KullaniciLimiti,
                MusteriLimiti = plan.MusteriLimiti,
                MuhasebeciPaneliAktif = true,
                OneCikmaAktif = isPro,
                DonemOtomasyonuAktif = isPro,
                MusteriSaglikSkoruAktif = isPro,
                GecerliBaslangicAt = gecerliBaslangicAt,
                GecerliBitisAt = gecerliBitisAt,
                AktifMusteriSayisi = aktifMusteriSayisi,
                MuhasebeciStandartAylikTutar = standartAylikTutar,
                MuhasebeciProOnerilir = proOnerilir
            };
        }

        private static bool IsActiveSubscription(Abonelik abonelik, DateTime current)
        {
            return IsActiveStatus(abonelik.Durum)
                && abonelik.DonemBaslangicAt <= current
                && (abonelik.DonemBitisAt is null || abonelik.DonemBitisAt >= current)
                && (abonelik.IptalAt is null || abonelik.IptalAt > current);
        }

        private static bool IsActiveTrial(IsletmeDeneme deneme, DateTime current)
        {
            return IsActiveStatus(deneme.Durum)
                && deneme.OdemeYontemiEklendi
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
