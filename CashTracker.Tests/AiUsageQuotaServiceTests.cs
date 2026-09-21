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
    public async Task SinirsizPlan_SaatlikOnBesMesajdanSonraEngellenir()
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

        Assert.Equal("Saatlik", initial.DonemTipi);
        Assert.Equal(15, initial.Limit);
        Assert.Equal(15, initial.Kalan);
        Assert.InRange(initial.DonemBitisAt, checkedAt, checkedAt.AddHours(1));

        AiUsageStatus? lastAllowed = null;
        for (var i = 0; i < 15; i++)
        {
            lastAllowed = await service.ConsumeAsync();
            Assert.True(lastAllowed.IzinVerildi);
        }

        Assert.NotNull(lastAllowed);
        Assert.True(lastAllowed.IzinVerildi);
        Assert.Equal(0, lastAllowed.Kalan);

        var exhaustedStatus = await service.GetStatusAsync();
        Assert.False(exhaustedStatus.IzinVerildi);
        Assert.True(exhaustedStatus.LimitAsildi);

        var blocked = await service.ConsumeAsync();

        Assert.False(blocked.IzinVerildi);
        Assert.True(blocked.LimitAsildi);
        Assert.Equal(15, blocked.Kullanilan);
        Assert.Contains("saatlik limit", blocked.Mesaj, StringComparison.OrdinalIgnoreCase);
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
