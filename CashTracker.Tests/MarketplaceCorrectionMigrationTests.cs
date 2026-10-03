using System.Reflection;
using CashTracker.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests;

public sealed class MarketplaceCorrectionMigrationTests
{
    [Fact]
    public async Task SQLiteLegacyBootstrap_CreatesCorrectionTableAndLinksWithoutChangingOriginalRows()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new CashTrackerDbContext(new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite(connection).Options);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE StokHareket (Id INTEGER PRIMARY KEY, IsletmeId INTEGER NOT NULL, SubeId INTEGER NULL,
                UrunHizmetId INTEGER NOT NULL, DepoId INTEGER NULL, StokDefterIslemiId INTEGER NULL,
                TedarikciSiparisId INTEGER NULL, TedarikciSevkiyatId INTEGER NULL, TedarikciMalKabulId INTEGER NULL,
                Tarih TEXT NOT NULL, Miktar NUMERIC NOT NULL, RezerveMiktar NUMERIC NOT NULL,
                BirimMaliyet NUMERIC NOT NULL, MaliyetParaBirimi TEXT NOT NULL, MaliyetKurSnapshot NUMERIC NOT NULL,
                BirimMaliyetTry NUMERIC NOT NULL, HareketTipi TEXT NOT NULL, Kaynak TEXT NOT NULL,
                Aciklama TEXT NULL, CreatedAt TEXT NOT NULL);
            INSERT INTO StokHareket (Id,IsletmeId,UrunHizmetId,Tarih,Miktar,RezerveMiktar,BirimMaliyet,MaliyetParaBirimi,
                MaliyetKurSnapshot,BirimMaliyetTry,HareketTipi,Kaynak,CreatedAt)
                VALUES (1,1,1,'2026-10-03',3,0,20,'TRY',1,20,'Giris','PazaryeriMalKabul','2026-10-03');
            """);
        var method = typeof(SchemaMigrator).GetMethod("EnsureMarketplaceAcceptanceColumns", BindingFlags.Static | BindingFlags.NonPublic)!;
        method.Invoke(null, [db, connection]);
        method.Invoke(null, [db, connection]);
        db.ChangeTracker.Clear();
        var stock = await db.StokHareketleri.SingleAsync();
        Assert.Equal(3m, stock.Miktar);
        Assert.Null(stock.TedarikciMalKabulDuzeltmeId);
        Assert.Empty(await db.TedarikciMalKabulDuzeltmeleri.ToListAsync());
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_key_check";
        await using var reader = await command.ExecuteReaderAsync();
        Assert.False(await reader.ReadAsync());
    }
}
