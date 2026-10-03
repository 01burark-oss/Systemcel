using System.Reflection;
using CashTracker.Infrastructure.Persistence;
using CashTracker.Infrastructure.Persistence.Migrations.PostgreSql;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace CashTracker.Tests;

public sealed class MarketplaceApprovalMigrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LegacyDispute_BackfillUsesRealTransitionAndResponseFromCurrentRound(bool sqliteBootstrap)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new CashTrackerDbContext(new DbContextOptionsBuilder<CashTrackerDbContext>().UseSqlite(connection).Options);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE TedarikciSiparis (Id INTEGER PRIMARY KEY, AnaSiparisId INTEGER, AliciIsletmeId INTEGER,
                TedarikciIsletmeId INTEGER, SiparisNo TEXT, Durum TEXT, UpdatedAt TEXT,
                ItirazAcildiAt TEXT NULL, ItirazIlkYanitAt TEXT NULL);
            CREATE TABLE TedarikciSiparisDurumKaydi (Id INTEGER PRIMARY KEY, TedarikciSiparisId INTEGER,
                OncekiDurum TEXT, YeniDurum TEXT, CreatedAt TEXT);
            CREATE TABLE TedarikciSiparisSikayeti (TedarikciSiparisId INTEGER, AliciIsletmeId INTEGER,
                TedarikciIsletmeId INTEGER, YanitlandiAt TEXT);
            INSERT INTO TedarikciSiparis (Id, AnaSiparisId, AliciIsletmeId, TedarikciIsletmeId, SiparisNo, Durum, UpdatedAt)
            VALUES (1, 1, 1, 2, 'Legacy-open', 'Itirazli', '2026-10-02 10:00:00'),
                   (2, 1, 1, 2, 'Legacy-closed', 'Tamamlandi', '2026-10-02 10:00:00');
            INSERT INTO TedarikciSiparisDurumKaydi (Id, TedarikciSiparisId, OncekiDurum, YeniDurum, CreatedAt)
            VALUES (1, 1, 'SevkEdildi', 'Itirazli', '2026-09-28 10:00:00'),
                   (2, 1, 'Itirazli', 'Itirazli', '2026-10-02 10:00:00');
            INSERT INTO TedarikciSiparisSikayeti (TedarikciSiparisId, AliciIsletmeId, TedarikciIsletmeId, YanitlandiAt)
            VALUES (1, 1, 2, '2026-09-27 12:00:00'), (1, 1, 2, '2026-09-29 12:00:00');
            """);
        if (sqliteBootstrap)
        {
            var method = typeof(SchemaMigrator).GetMethod("EnsureMarketplaceAcceptanceColumns", BindingFlags.Static | BindingFlags.NonPublic)!;
            method.Invoke(null, [db, connection]);
            method.Invoke(null, [db, connection]);
        }
        else
        {
            var migration = new MarketplaceReceiptApprovalAndDisputeTargets();
            var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
            typeof(MarketplaceReceiptApprovalAndDisputeTargets).GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(migration, [builder]);
            foreach (var sql in builder.Operations.OfType<SqlOperation>()) await db.Database.ExecuteSqlRawAsync(sql.Sql);
        }
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT ItirazAcildiAt, ItirazIlkYanitAt FROM TedarikciSiparis ORDER BY Id";
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(new DateTime(2026, 9, 28, 10, 0, 0), reader.GetDateTime(0));
        Assert.Equal(new DateTime(2026, 9, 29, 12, 0, 0), reader.GetDateTime(1));
        Assert.True(await reader.ReadAsync());
        Assert.True(reader.IsDBNull(0));
        Assert.True(reader.IsDBNull(1));
    }
}
