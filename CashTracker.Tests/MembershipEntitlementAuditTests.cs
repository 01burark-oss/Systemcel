using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests;

public sealed class MembershipEntitlementAuditTests
{
    [Fact]
    public async Task KullaniciDaveti_PlanLimitindeTransactionIcindeDurur()
    {
        await using var fixture = await Fixture.CreateAsync();
        var service = fixture.CreateMembershipService();
        Assert.False((await service.CreateInviteAsync(new IsletmeUyelikDavetRequest { Eposta = "bir@example.com" })).TekrarKullanildi);
        Assert.False((await service.CreateInviteAsync(new IsletmeUyelikDavetRequest { Eposta = "iki@example.com", Rol = "yonetici" })).TekrarKullanildi);
        var error = await Assert.ThrowsAsync<EntitlementViolationException>(() => service.CreateInviteAsync(new IsletmeUyelikDavetRequest { Eposta = "uc@example.com" }));
        Assert.Equal(EntitlementLimits.User, error.LimitName);
        await using var db = fixture.CreateDbContext();
        Assert.Equal(3, await db.IsletmeUyelikleri.CountAsync(x => x.Durum == "Aktif" || x.Durum == "DavetBekliyor"));
    }

    [Fact]
    public async Task AyniDavetTekrarindaKapasiteTuketilmez()
    {
        await using var fixture = await Fixture.CreateAsync();
        var service = fixture.CreateMembershipService();
        var first = await service.CreateInviteAsync(new IsletmeUyelikDavetRequest { Eposta = "same@example.com" });
        var second = await service.CreateInviteAsync(new IsletmeUyelikDavetRequest { Eposta = "SAME@example.com" });
        Assert.Equal(first.Id, second.Id);
        Assert.True(second.TekrarKullanildi);
    }

    [Fact]
    public async Task Davet_YalnizDavetEdilenEpostaTarafindanKabulEdilir()
    {
        await using var fixture = await Fixture.CreateAsync();
        var service = fixture.CreateMembershipService();
        var invite = await service.CreateInviteAsync(new IsletmeUyelikDavetRequest { Eposta = "invitee@example.com", Rol = "personel" });
        await fixture.AddUserAsync(2, "other", "other@example.com");
        fixture.User.Set("other", "other@example.com");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.AcceptInviteAsync(invite.DavetKodu));

        await fixture.AddUserAsync(3, "invitee", "invitee@example.com");
        fixture.User.Set("invitee", "invitee@example.com");
        var result = await service.AcceptInviteAsync(invite.DavetKodu);

