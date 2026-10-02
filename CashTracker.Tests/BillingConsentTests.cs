using CashTracker.Core.Models;
using CashTracker.Core.Services;
using Systemcel.Api.Api;
using Xunit;

namespace CashTracker.Tests;

public sealed class BillingConsentTests
{
    [Theory]
    [InlineData(PaymentBillingPeriods.Monthly)]
    [InlineData(PaymentBillingPeriods.Annual)]
    public void SingleCardPaymentConsent_DoesNotPromiseStoredCardsOrAutomaticRenewal(string period)
    {
        var quote = new PaymentPricingService().CreateQuote(
            PlanKodlari.IsletmeBuyume, HesapTipleri.Isletme, period, useFounderPrice: false);

        var consent = BillingApi.BuildConsentText(quote);

        Assert.Contains("seçtiğim karttan bugün alınmasını", consent);
        Assert.Contains("kartımı saklamaz ve otomatik yenileme başlatmaz", consent);
        Assert.Contains("Sonraki dönem için yeniden ödeme yapmam gerekir", consent);
        Assert.DoesNotContain("kayıtlı ödeme yöntemimden", consent);
        Assert.DoesNotContain("üzerinden yenileneceğini kabul", consent);
    }

    [Fact]
    public void AnnualFounderConsent_AppliesCampaignToTheFullPaidYear()
    {
        var quote = new PaymentPricingService().CreateQuote(
            PlanKodlari.IsletmeBuyume,
            HesapTipleri.Isletme,
            PaymentBillingPeriods.Annual,
            useFounderPrice: true);

        var consent = BillingApi.BuildConsentText(quote);

        Assert.Contains("12 aylık dönemin tamamına", consent);
        Assert.Contains("11.880,00 TL", consent);
        Assert.Contains("15.480,00 TL", consent);
        Assert.DoesNotContain("yalnızca ilk 3 aylık", consent);
        Assert.Contains("mevcut aylık dönem tamamlanır", consent);
        Assert.Contains("gerçekten ödenen yıllık tutarın 1/12", consent);
        Assert.DoesNotContain("geçmiş tahsilatı kendiliğinden iade", consent);
    }

    [Fact]
    public void MonthlyFounderConsent_KeepsThreeDiscountedRenewals()
    {
        var quote = new PaymentPricingService().CreateQuote(
            PlanKodlari.IsletmeBuyume,
            HesapTipleri.Isletme,
            PaymentBillingPeriods.Monthly,
            useFounderPrice: true);

        var consent = BillingApi.BuildConsentText(quote);

        Assert.Contains("ilk 3 aylık dönem", consent);
        Assert.Contains("990,00 TL", consent);
        Assert.Contains("1.290,00 TL", consent);
        Assert.Contains("ödenmiş aylık dönemin sonunda", consent);
    }
}
