using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Persistence
{
    public static partial class SchemaMigrator
    {
        private const string VarsayilanIsletmeAdi = "Mevcut İşletme";
        private const string EskiVarsayilanIsletmeAdi = "Mevcut Isletme";
        private const string SchemaBootstrappedKey = "SchemaBootstrappedV2";
        private const string ApprovedAccountantProfilesPublishedKey = "ApprovedAccountantProfilesPublishedV1";

        public static void EnsureKasaSchema(CashTrackerDbContext db)
        {
            var conn = db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                conn.Open();

            EnsureKasaTable(db, conn);
            EnsureIsletmeTable(db);
            EnsureKalemTanimiTable(db);
            EnsureAppSettingTable(db);
            EnsureCariKartTable(db);
            EnsureCariHareketTable(db);
            EnsureUrunHizmetTable(db);
            EnsureStokHareketTable(db);
            EnsureFaturaTable(db);
            EnsureFaturaSatirTable(db);
            EnsureTahsilatOdemeTable(db);
            EnsureBelgeDosyaTable(db);
            EnsureGibPortalAyarTable(db);
            EnsureGibPortalIslemLogTable(db);
            EnsureWebAuthTables(db);

            EnsureKasaColumns(db, conn);
            EnsureIsletmeColumns(db, conn);
            EnsureWebAuthColumns(db, conn);
            NormalizeLegacyBusinessNames(db);
            EnsureIndexes(db);

            var activeIsletmeId = EnsureActiveBusiness(db, conn);
            if (!HasAppSettingMarker(conn, SchemaBootstrappedKey))
            {
                BackfillKasaBusiness(db, activeIsletmeId);
                BackfillKasaKalem(db);
                SeedKalemFromKasa(db);
                SetAppSettingMarker(db, SchemaBootstrappedKey);
            }

            if (!HasAppSettingMarker(conn, ApprovedAccountantProfilesPublishedKey))
            {
                PublishApprovedCompleteAccountantProfiles(db);
                SetAppSettingMarker(db, ApprovedAccountantProfilesPublishedKey);
            }

            EnsureDefaultKalemler(db, activeIsletmeId);
        }

        private static bool TableExists(DbConnection conn, string tableName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=$name;";
            var p = cmd.CreateParameter();
            p.ParameterName = "$name";
            p.Value = tableName;
            cmd.Parameters.Add(p);
            var result = cmd.ExecuteScalar();
            return result != null && result != DBNull.Value;
        }

        private static bool ColumnExists(DbConnection conn, string tableName, string columnName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({tableName});";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var name = reader.GetString(1);
                if (string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static int ExecuteScalarInt(DbConnection conn, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var result = cmd.ExecuteScalar();
            if (result is null || result == DBNull.Value)
                return 0;
            return Convert.ToInt32(result);
        }

        private static bool HasAppSettingMarker(DbConnection conn, string key)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(1) FROM AppSetting WHERE Key = $key AND Value = '1';";
            var p = cmd.CreateParameter();
            p.ParameterName = "$key";
            p.Value = key;
            cmd.Parameters.Add(p);
            var result = cmd.ExecuteScalar();
            return result != null && result != DBNull.Value && Convert.ToInt32(result) > 0;
        }

        private static void SetAppSettingMarker(CashTrackerDbContext db, string key)
        {
            db.Database.ExecuteSqlRaw(@"
INSERT INTO AppSetting (Key, Value, CreatedAt, UpdatedAt)
SELECT {0}, '1', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
WHERE NOT EXISTS (
    SELECT 1 FROM AppSetting WHERE Key = {0}
);", key);
        }

        private static void NormalizeLegacyBusinessNames(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(
                "UPDATE Isletme SET Ad = {0} WHERE Ad = {1};",
                VarsayilanIsletmeAdi,
                EskiVarsayilanIsletmeAdi);
        }

        private static partial void EnsureKasaTable(CashTrackerDbContext db, DbConnection conn);
        private static partial void EnsureIsletmeTable(CashTrackerDbContext db);
        private static partial void EnsureKalemTanimiTable(CashTrackerDbContext db);
        private static partial void EnsureAppSettingTable(CashTrackerDbContext db);
        private static partial void EnsureCariKartTable(CashTrackerDbContext db);
        private static partial void EnsureCariHareketTable(CashTrackerDbContext db);
        private static partial void EnsureUrunHizmetTable(CashTrackerDbContext db);
        private static partial void EnsureStokHareketTable(CashTrackerDbContext db);
        private static partial void EnsureFaturaTable(CashTrackerDbContext db);
        private static partial void EnsureFaturaSatirTable(CashTrackerDbContext db);
        private static partial void EnsureTahsilatOdemeTable(CashTrackerDbContext db);
        private static partial void EnsureBelgeDosyaTable(CashTrackerDbContext db);
        private static partial void EnsureGibPortalAyarTable(CashTrackerDbContext db);
        private static partial void EnsureGibPortalIslemLogTable(CashTrackerDbContext db);
        private static partial void EnsureWebAuthTables(CashTrackerDbContext db);
        private static partial void EnsureKasaColumns(CashTrackerDbContext db, DbConnection conn);
        private static partial void EnsureIsletmeColumns(CashTrackerDbContext db, DbConnection conn);
        private static partial void EnsureWebAuthColumns(CashTrackerDbContext db, DbConnection conn);
        private static partial void EnsureIndexes(CashTrackerDbContext db);
        private static partial int EnsureActiveBusiness(CashTrackerDbContext db, DbConnection conn);
        private static partial void BackfillKasaBusiness(CashTrackerDbContext db, int activeIsletmeId);
        private static partial void BackfillKasaKalem(CashTrackerDbContext db);
        private static partial void SeedKalemFromKasa(CashTrackerDbContext db);
        private static partial void PublishApprovedCompleteAccountantProfiles(CashTrackerDbContext db);
        private static partial void EnsureDefaultKalemler(CashTrackerDbContext db, int isletmeId);
    }
}
