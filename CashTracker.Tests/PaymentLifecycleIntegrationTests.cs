using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Payments;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests;

public sealed class PaymentLifecycleIntegrationTests
{
    private const string Secret = "systemcel-fake-payment-secret";

    [Fact]
    public async Task Checkout_IsIdempotent_AndPersistsConsentOnce()
    {
        using var fixture = new PaymentFixture(HesapTipleri.Isletme);
        var command = fixture.CreateCommand("checkout-idempotent", PlanKodlari.IsletmeBaslangic);

        var first = await fixture.Service.BeginCheckoutAsync(command);
        var second = await fixture.Service.BeginCheckoutAsync(command);

        Assert.False(first.Reused);
        Assert.True(second.Reused);
        Assert.Equal(first.PaymentTransactionId, second.PaymentTransactionId);

        await using var db = fixture.Factory.CreateDbContext();
        Assert.Equal(1, await db.OdemeIslemleri.CountAsync());
        Assert.Equal(1, await db.AbonelikOnaylari.CountAsync());
        Assert.Equal(DateTimeKind.Unspecified, (await db.OdemeIslemleri.SingleAsync()).CreatedAt.Kind);
        var consent = await db.AbonelikOnaylari.SingleAsync();
        Assert.NotEmpty(consent.MetinHash);
        Assert.NotEmpty(consent.IstemciIpHash);
        Assert.NotEqual(command.ConsentText, consent.MetinHash);
        Assert.NotEqual(command.ClientIp, consent.IstemciIpHash);
    }

    [Fact]
    public async Task TrialAndPaymentSuccess_AreAppliedOnce_AndOutOfOrderFailureIsIgnored()
    {
        using var fixture = new PaymentFixture(HesapTipleri.Isletme);
        var checkout = await fixture.Service.BeginCheckoutAsync(
            fixture.CreateCommand("checkout-lifecycle", PlanKodlari.IsletmeBuyume));
        var startedAt = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);

        var trial = await fixture.SendEventAsync(
            "evt-trial",
            PaymentEventTypes.TrialAuthorized,
            "checkout-lifecycle",
            "card-1",
            0,
            startedAt);
        var duplicate = await fixture.SendEventAsync(
            "evt-trial",
            PaymentEventTypes.TrialAuthorized,
            "checkout-lifecycle",
            "card-1",
            0,
            startedAt);
        var successAt = startedAt.AddDays(30);
        var success = await fixture.SendEventAsync(
            "evt-success",
            PaymentEventTypes.PaymentSucceeded,
            "checkout-lifecycle",
            "subscription-1",
            checkout.Quote.TotalAmount,
            successAt);
        var sameTransactionAgain = await fixture.SendEventAsync(
            "evt-success-retry",
            PaymentEventTypes.PaymentSucceeded,
            "checkout-lifecycle",
            "subscription-1",
            checkout.Quote.TotalAmount,
            successAt.AddMinutes(1));
        var staleFailure = await fixture.SendEventAsync(
            "evt-stale-failure",
            PaymentEventTypes.PaymentFailed,
            "checkout-lifecycle",
            "subscription-1",
            0,
            successAt.AddMinutes(-1));

        Assert.True(trial.Accepted);
        Assert.True(duplicate.Duplicate);
        Assert.True(success.Accepted);
        Assert.Contains("yoksayildi", sameTransactionAgain.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("yoksayildi", staleFailure.Message, StringComparison.OrdinalIgnoreCase);

        await using var db = fixture.Factory.CreateDbContext();
        var trialRecord = await db.IsletmeDenemeleri.SingleAsync();
        Assert.Equal(30, (trialRecord.BitisAt - trialRecord.BaslangicAt).Days);
        Assert.Equal("Donusturuldu", trialRecord.Durum);
        var subscription = await db.Abonelikler.SingleAsync();
        Assert.Equal("Aktif", subscription.Durum);
        Assert.Null(subscription.OdemeSorunuAt);
        Assert.Equal("subscription-1", subscription.SaglayiciAbonelikId);
        Assert.Equal(1, await db.Abonelikler.CountAsync());
        Assert.Equal(4, await db.OdemeOlaylari.CountAsync());
        Assert.Equal(2, await db.OdemeOlaylari.CountAsync(x => x.IslenmeDurumu == "Yoksayildi"));
    }

