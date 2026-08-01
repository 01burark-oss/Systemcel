using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests;

public sealed class EntitlementGuardTests
{
    [Theory]
    [InlineData(EntitlementLimits.Business, 1)]
    [InlineData(EntitlementLimits.User, 1)]
    [InlineData(EntitlementLimits.Invoice, 0)]
    [InlineData(EntitlementLimits.CashTransaction, 100)]
    [InlineData(EntitlementLimits.CurrentAccount, 20)]
    [InlineData(EntitlementLimits.ProductOrService, 50)]
    public void FreeBusinessLimits_ReturnStableProblemContract(string limitName, int limit)
    {
        var entitlement = FreeBusinessEntitlement();
        var guard = new EntitlementGuard(new StubEntitlementService(entitlement));

        var error = Assert.Throws<EntitlementViolationException>(() =>
            guard.EnsureLimit(entitlement, limitName, limit));

        Assert.Equal(EntitlementErrorCodes.LimitReached, error.Code);
        Assert.Equal(limitName, error.LimitName);
        Assert.Equal(limit, error.Limit);
        Assert.Equal(limit, error.Current);
        Assert.Equal(PlanKodlari.IsletmeBaslangic, error.SuggestedPlanCode);
    }

    [Fact]
    public void ExpiredAccountant_IsReadOnlyAndRequiresStandardPlan()
    {
        var entitlement = new SubscriptionEntitlementStatus
        {
            HesapTipi = HesapTipleri.Muhasebeci,
            PlanKodu = PlanKodlari.MuhasebeciSaltOkunur,
            PlanAdi = "Salt okunur",
            SaltOkunur = true
        };
        var guard = new EntitlementGuard(new StubEntitlementService(entitlement));

        var error = Assert.Throws<EntitlementViolationException>(() => guard.EnsureWritable(entitlement));

        Assert.Equal(EntitlementErrorCodes.SubscriptionRequired, error.Code);
        Assert.Equal(PlanKodlari.MuhasebeciStandart, error.SuggestedPlanCode);
    }

    [Fact]
    public async Task KasaService_FreeMonthlyLimit_IsCheckedInsideCreateTransaction()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"systemcel_entitlement_{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;
            var factory = new SingleDbContextFactory(options);
            var month = new DateTime(2026, 8, 1);

            await using (var db = factory.CreateDbContext())
            {
                await db.Database.EnsureCreatedAsync();
                db.Isletmeler.Add(new Isletme { Id = 1, Ad = "Free", TenantTipi = HesapTipleri.Isletme });
                db.Kasalar.AddRange(Enumerable.Range(1, 100).Select(index => new Kasa
                {
                    IsletmeId = 1,
                    Tip = "Gelir",
                    Tutar = index,
                    Tarih = month.AddDays(index % 28),
                    Kalem = "Test",
                    OdemeYontemi = "Nakit",
                    CreatedAt = month
                }));
                await db.SaveChangesAsync();
            }

            var entitlementService = new SubscriptionEntitlementService(factory);
            var guard = new EntitlementGuard(entitlementService);
            var business = new FakeIsletmeService
            {
                Active = new Isletme { Id = 1, Ad = "Free", TenantTipi = HesapTipleri.Isletme, IsAktif = true }
            };
            var service = new KasaService(factory, business, guard);

            var error = await Assert.ThrowsAsync<EntitlementViolationException>(() => service.CreateAsync(new Kasa
            {
                Tip = "Gelir",
                Tutar = 1,
                Tarih = month.AddDays(10),
                Kalem = "Test",
                OdemeYontemi = "Nakit"
            }));

            Assert.Equal(EntitlementLimits.CashTransaction, error.LimitName);
            await using var verified = factory.CreateDbContext();
            Assert.Equal(100, await verified.Kasalar.CountAsync());
        }
        finally
        {
            try
            {
                File.Delete(dbPath);
            }
            catch
            {
            }
        }
    }

    private static SubscriptionEntitlementStatus FreeBusinessEntitlement()
    {
        return new SubscriptionEntitlementStatus
        {
            HesapTipi = HesapTipleri.Isletme,
            PlanKodu = PlanKodlari.IsletmeUcretsiz,
            PlanAdi = "Ücretsiz",
            IsletmeLimiti = 1,
            KullaniciLimiti = 1,
            FaturaLimiti = 0,
            GelirGiderIslemLimiti = 100,
            CariKartLimiti = 20,
            UrunHizmetLimiti = 50
        };
    }

    private sealed class StubEntitlementService : ISubscriptionEntitlementService
    {
        private readonly SubscriptionEntitlementStatus _status;

        public StubEntitlementService(SubscriptionEntitlementStatus status)
        {
            _status = status;
        }

        public Task<SubscriptionEntitlementStatus> GetIsletmeEntitlementAsync(
            int isletmeId,
            DateTime? now = null,
            CancellationToken ct = default) => Task.FromResult(_status);

        public Task<SubscriptionEntitlementStatus> GetMuhasebeciEntitlementAsync(
            int muhasebeciIsletmeId,
            DateTime? now = null,
            CancellationToken ct = default) => Task.FromResult(_status);
    }
}
