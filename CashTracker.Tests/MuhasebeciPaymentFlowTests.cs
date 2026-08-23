using System.Text.Json;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Payments;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests;

public sealed class MuhasebeciPaymentFlowTests
{
    private const string Secret = "accountant-payment-test-secret";

    [Fact]
    public async Task SuccessfulWebhook_CreatesOneConnectionAndOneMonthlyPayable()
    {
        using var fixture = await PaymentFixture.CreateAsync();
        var first = await fixture.BeginCheckoutAsync("accountant-checkout-001");
        var replay = await fixture.BeginCheckoutAsync("accountant-checkout-001");
        var paidAt = new DateTime(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);

        var paid = await fixture.SendEventAsync("evt-accountant-paid", PaymentEventTypes.PaymentSucceeded, first.AylikHizmetBedeli, paidAt);
        var duplicate = await fixture.SendEventAsync("evt-accountant-paid", PaymentEventTypes.PaymentSucceeded, first.AylikHizmetBedeli, paidAt);
        var semanticReplay = await fixture.SendEventAsync("evt-accountant-paid-replay", PaymentEventTypes.PaymentSucceeded, first.AylikHizmetBedeli, paidAt.AddSeconds(1));

        Assert.True(replay.Reused);
        Assert.True(paid.Accepted);
        Assert.True(duplicate.Duplicate);
        Assert.True(semanticReplay.Accepted);
        await using var db = fixture.CreateDbContext();
        Assert.Equal(1, await db.OdemeIslemleri.CountAsync());
        Assert.Equal(1, await db.MuhasebeciMusterileri.CountAsync());
        Assert.Equal(1, await db.MuhasebeciAktarimAlacaklari.CountAsync());
        Assert.Empty(await db.Abonelikler.ToListAsync());
        Assert.Equal(MuhasebeciTalepDurumlari.Kabul, (await db.MuhasebeciMusteriTalepleri.SingleAsync()).Durum);
        var payable = await db.MuhasebeciAktarimAlacaklari.SingleAsync();
        Assert.Equal("2026-08", payable.AktarimDonemi);
        Assert.Equal(2_500m, payable.AktarilacakTutar);
        Assert.Equal(MuhasebeciAktarimDurumlari.Bekliyor, payable.Durum);
    }

    [Fact]
    public async Task WrongAmountOrOtherTenant_CannotActivateOrReadPayment()
    {
        using var fixture = await PaymentFixture.CreateAsync();
        await fixture.BeginCheckoutAsync("accountant-checkout-002");

        var rejected = await fixture.SendEventAsync("evt-wrong-amount", PaymentEventTypes.PaymentSucceeded, 1m, DateTime.UtcNow);
        var tenantError = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.PaymentService.GetAsync(fixture.RequestId, fixture.CustomerId + 999));

