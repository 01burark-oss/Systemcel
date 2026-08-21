using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests;

public sealed class OdemeHatirlatmaServiceTests
{
    [Fact]
    public async Task SendAsync_SendsOpenSaleInvoice_RecordsDelivery_AndBlocksRepeatFor24Hours()
    {
        using var fixture = await ReminderFixture.CreateAsync("muhasebe@atlas.test");
        var sender = new FakeReminderSender();
        var service = new OdemeHatirlatmaService(fixture.Factory, fixture.Isletme, sender);

        var preview = await service.GetPreviewAsync(fixture.FaturaId);

        Assert.True(preview.Gonderilebilir);
        Assert.Equal("muhasebe@atlas.test", preview.AliciEposta);
        Assert.Contains("TEST-2026-001", preview.Konu);
        Assert.Contains("Systemcel ile gönderildi", preview.Mesaj);

        var first = await service.SendAsync(fixture.FaturaId);
        var second = await service.SendAsync(fixture.FaturaId);

        Assert.True(first.Gonderildi);
        Assert.False(second.Gonderildi);
        Assert.Contains("son 24 saat", second.Mesaj);
        Assert.Single(sender.Messages);
        Assert.Equal(750m, sender.Messages[0].KalanTutar);

        await using var db = fixture.CreateDbContext();
        var delivery = await db.OdemeHatirlatmalari.SingleAsync();
        Assert.Equal("Gonderildi", delivery.Durum);
        Assert.Equal("muhasebe@atlas.test", delivery.AliciEposta);
        Assert.NotNull(delivery.GonderildiAt);
    }

    [Fact]
    public async Task GetPreviewAsync_ExplainsMissingCustomerEmail_WithoutSending()
    {
        using var fixture = await ReminderFixture.CreateAsync(string.Empty);
        var sender = new FakeReminderSender();
        var service = new OdemeHatirlatmaService(fixture.Factory, fixture.Isletme, sender);

        var preview = await service.GetPreviewAsync(fixture.FaturaId);
        var result = await service.SendAsync(fixture.FaturaId);

        Assert.False(preview.Gonderilebilir);
        Assert.Contains("cari karta geçerli bir e-posta", preview.Engel);
        Assert.False(result.Gonderildi);
        Assert.Empty(sender.Messages);
    }

    private sealed class FakeReminderSender : IOdemeHatirlatmaSender
    {
        public bool IsConfigured => true;
        public List<OdemeHatirlatmaIcerigi> Messages { get; } = new();

        public Task<bool> SendAsync(OdemeHatirlatmaIcerigi reminder, CancellationToken ct = default)
        {
            Messages.Add(reminder);
            return Task.FromResult(true);
        }
    }

    private sealed class ReminderFixture : IDisposable
    {
        private ReminderFixture(string dbPath, DbContextOptions<CashTrackerDbContext> options)
        {
            DbPath = dbPath;
            Options = options;
            Factory = new SingleDbContextFactory(options);
            Isletme = new FakeIsletmeService { Active = new Isletme { Id = 1, Ad = "Systemcel Test İşletmesi", IsAktif = true } };
        }

        public string DbPath { get; }
        public DbContextOptions<CashTrackerDbContext> Options { get; }
        public SingleDbContextFactory Factory { get; }
        public FakeIsletmeService Isletme { get; }
        public int FaturaId { get; private set; }

        public static async Task<ReminderFixture> CreateAsync(string email)
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"systemcel_reminder_{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;
            var fixture = new ReminderFixture(dbPath, options);

            await using var db = fixture.CreateDbContext();
            await db.Database.EnsureCreatedAsync();
            db.Isletmeler.Add(new Isletme { Id = 1, Ad = "Systemcel Test İşletmesi", IsAktif = true });
            var cari = new CariKart
            {
                IsletmeId = 1,
                Tip = "Musteri",
                Unvan = "Atlas Yazılım",
                Eposta = email,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            db.CariKartlari.Add(cari);
            await db.SaveChangesAsync();

            var fatura = new Fatura
            {
                IsletmeId = 1,
                CariKartId = cari.Id,
                Tarih = new DateTime(2026, 8, 1),
                VadeTarihi = new DateTime(2026, 8, 25),
                FaturaTipi = "Satis",
                Durum = FaturaDurum.Kesildi,
                YerelFaturaNo = "TEST-2026-001",
                GenelToplam = 1_000m,
                OdenenTutar = 250m,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            db.Faturalar.Add(fatura);
            await db.SaveChangesAsync();
            fixture.FaturaId = fatura.Id;
            return fixture;
        }

        public CashTrackerDbContext CreateDbContext() => new(Options);

        public void Dispose()
        {
            try
            {
                if (File.Exists(DbPath)) File.Delete(DbPath);
            }
            catch
            {
            }
        }
    }
}
