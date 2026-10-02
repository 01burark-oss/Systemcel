using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Payments;

public sealed class SubscriptionCancellationRefundService(
    IDbContextFactory<CashTrackerDbContext> factory, ISystemcelYonetimService management,
    ICurrentUserContext currentUser, IPaymentRefundService refunds, IPaymentProvider provider)
    : ISubscriptionCancellationRefundService
{
    private bool TestEnabled => provider is IPaymentRefundProvider { ExpectedTestMode: true };

    public async Task<CancellationRefundQueueDto> ListAsync(CancellationToken ct = default)
    {
        await RequireAdminAsync(ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        var subscriptions = await db.Abonelikler.AsNoTracking()
            .Where(x => x.DonemSonundaIptal && x.FaturalamaDonemi == PaymentBillingPeriods.Annual &&
                x.IptalAt != null && x.IptalKalanAySayisi > 0 && x.IptalIadeDurumu != "")
            .OrderByDescending(x => x.IptalAt).ThenByDescending(x => x.Id).Take(100).ToListAsync(ct);
        var businessIds = subscriptions.Select(x => x.IsletmeId).Distinct().ToArray();
        var businesses = await db.Isletmeler.AsNoTracking().Where(x => businessIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Ad, ct);
        var instructionIds = subscriptions.Where(x => x.IptalIadeTalimatiId != null)
            .Select(x => x.IptalIadeTalimatiId!.Value).ToArray();
        var instructions = await db.OdemeIadeTalimatlari.AsNoTracking().Where(x => instructionIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);
        return new(TestEnabled, subscriptions.Select(x =>
        {
            var linked = x.IptalIadeTalimatiId is { } id && instructions.TryGetValue(id, out var instruction) ? instruction : null;
            var state = linked?.Durum ?? x.IptalIadeDurumu;
            return new CancellationRefundItemDto(x.Id, businesses.GetValueOrDefault(x.IsletmeId, "İşletme"),
                x.HesapTipi, x.PlanKodu, x.IptalAt, x.DonemBitisAt, x.IptalKalanAySayisi, x.IptalIadeTutari,
                x.ParaBirimi, state,
                TestEnabled && linked is null && state == "OnayBekliyor" && x.IptalIadeTutari > 0 && x.IptalIadeOdemeIslemiId != null,
                TestEnabled && linked is not null && state == "Hazir",
                TestEnabled && linked is not null && state is "Gonderiliyor" or "SonucBekliyor" or "IncelemeGerekli", x.IptalIadeOnayAt);
        }).ToArray());
    }

    public async Task ApproveAsync(int subscriptionId, CancellationToken ct = default)
    {
        var actor = await RequireAdminAsync(ct);
        if (!TestEnabled) throw new InvalidOperationException("Test iade işlemleri kapalı.");
        await refunds.ApproveCancellationAsync(subscriptionId, actor, ct);
    }

    public async Task DispatchAsync(int subscriptionId, CancellationToken ct = default)
    {
        var id = await ApprovedInstructionAsync(subscriptionId, ct);
        await refunds.DispatchAsync(id, ct);
    }

    public async Task ReconcileAsync(int subscriptionId, CancellationToken ct = default)
    {
        var id = await ApprovedInstructionAsync(subscriptionId, ct);
        await refunds.ReconcileAsync(id, ct);
    }

    private async Task<long> ApprovedInstructionAsync(int subscriptionId, CancellationToken ct)
    {
        await RequireAdminAsync(ct);
        if (!TestEnabled) throw new InvalidOperationException("Test iade işlemleri kapalı.");
        await using var db = await factory.CreateDbContextAsync(ct);
        var subscription = await db.Abonelikler.AsNoTracking().SingleOrDefaultAsync(x => x.Id == subscriptionId, ct)
            ?? throw new KeyNotFoundException("Abonelik bulunamadı.");
        if (subscription.IptalIadeTalimatiId is not { } id || subscription.IptalIadeOnayAt is null ||
            string.IsNullOrWhiteSpace(subscription.IptalIadeOnaylayanProviderKullaniciId))
            throw new InvalidOperationException("Gönderimden önce yetkili iade onayı gerekir.");
        var instruction = await db.OdemeIadeTalimatlari.AsNoTracking().SingleAsync(x => x.Id == id, ct);
        var payment = await db.OdemeIslemleri.AsNoTracking().SingleAsync(x => x.Id == instruction.OdemeIslemiId, ct);
        if (instruction.OdemeIslemiId != subscription.IptalIadeOdemeIslemiId || instruction.Tutar != subscription.IptalIadeTutari ||
            payment.IsletmeId != subscription.IsletmeId || payment.HesapTipi != subscription.HesapTipi ||
            payment.SaglayiciIslemId != subscription.SaglayiciAbonelikId || payment.OdemeSaglayici != subscription.OdemeSaglayici)
            throw new InvalidOperationException("Onay, ödeme ve iade kaydı eşleşmiyor; inceleme gerekir.");
        return id;
    }

    private async Task<string> RequireAdminAsync(CancellationToken ct)
    {
        if (!await management.IsCurrentUserAdminAsync(ct) || currentUser.GetCurrentUser() is not { } identity ||
            string.IsNullOrWhiteSpace(identity.ProviderUserId) || identity.ProviderUserId.Length > 200)
            throw new UnauthorizedAccessException("Yönetici yetkisi gerekir.");
        return identity.ProviderUserId;
    }
}
