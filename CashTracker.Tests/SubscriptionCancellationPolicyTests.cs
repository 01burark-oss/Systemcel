using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using Xunit;

namespace CashTracker.Tests;

public sealed class SubscriptionCancellationPolicyTests
{
    [Theory]
    [InlineData("2026-09-15", "2026-10-01", "2026-10-15", 11)]
    [InlineData("2026-01-31", "2026-02-28", "2026-03-31", 10)]
    [InlineData("2024-02-29", "2024-03-01", "2024-03-29", 11)]
    [InlineData("2026-01-15", "2026-12-20", "2027-01-15", 0)]
    [InlineData("2026-09-15", "2026-10-15", "2026-11-15", 10)]
    public void Annual_UsesAnchoredMonthlyBoundaryAndActualGrossPayment(
        string startText, string nowText, string cutoffText, int months)
    {
        var (subscription, payment) = Annual(DateTime.Parse(startText));
        var quote = SubscriptionCancellationPolicy.Calculate(subscription, payment, [], DateTime.Parse(nowText));
        Assert.Equal(DateTime.Parse(cutoffText), quote.AccessEndsAt);
        Assert.Equal(months, quote.RemainingMonths);
        Assert.Equal(decimal.Round(1000.01m * months / 12m, 2, MidpointRounding.AwayFromZero), quote.RefundAmount);
    }

    [Fact]
    public void Monthly_KeepsPaidPeriodAndDoesNotRefund()
    {
        var (subscription, payment) = Annual(new DateTime(2026, 9, 15));
        subscription.FaturalamaDonemi = PaymentBillingPeriods.Monthly;
        subscription.DonemBitisAt = subscription.DonemBaslangicAt.AddMonths(1);
        var quote = SubscriptionCancellationPolicy.Calculate(subscription, payment, [], new DateTime(2026, 10, 1));
        Assert.Equal(subscription.DonemBitisAt, quote.AccessEndsAt);
        Assert.Equal(0m, quote.RefundAmount);
        Assert.Equal("Gerekmiyor", quote.RefundStatus);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("tenant")]
    [InlineData("failed")]
    [InlineData("upgrade")]
    [InlineData("refund")]
    public void Annual_AmbiguousPaymentOrExistingRefundRequiresReview(string scenario)
    {
        var (subscription, payment) = Annual(new DateTime(2026, 9, 15));
        if (scenario == "tenant") payment.IsletmeId++;
        if (scenario == "failed") payment.Durum = PaymentTransactionStates.Failed;
        if (scenario == "upgrade") payment.IslemTipi = PaymentTransactionTypes.PlanUpgrade;
        var refunds = scenario == "refund" ? new[] { new OdemeIadeTalimati { OdemeIslemiId = payment.Id, Durum = "SonucBekliyor" } } : [];
        var quote = SubscriptionCancellationPolicy.Calculate(subscription, scenario == "missing" ? null : payment,
            refunds, new DateTime(2026, 10, 1));
        Assert.Equal(new DateTime(2026, 10, 15), quote.AccessEndsAt);
        Assert.Null(quote.RefundAmount);
        Assert.Equal("IncelemeGerekli", quote.RefundStatus);
    }

    private static (Abonelik, OdemeIslemi) Annual(DateTime start) => (
        new Abonelik { IsletmeId = 7, PlanKodu = "test", FaturalamaDonemi = PaymentBillingPeriods.Annual,
            DonemBaslangicAt = start, DonemBitisAt = start.AddYears(1), DonemTutari = 99999m,
            SaglayiciAbonelikId = "paid-annual", OdemeSaglayici = "Fake", ParaBirimi = "TRY" },
        new OdemeIslemi { Id = 9, IsletmeId = 7, PlanKodu = "test", FaturalamaDonemi = PaymentBillingPeriods.Annual,
            IslemTipi = PaymentTransactionTypes.SubscriptionStart, Durum = PaymentTransactionStates.Succeeded,
            ToplamTutar = 1000.01m, SaglayiciIslemId = "paid-annual", OdemeSaglayici = "Fake", ParaBirimi = "TRY" });
}
