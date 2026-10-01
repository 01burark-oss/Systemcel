using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Payments;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests;

public sealed class PaymentRefundServiceTests
{
    [Fact]
    public async Task Request_IsIdempotent_ReservesAmounts_AndRejectsOverRefund()
    {
        await using var fixture = await Fixture.CreateAsync();
        var paymentId = await fixture.AddPaymentAsync();

        var first = await fixture.Service.RequestAsync(7, paymentId, 40m, "refund-a");
        var replay = await fixture.Service.RequestAsync(7, paymentId, 40m, "refund-a");
        var second = await fixture.Service.RequestAsync(7, paymentId, 60m, "refund-b");

        Assert.Equal(first.Id, replay.Id);
        Assert.Equal("Hazir", first.Durum);
        Assert.Equal(40m, first.Tutar);
        Assert.Equal(60m, second.Tutar);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.RequestAsync(7, paymentId, 0.01m, "refund-c"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.RequestAsync(7, paymentId, 41m, "refund-a"));
        Assert.Equal(0, fixture.Provider.RefundCalls);
        await using var db = fixture.Factory.CreateDbContext();
        Assert.Equal(2, await db.OdemeIadeTalimatlari.CountAsync());
    }

    [Fact]
    public async Task Request_RejectsOtherBusinessAndNonSubscriptionPayments()
    {
        await using var fixture = await Fixture.CreateAsync();
        var subscriptionPayment = await fixture.AddPaymentAsync(businessId: 7);
        var otherBusinessPayment = await fixture.AddPaymentAsync(businessId: 8);
        var accountantPayment = await fixture.AddPaymentAsync(businessId: 7,
            type: PaymentTransactionTypes.AccountantService);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.RequestAsync(8, subscriptionPayment, 10m, "cross-business"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.RequestAsync(7, otherBusinessPayment, 10m, "wrong-payment"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.RequestAsync(7, accountantPayment, 10m, "accountant"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            fixture.Service.RequestAsync(7, subscriptionPayment, 10.001m, "bad-rounding"));
    }

    [Fact]
    public async Task Dispatch_ConfirmedPartialRefundLeavesPaymentAndSubscriptionActive_AndDoesNotResend()
    {
        await using var fixture = await Fixture.CreateAsync();
        var paymentId = await fixture.AddPaymentAsync(withActiveSubscription: true);
        var instruction = await fixture.Service.RequestAsync(7, paymentId, 25m, "partial-1");

        var completed = await fixture.Service.DispatchAsync(instruction.Id);
        var duplicateDispatch = await fixture.Service.DispatchAsync(instruction.Id);

        Assert.Equal("Tamamlandi", completed.Durum);
        Assert.Equal(instruction.Id, duplicateDispatch.Id);
        Assert.Equal(1, fixture.Provider.RefundCalls);
        Assert.Equal(fixture.OrderFor(paymentId), fixture.Provider.LastOrderId);
        Assert.Equal(25m, fixture.Provider.LastRefundAmount);
        Assert.Equal(instruction.ReferansNo, fixture.Provider.LastReferenceNo);
        await using var db = fixture.Factory.CreateDbContext();
        Assert.Equal(PaymentTransactionStates.Succeeded, (await db.OdemeIslemleri.SingleAsync(x => x.Id == paymentId)).Durum);
        Assert.Equal("Aktif", (await db.Abonelikler.SingleAsync()).Durum);
        var refundEvent = await db.OdemeOlaylari.SingleAsync();
        Assert.Equal("payment.refund.confirmed", refundEvent.OlayTipi);
        Assert.Equal(instruction.ReferansNo, refundEvent.SaglayiciIslemId);
    }

    [Fact]
    public async Task FullRefund_EndsOnlySubscriptionCreatedByRefundedPayment()
    {
        await using var fixture = await Fixture.CreateAsync();
        var paymentId = await fixture.AddPaymentAsync(withActiveSubscription: true);
        var instruction = await fixture.Service.RequestAsync(7, paymentId, 100m, "full-1");

        var completed = await fixture.Service.DispatchAsync(instruction.Id);

        Assert.Equal("Tamamlandi", completed.Durum);
        await using var db = fixture.Factory.CreateDbContext();
        Assert.Equal(PaymentTransactionStates.Refunded, (await db.OdemeIslemleri.SingleAsync(x => x.Id == paymentId)).Durum);
        Assert.Equal("IadeEdildi", (await db.Abonelikler.SingleAsync(x => x.SaglayiciAbonelikId == fixture.OrderFor(paymentId))).Durum);
    }

    [Fact]
    public async Task RefundOfOlderCharge_DoesNotEndNewerActiveSubscription()
    {
        await using var fixture = await Fixture.CreateAsync();
        var paymentId = await fixture.AddPaymentAsync(withActiveSubscription: true, olderSubscription: true);
        var instruction = await fixture.Service.RequestAsync(7, paymentId, 100m, "older-full");

        var completed = await fixture.Service.DispatchAsync(instruction.Id);

        Assert.Equal("Tamamlandi", completed.Durum);
        await using var db = fixture.Factory.CreateDbContext();
        Assert.Equal("IadeEdildi", (await db.Abonelikler.SingleAsync(x => x.SaglayiciAbonelikId == fixture.OrderFor(paymentId))).Durum);
        Assert.Equal("Aktif", (await db.Abonelikler.SingleAsync(x => x.SaglayiciAbonelikId == "newer-order")).Durum);
    }

    [Fact]
    public async Task RefundUpgradeAfterOlderRefund_DoesNotRestoreRefundedSubscriptionPeriod()
    {
        await using var fixture = await Fixture.CreateAsync();
        var olderPaymentId = await fixture.AddPaymentAsync(withActiveSubscription: true);
        var olderRefund = await fixture.Service.RequestAsync(7, olderPaymentId, 100m, "older-refund-first");
        await fixture.Service.DispatchAsync(olderRefund.Id);

        var upgradePaymentId = await fixture.AddPaymentAsync(type: PaymentTransactionTypes.PlanUpgrade);
        await fixture.AddSubscriptionAsync(7, fixture.OrderFor(upgradePaymentId), "Aktif");
        var upgradeRefund = await fixture.Service.RequestAsync(7, upgradePaymentId, 100m, "upgrade-refund-second");
        var completed = await fixture.Service.DispatchAsync(upgradeRefund.Id);

        Assert.Equal("Tamamlandi", completed.Durum);
        await using var db = fixture.Factory.CreateDbContext();
        Assert.Equal("IadeEdildi", (await db.Abonelikler.SingleAsync(x =>
            x.SaglayiciAbonelikId == fixture.OrderFor(olderPaymentId))).Durum);
        Assert.Equal("IadeEdildi", (await db.Abonelikler.SingleAsync(x =>
            x.SaglayiciAbonelikId == fixture.OrderFor(upgradePaymentId))).Durum);
        Assert.DoesNotContain(await db.Abonelikler.ToListAsync(), x => x.Durum == "Aktif");
    }

    [Fact]
    public async Task RefundUpgrade_RestoresPreviousNonRefundedPeriod()
    {
        await using var fixture = await Fixture.CreateAsync();
        var paymentId = await fixture.AddPaymentAsync(type: PaymentTransactionTypes.PlanUpgrade);
        await fixture.AddSubscriptionAsync(7, "previous-order", "Degistirildi");
        await fixture.AddSubscriptionAsync(7, fixture.OrderFor(paymentId), "Aktif");
        var instruction = await fixture.Service.RequestAsync(7, paymentId, 100m, "upgrade-restore");

        var completed = await fixture.Service.DispatchAsync(instruction.Id);

        Assert.Equal("Tamamlandi", completed.Durum);
        await using var db = fixture.Factory.CreateDbContext();
        Assert.Equal("IadeEdildi", (await db.Abonelikler.SingleAsync(x =>
            x.SaglayiciAbonelikId == fixture.OrderFor(paymentId))).Durum);
        Assert.Equal("Aktif", (await db.Abonelikler.SingleAsync(x => x.SaglayiciAbonelikId == "previous-order")).Durum);
    }

    [Fact]
    public async Task AmbiguousProviderResult_StaysPendingBlocksNewRefundAndCanReconcileWithoutResend()
    {
        await using var fixture = await Fixture.CreateAsync();
        var paymentId = await fixture.AddPaymentAsync();
        fixture.Provider.AutoConfirmRefund = false;
        fixture.Provider.RefundResult = new ProviderRefundResult(false, false, "timeout");
        var instruction = await fixture.Service.RequestAsync(7, paymentId, 30m, "ambiguous-1");

        var pending = await fixture.Service.DispatchAsync(instruction.Id);
        var repeatedDispatch = await fixture.Service.DispatchAsync(instruction.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.RequestAsync(7, paymentId, 10m, "blocked-while-pending"));
        fixture.Provider.Confirm(instruction.ReferansNo, 30m);
        var reconciled = await fixture.Service.ReconcileAsync(instruction.Id);

        Assert.Equal("SonucBekliyor", pending.Durum);
        Assert.Equal("SonucBekliyor", repeatedDispatch.Durum);
        Assert.Equal("Tamamlandi", reconciled.Durum);
        Assert.Equal(1, fixture.Provider.RefundCalls);
        await using var db = fixture.Factory.CreateDbContext();
        Assert.Single(await db.OdemeOlaylari.ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Reconcile_ConflictingReferenceOrAmountMovesRefundToReview(bool duplicateReference)
    {
        await using var fixture = await Fixture.CreateAsync();
        var paymentId = await fixture.AddPaymentAsync();
        fixture.Provider.AutoConfirmRefund = false;
        fixture.Provider.RefundResult = new ProviderRefundResult(true, false);
        var instruction = await fixture.Service.RequestAsync(7, paymentId, 20m, "review-1");
        await fixture.Service.DispatchAsync(instruction.Id);
        fixture.Provider.Confirm(instruction.ReferansNo, duplicateReference ? 20m : 19m,
            duplicateReference ? 2 : 1);

        var reviewed = await fixture.Service.ReconcileAsync(instruction.Id);

        Assert.Equal("IncelemeGerekli", reviewed.Durum);
        Assert.Equal(1, fixture.Provider.RefundCalls);
        await using var db = fixture.Factory.CreateDbContext();
        Assert.Empty(await db.OdemeOlaylari.ToListAsync());
        Assert.Equal(PaymentTransactionStates.Succeeded, (await db.OdemeIslemleri.SingleAsync(x => x.Id == paymentId)).Durum);
    }

    [Theory]
    [InlineData("amount")]
    [InlineData("test-mode")]
    public async Task Dispatch_PreflightMismatchRequiresReviewWithoutCallingRefundProvider(string mismatch)
    {
        await using var fixture = await Fixture.CreateAsync();
        var paymentId = await fixture.AddPaymentAsync();
        var instruction = await fixture.Service.RequestAsync(7, paymentId, 20m, "preflight-1");
        fixture.Provider.PaymentSnapshot = mismatch == "amount"
            ? fixture.Provider.PaymentSnapshot with { Amount = 99m, TotalAmount = 99m }
            : fixture.Provider.PaymentSnapshot with { IsTestPayment = false };

        var reviewed = await fixture.Service.DispatchAsync(instruction.Id);

        Assert.Equal("IncelemeGerekli", reviewed.Durum);
        Assert.Equal(0, fixture.Provider.RefundCalls);
        await using var db = fixture.Factory.CreateDbContext();
        Assert.Equal(PaymentTransactionStates.Succeeded, (await db.OdemeIslemleri.SingleAsync(x => x.Id == paymentId)).Durum);
        Assert.Empty(await db.OdemeOlaylari.ToListAsync());
    }

    [Fact]
    public async Task DailyReconciliation_MatchesConfirmedPartialRefundWithoutMismatch()
    {
        await using var fixture = await Fixture.CreateAsync();
        var paymentId = await fixture.AddPaymentAsync();
        var instruction = await fixture.Service.RequestAsync(7, paymentId, 20m, "daily-partial-1");
        await fixture.Service.DispatchAsync(instruction.Id);

        var result = await new PaymentReconciliationService(fixture.Factory, fixture.Provider)
            .ReconcileAsync(DateTime.UtcNow);

        Assert.Equal(1, result.CheckedPayments);
        Assert.Equal(0, result.DiscrepancyCount);
        Assert.Equal(0, result.RecordedFindings);
        Assert.Equal(1, fixture.Provider.RefundCalls);
        await using var db = fixture.Factory.CreateDbContext();
        Assert.Equal(PaymentTransactionStates.Succeeded, (await db.OdemeIslemleri.SingleAsync(x => x.Id == paymentId)).Durum);
        Assert.Equal("Tamamlandi", (await db.OdemeIadeTalimatlari.SingleAsync()).Durum);
        Assert.DoesNotContain(await db.OdemeOlaylari.ToListAsync(), x => x.OlayTipi == "payment.reconciliation.mismatch");
    }

    [Fact]
    public async Task DailyReconciliation_RecoversPendingRefundFromQueryWithoutResendingOrChangingPayment()
    {
        await using var fixture = await Fixture.CreateAsync();
        var paymentId = await fixture.AddPaymentAsync(withActiveSubscription: true);
        fixture.Provider.AutoConfirmRefund = false;
        fixture.Provider.RefundResult = new ProviderRefundResult(true, false);
        var instruction = await fixture.Service.RequestAsync(7, paymentId, 20m, "daily-recovery-1");
        await fixture.Service.DispatchAsync(instruction.Id);
        fixture.Provider.Confirm(instruction.ReferansNo, instruction.Tutar);

        var result = await new PaymentReconciliationService(fixture.Factory, fixture.Provider)
            .ReconcileAsync(DateTime.UtcNow);

        Assert.Equal(1, result.CheckedPayments);
        Assert.Equal(0, result.DiscrepancyCount);
        Assert.Equal(0, result.RecordedFindings);
        Assert.Equal(1, fixture.Provider.RefundCalls);
        await using var db = fixture.Factory.CreateDbContext();
        Assert.Equal("Tamamlandi", (await db.OdemeIadeTalimatlari.SingleAsync()).Durum);
        Assert.Equal(PaymentTransactionStates.Succeeded, (await db.OdemeIslemleri.SingleAsync(x => x.Id == paymentId)).Durum);
        Assert.Equal("Aktif", (await db.Abonelikler.SingleAsync()).Durum);
        Assert.Single(await db.OdemeOlaylari.ToListAsync());
    }

    [Fact]
    public async Task CancelledProviderCall_RemainsDurablePendingAndIsNotResent()
    {
        await using var fixture = await Fixture.CreateAsync();
        var paymentId = await fixture.AddPaymentAsync();
        var instruction = await fixture.Service.RequestAsync(7, paymentId, 20m, "cancelled-1");
        using var cancellation = new CancellationTokenSource();
        fixture.Provider.CancelDuringRefund = cancellation;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            fixture.Service.DispatchAsync(instruction.Id, cancellation.Token));
        var replay = await fixture.Service.DispatchAsync(instruction.Id);

        Assert.Equal("SonucBekliyor", replay.Durum);
        Assert.Equal(1, fixture.Provider.RefundCalls);
        await using var db = fixture.Factory.CreateDbContext();
        Assert.Equal("SonucBekliyor", (await db.OdemeIadeTalimatlari.SingleAsync()).Durum);
        Assert.Equal(PaymentTransactionStates.Succeeded, (await db.OdemeIslemleri.SingleAsync(x => x.Id == paymentId)).Durum);
    }

    [Fact]
    public async Task DefinitiveRejection_FreesReservedAmountButDoesNotRetrySameKey()
    {
        await using var fixture = await Fixture.CreateAsync();
        var paymentId = await fixture.AddPaymentAsync();
        fixture.Provider.RefundResult = new ProviderRefundResult(false, true, "declined");
        var rejected = await fixture.Service.RequestAsync(7, paymentId, 70m, "rejected-1");

        var firstResult = await fixture.Service.DispatchAsync(rejected.Id);
        var sameKeyReplay = await fixture.Service.RequestAsync(7, paymentId, 70m, "rejected-1");
        var sameKeyDispatch = await fixture.Service.DispatchAsync(rejected.Id);
        var replacement = await fixture.Service.RequestAsync(7, paymentId, 70m, "replacement-1");

        Assert.Equal("KesinBasarisiz", firstResult.Durum);
        Assert.Equal(rejected.Id, sameKeyReplay.Id);
        Assert.Equal("KesinBasarisiz", sameKeyDispatch.Durum);
        Assert.NotEqual(rejected.Id, replacement.Id);
        Assert.Equal(1, fixture.Provider.RefundCalls);
        await using var db = fixture.Factory.CreateDbContext();
        Assert.Equal(2, await db.OdemeIadeTalimatlari.CountAsync());
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
        private readonly DbContextOptions<CashTrackerDbContext> _options;
        private readonly Dictionary<int, string> _orderIds = new();
        public SingleDbContextFactory Factory { get; }
        public RefundProvider Provider { get; } = new();
        public PaymentRefundService Service { get; }

        private Fixture(Microsoft.Data.Sqlite.SqliteConnection connection, DbContextOptions<CashTrackerDbContext> options)
        {
            _connection = connection;
            _options = options;
            Factory = new SingleDbContextFactory(options);
            Service = new PaymentRefundService(Factory, Provider);
        }

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite(connection).Options;
            await using (var db = new CashTrackerDbContext(options)) await db.Database.EnsureCreatedAsync();
            return new Fixture(connection, options);
        }

        public async Task<int> AddPaymentAsync(int businessId = 7, string type = PaymentTransactionTypes.SubscriptionStart,
            bool withActiveSubscription = false, bool olderSubscription = false)
        {
            await using var db = new CashTrackerDbContext(_options);
            var payment = new OdemeIslemi
            {
                IsletmeId = businessId,
                CheckoutAnahtari = $"checkout-{Guid.NewGuid():N}",
                OdemeSaglayici = "PayTR",
                SaglayiciOturumId = $"order{DateTime.UtcNow.Ticks}{Guid.NewGuid():N}"[..30],
                SaglayiciIslemId = string.Empty,
                IslemTipi = type,
                Durum = PaymentTransactionStates.Succeeded,
                ToplamTutar = 100m,
                NetTutar = 83.33m,
                KdvOrani = 20m,
                KdvTutar = 16.67m,
                ParaBirimi = "TRY",
                TamamlandiAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            // The refund provider is keyed by the exact persisted order reference.
            payment.SaglayiciIslemId = payment.SaglayiciOturumId;
            Provider.PaymentOrderId = payment.SaglayiciOturumId;
            Provider.PaymentSnapshot = new ProviderPaymentSnapshot(payment.SaglayiciOturumId, 100m, 100m, "TRY", true, 0m, []);
            db.OdemeIslemleri.Add(payment);
            await db.SaveChangesAsync();
            _orderIds[payment.Id] = payment.SaglayiciOturumId;
            if (withActiveSubscription)
            {
                if (olderSubscription)
                    db.Abonelikler.Add(new Abonelik
                    {
                        IsletmeId = businessId, HesapTipi = payment.HesapTipi, PlanKodu = "old-plan",
                        Durum = "Degistirildi", OdemeSaglayici = "PayTR", SaglayiciAbonelikId = payment.SaglayiciIslemId,
                        DonemBaslangicAt = DateTime.UtcNow.AddMonths(-1), DonemBitisAt = DateTime.UtcNow.AddDays(1)
                    });
                db.Abonelikler.Add(new Abonelik
                {
                    IsletmeId = businessId, HesapTipi = payment.HesapTipi, PlanKodu = "current-plan",
                    Durum = "Aktif", OdemeSaglayici = "PayTR",
                    SaglayiciAbonelikId = olderSubscription ? "newer-order" : payment.SaglayiciIslemId,
                    DonemBaslangicAt = DateTime.UtcNow.AddDays(-1), DonemBitisAt = DateTime.UtcNow.AddDays(29)
                });
                await db.SaveChangesAsync();
            }
            return payment.Id;
        }

        public string OrderFor(int paymentId) => _orderIds[paymentId];

        public async Task AddSubscriptionAsync(int businessId, string providerSubscriptionId, string state)
        {
            await using var db = new CashTrackerDbContext(_options);
            db.Abonelikler.Add(new Abonelik
            {
                IsletmeId = businessId, HesapTipi = "Isletme", PlanKodu = "test-plan", Durum = state,
                OdemeSaglayici = "PayTR", SaglayiciAbonelikId = providerSubscriptionId,
                DonemBaslangicAt = DateTime.UtcNow.AddDays(-1), DonemBitisAt = DateTime.UtcNow.AddDays(29)
            });
            await db.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync() => await _connection.DisposeAsync();
    }

    private sealed class RefundProvider : IPaymentProvider, IPaymentRefundProvider
    {
        public string Name => "PayTR";
        public bool ExpectedTestMode => true;
        public string PaymentOrderId { get; set; } = "order123";
        public ProviderPaymentSnapshot PaymentSnapshot { get; set; } = new("order123", 100m, 100m, "TRY", true, 0m, []);
        public ProviderRefundResult RefundResult { get; set; } = new(true, false);
        public bool AutoConfirmRefund { get; set; } = true;
        public int RefundCalls { get; private set; }
        public string LastOrderId { get; private set; } = string.Empty;
        public decimal LastRefundAmount { get; private set; }
        public string LastReferenceNo { get; private set; } = string.Empty;
        public CancellationTokenSource? CancelDuringRefund { get; set; }

        public Task<PaymentCheckoutSession> CreateCheckoutAsync(PaymentCheckoutRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public PaymentWebhookVerificationResult VerifyWebhook(PaymentWebhookEnvelope envelope) =>
            PaymentWebhookVerificationResult.Invalid("unused");
        public Task<ProviderPaymentLookupResult> GetPaymentAsync(string providerOrderId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(providerOrderId == PaymentOrderId
                ? new ProviderPaymentLookupResult(true, PaymentSnapshot)
                : new ProviderPaymentLookupResult(true, null, "004"));
        }
        public Task<ProviderRefundResult> RefundAsync(string orderId, decimal amount, string referenceNo, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            RefundCalls++;
            LastOrderId = orderId;
            LastRefundAmount = amount;
            LastReferenceNo = referenceNo;
            if (CancelDuringRefund is { } cancellation)
            {
                cancellation.Cancel();
                throw new OperationCanceledException(ct);
            }
            if (RefundResult.Accepted && AutoConfirmRefund) Confirm(referenceNo, amount);
            return Task.FromResult(RefundResult);
        }
        public void Confirm(string referenceNo, decimal amount, int copies = 1)
        {
            var refunds = PaymentSnapshot.Refunds?.ToList() ?? [];
            for (var i = 0; i < copies; i++) refunds.Add(new ProviderRefundSnapshot(referenceNo, amount));
            PaymentSnapshot = PaymentSnapshot with
            {
                RefundedAmount = refunds.Sum(x => x.Amount),
                Refunds = refunds
            };
        }
    }
}
