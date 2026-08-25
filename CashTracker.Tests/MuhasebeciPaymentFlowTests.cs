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
    private static readonly string CurrentPeriod = DateTime.UtcNow.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public async Task SuccessfulWebhook_CreatesOneConnectionAndOneMonthlyPayable()
    {
        using var fixture = await PaymentFixture.CreateAsync();
        var first = await fixture.BeginCheckoutAsync("accountant-checkout-001");
        var replay = await fixture.BeginCheckoutAsync("accountant-checkout-001");
        var paidAt = DateTime.UtcNow;

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
        Assert.Equal(CurrentPeriod, payable.AktarimDonemi);
        Assert.Equal(250m, payable.PlatformKomisyonTutari);
        Assert.Equal(2_250m, payable.AktarilacakTutar);
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
        var paidAt = DateTime.UtcNow;
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
            DateTime.UtcNow);
        await using (var db = fixture.CreateDbContext())
        {
            var secondPayment = new MuhasebeciHizmetOdemesi
            {
                TalepId = fixture.RequestId + 100,
                MuhasebeciIsletmeId = fixture.AccountantId,
                MusteriIsletmeId = fixture.CustomerId,
                HizmetDonemi = CurrentPeriod,
                VadeAt = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                AylikHizmetBedeli = 500m,
                PlatformKomisyonOrani = 10m,
                Durum = MuhasebeciHizmetOdemeDurumlari.TahsilEdildi,
                TahsilEdilenTutar = 500m,
                PlatformKomisyonTutari = 50m,
                AktarilacakTutar = 450m,
                TahsilEdildiAt = DateTime.UtcNow
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
                PlatformKomisyonTutari = 50m,
                AktarilacakTutar = 450m,
                AktarimDonemi = CurrentPeriod,
                Durum = MuhasebeciAktarimDurumlari.Bekliyor,
                AktarimReferansi = $"pending-{secondPayment.Id}",
                TahakkukAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
        var admin = new StaticCurrentUserContext("admin-user", "admin@systemcel.test");
        var management = new SystemcelYonetimService(
            fixture.Factory,
            admin,
            new SystemcelYonetimOptions { AdminClerkUserIds = "admin-user" });

        var list = await management.GetMuhasebeciAktarimlariAsync(CurrentPeriod);
        var first = await management.CompleteMuhasebeciAktarimiAsync(fixture.AccountantId,
            new MuhasebeciAktarimTamamlaRequest { AktarimDonemi = CurrentPeriod, AktarimReferansi = "bank-ref-current-001" });
        var replay = await management.CompleteMuhasebeciAktarimiAsync(fixture.AccountantId,
            new MuhasebeciAktarimTamamlaRequest { AktarimDonemi = CurrentPeriod, AktarimReferansi = "bank-ref-current-001" });

        Assert.Single(list.Aktarimlar);
        Assert.Equal(2, list.Aktarimlar[0].AlacakSayisi);
        Assert.Equal(3_000m, list.Aktarimlar[0].TahsilEdilenTutar);
        Assert.Equal(300m, list.Aktarimlar[0].PlatformKomisyonTutari);
        Assert.Equal(2_700m, list.Aktarimlar[0].AktarilacakTutar);
        Assert.Equal(MuhasebeciAktarimDurumlari.Aktarildi, first.Durum);
        Assert.Equal(first.AktarimReferansi, replay.AktarimReferansi);
        await Assert.ThrowsAsync<InvalidOperationException>(() => management.CompleteMuhasebeciAktarimiAsync(
            fixture.AccountantId,
            new MuhasebeciAktarimTamamlaRequest { AktarimDonemi = CurrentPeriod, AktarimReferansi = "bank-ref-different" }));
    }

    [Fact]
    public async Task RefundAfterTransfer_CreatesNegativeCarryForwardAndNetsNextPayout()
    {
        using var fixture = await PaymentFixture.CreateAsync();
        var checkout = await fixture.BeginCheckoutAsync("accountant-checkout-clawback");
        var paidAt = DateTime.UtcNow;
        await fixture.SendEventAsync("evt-clawback-paid", PaymentEventTypes.PaymentSucceeded, checkout.AylikHizmetBedeli, paidAt);

        var admin = new StaticCurrentUserContext("admin-user", "admin@systemcel.test");
        var management = new SystemcelYonetimService(
            fixture.Factory,
            admin,
            new SystemcelYonetimOptions { AdminClerkUserIds = "admin-user" });
        await management.CompleteMuhasebeciAktarimiAsync(fixture.AccountantId,
            new MuhasebeciAktarimTamamlaRequest
            {
                AktarimDonemi = CurrentPeriod,
                AktarimReferansi = "bank-ref-clawback-original"
            });

        await fixture.SendEventAsync("evt-clawback-refund", PaymentEventTypes.PaymentRefunded,
            checkout.AylikHizmetBedeli, paidAt.AddDays(1));

        await using (var db = fixture.CreateDbContext())
        {
            var original = await db.MuhasebeciAktarimAlacaklari.SingleAsync(x => x.AktarilacakTutar > 0m);
            var adjustment = await db.MuhasebeciAktarimAlacaklari.SingleAsync(x => x.AktarilacakTutar < 0m);
            Assert.Equal(MuhasebeciAktarimDurumlari.Aktarildi, original.Durum);
            Assert.Equal(MuhasebeciAktarimDurumlari.Bekliyor, adjustment.Durum);
            Assert.Equal(-original.AktarilacakTutar, adjustment.AktarilacakTutar);

            var nextPayment = new MuhasebeciHizmetOdemesi
            {
                TalepId = fixture.RequestId + 500,
                MuhasebeciIsletmeId = fixture.AccountantId,
                MusteriIsletmeId = fixture.CustomerId,
                HizmetDonemi = CurrentPeriod,
                VadeAt = paidAt,
                AylikHizmetBedeli = 3_000m,
                PlatformKomisyonOrani = 10m,
                Durum = MuhasebeciHizmetOdemeDurumlari.TahsilEdildi,
                TahsilEdilenTutar = 3_000m,
                PlatformKomisyonTutari = 300m,
                AktarilacakTutar = 2_700m,
                TahsilEdildiAt = paidAt.AddDays(2)
            };
            db.MuhasebeciHizmetOdemeleri.Add(nextPayment);
            await db.SaveChangesAsync();
            db.MuhasebeciAktarimAlacaklari.Add(new MuhasebeciAktarimAlacagi
            {
                MuhasebeciHizmetOdemesiId = nextPayment.Id,
                MuhasebeciIsletmeId = fixture.AccountantId,
                MusteriIsletmeId = fixture.CustomerId,
                TalepId = nextPayment.TalepId,
                TahsilEdilenTutar = 3_000m,
                PlatformKomisyonTutari = 300m,
                AktarilacakTutar = 2_700m,
                AktarimDonemi = CurrentPeriod,
                Durum = MuhasebeciAktarimDurumlari.Bekliyor,
                AktarimReferansi = $"pending-{nextPayment.Id}",
                TahakkukAt = paidAt.AddDays(2)
            });
            await db.SaveChangesAsync();
        }

        var netted = await management.CompleteMuhasebeciAktarimiAsync(fixture.AccountantId,
            new MuhasebeciAktarimTamamlaRequest
            {
                AktarimDonemi = CurrentPeriod,
                AktarimReferansi = "bank-ref-clawback-netted"
            });
        var replay = await management.CompleteMuhasebeciAktarimiAsync(fixture.AccountantId,
            new MuhasebeciAktarimTamamlaRequest
            {
                AktarimDonemi = CurrentPeriod,
                AktarimReferansi = "bank-ref-clawback-netted"
            });

        Assert.Equal(450m, netted.AktarilacakTutar);
        Assert.Equal(450m, replay.AktarilacakTutar);
        await using var verified = fixture.CreateDbContext();
        Assert.Equal(2, await verified.MuhasebeciAktarimAlacaklari.CountAsync(x =>
            x.AktarimReferansi == "bank-ref-clawback-netted" &&
            x.Durum == MuhasebeciAktarimDurumlari.Aktarildi));
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

    [Fact]
    public async Task ActiveConnection_CreatesAndCollectsOnlyOnePaymentForCurrentPeriod()
    {
        using var fixture = await PaymentFixture.CreateAsync();
        await using (var db = fixture.CreateDbContext())
        {
            var initial = await db.MuhasebeciHizmetOdemeleri.SingleAsync();
            initial.HizmetDonemi = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
            var request = await db.MuhasebeciMusteriTalepleri.SingleAsync();
            request.Durum = MuhasebeciTalepDurumlari.Kabul;
            db.MuhasebeciMusterileri.Add(new MuhasebeciMusteri
            {
                MuhasebeciIsletmeId = fixture.AccountantId,
                MusteriIsletmeId = fixture.CustomerId,
                TalepId = fixture.RequestId,
                Durum = "Aktif",
                YetkiSeviyesi = request.YetkiSeviyesi,
                Kaynak = request.Tur
            });
            await db.SaveChangesAsync();
        }

        var generated = await fixture.PaymentService.EnsureDuePeriodsAsync(DateTime.UtcNow);
        var replayGeneration = await fixture.PaymentService.EnsureDuePeriodsAsync(DateTime.UtcNow);
        var summary = await fixture.PaymentService.GetAsync(fixture.RequestId, fixture.CustomerId);
        var checkout = await fixture.BeginCheckoutAsync("accountant-current-period-001");
        var paid = await fixture.SendEventAsync("evt-current-period-paid", PaymentEventTypes.PaymentSucceeded,
            checkout.AylikHizmetBedeli, DateTime.UtcNow);

        Assert.Equal(CurrentPeriod, summary.HizmetDonemi);
        Assert.Equal(1, generated);
        Assert.Equal(0, replayGeneration);
        Assert.True(summary.OdemeYapilabilir);
        Assert.True(paid.Accepted);
        await using var verify = fixture.CreateDbContext();
        Assert.Equal(2, await verify.MuhasebeciHizmetOdemeleri.CountAsync());
        Assert.Equal(1, await verify.MuhasebeciHizmetOdemeleri.CountAsync(x => x.HizmetDonemi == CurrentPeriod));
        Assert.Equal(1, await verify.MuhasebeciAktarimAlacaklari.CountAsync());
        Assert.Equal(1, await verify.MuhasebeciMusterileri.CountAsync());
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
                HizmetDonemi = CurrentPeriod,
                VadeAt = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                AylikHizmetBedeli = request.AylikHizmetBedeli,
                PlatformKomisyonOrani = 10m,
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
