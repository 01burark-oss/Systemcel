using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Payments;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests;

public sealed class PaytrPaymentReconciliationTests
{
    [Fact]
    public async Task Reconcile_MatchesPersistedSuccessfulPaymentWithoutChangingLedger()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.AddPayment("checkout-ok", "order-ok", PaymentTransactionStates.Succeeded, 360m,
            completedAt: fixture.Now.AddDays(-1));
        fixture.Provider.Set("order-ok", new ProviderPaymentSnapshot("order-ok", 360m, 360m, "TRY", true, 0m));

        var result = await fixture.ReconcileAsync();

        Assert.Equal(1, result.CheckedPayments);
        Assert.Equal(0, result.UnavailablePayments);
        Assert.Equal(0, result.DiscrepancyCount);
        Assert.Equal(0, result.RecordedFindings);
        await using var db = fixture.Factory.CreateDbContext();
        Assert.Empty(await db.OdemeOlaylari.ToListAsync());
        Assert.Equal(PaymentTransactionStates.Succeeded, (await db.OdemeIslemleri.SingleAsync()).Durum);
    }

    [Fact]
    public async Task Reconcile_RecordsExactPaymentMismatchOncePerDayWithoutApplyingProviderResult()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.AddPayment("checkout-a", "order-a", PaymentTransactionStates.CheckoutOpen, 360m,
            expiresAt: fixture.Now.AddHours(-1));
        fixture.Provider.Set("order-a", new ProviderPaymentSnapshot("order-a", 300m, 360m, "TRY", true, 0m));

        var first = await fixture.ReconcileAsync();
        var retry = await fixture.ReconcileAsync(fixture.Now.AddHours(2));
        await using var db = fixture.Factory.CreateDbContext();

        Assert.Equal(1, first.DiscrepancyCount);
        Assert.Equal(1, first.RecordedFindings);
        Assert.Equal(0, retry.RecordedFindings);
        var finding = await db.OdemeOlaylari.SingleAsync();
        Assert.Equal("PayTR", finding.OdemeSaglayici);
        Assert.Equal("payment.reconciliation.mismatch", finding.OlayTipi);
        Assert.Equal("IncelemeGerekli", finding.IslenmeDurumu);
        Assert.Equal("checkout-a", finding.CheckoutAnahtari);
        Assert.Equal("order-a", finding.SaglayiciIslemId);
        Assert.Equal(PaymentTransactionStates.CheckoutOpen, (await db.OdemeIslemleri.SingleAsync()).Durum);
        Assert.Empty(await db.Abonelikler.ToListAsync());
    }

    [Fact]
    public async Task Reconcile_NotFoundIsNotFailureForFreshPendingButFlagsSettledOrExpiredOrders()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.AddPayment("fresh", "order-fresh", PaymentTransactionStates.CheckoutOpen, 100m,
            expiresAt: fixture.Now.AddMinutes(15));
        fixture.AddPayment("paid", "order-paid", PaymentTransactionStates.Succeeded, 200m,
            completedAt: fixture.Now.AddDays(-1));
        fixture.AddPayment("expired", "order-expired", PaymentTransactionStates.CheckoutOpen, 300m,
            expiresAt: fixture.Now.AddMinutes(-5));
        fixture.Provider.SetNotFound("order-fresh");
        fixture.Provider.SetNotFound("order-paid");
        fixture.Provider.SetNotFound("order-expired");

        var result = await fixture.ReconcileAsync();
        await using var db = fixture.Factory.CreateDbContext();
        var findings = await db.OdemeOlaylari.OrderBy(x => x.CheckoutAnahtari).ToListAsync();

        Assert.Equal(3, result.CheckedPayments);
        Assert.Equal(2, result.DiscrepancyCount);
        Assert.Equal(2, findings.Count);
        Assert.DoesNotContain(findings, x => x.CheckoutAnahtari == "fresh");
        Assert.Contains(findings, x => x.CheckoutAnahtari == "paid");
        Assert.Contains(findings, x => x.CheckoutAnahtari == "expired");
        Assert.All(await db.OdemeIslemleri.ToListAsync(), payment =>
            Assert.Contains(payment.Durum, new[] { PaymentTransactionStates.CheckoutOpen, PaymentTransactionStates.Succeeded }));
    }

    [Theory]
    [InlineData("order-amount", "300", "361", "TRY", true, "0")]
    [InlineData("order-currency", "360", "360", "USD", true, "0")]
    [InlineData("order-mode", "360", "360", "TRY", false, "0")]
    [InlineData("order-refund", "360", "360", "TRY", true, "1")]
    public async Task Reconcile_AmountCurrencyModeOrRefundMismatchIsManualReview(
        string orderId, string amountText, string totalText, string currency, bool testMode, string refundedText)
    {
        var amount = decimal.Parse(amountText, System.Globalization.CultureInfo.InvariantCulture);
        var total = decimal.Parse(totalText, System.Globalization.CultureInfo.InvariantCulture);
        var refunded = decimal.Parse(refundedText, System.Globalization.CultureInfo.InvariantCulture);
        await using var fixture = await Fixture.CreateAsync();
        fixture.AddPayment("checkout-" + orderId, orderId, PaymentTransactionStates.Succeeded, 360m,
            completedAt: fixture.Now.AddDays(-1));
        fixture.Provider.Set(orderId, new ProviderPaymentSnapshot(orderId, amount, total, currency, testMode, refunded));

        var result = await fixture.ReconcileAsync();
        await using var db = fixture.Factory.CreateDbContext();

        Assert.Equal(1, result.DiscrepancyCount);
        var finding = await db.OdemeOlaylari.SingleAsync();
        Assert.Equal("IncelemeGerekli", finding.IslenmeDurumu);
        Assert.Equal(PaymentTransactionStates.Succeeded, (await db.OdemeIslemleri.SingleAsync()).Durum);
        Assert.Empty(await db.Abonelikler.ToListAsync());
    }

    [Fact]
    public async Task Reconcile_FailedLocalPaymentWithProviderChargeRequiresManualReview()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.AddPayment("checkout-failed", "order-failed", PaymentTransactionStates.Failed, 360m);
        fixture.Provider.Set("order-failed", new ProviderPaymentSnapshot("order-failed", 360m, 360m, "TRY", true, 0m));

        var result = await fixture.ReconcileAsync();
        await using var db = fixture.Factory.CreateDbContext();

        Assert.Equal(1, result.DiscrepancyCount);
        Assert.Equal("IncelemeGerekli", (await db.OdemeOlaylari.SingleAsync()).IslenmeDurumu);
        Assert.Equal(PaymentTransactionStates.Failed, (await db.OdemeIslemleri.SingleAsync()).Durum);
    }

    [Fact]
    public async Task Reconcile_FullyRefundedPaymentMatchesProviderRefundTotal()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.AddPayment("checkout-refunded", "order-refunded", PaymentTransactionStates.Refunded, 360m,
            completedAt: fixture.Now.AddDays(-1));
        fixture.Provider.Set("order-refunded", new ProviderPaymentSnapshot("order-refunded", 360m, 360m, "TRY", true, 360m));

        var result = await fixture.ReconcileAsync();
        await using var db = fixture.Factory.CreateDbContext();

        Assert.Equal(0, result.DiscrepancyCount);
        Assert.Empty(await db.OdemeOlaylari.ToListAsync());
        Assert.Equal(PaymentTransactionStates.Refunded, (await db.OdemeIslemleri.SingleAsync()).Durum);
    }

    [Fact]
    public async Task Reconcile_UnexpectedProviderOrderReferenceIsManualReview()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.AddPayment("checkout-order", "order-expected", PaymentTransactionStates.Succeeded, 360m,
            completedAt: fixture.Now.AddDays(-1));
        fixture.Provider.Set("order-expected", new ProviderPaymentSnapshot("order-other", 360m, 360m, "TRY", true, 0m));

        var result = await fixture.ReconcileAsync();
        await using var db = fixture.Factory.CreateDbContext();

        Assert.Equal(1, result.DiscrepancyCount);
        var finding = await db.OdemeOlaylari.SingleAsync();
        Assert.Equal("checkout-order", finding.CheckoutAnahtari);
        Assert.Equal("order-expected", finding.SaglayiciIslemId);
        Assert.Equal(PaymentTransactionStates.Succeeded, (await db.OdemeIslemleri.SingleAsync()).Durum);
    }

    [Fact]
    public async Task Reconcile_UnavailableQueryIsCountedAndLeavesPaymentsUntouched()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.AddPayment("checkout", "order", PaymentTransactionStates.CheckoutOpen, 100m,
            expiresAt: fixture.Now.AddDays(-1));
        fixture.Provider.SetUnavailable("order");

        var result = await fixture.ReconcileAsync();
        await using var db = fixture.Factory.CreateDbContext();

        Assert.Equal(0, result.CheckedPayments);
        Assert.Equal(1, result.UnavailablePayments);
        Assert.Equal(0, result.RecordedFindings);
        Assert.Empty(await db.OdemeOlaylari.ToListAsync());
        Assert.Equal(PaymentTransactionStates.CheckoutOpen, (await db.OdemeIslemleri.SingleAsync()).Durum);
    }

    [Fact]
    public async Task Reconcile_QueriesOnlyPaytrOrdersAndFindingsKeepExactOrderAndCheckoutMapping()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.AddPayment("paytr-checkout", "paytr-order", PaymentTransactionStates.Succeeded, 50m,
            completedAt: fixture.Now.AddDays(-1));
        fixture.AddPayment("other-checkout", "other-order", PaymentTransactionStates.Succeeded, 50m,
            provider: "Other", completedAt: fixture.Now.AddDays(-1));
        fixture.Provider.SetNotFound("paytr-order");

        var result = await fixture.ReconcileAsync();
        await using var db = fixture.Factory.CreateDbContext();

        Assert.Equal(new[] { "paytr-order" }, fixture.Provider.RequestedOrders);
        Assert.Equal(1, result.DiscrepancyCount);
        var finding = await db.OdemeOlaylari.SingleAsync();
        Assert.Equal("paytr-checkout", finding.CheckoutAnahtari);
        Assert.Equal("paytr-order", finding.SaglayiciIslemId);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
        private readonly DbContextOptions<CashTrackerDbContext> _options;
        public static readonly DateTime FixedNow = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        public DateTime Now => FixedNow;
        public SingleDbContextFactory Factory { get; }
        public StatusProvider Provider { get; } = new();

        private Fixture(Microsoft.Data.Sqlite.SqliteConnection connection, DbContextOptions<CashTrackerDbContext> options)
        {
            _connection = connection;
            _options = options;
            Factory = new SingleDbContextFactory(options);
        }

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite(connection).Options;
            await using (var db = new CashTrackerDbContext(options)) await db.Database.EnsureCreatedAsync();
            return new Fixture(connection, options);
        }

        public void AddPayment(string checkout, string order, string state, decimal total,
            string provider = "PayTR", DateTime? expiresAt = null, DateTime? completedAt = null)
        {
            using var db = new CashTrackerDbContext(_options);
            db.OdemeIslemleri.Add(new OdemeIslemi
            {
                IsletmeId = 1,
                CheckoutAnahtari = checkout,
                SaglayiciOturumId = order,
                SaglayiciIslemId = "tx-" + order,
                OdemeSaglayici = provider,
                Durum = state,
                IslemTipi = PaymentTransactionTypes.SubscriptionStart,
                ToplamTutar = total,
                NetTutar = total / 1.2m,
                KdvOrani = 20m,
                KdvTutar = total - total / 1.2m,
                ParaBirimi = "TRY",
                CheckoutExpiresAt = expiresAt,
                TamamlandiAt = completedAt,
                CreatedAt = Now.AddDays(-2),
                UpdatedAt = Now.AddDays(-1)
            });
            db.SaveChanges();
        }

        public Task<ProviderReconciliationResult> ReconcileAsync(DateTime? now = null) =>
            new PaymentReconciliationService(Factory, Provider).ReconcileAsync(now ?? Now);
        public async ValueTask DisposeAsync() => await _connection.DisposeAsync();
    }

    private sealed class StatusProvider : IPaymentProvider, IPaymentStatusQueryProvider
    {
        private readonly Dictionary<string, ProviderPaymentLookupResult> _results = new(StringComparer.Ordinal);
        public string Name => "PayTR";
        public bool ExpectedTestMode => true;
        public List<string> RequestedOrders { get; } = [];
        public Task<PaymentCheckoutSession> CreateCheckoutAsync(PaymentCheckoutRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public PaymentWebhookVerificationResult VerifyWebhook(PaymentWebhookEnvelope envelope) =>
            PaymentWebhookVerificationResult.Invalid("not used");
        public void Set(string orderId, ProviderPaymentSnapshot payment) =>
            _results[orderId] = new ProviderPaymentLookupResult(true, payment);
        public void SetNotFound(string orderId) =>
            _results[orderId] = new ProviderPaymentLookupResult(true, null, "004");
        public void SetUnavailable(string orderId) =>
            _results[orderId] = new ProviderPaymentLookupResult(false, null, "network");
        public Task<ProviderPaymentLookupResult> GetPaymentAsync(string providerOrderId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            RequestedOrders.Add(providerOrderId);
            return Task.FromResult(_results.TryGetValue(providerOrderId, out var result)
                ? result : new ProviderPaymentLookupResult(false, null, "not configured"));
        }
    }
}
