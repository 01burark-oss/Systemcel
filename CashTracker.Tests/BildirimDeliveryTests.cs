using CashTracker.Core.Models;
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
}
