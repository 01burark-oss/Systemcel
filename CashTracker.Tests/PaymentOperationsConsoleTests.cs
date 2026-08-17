using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests;

public sealed class PaymentOperationsConsoleTests
{
    [Fact]
    public async Task AdminOdemeIncelemesi_HatalariFiltrelerVeReferanslariMaskeler()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"systemcel_payment_ops_{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite($"Data Source={dbPath}").Options;
            await using (var db = new CashTrackerDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                db.Isletmeler.Add(new Isletme { Id = 7, Ad = "Ornek Isletme", IsAktif = true });
                db.OdemeIslemleri.AddRange(
                    new OdemeIslemi { IsletmeId = 7, CheckoutAnahtari = "checkout-error", PlanKodu = "Standard", Durum = "Basarisiz", OdemeSaglayici = "Fake", SaglayiciOturumId = "session_123456789", SaglayiciIslemId = "payment_987654321", ToplamTutar = 1200, HataKodu = "declined", HataMesaji = "Kart reddedildi" },
                    new OdemeIslemi { IsletmeId = 7, CheckoutAnahtari = "checkout-ok", PlanKodu = "Standard", Durum = "Basarili", OdemeSaglayici = "Fake", ToplamTutar = 1200 });
                db.OdemeOlaylari.Add(new OdemeOlayi { OdemeSaglayici = "Fake", OlayId = "event_123456789", OlayTipi = "payment.failed", CheckoutAnahtari = "checkout-error", SaglayiciIslemId = "payment_987654321", IslenmeDurumu = "Hata", PayloadHash = "0123456789abcdef0123456789abcdef", HataMesaji = "Islenemedi", SaglayiciAt = DateTime.UtcNow });
                await db.SaveChangesAsync();
            }

            var service = new SystemcelYonetimService(new SingleDbContextFactory(options), new StaticUserContext("admin-user"), new SystemcelYonetimOptions { AdminClerkUserIds = "admin-user" });
            var result = await service.GetOdemeIncelemeAsync(sadeceHatalar: true);

            var payment = Assert.Single(result.Islemler);
            Assert.True(result.YoneticiMi);
            Assert.Equal(2, result.ToplamSayisi);
            Assert.Equal(1, result.HataSayisi);
            Assert.Equal(1, result.IslenemeyenOlaySayisi);
            Assert.Equal("sess...6789", payment.SaglayiciOturumReferansi);
            Assert.Equal("paym...4321", payment.SaglayiciIslemReferansi);
            Assert.DoesNotContain("session_123456789", payment.SaglayiciOturumReferansi);
            Assert.Equal("0123456789ab...", Assert.Single(payment.Olaylar).PayloadHash);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [Fact]
    public async Task OdemeIncelemesi_YoneticiOlmayanaKapali()
    {
        var options = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite("Data Source=:memory:").Options;
        var service = new SystemcelYonetimService(new SingleDbContextFactory(options), new StaticUserContext("regular-user"), new SystemcelYonetimOptions { AdminClerkUserIds = "admin-user" });
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetOdemeIncelemeAsync());
    }

    [Fact]
    public async Task AdminEpostasi_TokendeYokkenKayitliKullaniciEpostasindanDogrulanir()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"systemcel_admin_email_{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite($"Data Source={dbPath}").Options;
            await using (var db = new CashTrackerDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                db.Kullanicilar.Add(new Kullanici
                {
                    AuthProvider = "clerk",
                    AuthProviderUserId = "owner-user",
                    Eposta = "owner@example.com",
                    AdSoyad = "Owner",
                    HesapTipi = HesapTipleri.Isletme,
                    Durum = KullaniciDurumlari.Aktif
                });
                await db.SaveChangesAsync();
            }

            var service = new SystemcelYonetimService(
                new SingleDbContextFactory(options),
                new StaticUserContext("owner-user", includeEmail: false),
                new SystemcelYonetimOptions { AdminEmails = "owner@example.com" });

            Assert.True(await service.IsCurrentUserAdminAsync());
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    private sealed class StaticUserContext(string userId, bool includeEmail = true) : ICurrentUserContext
    {
        public CurrentUserIdentity GetCurrentUser() => new(userId, includeEmail ? $"{userId}@example.com" : null, userId);
    }
}
