using CashTracker.Core.Models;
using CashTracker.Core.Services;
using Systemcel.Api.Api;
using Xunit;

namespace CashTracker.Tests;

public sealed class BillingConsentTests
{
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
    }
}
