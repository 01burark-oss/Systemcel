using CashTracker.Core.Models;
using CashTracker.Core.Entities;
using CashTracker.Infrastructure.Payments;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests;

public sealed class BildirimDeliveryTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"systemcel_notifications_{Guid.NewGuid():N}.db");
    private readonly Factory _factory;
    private readonly BildirimService _service;

    public BildirimDeliveryTests()
    {
        _factory = new Factory(_dbPath);
        using var db = _factory.CreateDbContext();
        SchemaMigrator.EnsureKasaSchema(db);
        _service = new BildirimService(_factory);
    }

    [Fact]
    public async Task SnapshotUpsert_IsIdempotentAndReadStateIsTenantUserScoped()
    {
        var snapshot = new BildirimSnapshot("invoice:9", "odeme", "yuksek", "Vade geçti", "Ödeme bekliyor", "İncele", "/app/faturalar");
        var first = await _service.SyncAndListAsync(7, "user-a", new[] { snapshot });
        var second = await _service.SyncAndListAsync(7, "user-a", new[] { snapshot with { Mesaj = "Güncel ödeme bekliyor" } });

        Assert.Single(first);
        Assert.Single(second);
        Assert.Equal("Güncel ödeme bekliyor", second[0].Mesaj);
        Assert.Equal(-1, await _service.MarkReadAsync(8, "user-a", first[0].Id));
        Assert.Equal(-1, await _service.MarkReadAsync(7, "user-b", first[0].Id));
        Assert.Equal(0, await _service.MarkReadAsync(7, "user-a", first[0].Id));

        await using var db = _factory.CreateDbContext();
        Assert.Equal(1, await db.BildirimKayitlari.CountAsync());
        Assert.Equal(1, await db.BildirimTeslimOutboxlari.CountAsync());
    }

    [Fact]
    public async Task MultipleSnapshots_PersistEachOutboxAndRespectDisabledAppChannel()
    {
        var snapshots = new[]
        {
            new BildirimSnapshot("invoice:10", "odeme", "normal", "Bir", "İlk", "Aç", "/app/faturalar"),
            new BildirimSnapshot("invoice:11", "odeme", "normal", "İki", "İkinci", "Aç", "/app/faturalar")
        };

        await _service.SyncAndListAsync(7, "user-a", snapshots);
        await using (var db = _factory.CreateDbContext())
        {
            Assert.Equal(2, await db.BildirimKayitlari.CountAsync());
            Assert.Equal(2, await db.BildirimTeslimOutboxlari.CountAsync());
        }

        await _service.SavePreferencesAsync(8, "user-b", new BildirimTercihModeli(
            UygulamaAktif: false,
            EpostaAktif: false,
            TelegramAktif: false,
            SessizSaatAktif: false,
            SessizBaslangicDakika: 1320,
            SessizBitisDakika: 480,
            SaatDilimi: "Europe/Istanbul"));
        await _service.SyncAndListAsync(8, "user-b", new[] { snapshots[0] });

        await using var verified = _factory.CreateDbContext();
        Assert.Equal(0, await verified.BildirimTeslimOutboxlari.CountAsync(x => x.IsletmeId == 8));
    }

    [Fact]
    public async Task OutboxClaim_RequiresLeaseTokenAndMovesRepeatedFailureToDeadLetter()
    {
        var now = new DateTime(2026, 8, 24, 8, 0, 0, DateTimeKind.Utc);
        await _service.EnqueueAsync(7, "user-a", null, "evt-1", BildirimKanallari.Eposta, "{\"title\":\"test\"}", now);
        var claim = Assert.Single(await _service.ClaimAsync(10, now, TimeSpan.FromMinutes(2)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CompleteAsync(claim.Id, "wrong-token", now));

        await _service.FailAsync(claim.Id, claim.ClaimToken, "smtp_unavailable", now, maxAttempts: 1);
        await using var db = _factory.CreateDbContext();
        var row = await db.BildirimTeslimOutboxlari.SingleAsync();
        Assert.Equal(BildirimTeslimDurumlari.DeadLetter, row.Durum);
        Assert.Equal(1, row.DenemeSayisi);
        Assert.NotNull(row.DeadLetterAt);
        Assert.Equal("smtp_unavailable", row.SonHataKodu);
    }

    [Fact]
    public async Task FailedDelivery_CanBeListedAndRetriedOnceWithoutCreatingAnotherOutboxRecord()
    {
        var now = new DateTime(2026, 9, 23, 8, 0, 0, DateTimeKind.Utc);
        await _service.SavePreferencesAsync(7, "user-a", new BildirimTercihModeli(
            true, true, false, false, 1320, 480, "Europe/Istanbul"));
        await _service.EnqueueAsync(7, "user-a", null, "evt-retry", BildirimKanallari.Eposta,
            "{\"title\":\"test\"}", now);
        var claim = Assert.Single(await _service.ClaimAsync(10, now, TimeSpan.FromMinutes(2)));
        await _service.FailAsync(claim.Id, claim.ClaimToken, "smtp_unavailable", now, maxAttempts: 1);

        var failed = Assert.Single(await _service.ListFailedAsync());
        Assert.Equal(claim.Id, failed.Id);
        Assert.Equal("smtp_unavailable", failed.SonHataKodu);
        await _service.RetryFailedAsync(failed.Id, now.AddMinutes(1));
        Assert.Empty(await _service.ListFailedAsync());
        var retried = Assert.Single(await _service.ClaimAsync(10, now.AddMinutes(1), TimeSpan.FromMinutes(2)));
        Assert.Equal(claim.Id, retried.Id);
        Assert.Equal(0, retried.DenemeSayisi);
        await _service.CompleteAsync(retried.Id, retried.ClaimToken, now.AddMinutes(1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RetryFailedAsync(failed.Id, now.AddMinutes(2)));

        await using var db = _factory.CreateDbContext();
        Assert.Equal(1, await db.BildirimTeslimOutboxlari.CountAsync());
    }

    [Fact]
    public async Task EmailAdapter_ResolvesRecipientThroughActiveTenantMembership()
    {
        await using (var db = _factory.CreateDbContext())
        {
            var user = new Kullanici { AuthProviderUserId = "user-a", Eposta = "user-a@systemcel.local", AdSoyad = "A", Durum = "Aktif" };
            db.Kullanicilar.Add(user);
            await db.SaveChangesAsync();
            db.IsletmeUyelikleri.Add(new IsletmeUyelik { IsletmeId = 7, KullaniciId = user.Id, Rol = "isletme_sahibi", Durum = "Aktif", DavetEposta = user.Eposta });
            await db.SaveChangesAsync();
        }
        var client = new CapturingEmailClient();
        var adapter = new EpostaBildirimAdapter(_factory, client, new SubscriptionReminderEmailOptions { Host = "smtp.test", FromAddress = "no-reply@systemcel.local" });
        await adapter.SendAsync(new BildirimOutboxClaim(1, 7, "user-a", BildirimKanallari.Eposta,
            "{\"Baslik\":\"Vade geçti\",\"Mesaj\":\"Ödeme bekliyor\",\"Url\":\"/app/faturalar\"}", "claim", 0));

        Assert.Equal("user-a@systemcel.local", client.Recipient);
        Assert.Equal("Vade geçti", client.Subject);
        Assert.Contains("https://systemcel.app/app/faturalar", client.Body);

        await Assert.ThrowsAsync<InvalidOperationException>(() => adapter.SendAsync(new BildirimOutboxClaim(
            2, 8, "user-a", BildirimKanallari.Eposta, "{\"Baslik\":\"X\",\"Mesaj\":\"Y\"}", "claim", 0)));
    }

    [Fact]
    public async Task LegacyTrialSnapshot_DoesNotDuplicateLifecycleEmailSender()
    {
        await _service.SavePreferencesAsync(7, "user-a", new BildirimTercihModeli(
            UygulamaAktif: true, EpostaAktif: true, TelegramAktif: false,
            SessizSaatAktif: false, SessizBaslangicDakika: 1320, SessizBitisDakika: 480,
            SaatDilimi: "Europe/Istanbul"));
        await _service.SyncAndListAsync(7, "user-a", new[]
        {
            new BildirimSnapshot("abonelik-deneme-4-7", "abonelik", "orta", "Deneme", "7 gün kaldı", "İncele", "/app/abonelik")
        });

        await using var db = _factory.CreateDbContext();
        Assert.Single(await db.BildirimTeslimOutboxlari.ToListAsync());
        Assert.Equal(BildirimKanallari.Uygulama, (await db.BildirimTeslimOutboxlari.SingleAsync()).Kanal);
    }

    public void Dispose()
    {
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { }
    }

    private sealed class Factory : IDbContextFactory<CashTrackerDbContext>
    {
        private readonly DbContextOptions<CashTrackerDbContext> _options;
        public Factory(string path) => _options = new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite($"Data Source={path}").Options;
        public CashTrackerDbContext CreateDbContext() => new(_options);
        public Task<CashTrackerDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }

    private sealed class CapturingEmailClient : IEmailDeliveryClient
    {
        public bool IsConfigured => true;
        public string Recipient { get; private set; } = string.Empty;
        public string Subject { get; private set; } = string.Empty;
        public string Body { get; private set; } = string.Empty;
        public Task SendAsync(string recipient, string subject, string body, CancellationToken ct = default)
        {
            Recipient = recipient;
            Subject = subject;
            Body = body;
            return Task.CompletedTask;
        }
    }
}
