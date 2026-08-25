using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests;

public sealed class DestekTalebiFlowTests
{
    [Fact]
    public async Task Create_FreezesPriorityFromServerEntitlement_AndReplaysSameKey()
    {
        using var fixture = await Fixture.CreateAsync(priority: true);
        var request = new DestekTalebiOlusturRequest
        {
            Konu = "Rapor açılmıyor",
            Kategori = DestekKategorileri.Teknik,
            Aciklama = "Aylık raporu açarken boş ekran görüyorum."
        };

        var created = await fixture.Service.CreateAsync(request, "support-request-001");
        fixture.Entitlements.Priority = false;
        var replay = await fixture.Service.CreateAsync(request, "support-request-001");
        var conflict = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CreateAsync(
            new DestekTalebiOlusturRequest { Konu = "Farklı konu", Kategori = DestekKategorileri.Teknik, Aciklama = request.Aciklama },
            "support-request-001"));

        Assert.Equal(created.Id, replay.Id);
        Assert.Equal(DestekOncelikleri.Oncelikli, created.Oncelik);
        Assert.Contains("Idempotency-Key", conflict.Message);
        await using var db = fixture.CreateDbContext();
        Assert.Equal(1, await db.DestekTalepleri.CountAsync());
    }

    [Fact]
    public async Task List_ReturnsOnlyActiveBusinessTickets_AndStandardPlanCannotClaimPriority()
    {
        using var fixture = await Fixture.CreateAsync(priority: false);
        var own = await fixture.Service.CreateAsync(new DestekTalebiOlusturRequest
        {
            Konu = "Fatura sorusu",
            Kategori = DestekKategorileri.Faturalama,
            Aciklama = "Faturamdaki dönem bilgisini kontrol etmek istiyorum."
        }, "support-request-002");
        await using (var db = fixture.CreateDbContext())
        {
            db.DestekTalepleri.Add(new DestekTalebi
            {
                IsletmeId = fixture.OtherBusinessId,
                OlusturmaAnahtari = "other-tenant-key",
                Konu = "Başka işletme",
                Kategori = DestekKategorileri.Diger,
                Aciklama = "Bu kayıt diğer işletmeye aittir.",
                Oncelik = DestekOncelikleri.Oncelikli,
                Durum = DestekTalebiDurumlari.Acik
            });
            await db.SaveChangesAsync();
        }

        var list = await fixture.Service.GetMineAsync();

        Assert.Equal(DestekOncelikleri.Standart, own.Oncelik);
        Assert.Single(list.Talepler);
        Assert.Equal(fixture.BusinessId, list.Talepler[0].IsletmeId);
    }

    [Fact]
    public async Task Admin_SeesPriorityFirst_UpdatesStatusAndReply_WhileRegularUserIsRejected()
    {
        using var fixture = await Fixture.CreateAsync(priority: false);
        await using (var db = fixture.CreateDbContext())
        {
            db.DestekTalepleri.AddRange(
                new DestekTalebi { IsletmeId = fixture.BusinessId, OlusturmaAnahtari = "standard-key", Konu = "Standart", Kategori = DestekKategorileri.Diger, Aciklama = "Standart talep", Oncelik = DestekOncelikleri.Standart, Durum = DestekTalebiDurumlari.Acik, CreatedAt = DateTime.UtcNow.AddHours(-2) },
                new DestekTalebi { IsletmeId = fixture.OtherBusinessId, OlusturmaAnahtari = "priority-key", Konu = "Öncelikli", Kategori = DestekKategorileri.Teknik, Aciklama = "Öncelikli talep", Oncelik = DestekOncelikleri.Oncelikli, Durum = DestekTalebiDurumlari.Acik, CreatedAt = DateTime.UtcNow.AddHours(-1) });
            await db.SaveChangesAsync();
        }
        var admin = new SystemcelYonetimService(fixture.Factory, new StaticUser("admin-user"), new SystemcelYonetimOptions { AdminClerkUserIds = "admin-user" });
        var regular = new SystemcelYonetimService(fixture.Factory, new StaticUser("regular-user"), new SystemcelYonetimOptions { AdminClerkUserIds = "admin-user" });

        var list = await admin.GetDestekTalepleriAsync();
        var updated = await admin.UpdateDestekTalebiAsync(list.Talepler[0].Id, new DestekTalebiGuncelleRequest { Durum = DestekTalebiDurumlari.Islemde, YoneticiYaniti = "İnceliyoruz." });

        Assert.Equal(DestekOncelikleri.Oncelikli, list.Talepler[0].Oncelik);
        Assert.Equal(DestekTalebiDurumlari.Islemde, updated.Durum);
        Assert.Equal("İnceliyoruz.", updated.YoneticiYaniti);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => regular.GetDestekTalepleriAsync());
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string _dbPath;
        private Fixture(string dbPath, DbContextOptions<CashTrackerDbContext> options, FakeIsletmeService business, bool priority)
        {
            _dbPath = dbPath;
            Options = options;
            Factory = new SingleDbContextFactory(options);
            Entitlements = new FakeEntitlementGuard { Priority = priority };
            Service = new DestekTalebiService(Factory, business, Entitlements, new StaticUser("owner-user"));
        }

        public DbContextOptions<CashTrackerDbContext> Options { get; }
        public SingleDbContextFactory Factory { get; }
        public DestekTalebiService Service { get; }
        public FakeEntitlementGuard Entitlements { get; }
        public int BusinessId { get; private set; }
        public int OtherBusinessId { get; private set; }

        public static async Task<Fixture> CreateAsync(bool priority)
        {
            var path = Path.Combine(Path.GetTempPath(), $"systemcel_support_{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite($"Data Source={path}").Options;
            await using var db = new CashTrackerDbContext(options);
            await db.Database.EnsureCreatedAsync();
            var own = new Isletme { Ad = "Bahar Kafe", TenantTipi = HesapTipleri.Isletme, IsAktif = true };
            var other = new Isletme { Ad = "Ada Market", TenantTipi = HesapTipleri.Isletme, IsAktif = true };
            db.Isletmeler.AddRange(own, other);
            await db.SaveChangesAsync();
            var business = new FakeIsletmeService { Active = own };
            return new Fixture(path, options, business, priority) { BusinessId = own.Id, OtherBusinessId = other.Id };
        }

        public CashTrackerDbContext CreateDbContext() => new(Options);
        public void Dispose() { try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { } }
    }

    private sealed class StaticUser(string userId) : ICurrentUserContext
    {
        public CurrentUserIdentity? GetCurrentUser() => new(userId, $"{userId}@systemcel.test", "User");
    }

    private sealed class FakeEntitlementGuard : IEntitlementGuard
    {
        public bool Priority { get; set; }
        public Task<SubscriptionEntitlementStatus> GetAsync(int businessId, string accountType, CancellationToken ct = default) =>
            Task.FromResult(new SubscriptionEntitlementStatus { IsletmeId = businessId, HesapTipi = accountType, OncelikliDestekAktif = Priority });
        public void EnsureLimit(SubscriptionEntitlementStatus entitlement, string limitName, int currentCount, int requestedCount = 1) { }
        public void EnsureWritable(SubscriptionEntitlementStatus entitlement) { }
        public void EnsureFeature(SubscriptionEntitlementStatus entitlement, string featureName) { }
    }
}
