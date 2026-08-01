using System;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace CashTracker.Infrastructure.Services
{
    public sealed class EntitlementGuard : IEntitlementGuard
    {
        private readonly ISubscriptionEntitlementService _entitlementService;

        public EntitlementGuard(ISubscriptionEntitlementService entitlementService)
        {
            _entitlementService = entitlementService;
        }

        public Task<SubscriptionEntitlementStatus> GetAsync(
            int businessId,
            string accountType,
            CancellationToken ct = default)
        {
            return string.Equals(accountType, HesapTipleri.Muhasebeci, StringComparison.OrdinalIgnoreCase)
                ? _entitlementService.GetMuhasebeciEntitlementAsync(businessId, ct: ct)
                : _entitlementService.GetIsletmeEntitlementAsync(businessId, ct: ct);
        }

        public void EnsureLimit(
            SubscriptionEntitlementStatus entitlement,
            string limitName,
            int currentCount,
            int requestedCount = 1)
        {
            if (requestedCount < 1)
                throw new ArgumentOutOfRangeException(nameof(requestedCount));

            var limit = ResolveLimit(entitlement, limitName);
            if (limit is null || currentCount + requestedCount <= limit.Value)
                return;

            throw new EntitlementViolationException(
                EntitlementErrorCodes.LimitReached,
                $"{entitlement.PlanAdi} planındaki {DisplayName(limitName)} sınırına ulaştınız. Devam etmek için planınızı yükseltin.",
                limitName,
                limit,
                currentCount,
                SuggestedPlan(entitlement));
        }

        public void EnsureWritable(SubscriptionEntitlementStatus entitlement)
        {
            if (!entitlement.SaltOkunur)
                return;

            throw new EntitlementViolationException(
                EntitlementErrorCodes.SubscriptionRequired,
                "Bu çalışma alanı salt okunur durumda. Devam etmek için bir plan seçin.",
                suggestedPlanCode: entitlement.HesapTipi == HesapTipleri.Muhasebeci
                    ? PlanKodlari.MuhasebeciStandart
                    : PlanKodlari.IsletmeBaslangic);
        }

        public void EnsureFeature(SubscriptionEntitlementStatus entitlement, string featureName)
        {
            EnsureWritable(entitlement);
            var available = featureName switch
            {
                EntitlementFeatures.Ai => entitlement.AiAktif,
                EntitlementFeatures.OfficialEInvoice => entitlement.GibAktif,
                EntitlementFeatures.TelegramAutomation => entitlement.TelegramAktif,
                EntitlementFeatures.AdvancedExport => entitlement.GelismisDisaAktarimAktif,
                EntitlementFeatures.BankReconciliation => entitlement.BankaMutabakatiAktif,
                EntitlementFeatures.StockReport => entitlement.StokRaporAktif,
                EntitlementFeatures.MultipleBranches => entitlement.CokluSubeAktif,
                EntitlementFeatures.MultipleCurrencies => entitlement.CokluParaBirimiAktif,
                EntitlementFeatures.ApiAccess => entitlement.ApiErisimiAktif,
                _ => throw new ArgumentOutOfRangeException(nameof(featureName), featureName, "Bilinmeyen plan özelliği.")
            };

            if (available)
                return;

            throw new EntitlementViolationException(
                EntitlementErrorCodes.FeatureNotAvailable,
                $"{entitlement.PlanAdi} planında bu özellik kullanılamaz. Devam etmek için planınızı yükseltin.",
                suggestedPlanCode: SuggestedPlan(entitlement));
        }

        private static int? ResolveLimit(SubscriptionEntitlementStatus entitlement, string limitName)
        {
            return limitName switch
            {
                EntitlementLimits.Business => entitlement.IsletmeLimiti,
                EntitlementLimits.User => entitlement.KullaniciLimiti,
                EntitlementLimits.Invoice => entitlement.FaturaLimiti,
                EntitlementLimits.CashTransaction => entitlement.GelirGiderIslemLimiti,
                EntitlementLimits.CurrentAccount => entitlement.CariKartLimiti,
                EntitlementLimits.ProductOrService => entitlement.UrunHizmetLimiti,
                EntitlementLimits.AccountantCustomer => entitlement.MusteriLimiti,
                _ => throw new ArgumentOutOfRangeException(nameof(limitName), limitName, "Bilinmeyen plan limiti.")
            };
        }

        private static string DisplayName(string limitName)
        {
            return limitName switch
            {
                EntitlementLimits.Business => "işletme",
                EntitlementLimits.User => "kullanıcı",
                EntitlementLimits.Invoice => "fatura",
                EntitlementLimits.CashTransaction => "aylık gelir-gider kaydı",
                EntitlementLimits.CurrentAccount => "cari kart",
                EntitlementLimits.ProductOrService => "ürün/hizmet",
                EntitlementLimits.AccountantCustomer => "müşteri",
                _ => "kullanım"
            };
        }

        private static string SuggestedPlan(SubscriptionEntitlementStatus entitlement)
        {
            if (entitlement.HesapTipi == HesapTipleri.Muhasebeci)
            {
                return entitlement.PlanKodu == PlanKodlari.MuhasebeciStandart
                    ? PlanKodlari.MuhasebeciPro
                    : PlanKodlari.MuhasebeciStandart;
            }

            return entitlement.PlanKodu switch
            {
                PlanKodlari.IsletmeUcretsiz => PlanKodlari.IsletmeBaslangic,
                PlanKodlari.IsletmeBaslangic => PlanKodlari.IsletmeBuyume,
                _ => PlanKodlari.IsletmeKurumsal
            };
        }
    }
}
