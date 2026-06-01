using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Systemcel.Api.Services;

public static class PostgreSqlMigrationGuard
{
    public static async Task ApplyMigrationsAsync(CashTrackerDbContext db)
    {
        if (await HasEfMigrationHistoryAsync(db))
        {
            if (await AllLocalMigrationsAppliedAsync(db))
                return;

            await db.Database.MigrateAsync();
            return;
        }

        if (await HasSystemcelTablesAsync(db))
        {
            throw new InvalidOperationException(
                "PostgreSQL veritabaninda Systemcel tablolari var ama EF migration gecmisi yok. " +
                "Bu genelde eski EnsureCreated denemesinden kalir. Gelistirme veritabanini temizleyip " +
                "migration ile yeniden olusturun veya semayi dogruladiktan sonra manuel baseline uygulayin.");
        }

        await db.Database.MigrateAsync();
    }

    private static async Task<bool> AllLocalMigrationsAppliedAsync(CashTrackerDbContext db)
    {
        var migrationsAssembly = db.GetService<IMigrationsAssembly>();
        var localIds = migrationsAssembly.Migrations.Keys.ToList();
        if (localIds.Count == 0)
            return true;

        var appliedIds = await GetAppliedMigrationIdsAsync(db);
        return localIds.All(appliedIds.Contains);
    }

    private static async Task<HashSet<string>> GetAppliedMigrationIdsAsync(CashTrackerDbContext db)
    {
        const string sql = """SELECT "MigrationId" FROM "__EFMigrationsHistory";""";

        var rows = new HashSet<string>(StringComparer.Ordinal);
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (!reader.IsDBNull(0))
                rows.Add(reader.GetString(0));
        }

        return rows;
    }

    private static async Task<bool> HasEfMigrationHistoryAsync(CashTrackerDbContext db)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND table_name = '__EFMigrationsHistory'
            );
            """;

        return await ExecuteBooleanScalarAsync(db, sql);
    }

    private static async Task<bool> HasSystemcelTablesAsync(CashTrackerDbContext db)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND table_type = 'BASE TABLE'
                  AND table_name IN (
                    'AppSetting',
                    'BelgeDosya',
                    'CariHareket',
                    'CariKart',
                    'Fatura',
                    'FaturaSatir',
                    'GibPortalAyar',
                    'GibPortalIslemLog',
                    'Isletme',
                    'KalemTanimi',
                    'Kasa',
                    'StokHareket',
                    'TahsilatOdeme',
                    'UrunHizmet'
                  )
            );
            """;

        return await ExecuteBooleanScalarAsync(db, sql);
    }

    private static async Task<bool> ExecuteBooleanScalarAsync(CashTrackerDbContext db, string sql)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        var result = await cmd.ExecuteScalarAsync();
        return result is bool value && value;
    }
}
