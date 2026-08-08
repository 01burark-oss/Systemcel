using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Infrastructure.Payments;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests;

public sealed class PaymentReconciliationTests
{
    private const string Secret = "fake-payment-secret-for-reconciliation";

    [Fact]
    public async Task GunlukMutabakat_FarkiKaydederVeAyniGunTekrarlamaz()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"systemcel_reconciliation_{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite($"Data Source={dbPath}").Options;
            var periodEnd = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
            await using (var db = new CashTrackerDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                db.Abonelikler.Add(new Abonelik { IsletmeId = 42, PlanKodu = "Standard", Durum = "Aktif", DonemBitisAt = periodEnd, OdemeSaglayici = "Fake", SaglayiciAbonelikId = "subscription-42" });
                db.OdemeIslemleri.Add(new OdemeIslemi { IsletmeId = 42, CheckoutAnahtari = "checkout-42", OdemeSaglayici = "Fake", Durum = "Basarili" });
                await db.SaveChangesAsync();
            }

            var provider = new FakePaymentProvider(Secret);
            provider.SetReconciliationSubscription(new ProviderSubscriptionSnapshot("subscription-42", "OdemeBasarisiz", "Pro", periodEnd.AddDays(1), true));
            var service = new PaymentReconciliationService(new SingleDbContextFactory(options), provider);
            var now = new DateTime(2026, 8, 9, 3, 0, 0, DateTimeKind.Utc);

            var first = await service.ReconcileAsync(now);
            var second = await service.ReconcileAsync(now.AddHours(2));
            var nextDay = await service.ReconcileAsync(now.AddDays(1));

            Assert.True(first.ProviderAvailable);
            Assert.Equal(1, first.CheckedSubscriptions);
            Assert.Equal(1, first.DiscrepancyCount);
            Assert.Equal(1, first.RecordedFindings);
            Assert.Equal(0, second.RecordedFindings);
            Assert.Equal(1, nextDay.RecordedFindings);
            await using var verified = new CashTrackerDbContext(options);
            var findings = await verified.OdemeOlaylari.OrderBy(x => x.Id).ToListAsync();
            Assert.Equal(2, findings.Count);
            Assert.All(findings, x => Assert.Equal("subscription.reconciliation.mismatch", x.OlayTipi));
            Assert.All(findings, x => Assert.Equal("checkout-42", x.CheckoutAnahtari));
            Assert.Contains("Durum farki", findings[0].HataMesaji);
            Assert.Contains("Plan farki", findings[0].HataMesaji);
            Assert.Contains("Donem sonu iptal farki", findings[0].HataMesaji);
            Assert.Contains("Donem bitisi farki", findings[0].HataMesaji);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [Fact]
    public async Task GunlukMutabakat_EslesenKaydiHataOlarakYazmaz()
    {
        await using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var sharedOptions = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite(connection).Options;
        await using (var db = new CashTrackerDbContext(sharedOptions))
        {
            await db.Database.EnsureCreatedAsync();
            db.Abonelikler.Add(new Abonelik { IsletmeId = 1, PlanKodu = "Standard", Durum = "Aktif", OdemeSaglayici = "Fake", SaglayiciAbonelikId = "subscription-ok" });
            await db.SaveChangesAsync();
        }
        var provider = new FakePaymentProvider(Secret);
        provider.SetReconciliationSubscription(new ProviderSubscriptionSnapshot("subscription-ok", "Aktif", "Standard", null, false));
        var result = await new PaymentReconciliationService(new SingleDbContextFactory(sharedOptions), provider).ReconcileAsync(DateTime.UtcNow);
        Assert.Equal(0, result.DiscrepancyCount);
        Assert.Equal(0, result.RecordedFindings);
    }
}