        var membership = Assert.Single(result.Uyelikler, x => x.KullaniciId == 3);
        Assert.Equal("Aktif", membership.Durum);
        Assert.Equal("personel", membership.Rol);
        Assert.Empty(membership.DavetKodu);
    }

    [Fact]
    public async Task Davet_DogrulanmamisEpostaClaimiyleKabulEdilmez()
    {
        await using var fixture = await Fixture.CreateAsync();
        var service = fixture.CreateMembershipService();
        var invite = await service.CreateInviteAsync(new IsletmeUyelikDavetRequest { Eposta = "invitee@example.com" });
        await fixture.AddUserAsync(2, "invitee", "invitee@example.com");
        fixture.User.Set("invitee", "invitee@example.com", emailVerified: false);

        var error = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.AcceptInviteAsync(invite.DavetKodu));

        Assert.Contains("doğrulayın", error.Message);
    }

    [Fact]
    public async Task IsletmeSahibi_RolSilmeVeSahiplikDevriniYonetir()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.AddUserAsync(2, "member", "member@example.com");
        await using (var db = fixture.CreateDbContext())
        {
            db.IsletmeUyelikleri.Add(new IsletmeUyelik { IsletmeId = 1, KullaniciId = 2, Rol = "personel", Durum = "Aktif", DavetEposta = "member@example.com" });
            await db.SaveChangesAsync();
        }

        var service = fixture.CreateMembershipService();
        var member = Assert.Single((await service.GetMembershipsAsync()).Uyelikler, x => x.KullaniciId == 2);
        var updated = await service.UpdateRoleAsync(member.Id, "yonetici");
        Assert.Equal("yonetici", Assert.Single(updated.Uyelikler, x => x.Id == member.Id).Rol);

        var transferred = await service.TransferOwnershipAsync(member.Id);
        Assert.Equal("isletme_sahibi", Assert.Single(transferred.Uyelikler, x => x.Id == member.Id).Rol);
        Assert.False(transferred.SahibiMi);

        fixture.User.Set("member", "member@example.com");
        var oldOwner = Assert.Single((await service.GetMembershipsAsync()).Uyelikler, x => x.KullaniciId == 1);
        var removed = await service.RemoveAsync(oldOwner.Id);
        Assert.DoesNotContain(removed.Uyelikler, x => x.Id == oldOwner.Id);
    }

    [Fact]
    public async Task NormalUye_BekleyenDavetinTekKullanimlikKodunuGoremez()
    {
        await using var fixture = await Fixture.CreateAsync();
        var ownerService = fixture.CreateMembershipService();
        var pending = await ownerService.CreateInviteAsync(new IsletmeUyelikDavetRequest { Eposta = "pending@example.com" });
        Assert.NotEmpty(pending.DavetKodu);

        await fixture.AddUserAsync(2, "member", "member@example.com");
        await using (var db = fixture.CreateDbContext())
        {
            db.IsletmeUyelikleri.Add(new IsletmeUyelik
            {
                IsletmeId = 1,
                KullaniciId = 2,
                Rol = "personel",
                Durum = "Aktif",
                DavetEposta = "member@example.com"
            });
            await db.SaveChangesAsync();
        }

        fixture.User.Set("member", "member@example.com");
        var list = await fixture.CreateMembershipService().GetMembershipsAsync();

        Assert.False(list.SahibiMi);
        Assert.Empty(Assert.Single(list.Uyelikler, x => x.Id == pending.Id).DavetKodu);
    }

    [Fact]
    public async Task KaldirilanUye_YenidenDavetEdilipKabulEdilebilir()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.AddUserAsync(2, "member", "member@example.com");
        await using (var db = fixture.CreateDbContext())
        {
            db.IsletmeUyelikleri.Add(new IsletmeUyelik
            {
                IsletmeId = 1,
                KullaniciId = 2,
                Rol = "personel",
                Durum = "Aktif",
                DavetEposta = "member@example.com"
            });
            await db.SaveChangesAsync();
        }

        var ownerService = fixture.CreateMembershipService();
        var member = Assert.Single((await ownerService.GetMembershipsAsync()).Uyelikler, x => x.KullaniciId == 2);
        await ownerService.RemoveAsync(member.Id);
        var invite = await ownerService.CreateInviteAsync(new IsletmeUyelikDavetRequest
        {
            Eposta = "member@example.com",
            Rol = "yonetici"
        });

        fixture.User.Set("member", "member@example.com");
        var accepted = await fixture.CreateMembershipService().AcceptInviteAsync(invite.DavetKodu);

        var rejoined = Assert.Single(accepted.Uyelikler, x => x.KullaniciId == 2);
        Assert.Equal(member.Id, rejoined.Id);
        Assert.Equal("yonetici", rejoined.Rol);
        await using var verificationDb = fixture.CreateDbContext();
        Assert.Single(await verificationDb.IsletmeUyelikleri.Where(x => x.IsletmeId == 1 && x.KullaniciId == 2).ToListAsync());
    }

    [Fact]
    public async Task YoneticiHakDegisikligi_OncekiVeYeniDegerleDenetlenir()
    {
        await using var fixture = await Fixture.CreateAsync();
        var admin = new SystemcelYonetimService(new SingleDbContextFactory(fixture.Options), fixture.User, new SystemcelYonetimOptions { AdminClerkUserIds = "owner-user" });
        var result = await admin.ApplyEntitlementOverrideAsync(1, new EntitlementOverrideRequest { PlanKodu = PlanKodlari.IsletmeKurumsal, AiAktif = true, KullaniciLimiti = 25, Gerekce = "Pilot ekip kapasitesi" });
        await using var db = fixture.CreateDbContext();
        var audit = await db.YonetimDenetimKayitlari.SingleAsync();
        Assert.Equal(result.DenetimKaydiId, audit.Id);
        Assert.Equal("owner-user", audit.AktorProviderKullaniciId);
        Assert.Contains("isletme_buyume", audit.OncekiDeger);
        Assert.Contains("isletme_kurumsal", audit.YeniDeger);
        Assert.Equal("Pilot ekip kapasitesi", audit.Gerekce);
    }

    [Fact]
    public async Task YoneticiHakDegisikligi_FarkliHesapTipininPlaniniReddeder()
    {
        await using var fixture = await Fixture.CreateAsync();
        var admin = new SystemcelYonetimService(new SingleDbContextFactory(fixture.Options), fixture.User, new SystemcelYonetimOptions { AdminClerkUserIds = "owner-user" });

        var error = await Assert.ThrowsAsync<ArgumentException>(() => admin.ApplyEntitlementOverrideAsync(
            1,
            new EntitlementOverrideRequest
            {
                PlanKodu = PlanKodlari.MuhasebeciStandart,
                KullaniciLimiti = 2,
                MusteriLimiti = 10,
                Gerekce = "Yanlis rol plani testi"
            }));

        Assert.Contains("hesap tipiyle uyumlu", error.Message);
        await using var db = fixture.CreateDbContext();
        Assert.Empty(await db.YonetimDenetimKayitlari.ToListAsync());
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string _path;
        private Fixture(string path, DbContextOptions<CashTrackerDbContext> options) { _path = path; Options = options; User = new MutableUser(); }
        public DbContextOptions<CashTrackerDbContext> Options { get; }
        public MutableUser User { get; }
        public static async Task<Fixture> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"systemcel_membership_{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite($"Data Source={path}").Options;
            var fixture = new Fixture(path, options);
            await using var db = fixture.CreateDbContext();
            await db.Database.EnsureCreatedAsync();
            db.Isletmeler.Add(new Isletme { Id = 1, Ad = "Pilot", TenantTipi = HesapTipleri.Isletme, IsAktif = true });
            db.Kullanicilar.Add(new Kullanici { Id = 1, AuthProvider = "clerk", AuthProviderUserId = "owner-user", Eposta = "owner@example.com", AdSoyad = "Owner", HesapTipi = HesapTipleri.Isletme, Durum = "Aktif" });
            db.IsletmeUyelikleri.Add(new IsletmeUyelik { IsletmeId = 1, KullaniciId = 1, Rol = "isletme_sahibi", Durum = "Aktif", DavetEposta = "owner@example.com" });
            db.IsletmeEntitlementlari.Add(new IsletmeEntitlement { IsletmeId = 1, PlanKodu = PlanKodlari.IsletmeBuyume, Kaynak = "YoneticiOverride", AiAktif = true, KullaniciLimiti = 3, GecerliBaslangicAt = DateTime.UtcNow.AddMinutes(-1) });
            await db.SaveChangesAsync();
            fixture.User.Set("owner-user");
            return fixture;
        }
        public CashTrackerDbContext CreateDbContext() => new(Options);
        public async Task AddUserAsync(int id, string providerUserId, string email)
        {
            await using var db = CreateDbContext();
            db.Kullanicilar.Add(new Kullanici { Id = id, AuthProvider = "clerk", AuthProviderUserId = providerUserId, Eposta = email, AdSoyad = providerUserId, HesapTipi = HesapTipleri.Isletme, Durum = "Aktif" });
            await db.SaveChangesAsync();
        }
        public IsletmeUyelikService CreateMembershipService()
        {
            var factory = new SingleDbContextFactory(Options);
            var entitlement = new SubscriptionEntitlementService(factory);
            return new IsletmeUyelikService(factory, User, new StaticBusinessService(), new EntitlementGuard(entitlement));
        }
        public ValueTask DisposeAsync() { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); try { File.Delete(_path); } catch { } return ValueTask.CompletedTask; }
    }

    private sealed class MutableUser : ICurrentUserContext
    {
        private string _id = string.Empty;
        private string _email = string.Empty;
        private bool _emailVerified = true;
        public void Set(string id, string? email = null, bool emailVerified = true) { _id = id; _email = email ?? $"{id}@example.com"; _emailVerified = emailVerified; }
        public CurrentUserIdentity GetCurrentUser() => new(_id, _email, _id, _emailVerified);
    }

    private sealed class StaticBusinessService : IIsletmeService
    {
        public Task<Isletme> GetActiveAsync() => Task.FromResult(new Isletme { Id = 1, Ad = "Pilot", TenantTipi = HesapTipleri.Isletme, IsAktif = true });
        public Task<int> GetActiveIdAsync() => Task.FromResult(1);
        public Task<List<Isletme>> GetAllAsync() => throw new NotSupportedException();
        public Task<Isletme?> GetByIdAsync(int id) => throw new NotSupportedException();
        public Task<int> CreateAsync(string ad, bool makeActive = false) => throw new NotSupportedException();
        public Task RenameAsync(int id, string ad) => throw new NotSupportedException();
        public Task UpdateSetupAsync(int id, string ad, string isletmeTuru, string konum, bool tamamlandi, string? hesapTipi = null, bool? muhasebeciVarMi = null, MuhasebeciProfilKaydetRequest? muhasebeciProfil = null) => throw new NotSupportedException();
        public Task SetActiveAsync(int id) => throw new NotSupportedException();
        public Task SetActiveCustomerContextAsync(int musteriIsletmeId) => throw new NotSupportedException();
        public Task ClearActiveCustomerContextAsync() => throw new NotSupportedException();
        public Task<ActiveBusinessAccess> GetActiveAccessAsync() => throw new NotSupportedException();
        public Task DeleteAsync(int id) => throw new NotSupportedException();
    }
}
