using System;
using System.Collections.Generic;
using System.Linq;

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
        public const string MuhasebeciSaltOkunur = "muhasebeci_salt_okunur";
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
        public decimal KurucuAylikTutar { get; init; }
        public decimal KurucuYillikTutar { get; init; }
        public int? FaturaLimiti { get; init; }
        public int? IsletmeLimiti { get; init; }
        public int? GelirGiderIslemLimiti { get; init; }
        public int? CariKartLimiti { get; init; }
        public int? UrunHizmetLimiti { get; init; }
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
        public const string KurucuKampanyaKodu = "kurucu-100-2026";
        public const int KurucuKampanyaKontenjani = 50;
        public const int KurucuAylikDonemSayisi = 3;
        public const int MuhasebeciStandartDahilMusteriSayisi = 10;
        public const decimal EkMusteriKredisiAylikTutar = 50m;
        public const decimal EkMusteriKredisiYillikTutar = 504m;

        public static IReadOnlyList<SubscriptionPlanDefinition> Plans { get; } =
            new List<SubscriptionPlanDefinition>
            {
                new(PlanKodlari.IsletmeUcretsiz, HesapTipleri.Isletme, "Ücretsiz", 0, 0, 1, null)
                {
                    FaturaLimiti = 0,
                    IsletmeLimiti = 1,
                    GelirGiderIslemLimiti = 100,
                    CariKartLimiti = 20,
                    UrunHizmetLimiti = 50
                },
                new(PlanKodlari.IsletmeBaslangic, HesapTipleri.Isletme, "Başlangıç", 690, 100, 1, null)
                {
                    YillikTutar = 6624,
                    KurucuAylikTutar = 490,
                    KurucuYillikTutar = 6144,
                    FaturaLimiti = 50
                },
                new(PlanKodlari.IsletmeBuyume, HesapTipleri.Isletme, "Büyüme", 1290, null, 3, null)
                {
                    YillikTutar = 15480,
                    KurucuAylikTutar = 990,
                    KurucuYillikTutar = 11880,
                    StokRaporAktif = true,
                    MuhasebeciErisimiAktif = true
                },
                new(PlanKodlari.IsletmeKurumsal, HesapTipleri.Isletme, "Kurumsal", 2490, null, null, null)
                {
                    YillikTutar = 23904,
                    KurucuAylikTutar = 1990,
                    KurucuYillikTutar = 22704,
                    StokRaporAktif = true,
                    MuhasebeciErisimiAktif = true,
                    OncelikliDestekAktif = true
                },
                // Eski "Isletme" abonelikleri Büyüme haklariyla devam eder.
                new(PlanKodlari.IsletmeIsletme, HesapTipleri.Isletme, "Büyüme (eski)", 1290, null, 3, null)
                {
                    YillikTutar = 15480,
                    StokRaporAktif = true,
                    MuhasebeciErisimiAktif = true
                },
                new(PlanKodlari.MuhasebeciUcretsiz, HesapTipleri.Muhasebeci, "Ücretsiz", 0, 0, 1, 3),
                new(PlanKodlari.MuhasebeciSaltOkunur, HesapTipleri.Muhasebeci, "Salt okunur", 0, 0, 0, 0),
                new(PlanKodlari.MuhasebeciStandart, HesapTipleri.Muhasebeci, "Standart", 899, 100, 1, 10)
                {
                    YillikTutar = 9061.92m,
                    KurucuAylikTutar = 699m,
                    KurucuYillikTutar = 8557.92m
                },
                new(PlanKodlari.MuhasebeciPro, HesapTipleri.Muhasebeci, "Pro", 1499, null, null, null)
                {
                    YillikTutar = 15109.92m,
                    KurucuAylikTutar = 1199m,
                    KurucuYillikTutar = 14353.92m
                }
            };

        public static decimal CalculateMuhasebeciStandartAylikTutar(int ekMusteriKredisi)
        {
            if (ekMusteriKredisi < 0)
                throw new ArgumentOutOfRangeException(nameof(ekMusteriKredisi), "Ek musteri kredisi negatif olamaz.");

            return 899 + ekMusteriKredisi * EkMusteriKredisiAylikTutar;
        }

        public static decimal CalculateMuhasebeciStandartYillikTutar(int ekMusteriKredisi)
        {
            if (ekMusteriKredisi < 0)
                throw new ArgumentOutOfRangeException(nameof(ekMusteriKredisi), "Ek musteri kredisi negatif olamaz.");

            var plan = Plans.Single(x => x.Kod == PlanKodlari.MuhasebeciStandart);
            return plan.YillikTutar + ekMusteriKredisi * EkMusteriKredisiYillikTutar;
        }

        public static decimal CalculateMuhasebeciStandartKurucuAylikTutar(int ekMusteriKredisi)
        {
            if (ekMusteriKredisi < 0)
                throw new ArgumentOutOfRangeException(nameof(ekMusteriKredisi), "Ek musteri kredisi negatif olamaz.");

            var plan = Plans.Single(x => x.Kod == PlanKodlari.MuhasebeciStandart);
            return plan.KurucuAylikTutar + ekMusteriKredisi * EkMusteriKredisiAylikTutar;
        }

        public static decimal CalculateMuhasebeciStandartKurucuYillikTutar(int ekMusteriKredisi)
        {
            if (ekMusteriKredisi < 0)
                throw new ArgumentOutOfRangeException(nameof(ekMusteriKredisi), "Ek musteri kredisi negatif olamaz.");

            var plan = Plans.Single(x => x.Kod == PlanKodlari.MuhasebeciStandart);
            return plan.KurucuYillikTutar + ekMusteriKredisi * EkMusteriKredisiYillikTutar;
        }

        public static bool ShouldRecommendMuhasebeciPro(int ekMusteriKredisi)
        {
            return CalculateMuhasebeciStandartAylikTutar(ekMusteriKredisi) >= 1499;
        }
    }
}
