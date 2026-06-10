using System;
using System.Collections.Generic;

namespace CashTracker.Core.Models
{
    public static class HesapTipleri
    {
        public const string Isletme = "Isletme";
        public const string Muhasebeci = "Muhasebeci";
        public const string Admin = "Admin";
    }

    public static class PlanKodlari
    {
        public const string IsletmeUcretsiz = "isletme_ucretsiz";
        public const string IsletmeBaslangic = "isletme_baslangic";
        public const string IsletmeIsletme = "isletme_isletme";
        public const string MuhasebeciUcretsiz = "muhasebeci_ucretsiz";
        public const string MuhasebeciStandart = "muhasebeci_standart";
        public const string MuhasebeciPro = "muhasebeci_pro";
    }

    public static class EntitlementKaynaklari
    {
        public const string KendiAboneligi = "KendiAboneligi";
        public const string IsletmeDenemesi = "IsletmeDenemesi";
        public const string MuhasebeciProSponsor = "MuhasebeciProSponsor";
        public const string Ucretsiz = "Ucretsiz";
    }

    public sealed record SubscriptionPlanDefinition(
        string Kod,
        string HesapTipi,
        string Ad,
        decimal AylikTutar,
        int? AiMesajLimiti,
        int? KullaniciLimiti,
        int? MusteriLimiti);

    public static class SubscriptionPlanCatalog
    {
        public static IReadOnlyList<SubscriptionPlanDefinition> Plans { get; } =
            new List<SubscriptionPlanDefinition>
            {
                new(PlanKodlari.IsletmeUcretsiz, HesapTipleri.Isletme, "Ücretsiz", 0, 0, 1, null),
                new(PlanKodlari.IsletmeBaslangic, HesapTipleri.Isletme, "Başlangıç", 199, 50, 1, null),
                new(PlanKodlari.IsletmeIsletme, HesapTipleri.Isletme, "İşletme", 399, null, 3, null),
                new(PlanKodlari.MuhasebeciUcretsiz, HesapTipleri.Muhasebeci, "Ücretsiz", 0, 0, 1, 3),
                new(PlanKodlari.MuhasebeciStandart, HesapTipleri.Muhasebeci, "Standart", 699, 100, 1, 10),
                new(PlanKodlari.MuhasebeciPro, HesapTipleri.Muhasebeci, "Pro", 1199, null, null, null)
            };

        public static decimal CalculateMuhasebeciStandartAylikTutar(int musteriSayisi)
        {
            if (musteriSayisi < 0)
                throw new ArgumentOutOfRangeException(nameof(musteriSayisi), "Musteri sayisi negatif olamaz.");

            return 699 + Math.Max(0, musteriSayisi - 10) * 50;
        }

        public static bool ShouldRecommendMuhasebeciPro(int musteriSayisi)
        {
            return CalculateMuhasebeciStandartAylikTutar(musteriSayisi) >= 1199;
        }
    }
}
