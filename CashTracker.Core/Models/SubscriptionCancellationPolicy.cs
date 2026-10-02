using CashTracker.Core.Entities;

namespace CashTracker.Core.Models;

public sealed record SubscriptionCancellationQuote(
    DateTime AccessEndsAt, int RemainingMonths, decimal? RefundAmount,
    string Currency, string RefundStatus, int? PaymentId);

public static class SubscriptionCancellationPolicy
{
    public static SubscriptionCancellationQuote Calculate(
        Abonelik subscription, OdemeIslemi? payment,
        IReadOnlyList<OdemeIadeTalimati> refunds, DateTime now)
    {
        if (subscription.DonemSonundaIptal && subscription.IptalAt is not null)
            return new(subscription.DonemBitisAt ?? now, subscription.IptalKalanAySayisi,
                subscription.IptalIadeTutari, subscription.ParaBirimi,
                subscription.IptalIadeDurumu, subscription.IptalIadeOdemeIslemiId);

        var end = Utc(subscription.DonemBitisAt ?? now);
        if (subscription.FaturalamaDonemi != PaymentBillingPeriods.Annual)
            return new(end, 0, 0m, subscription.ParaBirimi, "Gerekmiyor", null);

        var start = Utc(subscription.DonemBaslangicAt);
        var current = Utc(now);
        var completedMonths = 1;
        while (completedMonths < 12 && start.AddMonths(completedMonths) <= current)
            completedMonths++;
        var cutoff = start.AddMonths(completedMonths);
        if (cutoff > end) cutoff = end;
        var remaining = end <= current ? 0 : 12 - completedMonths;
        if (remaining == 0)
            return new(end, 0, 0m, subscription.ParaBirimi, "Gerekmiyor", null);

        // Legacy or prorated upgrades need review instead of inventing an annual payment.
        var eligible = end == start.AddYears(1) && current >= start && payment is not null &&
            !string.IsNullOrWhiteSpace(subscription.SaglayiciAbonelikId) &&
            payment.IsletmeId == subscription.IsletmeId && payment.HesapTipi == subscription.HesapTipi &&
            payment.SaglayiciIslemId == subscription.SaglayiciAbonelikId &&
            payment.OdemeSaglayici == subscription.OdemeSaglayici && payment.PlanKodu == subscription.PlanKodu &&
            payment.FaturalamaDonemi == PaymentBillingPeriods.Annual &&
            payment.IslemTipi == PaymentTransactionTypes.SubscriptionStart &&
            payment.Durum == PaymentTransactionStates.Succeeded && payment.ToplamTutar > 0m &&
            payment.ParaBirimi == subscription.ParaBirimi &&
            !refunds.Any(x => x.OdemeIslemiId == payment.Id && x.Durum != "KesinBasarisiz");
        return eligible
            ? new(cutoff, remaining, decimal.Round(payment!.ToplamTutar * remaining / 12m, 2,
                MidpointRounding.AwayFromZero), payment.ParaBirimi, "OnayBekliyor", payment.Id)
            : new(cutoff, remaining, null, subscription.ParaBirimi, "IncelemeGerekli", null);
    }

    private static DateTime Utc(DateTime value) => value.Kind == DateTimeKind.Local
        ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
