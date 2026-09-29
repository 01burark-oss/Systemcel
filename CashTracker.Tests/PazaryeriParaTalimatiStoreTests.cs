using CashTracker.Core.Entities;
using CashTracker.Infrastructure.Payments;
using CashTracker.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace CashTracker.Tests;

public sealed class PazaryeriParaTalimatiStoreTests
{
    [Fact]
    public async Task InstructionRollsBackWithBusinessTransaction()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using (var db = fixture.CreateDb())
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            await PazaryeriParaTalimatiStore.EnqueueAsync(db, Draft());
            await db.SaveChangesAsync();
            await tx.RollbackAsync();
        }

        await using var verified = fixture.CreateDb();
        Assert.Empty(await verified.PazaryeriParaTalimatlari.ToListAsync());
    }

    [Fact]
    public async Task ReplayReturnsSameInstructionAndDifferentPayloadIsRejected()
    {
        await using var fixture = await Fixture.CreateAsync();
        long firstId;
        await using (var db = fixture.CreateDb())
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            var first = await PazaryeriParaTalimatiStore.EnqueueAsync(db, Draft());
            var sameTransactionReplay = await PazaryeriParaTalimatiStore.EnqueueAsync(db, Draft());
            Assert.Same(first, sameTransactionReplay);
            await db.SaveChangesAsync();
            firstId = first.Id;
            await tx.CommitAsync();
        }
        await using (var db = fixture.CreateDb())
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            var replay = await PazaryeriParaTalimatiStore.EnqueueAsync(db, Draft());
            Assert.Equal(firstId, replay.Id);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                PazaryeriParaTalimatiStore.EnqueueAsync(db, Draft() with { Tutar = 2m }));
        }
        await using var verified = fixture.CreateDb();
        Assert.Single(await verified.PazaryeriParaTalimatlari.ToListAsync());
    }

    [Fact]
    public async Task UnknownResultIsNeverAutomaticallyClaimedAgain()
    {
        await using var fixture = await Fixture.CreateAsync();
        long instructionId;
        await using (var db = fixture.CreateDb())
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            var instruction = await PazaryeriParaTalimatiStore.EnqueueAsync(db, Draft());
            await db.SaveChangesAsync();
            instructionId = instruction.Id;
            await tx.CommitAsync();
        }
        var store = new PazaryeriParaTalimatiStore(fixture.Factory);
        var now = DateTime.UtcNow;
        var claimed = await store.ClaimReadyAsync(now);
        Assert.Equal(instructionId, claimed!.Id);
        Assert.Equal(1, claimed.DenemeSayisi);
        Assert.Null(await store.ClaimReadyAsync(now.AddHours(1)));
        await store.MarkUnknownAsync(instructionId, "timeout", now.AddMinutes(1));
        Assert.Null(await store.ClaimReadyAsync(now.AddDays(1)));

        await using (var db = fixture.CreateDb())
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            await PazaryeriParaTalimatiStore.ApplyCompletedAsync(db, instructionId, "provider-ref", now.AddMinutes(2));
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        await using var verified = fixture.CreateDb();
        var result = await verified.PazaryeriParaTalimatlari.SingleAsync();
        Assert.Equal(PazaryeriParaTalimatiDurumlari.Tamamlandi, result.Durum);
        Assert.Equal("provider-ref", result.SaglayiciIslemId);
        Assert.Equal(1, result.DenemeSayisi);
        Assert.Null(await store.ClaimReadyAsync(now.AddDays(2)));
    }

    private static PazaryeriParaTalimatiTaslagi Draft() => new(
        1, null, null, PazaryeriParaTalimatiTurleri.Iade,
        "Fake", "refund:order:1", "order:1", 1m, "TRY");

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public IDbContextFactory<CashTrackerDbContext> Factory { get; }

        private Fixture(SqliteConnection connection, IDbContextFactory<CashTrackerDbContext> factory)
        {
            _connection = connection;
            Factory = factory;
        }

        public CashTrackerDbContext CreateDb() => Factory.CreateDbContext();

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
                .UseSqlite(connection).Options;
            var factory = new PooledDbContextFactory<CashTrackerDbContext>(options);
            var fixture = new Fixture(connection, factory);
            await using var db = fixture.CreateDb();
            await db.Database.EnsureCreatedAsync();
            db.Isletmeler.Add(new Isletme { Id = 1, Ad = "buyer" });
            await db.SaveChangesAsync();
            return fixture;
        }

        public async ValueTask DisposeAsync() => await _connection.DisposeAsync();
    }
}