    [Fact]
    public async Task AccountantTrial_IsFourteenDays_AndCannotBeGrantedTwice()
    {
        using var fixture = new PaymentFixture(HesapTipleri.Muhasebeci);
        await fixture.Service.BeginCheckoutAsync(
            fixture.CreateCommand("checkout-accountant", PlanKodlari.MuhasebeciStandart, extraCustomerCredits: 2));
        var startedAt = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);

        await fixture.SendEventAsync(
            "evt-accountant-trial",
            PaymentEventTypes.TrialAuthorized,
            "checkout-accountant",
            "accountant-card",
            0,
            startedAt);
        var repeat = await fixture.SendEventAsync(
            "evt-accountant-trial-repeat",
            PaymentEventTypes.TrialAuthorized,
            "checkout-accountant",
            "accountant-card",
            0,
            startedAt.AddMinutes(1));

        Assert.True(repeat.Accepted);
        await using var db = fixture.Factory.CreateDbContext();
        var trial = await db.IsletmeDenemeleri.SingleAsync();
        Assert.Equal(14, (trial.BitisAt - trial.BaslangicAt).Days);
        Assert.Equal(2, trial.EkMusteriKredisi);
        Assert.Equal(1, await db.IsletmeDenemeleri.CountAsync());
    }

    [Fact]
    public async Task InitialCheckout_AcceptsAnnualBillingAndCreatesOneYearSubscription()
    {
        using var fixture = new PaymentFixture(HesapTipleri.Isletme);
        var command = fixture.CreateCommand("checkout-annual", PlanKodlari.IsletmeBaslangic) with
        {
            BillingPeriod = PaymentBillingPeriods.Annual
        };

        var checkout = await fixture.Service.BeginCheckoutAsync(command);
        var paidAt = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);
        var paid = await fixture.SendEventAsync(
            "evt-annual-paid",
            PaymentEventTypes.PaymentSucceeded,
            "checkout-annual",
            "subscription-annual",
            checkout.Quote.TotalAmount,
            paidAt);

        Assert.True(paid.Accepted);
        Assert.Equal(6624m, checkout.Quote.NetAmount);
        await using var db = fixture.Factory.CreateDbContext();
        var subscription = await db.Abonelikler.SingleAsync();
        Assert.Equal(PaymentBillingPeriods.Annual, subscription.FaturalamaDonemi);
        Assert.Equal(paidAt.AddYears(1), subscription.DonemBitisAt);
    }

    [Fact]
    public async Task Founder100_ReservesSlotPersistsRenewalPriceAndIsIdempotent()
    {
        using var fixture = new PaymentFixture(HesapTipleri.Isletme);
        var command = fixture.CreateCommand("checkout-founder", PlanKodlari.IsletmeBuyume) with
        {
            CampaignCode = SubscriptionPlanCatalog.KurucuKampanyaKodu
        };

        var first = await fixture.Service.BeginCheckoutAsync(command);
        var repeated = await fixture.Service.BeginCheckoutAsync(command);

        Assert.Equal(990m, first.Quote.NetAmount);
        Assert.Equal(1290m, first.Quote.ListNetAmount);
        Assert.Equal(1290m, first.Quote.RenewalNetAmount);
        Assert.Equal(3, first.Quote.DiscountedPeriodCount);
        Assert.True(repeated.Reused);

        var paidAt = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);
        var paid = await fixture.SendEventAsync(
            "evt-founder-paid",
            PaymentEventTypes.PaymentSucceeded,
            "checkout-founder",
            "subscription-founder",
            first.Quote.TotalAmount,
            paidAt);

        Assert.True(paid.Accepted);
        await using var db = fixture.Factory.CreateDbContext();
        var right = await db.KurucuKampanyaHaklari.SingleAsync();
        Assert.Equal(1, right.SiraNo);
        Assert.Equal("Kazanildi", right.Durum);
        var subscription = await db.Abonelikler.SingleAsync();
        Assert.Equal(990m, subscription.DonemTutari);
        Assert.Equal(1290m, subscription.YenilemeDonemTutari);
        Assert.Equal(2, subscription.IndirimliDonemKalan);
        Assert.Equal(SubscriptionPlanCatalog.KurucuKampanyaKodu, subscription.KampanyaKodu);
    }

    [Fact]
    public async Task Founder100_RejectsCheckoutWhenAllSlotsAreClaimed()
    {
        using var fixture = new PaymentFixture(HesapTipleri.Isletme);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            var now = DateTime.UtcNow;
            db.KurucuKampanyaHaklari.AddRange(Enumerable.Range(1, SubscriptionPlanCatalog.KurucuKampanyaKontenjani)
                .Select(slot => new KurucuKampanyaHakki
                {
                    IsletmeId = fixture.BusinessId + slot,
                    KampanyaKodu = SubscriptionPlanCatalog.KurucuKampanyaKodu,
                    SiraNo = slot,
                    CheckoutAnahtari = $"claimed-founder-slot-{slot}",
                    Durum = "Kazanildi",
                    RezerveAt = now.AddDays(-1),
                    RezervasyonBitisAt = now.AddDays(-1),
                    KazanildiAt = now.AddDays(-1),
                    UpdatedAt = now.AddDays(-1)
                }));
            await db.SaveChangesAsync();
        }

        var command = fixture.CreateCommand("checkout-founder-full", PlanKodlari.IsletmeBaslangic) with
        {
            CampaignCode = SubscriptionPlanCatalog.KurucuKampanyaKodu
        };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.BeginCheckoutAsync(command));

        Assert.Contains("kontenjani doldu", error.Message, StringComparison.OrdinalIgnoreCase);
        await using var verified = fixture.Factory.CreateDbContext();
        Assert.Equal(SubscriptionPlanCatalog.KurucuKampanyaKontenjani, await verified.KurucuKampanyaHaklari.CountAsync());
        Assert.Empty(await verified.OdemeIslemleri.ToListAsync());
    }

    [Fact]
    public async Task UsedTrial_StartsPaidSubscriptionWithoutGrantingAnotherTrial()
    {
        using var fixture = new PaymentFixture(HesapTipleri.Isletme);
        var now = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            db.IsletmeDenemeleri.Add(new IsletmeDeneme
            {
                IsletmeId = fixture.BusinessId,
                HesapTipi = HesapTipleri.Isletme,
                PlanKodu = PlanKodlari.IsletmeBaslangic,
                FaturalamaDonemi = PaymentBillingPeriods.Monthly,
                Durum = "SonaErdi",
                BaslangicAt = now.AddDays(-31),
                BitisAt = now.AddDays(-1),
                OdemeYontemiEklendi = true,
                CreatedAt = now.AddDays(-31),
                UpdatedAt = now.AddDays(-1)
            });
            await db.SaveChangesAsync();
        }

        var checkout = await fixture.Service.BeginCheckoutAsync(
            fixture.CreateCommand("checkout-after-trial", PlanKodlari.IsletmeBaslangic));

        Assert.Equal(0, checkout.Quote.TrialDays);
        await using (var db = fixture.Factory.CreateDbContext())
            Assert.Equal("AbonelikBaslatma", (await db.OdemeIslemleri.SingleAsync()).IslemTipi);

        var paid = await fixture.SendEventAsync(
            "evt-after-trial-paid",
            PaymentEventTypes.PaymentSucceeded,
            "checkout-after-trial",
            "subscription-after-trial",
            checkout.Quote.TotalAmount,
            now);

        Assert.True(paid.Accepted);
        await using var verified = fixture.Factory.CreateDbContext();
        Assert.Single(await verified.IsletmeDenemeleri.ToListAsync());
        Assert.Equal("SonaErdi", (await verified.IsletmeDenemeleri.SingleAsync()).Durum);
        Assert.Equal("Aktif", (await verified.Abonelikler.SingleAsync()).Durum);
    }

    [Fact]
    public async Task FailedRenewal_OpensSevenDayGrace_ThenCancellationAndRefundAreRecorded()
    {
        using var fixture = new PaymentFixture(HesapTipleri.Isletme);
        var checkout = await fixture.Service.BeginCheckoutAsync(
            fixture.CreateCommand("checkout-grace", PlanKodlari.IsletmeBaslangic));
        var paidAt = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);
        await fixture.SendEventAsync(
            "evt-paid",
            PaymentEventTypes.PaymentSucceeded,
            "checkout-grace",
            "subscription-grace",
            checkout.Quote.TotalAmount,
            paidAt);
        var failedAt = paidAt.AddMonths(1);
        await fixture.SendEventAsync(
            "evt-failed",
            PaymentEventTypes.PaymentFailed,
            "checkout-grace",
            "subscription-grace",
            0,
            failedAt);

        await using (var db = fixture.Factory.CreateDbContext())
        {
            var subscription = await db.Abonelikler.SingleAsync();
            Assert.Equal(failedAt, subscription.OdemeSorunuAt);
            Assert.Equal(failedAt.AddDays(7), subscription.ToleransBitisAt);
        }

        await fixture.Service.CancelAtPeriodEndAsync(fixture.BusinessId);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            var subscription = await db.Abonelikler.SingleAsync();
            Assert.True(subscription.DonemSonundaIptal);
            Assert.NotNull(subscription.IptalAt);
        }

        var refundedAt = failedAt.AddDays(2);
        await fixture.SendEventAsync(
            "evt-refund",
            PaymentEventTypes.PaymentRefunded,
            "checkout-grace",
            "subscription-grace",
            checkout.Quote.TotalAmount,
            refundedAt);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            var subscription = await db.Abonelikler.SingleAsync();
            Assert.Equal("IadeEdildi", subscription.Durum);
            Assert.Equal(refundedAt, subscription.DonemBitisAt);
        }
    }

    [Fact]
    public async Task UnconfiguredProvider_FailsClosed()
    {
        using var fixture = new PaymentFixture(HesapTipleri.Isletme, new UnconfiguredPaymentProvider());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.BeginCheckoutAsync(
                fixture.CreateCommand("checkout-no-provider", PlanKodlari.IsletmeBaslangic)));

        Assert.Contains("yapilandirilmadi", error.Message, StringComparison.OrdinalIgnoreCase);
        await using var db = fixture.Factory.CreateDbContext();
        var payment = await db.OdemeIslemleri.SingleAsync();
        Assert.Equal(PaymentTransactionStates.Failed, payment.Durum);
        Assert.Equal("checkout_create_failed", payment.HataKodu);
    }

    [Fact]
    public async Task Reconcile_ExpiresTrials_Cancellations_AndGracePeriods()
    {
        using var fixture = new PaymentFixture(HesapTipleri.Isletme);
        var now = new DateTime(2026, 8, 20, 9, 0, 0, DateTimeKind.Utc);

        await using (var db = fixture.Factory.CreateDbContext())
        {
            db.IsletmeDenemeleri.Add(new IsletmeDeneme
            {
                IsletmeId = fixture.BusinessId,
                HesapTipi = HesapTipleri.Isletme,
                PlanKodu = PlanKodlari.IsletmeBaslangic,
                Durum = "Aktif",
                BaslangicAt = now.AddDays(-31),
                BitisAt = now.AddDays(-1),
                CreatedAt = now.AddDays(-31),
                UpdatedAt = now.AddDays(-31)
            });
            db.Abonelikler.AddRange(
                new Abonelik
                {
                    IsletmeId = fixture.BusinessId,
                    HesapTipi = HesapTipleri.Isletme,
                    PlanKodu = PlanKodlari.IsletmeBaslangic,
                    Durum = "Aktif",
                    DonemBaslangicAt = now.AddMonths(-1),
                    DonemBitisAt = now.AddMinutes(-1),
                    DonemSonundaIptal = true,
                    CreatedAt = now.AddMonths(-1),
                    UpdatedAt = now.AddMonths(-1)
                },
                new Abonelik
                {
                    IsletmeId = fixture.BusinessId + 1,
                    HesapTipi = HesapTipleri.Isletme,
                    PlanKodu = PlanKodlari.IsletmeBuyume,
                    Durum = "Aktif",
                    DonemBaslangicAt = now.AddMonths(-1),
                    DonemBitisAt = now.AddDays(-7),
                    OdemeSorunuAt = now.AddDays(-7),
                    ToleransBitisAt = now.AddMinutes(-1),
                    CreatedAt = now.AddMonths(-1),
                    UpdatedAt = now.AddDays(-7)
                });
            await db.SaveChangesAsync();
        }

        var result = await fixture.Service.ReconcileAsync(now);

        Assert.Equal(1, result.ExpiredTrials);
        Assert.Equal(1, result.CancelledSubscriptions);
        Assert.Equal(1, result.GracePeriodsEnded);
        await using var verified = fixture.Factory.CreateDbContext();
        Assert.Equal("SonaErdi", (await verified.IsletmeDenemeleri.SingleAsync()).Durum);
        Assert.Contains(await verified.Abonelikler.ToListAsync(), x => x.Durum == "IptalEdildi");
        Assert.Contains(await verified.Abonelikler.ToListAsync(), x => x.Durum == "OdemeBasarisiz");
    }

    [Fact]
    public async Task Reconcile_SendsSevenAndThreeDayRemindersOnce()
    {
        var sender = new FakeReminderSender();
        using var fixture = new PaymentFixture(HesapTipleri.Isletme, reminderSender: sender);
        var now = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);

        await using (var db = fixture.Factory.CreateDbContext())
        {
            var user = new Kullanici
            {
                AuthProviderUserId = "reminder-user",
                Eposta = "reminder@systemcel.local",
                AdSoyad = "Reminder User",
                HesapTipi = HesapTipleri.Isletme,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Kullanicilar.Add(user);
            await db.SaveChangesAsync();
            db.IsletmeUyelikleri.Add(new IsletmeUyelik
            {
                IsletmeId = fixture.BusinessId,
                KullaniciId = user.Id,
                Rol = "isletme_sahibi",
                Durum = "Aktif",
                DavetEposta = user.Eposta,
                CreatedAt = now,
                UpdatedAt = now
            });
            db.IsletmeDenemeleri.Add(new IsletmeDeneme
            {
                IsletmeId = fixture.BusinessId,
                HesapTipi = HesapTipleri.Isletme,
                PlanKodu = PlanKodlari.IsletmeBaslangic,
                FaturalamaDonemi = PaymentBillingPeriods.Monthly,
                Durum = "Aktif",
                BaslangicAt = now.AddDays(-23),
                BitisAt = now.AddDays(7),
                OdemeYontemiEklendi = true,
                CreatedAt = now.AddDays(-23),
                UpdatedAt = now.AddDays(-23)
            });
            await db.SaveChangesAsync();
        }

        var sevenDay = await fixture.Service.ReconcileAsync(now);
        var threeDay = await fixture.Service.ReconcileAsync(now.AddDays(4));
        var duplicate = await fixture.Service.ReconcileAsync(now.AddDays(4).AddMinutes(15));

        Assert.Equal(1, sevenDay.SevenDayReminders);
        Assert.Equal(1, threeDay.ThreeDayReminders);
        Assert.Equal(0, duplicate.SevenDayReminders + duplicate.ThreeDayReminders);
        Assert.Equal(new[] { 7, 3 }, sender.Reminders.Select(x => x.DaysRemaining));
        Assert.All(sender.Reminders, x => Assert.Equal("reminder@systemcel.local", x.Email));
        Assert.Equal(828m, sender.Reminders[0].TotalAmount);
        await using var verified = fixture.Factory.CreateDbContext();
        var trial = await verified.IsletmeDenemeleri.SingleAsync();
        Assert.NotNull(trial.YediGunHatirlatmaAt);
        Assert.NotNull(trial.UcGunHatirlatmaAt);
    }

    private sealed class PaymentFixture : IDisposable
    {
        private readonly string _dbPath;
        private readonly FakePaymentProvider _fakeProvider = new(Secret);

        public PaymentFixture(
            string accountType,
            IPaymentProvider? provider = null,
            ISubscriptionReminderSender? reminderSender = null)
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"systemcel_payment_{Guid.NewGuid():N}.db");
            Factory = new TestDbContextFactory(_dbPath);
            using var db = Factory.CreateDbContext();
            SchemaMigrator.EnsureKasaSchema(db);
            var business = new Isletme
            {
                Ad = $"Payment test {Guid.NewGuid():N}",
                TenantTipi = accountType,
                IsAktif = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Isletmeler.Add(business);
            db.SaveChanges();
            BusinessId = business.Id;
            Service = new SubscriptionLifecycleService(
                Factory,
                provider ?? _fakeProvider,
                new PaymentPricingService(),
                reminderSender);
        }

        public int BusinessId { get; }
        public TestDbContextFactory Factory { get; }
        public SubscriptionLifecycleService Service { get; }

        public SubscriptionCheckoutCommand CreateCommand(string checkoutKey, string planCode, int extraCustomerCredits = 0) => new(
            BusinessId,
            planCode.StartsWith("muhasebeci_", StringComparison.Ordinal) ? HesapTipleri.Muhasebeci : HesapTipleri.Isletme,
            planCode,
            PaymentBillingPeriods.Monthly,
            extraCustomerCredits,
            string.Empty,
            checkoutKey,
            "user-test",
            "payment-test@systemcel.local",
            "trial-consent-v1",
            "Deneme sonunda aylik ucret tahsil edilir.",
            "127.0.0.1",
            "SystemcelTests/1.0",
            new Uri("https://systemcel.local/payment/success"),
            new Uri("https://systemcel.local/payment/failure"),
            new Uri("https://systemcel.local/api/odeme/webhook"));

        public Task<PaymentWebhookProcessingResult> SendEventAsync(
            string eventId,
            string eventType,
            string checkoutKey,
            string transactionId,
            decimal amount,
            DateTime occurredAt)
        {
            var payload = JsonSerializer.Serialize(new
            {
                eventId,
                eventType,
                merchantReference = checkoutKey,
                providerTransactionId = transactionId,
                amount,
                currency = "TRY",
                occurredAt
            });
            return Service.ProcessWebhookAsync(new PaymentWebhookEnvelope(payload, _fakeProvider.SignPayload(payload)));
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_dbPath))
                    File.Delete(_dbPath);
            }
            catch
            {
            }
        }
    }

    private sealed class FakeReminderSender : ISubscriptionReminderSender
    {
        public List<SubscriptionTrialReminder> Reminders { get; } = new();

        public Task<bool> SendTrialEndingAsync(
            SubscriptionTrialReminder reminder,
            CancellationToken ct = default)
        {
            Reminders.Add(reminder);
            return Task.FromResult(true);
        }
    }

    private sealed class TestDbContextFactory : IDbContextFactory<CashTrackerDbContext>
    {
        private readonly DbContextOptions<CashTrackerDbContext> _options;

        public TestDbContextFactory(string dbPath)
        {
            _options = new DbContextOptionsBuilder<CashTrackerDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;
        }

        public CashTrackerDbContext CreateDbContext() => new(_options);

        public Task<CashTrackerDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}
