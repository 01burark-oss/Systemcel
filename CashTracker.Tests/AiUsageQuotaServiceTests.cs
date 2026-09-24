using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests;

public sealed class AiUsageQuotaServiceTests
{
    [Fact]
    public async Task SinirsizPlan_AylikTakipEderAmaMesajSayisinaGoreEngellemez()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var db = new CashTrackerDbContext(options))
            await db.Database.EnsureCreatedAsync();

        var service = new AiUsageQuotaService(
            new FakeIsletmeService(),
            new StaticEntitlementService(new SubscriptionEntitlementStatus
            {
                PlanKodu = PlanKodlari.IsletmeBuyume,
                PlanAdi = "Büyüme",
                AiAktif = true,
                AiMesajLimiti = null
            }),
            new SingleDbContextFactory(options));

        var checkedAt = DateTime.UtcNow;
        var initial = await service.GetStatusAsync();

        Assert.Equal("Aylık", initial.DonemTipi);
        Assert.Null(initial.Limit);
        Assert.Null(initial.Kalan);
        Assert.Equal(new DateTime(checkedAt.Year, checkedAt.Month, 1).AddMonths(1), initial.DonemBitisAt);

        AiUsageStatus? lastAllowed = null;
        for (var i = 0; i < 40; i++)
        {
            lastAllowed = await service.ConsumeAsync();
            Assert.True(lastAllowed.IzinVerildi);
            Assert.False(lastAllowed.LimitAsildi);
            Assert.Null(lastAllowed.Limit);
        }

        Assert.NotNull(lastAllowed);
        Assert.True(lastAllowed.IzinVerildi);
        Assert.Null(lastAllowed.Kalan);
        Assert.Equal(40, lastAllowed.Kullanilan);

        var exhaustedStatus = await service.GetStatusAsync();
        Assert.True(exhaustedStatus.IzinVerildi);
        Assert.False(exhaustedStatus.LimitAsildi);

        var next = await service.ConsumeAsync();

        Assert.True(next.IzinVerildi);
        Assert.False(next.LimitAsildi);
        Assert.Equal(41, next.Kullanilan);
        Assert.Equal("AI hakkı sınırsız.", next.Mesaj);
    }

    [Fact]
    public async Task SonluPlan_AylikLimitteEngellenir()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var db = new CashTrackerDbContext(options))
            await db.Database.EnsureCreatedAsync();

        var service = new AiUsageQuotaService(
            new FakeIsletmeService(),
            new StaticEntitlementService(new SubscriptionEntitlementStatus
            {
                PlanKodu = PlanKodlari.IsletmeBaslangic,
                PlanAdi = "Başlangıç",
                AiAktif = true,
                AiMesajLimiti = 2
            }),
            new SingleDbContextFactory(options));

        var first = await service.ConsumeAsync();
        var second = await service.ConsumeAsync();
        var third = await service.ConsumeAsync();

        Assert.Equal("Aylık", first.DonemTipi);
        Assert.Equal(2, first.Limit);
        Assert.True(second.IzinVerildi);
        Assert.False(third.IzinVerildi);
        Assert.True(third.LimitAsildi);
        Assert.Equal(2, third.Kullanilan);
    }

    private sealed class StaticEntitlementService(SubscriptionEntitlementStatus status)
        : ISubscriptionEntitlementService
    {
        public Task<SubscriptionEntitlementStatus> GetIsletmeEntitlementAsync(
            int isletmeId,
            DateTime? now = null,
            CancellationToken ct = default) => Task.FromResult(status);

        public Task<SubscriptionEntitlementStatus> GetMuhasebeciEntitlementAsync(
            int muhasebeciIsletmeId,
            DateTime? now = null,
            CancellationToken ct = default) => Task.FromResult(status);
    }
}
