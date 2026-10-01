using System.Data;
using System.Security.Cryptography;
using System.Text;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Payments;

public sealed class PaymentRefundService(IDbContextFactory<CashTrackerDbContext> factory, IPaymentProvider paymentProvider) : IPaymentRefundService
{
    private IPaymentRefundProvider Provider => paymentProvider is IPaymentRefundProvider { ExpectedTestMode: true } provider
        ? provider : throw new InvalidOperationException("Refund provider is not configured for test mode.");

    public async Task<OdemeIadeTalimati> RequestAsync(int businessId, int paymentId, decimal amount, string idempotencyKey, CancellationToken ct = default)
    {
        _ = Provider;
        if (businessId <= 0 || amount <= 0 || decimal.Round(amount, 2) != amount ||
            string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 100)
            throw new ArgumentException("Invalid refund request.");
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var payment = await db.OdemeIslemleri.SingleAsync(x => x.Id == paymentId && x.IsletmeId == businessId, ct);
        if (payment.OdemeSaglayici != paymentProvider.Name || payment.ParaBirimi != "TRY" ||
            payment.IslemTipi is not (PaymentTransactionTypes.SubscriptionStart or PaymentTransactionTypes.PlanUpgrade) ||
            string.IsNullOrWhiteSpace(payment.SaglayiciOturumId) || payment.SaglayiciOturumId.Length > 64 ||
            payment.SaglayiciOturumId.Any(c => !char.IsAsciiLetterOrDigit(c)))
            throw new InvalidOperationException("Payment is not an eligible subscription charge.");
        var instructions = await db.OdemeIadeTalimatlari.Where(x => x.OdemeIslemiId == paymentId).ToListAsync(ct);
        var existing = instructions.SingleOrDefault(x => x.IdempotencyAnahtari == idempotencyKey);
        if (existing is not null)
        {
            if (existing.Tutar != amount) throw new InvalidOperationException("Refund key was used with a different amount.");
            return existing;
        }
        if (payment.Durum != PaymentTransactionStates.Succeeded ||
            instructions.Any(x => x.Durum is "Gonderiliyor" or "SonucBekliyor" or "IncelemeGerekli") ||
            instructions.Where(x => x.Durum != "KesinBasarisiz").Sum(x => x.Tutar) + amount > payment.ToplamTutar)
            throw new InvalidOperationException("Refund amount is unavailable or another refund needs review.");
        var reference = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            payment.OdemeSaglayici + ":" + paymentId + ":" + idempotencyKey))).ToLowerInvariant();
        var instruction = new OdemeIadeTalimati
        {
            OdemeIslemiId = paymentId, Tutar = amount, IdempotencyAnahtari = idempotencyKey, ReferansNo = reference
        };
        db.OdemeIadeTalimatlari.Add(instruction);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return instruction;
    }

    public async Task<OdemeIadeTalimati> DispatchAsync(long instructionId, CancellationToken ct = default)
    {
        var provider = Provider;
        ct.ThrowIfCancellationRequested();
        OdemeIadeTalimati instruction;
        OdemeIslemi payment;
        decimal completedTotal;
        await using (var db = await factory.CreateDbContextAsync(ct))
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            instruction = await db.OdemeIadeTalimatlari.SingleAsync(x => x.Id == instructionId, ct);
            if (instruction.Durum != "Hazir") return instruction;
            payment = await db.OdemeIslemleri.SingleAsync(x => x.Id == instruction.OdemeIslemiId, ct);
            var others = await db.OdemeIadeTalimatlari.Where(x => x.OdemeIslemiId == payment.Id && x.Id != instructionId).ToListAsync(ct);
            if (payment.Durum != PaymentTransactionStates.Succeeded || payment.OdemeSaglayici != paymentProvider.Name ||
                others.Any(x => x.Durum is "Gonderiliyor" or "SonucBekliyor" or "IncelemeGerekli"))
                throw new InvalidOperationException("Payment or refund is blocked.");
            completedTotal = others.Where(x => x.Durum == "Tamamlandi").Sum(x => x.Tutar);
            instruction.Durum = "Gonderiliyor";
            instruction.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        try
        {
            var lookup = await provider.GetPaymentAsync(payment.SaglayiciOturumId, ct);
            if (!MatchesPayment(payment, lookup.Payment, provider.ExpectedTestMode) || !lookup.Available ||
                lookup.Payment!.RefundedAmount != completedTotal || completedTotal + instruction.Tutar > payment.ToplamTutar)
                return await SetStateAsync(instructionId, "IncelemeGerekli", "preflight_mismatch", CancellationToken.None);
            var result = await provider.RefundAsync(payment.SaglayiciOturumId, instruction.Tutar, instruction.ReferansNo, ct);
            if (!result.Accepted)
                return await SetStateAsync(instructionId, result.IsDefinitive ? "KesinBasarisiz" : "SonucBekliyor",
                    result.ErrorCode, CancellationToken.None);
            await SetStateAsync(instructionId, "SonucBekliyor", "confirmation_pending", CancellationToken.None);
            return await ReconcileAsync(instructionId, ct);
        }
        catch (OperationCanceledException)
        {
            await SetStateAsync(instructionId, "SonucBekliyor", "cancelled_or_timeout", CancellationToken.None);
            throw;
        }
        catch (HttpRequestException) { return await SetStateAsync(instructionId, "SonucBekliyor", "transport_error", CancellationToken.None); }
    }

    public async Task<OdemeIadeTalimati> ReconcileAsync(long instructionId, CancellationToken ct = default)
    {
        var provider = Provider;
        await using var db = await factory.CreateDbContextAsync(ct);
        var selected = await db.OdemeIadeTalimatlari.AsNoTracking().SingleAsync(x => x.Id == instructionId, ct);
        if (selected.Durum is "Hazir" or "Tamamlandi" or "KesinBasarisiz") return selected;
        var payment = await db.OdemeIslemleri.AsNoTracking().SingleAsync(x => x.Id == selected.OdemeIslemiId, ct);
        var lookup = await provider.GetPaymentAsync(payment.SaglayiciOturumId, ct);
        if (!lookup.Available) return selected;
        if (!MatchesPayment(payment, lookup.Payment, provider.ExpectedTestMode))
            return await SetStateAsync(instructionId, "IncelemeGerekli", "query_payment_mismatch", ct);
        var matches = lookup.Payment!.Refunds?.Where(x => x.ReferenceNo == selected.ReferansNo).ToList() ?? [];
        if (matches.Count == 0) return selected;
        if (matches.Count != 1 || matches[0].Amount != selected.Tutar)
            return await SetStateAsync(instructionId, "IncelemeGerekli", "query_refund_mismatch", ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var instruction = await db.OdemeIadeTalimatlari.SingleAsync(x => x.Id == instructionId, ct);
        if (instruction.Durum == "Tamamlandi") return instruction;
        var local = await db.OdemeIslemleri.SingleAsync(x => x.Id == instruction.OdemeIslemiId, ct);
        var completed = await db.OdemeIadeTalimatlari.Where(x => x.OdemeIslemiId == local.Id && x.Durum == "Tamamlandi").ToListAsync(ct);
        var total = completed.Sum(x => x.Tutar) + instruction.Tutar;
        if (total > local.ToplamTutar || lookup.Payment.RefundedAmount < total)
            throw new InvalidOperationException("Refund totals are inconsistent.");
        instruction.Durum = "Tamamlandi";
        instruction.SonHataKodu = string.Empty;
        var now = DateTime.UtcNow;
        instruction.UpdatedAt = now;
        db.OdemeOlaylari.Add(new OdemeOlayi
        {
            OdemeSaglayici = local.OdemeSaglayici, OlayId = "refund:" + instruction.ReferansNo,
            OlayTipi = "payment.refund.confirmed", CheckoutAnahtari = local.CheckoutAnahtari,
            SaglayiciIslemId = instruction.ReferansNo, IslenmeDurumu = "Islendi", SaglayiciAt = now, AlindiAt = now, IslendiAt = now
        });
        if (total == local.ToplamTutar)
        {
            local.Durum = PaymentTransactionStates.Refunded;
            local.UpdatedAt = now;
            // Refund only the subscription created by this charge, preserving a newer paid period.
            var subscription = await db.Abonelikler.SingleOrDefaultAsync(x => x.IsletmeId == local.IsletmeId &&
                x.HesapTipi == local.HesapTipi && x.OdemeSaglayici == local.OdemeSaglayici &&
                x.SaglayiciAbonelikId == local.SaglayiciIslemId, ct);
            if (subscription is not null)
            {
                var wasActive = subscription.Durum == "Aktif";
                subscription.Durum = "IadeEdildi";
                if (subscription.DonemBitisAt is null || subscription.DonemBitisAt > now)
                    subscription.DonemBitisAt = now;
                subscription.UpdatedAt = now;
                if (wasActive && local.IslemTipi == PaymentTransactionTypes.PlanUpgrade &&
                    !await db.Abonelikler.AnyAsync(x => x.IsletmeId == local.IsletmeId && x.HesapTipi == local.HesapTipi &&
                        x.Id != subscription.Id && x.Durum == "Aktif", ct))
                {
                    var previous = await db.Abonelikler.Where(x => x.IsletmeId == local.IsletmeId && x.HesapTipi == local.HesapTipi &&
                        x.Durum == "Degistirildi" && x.DonemBitisAt > now).OrderByDescending(x => x.UpdatedAt).FirstOrDefaultAsync(ct);
                    if (previous is not null) { previous.Durum = "Aktif"; previous.UpdatedAt = now; }
                }
            }
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return instruction;
    }

    private async Task<OdemeIadeTalimati> SetStateAsync(long id, string state, string error, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var instruction = await db.OdemeIadeTalimatlari.SingleAsync(x => x.Id == id, ct);
        if (instruction.Durum is "Tamamlandi" or "KesinBasarisiz") return instruction;
        instruction.Durum = state;
        instruction.SonHataKodu = error.Length <= 80 && error.All(c => char.IsAsciiLetterOrDigit(c) || c == '_')
            ? error : "provider_error";
        instruction.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return instruction;
    }

    private static bool MatchesPayment(OdemeIslemi local, ProviderPaymentSnapshot? remote, bool testMode) =>
        remote is not null && remote.ProviderOrderId == local.SaglayiciOturumId && remote.Amount == local.ToplamTutar &&
        remote.TotalAmount == local.ToplamTutar && remote.Currency == local.ParaBirimi && remote.IsTestPayment == testMode;
}
