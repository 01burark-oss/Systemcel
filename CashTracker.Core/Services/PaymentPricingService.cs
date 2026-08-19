using System;
using System.Linq;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public sealed class PaymentPricingService : IPaymentPricingService
    {
        private readonly decimal _vatRate;
        private readonly bool _freeTrialEnabled;
        private readonly int _businessTrialDays;
        private readonly int _accountantTrialDays;

        public PaymentPricingService(
            decimal vatRate = 20m,
            bool freeTrialEnabled = false,
            int businessTrialDays = 30,
            int accountantTrialDays = 14)
        {
            if (vatRate is < 0 or > 100)
                throw new ArgumentOutOfRangeException(nameof(vatRate), "KDV orani 0 ile 100 arasinda olmalidir.");
            if (businessTrialDays is < 1 or > 90)
                throw new ArgumentOutOfRangeException(nameof(businessTrialDays), "Isletme denemesi 1 ile 90 gun arasinda olmalidir.");
            if (accountantTrialDays is < 1 or > 90)
                throw new ArgumentOutOfRangeException(nameof(accountantTrialDays), "Muhasebeci denemesi 1 ile 90 gun arasinda olmalidir.");

            _vatRate = vatRate;
            _freeTrialEnabled = freeTrialEnabled;
            _businessTrialDays = businessTrialDays;
            _accountantTrialDays = accountantTrialDays;
        }

        public PaymentQuote CreateQuote(
            string planCode,
            string accountType,
            string billingPeriod,
            int extraCustomerCredits = 0,
            bool useFounderPrice = false)
        {
            if (string.IsNullOrWhiteSpace(planCode))
                throw new ArgumentException("Plan kodu zorunludur.", nameof(planCode));
            if (string.IsNullOrWhiteSpace(accountType))
                throw new ArgumentException("Hesap tipi zorunludur.", nameof(accountType));

            var plan = SubscriptionPlanCatalog.Plans.SingleOrDefault(x =>
                string.Equals(x.Kod, planCode.Trim(), StringComparison.OrdinalIgnoreCase));
            if (plan is null)
                throw new InvalidOperationException("Gecersiz abonelik plani.");
            if (!string.Equals(plan.HesapTipi, accountType.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Secilen plan hesap tipiyle uyumlu degil.");
            if (plan.AylikTutar <= 0 || plan.Kod is PlanKodlari.IsletmeIsletme or PlanKodlari.IsletmeUcretsiz or PlanKodlari.MuhasebeciUcretsiz)
                throw new InvalidOperationException("Bu plan icin odeme oturumu acilamaz.");
            if (extraCustomerCredits is < 0 or > 10000)
                throw new InvalidOperationException("Ek musteri kredisi 0 ile 10000 arasinda olmalidir.");
            if (extraCustomerCredits > 0 && plan.Kod != PlanKodlari.MuhasebeciStandart)
                throw new InvalidOperationException("Ek musteri kredisi yalnizca Muhasebeci Standart planina eklenebilir.");

            var normalizedPeriod = string.Equals(billingPeriod, PaymentBillingPeriods.Annual, StringComparison.OrdinalIgnoreCase)
                ? PaymentBillingPeriods.Annual
                : string.Equals(billingPeriod, PaymentBillingPeriods.Monthly, StringComparison.OrdinalIgnoreCase)
                    ? PaymentBillingPeriods.Monthly
                    : throw new InvalidOperationException("Gecersiz faturalama donemi.");

            var listNetAmount = normalizedPeriod == PaymentBillingPeriods.Annual
                ? plan.Kod == PlanKodlari.MuhasebeciStandart
                    ? SubscriptionPlanCatalog.CalculateMuhasebeciStandartYillikTutar(extraCustomerCredits)
                    : plan.YillikTutar
                : plan.Kod == PlanKodlari.MuhasebeciStandart
                    ? SubscriptionPlanCatalog.CalculateMuhasebeciStandartAylikTutar(extraCustomerCredits)
                    : plan.AylikTutar;
            var founderNetAmount = normalizedPeriod == PaymentBillingPeriods.Annual
                ? plan.Kod == PlanKodlari.MuhasebeciStandart
                    ? SubscriptionPlanCatalog.CalculateMuhasebeciStandartKurucuYillikTutar(extraCustomerCredits)
                    : plan.KurucuYillikTutar
                : plan.Kod == PlanKodlari.MuhasebeciStandart
                    ? SubscriptionPlanCatalog.CalculateMuhasebeciStandartKurucuAylikTutar(extraCustomerCredits)
                    : plan.KurucuAylikTutar;
            var netAmount = useFounderPrice ? founderNetAmount : listNetAmount;
            if (netAmount <= 0)
                throw new InvalidOperationException("Secilen donem icin katalog fiyati tanimli degil.");

            var vatAmount = decimal.Round(netAmount * _vatRate / 100m, 2, MidpointRounding.AwayFromZero);
            // Lansman boyunca fail-closed kalir. Gelecekte acilsa bile kurucu kampanyasi
            // veya yillik pesin odemeyle birlesmez; yalniz ilk aylik abonelikte kullanilir.
            var trialDays = _freeTrialEnabled &&
                            !useFounderPrice &&
                            normalizedPeriod == PaymentBillingPeriods.Monthly
                ? string.Equals(plan.HesapTipi, HesapTipleri.Muhasebeci, StringComparison.OrdinalIgnoreCase)
                    ? _accountantTrialDays
                    : _businessTrialDays
                : 0;

            return new PaymentQuote(
                plan.Kod,
                plan.HesapTipi,
                normalizedPeriod,
                "TRY",
                netAmount,
                _vatRate,
                vatAmount,
                netAmount + vatAmount,
                trialDays,
                extraCustomerCredits,
                plan.Kod == PlanKodlari.MuhasebeciStandart
                    ? SubscriptionPlanCatalog.MuhasebeciStandartDahilMusteriSayisi
                    : 0,
                normalizedPeriod == PaymentBillingPeriods.Annual
                    ? SubscriptionPlanCatalog.EkMusteriKredisiYillikTutar
                    : SubscriptionPlanCatalog.EkMusteriKredisiAylikTutar,
                useFounderPrice ? SubscriptionPlanCatalog.KurucuKampanyaKodu : string.Empty,
                useFounderPrice,
                listNetAmount,
                listNetAmount,
                useFounderPrice && normalizedPeriod == PaymentBillingPeriods.Monthly
                    ? SubscriptionPlanCatalog.KurucuAylikDonemSayisi
                    : useFounderPrice ? 1 : 0);
        }
    }
}
