using System;
using System.Data.Common;
using System.IO;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class SchemaMigratorAuthSchemaTests
    {
        [Fact]
        public void EnsureKasaSchema_WebAuthVeAbonelikTablolariniOlusturur()
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"cashtracker_schema_{Guid.NewGuid():N}.db");

            try
            {
                var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
                    .UseSqlite($"Data Source={dbPath}")
                    .Options;

                using var db = new CashTrackerDbContext(options);
                SchemaMigrator.EnsureKasaSchema(db);
                var conn = db.Database.GetDbConnection();

                Assert.True(TableExists(conn, "Kullanici"));
                Assert.True(TableExists(conn, "IsletmeUyelik"));
                Assert.True(TableExists(conn, "MuhasebeciMusteri"));
                Assert.True(TableExists(conn, "Abonelik"));
                Assert.True(TableExists(conn, "IsletmeDeneme"));
                Assert.True(TableExists(conn, "IsletmeEntitlement"));
                Assert.True(TableExists(conn, "AiKullanimDonemi"));
                Assert.True(ColumnExists(conn, "Isletme", "TenantTipi"));
                Assert.True(ColumnExists(conn, "Isletme", "ClerkOrganizationId"));
            }
            finally
            {
                try
                {
                    if (File.Exists(dbPath))
                        File.Delete(dbPath);
                }
                catch
                {
                }
            }
        }

        private static bool TableExists(DbConnection conn, string tableName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name=$name;";
            var p = cmd.CreateParameter();
            p.ParameterName = "$name";
            p.Value = tableName;
            cmd.Parameters.Add(p);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static bool ColumnExists(DbConnection conn, string tableName, string columnName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({tableName});";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
