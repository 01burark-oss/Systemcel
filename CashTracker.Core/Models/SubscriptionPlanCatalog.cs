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
        public const string IsletmeBuyume = "isletme_buyume";
        public const string IsletmeKurumsal = "isletme_kurumsal";
        // Eski kayitlari okuyabilmek icin tutulur; yeni aboneliklerde kullanilmaz.
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
        int? MusteriLimiti)
    {
        public decimal YillikTutar { get; init; }
        public int? FaturaLimiti { get; init; }
        public bool BankaMutabakatiAktif { get; init; }
        public bool StokRaporAktif { get; init; }
        public bool MuhasebeciErisimiAktif { get; init; }
        public bool CokluSubeAktif { get; init; }
        public bool CokluParaBirimiAktif { get; init; }
        public bool ApiErisimiAktif { get; init; }
        public bool OncelikliDestekAktif { get; init; }
    }

    public static class SubscriptionPlanCatalog
    {
        public static IReadOnlyList<SubscriptionPlanDefinition> Plans { get; } =
            new List<SubscriptionPlanDefinition>
            {
                new(PlanKodlari.IsletmeUcretsiz, HesapTipleri.Isletme, "Ücretsiz", 0, 0, 1, null),
                new(PlanKodlari.IsletmeBaslangic, HesapTipleri.Isletme, "Başlangıç", 490, 100, 1, null)
                {
                    YillikTutar = 4704,
                    FaturaLimiti = 50
                },
                new(PlanKodlari.IsletmeBuyume, HesapTipleri.Isletme, "Büyüme", 990, null, 3, null)
                {
                    YillikTutar = 9504,
                    BankaMutabakatiAktif = true,
                    StokRaporAktif = true,
                    MuhasebeciErisimiAktif = true
                },
                new(PlanKodlari.IsletmeKurumsal, HesapTipleri.Isletme, "Kurumsal", 1990, null, null, null)
                {
                    YillikTutar = 19104,
                    BankaMutabakatiAktif = true,
                    StokRaporAktif = true,
                    MuhasebeciErisimiAktif = true,
                    CokluSubeAktif = true,
                    CokluParaBirimiAktif = true,
                    ApiErisimiAktif = true,
                    OncelikliDestekAktif = true
                },
                // Eski "Isletme" abonelikleri Büyüme haklariyla devam eder.
                new(PlanKodlari.IsletmeIsletme, HesapTipleri.Isletme, "Büyüme (eski)", 990, null, 3, null)
                {
                    YillikTutar = 9504,
                    BankaMutabakatiAktif = true,
                    StokRaporAktif = true,
                    MuhasebeciErisimiAktif = true
                },
                new(PlanKodlari.MuhasebeciUcretsiz, HesapTipleri.Muhasebeci, "Ücretsiz", 0, 0, 1, 3),
                new(PlanKodlari.MuhasebeciStandart, HesapTipleri.Muhasebeci, "Standart", 699, 100, 1, 10)
                {
                    YillikTutar = 7045.92m
                },
                new(PlanKodlari.MuhasebeciPro, HesapTipleri.Muhasebeci, "Pro", 1199, null, null, null)
                {
                    YillikTutar = 12085.92m
                }
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
