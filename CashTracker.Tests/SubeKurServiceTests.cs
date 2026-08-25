using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests;

public sealed class SubeKurServiceTests
{
    [Fact]
    public async Task GetContextAsync_LegacyBusiness_CreatesCenterAndUsesTryFallback()
    {
        using var fixture = await Fixture.CreateAsync();

        var context = await fixture.Service.GetContextAsync();

        Assert.Equal("MERKEZ", context.AktifSube.Kod);
        Assert.True(context.AktifSube.Varsayilan);
        Assert.Equal(1m, context.Kurlar.Single(x => x.ParaBirimi == "TRY").Kur);
    }

    [Fact]
    public async Task SetActiveBranchAsync_ForeignOrInactiveBranch_IsRejected()
    {
        using var fixture = await Fixture.CreateAsync();
        var inactive = await fixture.AddBranchAsync(1, "Pasif", "PSF", active: false);
        var foreign = await fixture.AddBranchAsync(2, "Yabancı", "YBN");

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.SetActiveBranchAsync(inactive));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.SetActiveBranchAsync(foreign));

        var context = await fixture.Service.GetContextAsync();
        Assert.Equal("MERKEZ", context.AktifSube.Kod);
    }

    [Fact]
    public async Task Mutations_WithoutEntitlements_AreDeniedServerSide()
    {
        using var fixture = await Fixture.CreateAsync();
        var guard = new EntitlementGuard(new StaticEntitlementService(new SubscriptionEntitlementStatus
        {
            HesapTipi = HesapTipleri.Isletme,
            PlanKodu = PlanKodlari.IsletmeUcretsiz,
            PlanAdi = "Ücretsiz",
            CokluSubeAktif = false,
            CokluParaBirimiAktif = false
        }));
        var service = new SubeKurService(fixture.Factory, fixture.Business, guard);

        await Assert.ThrowsAsync<EntitlementViolationException>(() => service.CreateBranchAsync(
            new SubeOlusturRequest { Ad = "Ankara", Kod = "ANK" }, "denied-branch"));
        await Assert.ThrowsAsync<EntitlementViolationException>(() => service.SaveRateAsync(
            new DovizKuruKaydetRequest { ParaBirimi = "USD", Kur = 32m }, "denied-rate"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000001)]
    public async Task SaveRateAsync_RejectsManipulatedRates(decimal rate)
    {
        using var fixture = await Fixture.CreateAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Service.SaveRateAsync(
            new DovizKuruKaydetRequest { ParaBirimi = "USD", Kur = rate },
            $"rate-{rate}"));

        await using var db = fixture.CreateDbContext();
        Assert.False(await db.DovizKurlari.AnyAsync());
    }

    [Fact]
    public async Task KasaCreate_RateUpdatesDoNotChangeHistoricalSnapshot()
    {
        using var fixture = await Fixture.CreateAsync();
        var now = DateTime.Now;
        await fixture.Service.SaveRateAsync(new DovizKuruKaydetRequest { ParaBirimi = "USD", Kur = 32m, GecerliAt = now.AddMinutes(-2) }, "usd-32");
        var cashService = new KasaService(fixture.Factory, fixture.Business, null, fixture.Service);

        var firstId = await cashService.CreateAsync(new Kasa { Tip = "Gelir", Tutar = 10m, ParaBirimi = "USD" });
        await fixture.Service.SaveRateAsync(new DovizKuruKaydetRequest { ParaBirimi = "USD", Kur = 35m, GecerliAt = now.AddMinutes(-1) }, "usd-35");
        var secondId = await cashService.CreateAsync(new Kasa { Tip = "Gelir", Tutar = 10m, ParaBirimi = "USD" });

        await using var db = fixture.CreateDbContext();
        var first = await db.Kasalar.FindAsync(firstId);
        var second = await db.Kasalar.FindAsync(secondId);
        Assert.Equal(32m, first!.KurSnapshot);
        Assert.Equal(10m, first.OrijinalTutar);
        Assert.Equal(320m, first.TryKarsiligi);
        Assert.Equal(35m, second!.KurSnapshot);
        Assert.Equal(350m, second.TryKarsiligi);
    }

    [Fact]
    public async Task CreateBranch_SameKeyReplays_DifferentPayloadConflicts()
    {
        using var fixture = await Fixture.CreateAsync();
        var request = new SubeOlusturRequest { Ad = "Kadıköy", Kod = "KDK" };

        var first = await fixture.Service.CreateBranchAsync(request, "branch-1");
        var replay = await fixture.Service.CreateBranchAsync(request, "branch-1");
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CreateBranchAsync(
            new SubeOlusturRequest { Ad = "Beşiktaş", Kod = "BSK" },
            "branch-1"));

        Assert.False(first.Tekrarlandi);
        Assert.True(replay.Tekrarlandi);
        Assert.Equal(first.Sube.Id, replay.Sube.Id);
        await using var db = fixture.CreateDbContext();
        Assert.Equal(2, await db.Subeler.CountAsync());
    }

    [Fact]
    public async Task CreateBranch_ConcurrentSameKey_CreatesSingleBranch()
    {
        using var fixture = await Fixture.CreateAsync();
        await fixture.Service.GetContextAsync();
        var request = new SubeOlusturRequest { Ad = "Ankara", Kod = "ANK" };

        var results = await Task.WhenAll(Enumerable.Range(0, 4)
            .Select(_ => fixture.Service.CreateBranchAsync(request, "branch-concurrent")));

        Assert.Single(results.Select(x => x.Sube.Id).Distinct());
        await using var db = fixture.CreateDbContext();
        Assert.Equal(1, await db.Subeler.CountAsync(x => x.Kod == "ANK"));
    }

    [Fact]
    public async Task FinancialSummary_ConsolidatesTryAndFiltersBranch_WithLegacyCenterFallback()
    {
        using var fixture = await Fixture.CreateAsync();
        var context = await fixture.Service.GetContextAsync();
        var branchId = await fixture.AddBranchAsync(1, "Kadıköy", "KDK");
        await using (var db = fixture.CreateDbContext())
        {
            db.Kasalar.AddRange(
                new Kasa { IsletmeId = 1, Tip = "Gelir", Tutar = 100m, SubeId = null, ParaBirimi = "TRY", KurSnapshot = 1m, OrijinalTutar = 100m, TryKarsiligi = 100m },
                new Kasa { IsletmeId = 1, Tip = "Gelir", Tutar = 10m, SubeId = branchId, ParaBirimi = "USD", KurSnapshot = 32m, OrijinalTutar = 10m, TryKarsiligi = 320m },
                new Kasa { IsletmeId = 1, Tip = "Gider", Tutar = 2m, SubeId = branchId, ParaBirimi = "USD", KurSnapshot = 32m, OrijinalTutar = 2m, TryKarsiligi = 64m },
                new Kasa { IsletmeId = 2, Tip = "Gelir", Tutar = 999m, ParaBirimi = "TRY", KurSnapshot = 1m, OrijinalTutar = 999m, TryKarsiligi = 999m });
            await db.SaveChangesAsync();
        }

        var consolidated = await fixture.Service.GetFinancialSummaryAsync();
        var center = await fixture.Service.GetFinancialSummaryAsync(context.AktifSube.Id);
        var branch = await fixture.Service.GetFinancialSummaryAsync(branchId);

        Assert.Equal(420m, consolidated.GelirTry);
        Assert.Equal(64m, consolidated.GiderTry);
        Assert.Equal(100m, center.GelirTry);
        Assert.Equal(320m, branch.GelirTry);
        Assert.Equal(10m, branch.ParaBirimleri.Single().GelirOrijinal);
        Assert.DoesNotContain(consolidated.ParaBirimleri, x => x.GelirTry == 999m);
    }

    private sealed class Fixture : IDisposable
    {
        private Fixture(string path, DbContextOptions<CashTrackerDbContext> options)
        {
            Path = path;
            Options = options;
            Factory = new SingleDbContextFactory(options);
            Business = new FakeIsletmeService { Active = new Isletme { Id = 1, Ad = "Tenant", IsAktif = true } };
            Service = new SubeKurService(Factory, Business);
        }

        public string Path { get; }
        public DbContextOptions<CashTrackerDbContext> Options { get; }
        public SingleDbContextFactory Factory { get; }
        public FakeIsletmeService Business { get; }
        public SubeKurService Service { get; }

        public static async Task<Fixture> CreateAsync()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"cashtracker_branch_currency_{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite($"Data Source={path}").Options;
            var fixture = new Fixture(path, options);
            await using var db = fixture.CreateDbContext();
            await db.Database.EnsureCreatedAsync();
            db.Isletmeler.AddRange(
                new Isletme { Id = 1, Ad = "Tenant", IsAktif = true },
                new Isletme { Id = 2, Ad = "Other", IsAktif = true });
            await db.SaveChangesAsync();
            return fixture;
        }

        public async Task<int> AddBranchAsync(int businessId, string name, string code, bool active = true)
        {
            await using var db = CreateDbContext();
            var row = new Sube { IsletmeId = businessId, Ad = name, Kod = code, Aktif = active };
            db.Subeler.Add(row);
            await db.SaveChangesAsync();
            return row.Id;
        }

        public CashTrackerDbContext CreateDbContext() => new(Options);

        public void Dispose()
        {
            try { if (File.Exists(Path)) File.Delete(Path); }
            catch { }
        }
    }

    private sealed class StaticEntitlementService : ISubscriptionEntitlementService
    {
        private readonly SubscriptionEntitlementStatus _status;
        public StaticEntitlementService(SubscriptionEntitlementStatus status) => _status = status;
        public Task<SubscriptionEntitlementStatus> GetIsletmeEntitlementAsync(int isletmeId, DateTime? now = null, System.Threading.CancellationToken ct = default) => Task.FromResult(_status);
        public Task<SubscriptionEntitlementStatus> GetMuhasebeciEntitlementAsync(int muhasebeciIsletmeId, DateTime? now = null, System.Threading.CancellationToken ct = default) => Task.FromResult(_status);
    }
}