        Assert.False(rejected.Accepted);
        Assert.Contains("bulunamadı", tenantError.Message, StringComparison.OrdinalIgnoreCase);
        await using var db = fixture.CreateDbContext();
        Assert.Empty(await db.MuhasebeciMusterileri.ToListAsync());
        Assert.Empty(await db.MuhasebeciAktarimAlacaklari.ToListAsync());
    }

    [Fact]
    public async Task Refund_DeactivatesConnectionAndCreatesPayableReversal()
    {
        using var fixture = await PaymentFixture.CreateAsync();
        var checkout = await fixture.BeginCheckoutAsync("accountant-checkout-003");
        var paidAt = new DateTime(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);
        await fixture.SendEventAsync("evt-refund-paid", PaymentEventTypes.PaymentSucceeded, checkout.AylikHizmetBedeli, paidAt);

        var refunded = await fixture.SendEventAsync("evt-refund", PaymentEventTypes.PaymentRefunded, checkout.AylikHizmetBedeli, paidAt.AddDays(1));

        Assert.True(refunded.Accepted);
        await using var db = fixture.CreateDbContext();
        Assert.Equal("Pasif", (await db.MuhasebeciMusterileri.SingleAsync()).Durum);
        Assert.Equal(MuhasebeciHizmetOdemeDurumlari.IadeEdildi, (await db.MuhasebeciHizmetOdemeleri.SingleAsync()).Durum);
        Assert.Equal(MuhasebeciAktarimDurumlari.TersKayit, (await db.MuhasebeciAktarimAlacaklari.SingleAsync()).Durum);
    }

    [Fact]
    public async Task AdminMonthlyTransfer_IsSummarizedAndCompletedIdempotently()
    {
        using var fixture = await PaymentFixture.CreateAsync();
        var checkout = await fixture.BeginCheckoutAsync("accountant-checkout-004");
        await fixture.SendEventAsync("evt-transfer-paid", PaymentEventTypes.PaymentSucceeded, checkout.AylikHizmetBedeli,
            new DateTime(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc));
        await using (var db = fixture.CreateDbContext())
        {
            var secondPayment = new MuhasebeciHizmetOdemesi
            {
                TalepId = fixture.RequestId + 100,
                MuhasebeciIsletmeId = fixture.AccountantId,
                MusteriIsletmeId = fixture.CustomerId,
                AylikHizmetBedeli = 500m,
                Durum = MuhasebeciHizmetOdemeDurumlari.TahsilEdildi,
                TahsilEdilenTutar = 500m,
                TahsilEdildiAt = new DateTime(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc)
            };
            db.MuhasebeciHizmetOdemeleri.Add(secondPayment);
            await db.SaveChangesAsync();
            db.MuhasebeciAktarimAlacaklari.Add(new MuhasebeciAktarimAlacagi
            {
                MuhasebeciHizmetOdemesiId = secondPayment.Id,
                MuhasebeciIsletmeId = fixture.AccountantId,
                MusteriIsletmeId = fixture.CustomerId,
                TalepId = secondPayment.TalepId,
                TahsilEdilenTutar = 500m,
                AktarilacakTutar = 500m,
                AktarimDonemi = "2026-08",
                Durum = MuhasebeciAktarimDurumlari.Bekliyor,
                AktarimReferansi = $"pending-{secondPayment.Id}",
                TahakkukAt = new DateTime(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc)
            });
            await db.SaveChangesAsync();
        }
        var admin = new StaticCurrentUserContext("admin-user", "admin@systemcel.test");
        var management = new SystemcelYonetimService(
            fixture.Factory,
            admin,
            new SystemcelYonetimOptions { AdminClerkUserIds = "admin-user" });

        var list = await management.GetMuhasebeciAktarimlariAsync("2026-08");
        var first = await management.CompleteMuhasebeciAktarimiAsync(fixture.AccountantId,
            new MuhasebeciAktarimTamamlaRequest { AktarimDonemi = "2026-08", AktarimReferansi = "bank-ref-2026-08-001" });
        var replay = await management.CompleteMuhasebeciAktarimiAsync(fixture.AccountantId,
            new MuhasebeciAktarimTamamlaRequest { AktarimDonemi = "2026-08", AktarimReferansi = "bank-ref-2026-08-001" });

        Assert.Single(list.Aktarimlar);
        Assert.Equal(2, list.Aktarimlar[0].AlacakSayisi);
        Assert.Equal(3_000m, list.Aktarimlar[0].AktarilacakTutar);
        Assert.Equal(MuhasebeciAktarimDurumlari.Aktarildi, first.Durum);
        Assert.Equal(first.AktarimReferansi, replay.AktarimReferansi);
        await Assert.ThrowsAsync<InvalidOperationException>(() => management.CompleteMuhasebeciAktarimiAsync(
            fixture.AccountantId,
            new MuhasebeciAktarimTamamlaRequest { AktarimDonemi = "2026-08", AktarimReferansi = "bank-ref-different" }));
    }

    [Fact]
    public async Task PendingPayment_AllowsTextChatButBlocksFinancialDataRequestAndShare()
    {
        using var fixture = await PaymentFixture.CreateAsync();
        int conversationId;
        await using (var db = fixture.CreateDbContext())
        {
            var conversation = new MuhasebeciSohbet
            {
                MuhasebeciIsletmeId = fixture.AccountantId,
                MusteriIsletmeId = fixture.CustomerId,
                TalepId = fixture.RequestId,
                Konu = "Ödeme bekleyen teklif"
            };
            db.MuhasebeciSohbetleri.Add(conversation);
            await db.SaveChangesAsync();
            conversationId = conversation.Id;
        }
        var business = new FakeIsletmeService
        {
            Active = new Isletme { Id = fixture.AccountantId, Ad = "Ada Muhasebe", TenantTipi = HesapTipleri.Muhasebeci, IsAktif = true }
        };
        var service = new MuhasebeciSohbetMerkeziService(
            fixture.Factory,
            business,
            new MuhasebeciSohbetStorageOptions { AppDataPath = fixture.StoragePath });

        var text = await service.MesajGonderAsync(conversationId,
            new MuhasebeciSohbetMesajiOlusturRequest { Mesaj = "Ödeme sonrası başlayabiliriz.", ClientMessageId = "pending-chat-001" });
        var requestError = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.VeriIstegiOlusturAsync(conversationId, new MuhasebeciSohbetVeriIstegiRequest()));
        business.Active = new Isletme { Id = fixture.CustomerId, Ad = "Bahar Kafe", TenantTipi = HesapTipleri.Isletme, IsAktif = true };
        var shareError = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.VeriPaylasAsync(conversationId, new MuhasebeciSohbetVeriPaylasimiRequest()));

        Assert.Contains("Ödeme sonrası", text.Mesaj);
        Assert.Contains("aktif bağlantı", requestError.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("aktif bağlantı", shareError.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class PaymentFixture : IDisposable
    {
        private readonly FakePaymentProvider _provider = new(Secret);
        private readonly string _dbPath;
        private string _checkoutKey = string.Empty;

        private PaymentFixture(string dbPath, DbContextOptions<CashTrackerDbContext> options)
        {
            _dbPath = dbPath;
            Options = options;
            Factory = new SingleDbContextFactory(options);
            PaymentService = new MuhasebeciOdemeService(Factory, _provider);
            Lifecycle = new SubscriptionLifecycleService(Factory, _provider, new PaymentPricingService());
            StoragePath = Path.Combine(Path.GetTempPath(), $"systemcel_accountant_chat_{Guid.NewGuid():N}");
        }

        public DbContextOptions<CashTrackerDbContext> Options { get; }
        public SingleDbContextFactory Factory { get; }
        public MuhasebeciOdemeService PaymentService { get; }
        public SubscriptionLifecycleService Lifecycle { get; }
        public int AccountantId { get; private set; }
        public int CustomerId { get; private set; }
        public int RequestId { get; private set; }
        public string StoragePath { get; }

        public static async Task<PaymentFixture> CreateAsync()
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"systemcel_accountant_payment_{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite($"Data Source={dbPath}").Options;
            var fixture = new PaymentFixture(dbPath, options);
            await using var db = fixture.CreateDbContext();
            await db.Database.EnsureCreatedAsync();
            var accountant = new Isletme { Ad = "Ada Muhasebe", TenantTipi = HesapTipleri.Muhasebeci, IsAktif = true };
            var customer = new Isletme { Ad = "Bahar Kafe", TenantTipi = HesapTipleri.Isletme, IsAktif = true };
            db.Isletmeler.AddRange(accountant, customer);
            await db.SaveChangesAsync();
            var request = new MuhasebeciMusteriTalebi
            {
                MuhasebeciIsletmeId = accountant.Id,
                MusteriIsletmeId = customer.Id,
                TalepEdenIsletmeId = customer.Id,
                Tur = MuhasebeciTalepTurleri.Pazaryeri,
                Durum = MuhasebeciTalepDurumlari.OdemeBekliyor,
                YetkiSeviyesi = MuhasebeciYetkiSeviyeleri.TamIslem,
                AylikHizmetBedeli = 2_500m
            };
            db.MuhasebeciMusteriTalepleri.Add(request);
            await db.SaveChangesAsync();
            db.MuhasebeciHizmetOdemeleri.Add(new MuhasebeciHizmetOdemesi
            {
                TalepId = request.Id,
                MuhasebeciIsletmeId = accountant.Id,
                MusteriIsletmeId = customer.Id,
                AylikHizmetBedeli = request.AylikHizmetBedeli,
                Durum = MuhasebeciHizmetOdemeDurumlari.OdemeBekliyor
            });
            await db.SaveChangesAsync();
            fixture.AccountantId = accountant.Id;
            fixture.CustomerId = customer.Id;
            fixture.RequestId = request.Id;
            return fixture;
        }

        public CashTrackerDbContext CreateDbContext() => new(Options);

        public Task<MuhasebeciOdemeCheckoutResult> BeginCheckoutAsync(string key)
        {
            _checkoutKey = key;
            return PaymentService.BeginCheckoutAsync(new MuhasebeciOdemeCheckoutCommand(
                RequestId,
                CustomerId,
                key,
                "customer-user",
                "customer@systemcel.test",
                new Uri("https://systemcel.test/payment/success"),
                new Uri("https://systemcel.test/payment/failure"),
                new Uri("https://systemcel.test/api/odeme/webhook")));
        }

        public Task<PaymentWebhookProcessingResult> SendEventAsync(string eventId, string eventType, decimal amount, DateTime occurredAt)
        {
            var payload = JsonSerializer.Serialize(new
            {
                eventId,
                eventType,
                merchantReference = _checkoutKey,
                providerTransactionId = $"accountant-provider-tx-{RequestId}",
                amount,
                currency = "TRY",
                occurredAt
            });
            return Lifecycle.ProcessWebhookAsync(new PaymentWebhookEnvelope(payload, _provider.SignPayload(payload)));
        }

        public void Dispose()
        {
            try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { }
            try { if (Directory.Exists(StoragePath)) Directory.Delete(StoragePath, true); } catch { }
        }
    }

    private sealed class StaticCurrentUserContext(string userId, string email) : ICurrentUserContext
    {
        public CurrentUserIdentity? GetCurrentUser() => new(userId, email, "Admin");
    }
}
