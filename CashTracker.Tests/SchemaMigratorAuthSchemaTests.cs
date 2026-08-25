using System;
using System.Data.Common;
using System.IO;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class SchemaMigratorAuthSchemaTests
    {
        [Fact]
        public void PostgreSqlMigrations_AccountantPaymentGateIsFollowedByMonthlyPeriods()
        {
            var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
                .UseNpgsql("Host=localhost;Database=systemcel_migration_metadata;Username=test;Password=test")
                .Options;
            using var db = new CashTrackerDbContext(options);

            var migrations = db.Database.GetMigrations().ToList();
            var gateIndex = migrations.IndexOf("20260824120000_AccountantServicePaymentGate");
            var monthlyIndex = migrations.IndexOf("20260824143000_AccountantMonthlyServicePeriods");
            var supportIndex = migrations.IndexOf("20260824160000_ProductSupportTickets");
            var planChangesIndex = migrations.IndexOf("20260824173000_SubscriptionPlanChanges");
            var notificationsIndex = migrations.IndexOf("20260824180000_PersistentNotificationDelivery");
            var bankIndex = migrations.IndexOf("20260824190000_BankReconciliationMvp");
            var stockLedgerIndex = migrations.IndexOf("20260824200000_AdvancedStockLedger");
            var developerApiIndex = migrations.IndexOf("20260824210000_DeveloperApiAccess");

            Assert.True(gateIndex >= 0);
            Assert.True(monthlyIndex > gateIndex);
            Assert.True(supportIndex > monthlyIndex);
            Assert.True(planChangesIndex > supportIndex);
            Assert.True(notificationsIndex > planChangesIndex);
            Assert.True(bankIndex > notificationsIndex);
            Assert.True(stockLedgerIndex > bankIndex);
            Assert.True(developerApiIndex > stockLedgerIndex);
        }

        [Fact]
        public void PostgreSqlSnapshot_AdvancedStockMatchesCurrentModel()
        {
            var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
                .UseNpgsql("Host=localhost;Database=systemcel_migration_metadata;Username=test;Password=test")
                .Options;
            using var db = new CashTrackerDbContext(options);

            var migrationsAssembly = db.GetService<IMigrationsAssembly>();
            var modelInitializer = db.GetService<IModelRuntimeInitializer>();
            var modelDiffer = db.GetService<IMigrationsModelDiffer>();
            var snapshotModel = modelInitializer.Initialize(migrationsAssembly.ModelSnapshot!.Model, designTime: true);
            var currentModel = db.GetService<IDesignTimeModel>().Model;
            var differences = modelDiffer.GetDifferences(snapshotModel.GetRelationalModel(), currentModel.GetRelationalModel())
                .Where(x => x switch
                {
                    AlterColumnOperation column => column.Table.StartsWith("Stok", StringComparison.Ordinal),
                    CreateIndexOperation index => index.Table.StartsWith("Stok", StringComparison.Ordinal),
                    _ => false
                })
                .ToList();
            Assert.True(differences.Count == 0, string.Join(Environment.NewLine, differences.Select(x => x switch
            {
                AlterColumnOperation column => $"AlterColumn {column.Table}.{column.Name} type={column.ColumnType} max={column.MaxLength}",
                CreateIndexOperation index => $"CreateIndex {index.Table}.{index.Name} ({string.Join(',', index.Columns)}) unique={index.IsUnique}",
                _ => x.GetType().Name
            })));
        }

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
                Assert.True(TableExists(conn, "MuhasebeciHizmetOdemesi"));
                Assert.True(TableExists(conn, "MuhasebeciAktarimAlacagi"));
                Assert.True(TableExists(conn, "DestekTalebi"));
                Assert.True(TableExists(conn, "BildirimKaydi"));
                Assert.True(TableExists(conn, "BildirimTercihi"));
                Assert.True(TableExists(conn, "BildirimTeslimOutbox"));
                Assert.True(TableExists(conn, "BankaHareketi"));
                Assert.True(TableExists(conn, "StokDepo"));
                Assert.True(TableExists(conn, "StokDefterIslemi"));
                Assert.True(TableExists(conn, "GelistiriciApiAnahtari"));
                Assert.True(TableExists(conn, "Abonelik"));
                Assert.True(TableExists(conn, "IsletmeDeneme"));
                Assert.True(TableExists(conn, "AbonelikOnayi"));
                Assert.True(TableExists(conn, "OdemeIslemi"));
                Assert.True(TableExists(conn, "OdemeOlayi"));
                Assert.True(TableExists(conn, "IsletmeEntitlement"));
                Assert.True(TableExists(conn, "AiKullanimDonemi"));
                Assert.True(TableExists(conn, "YonetimDenetimKaydi"));
                Assert.True(ColumnExists(conn, "Isletme", "TenantTipi"));
                Assert.True(ColumnExists(conn, "Isletme", "ClerkOrganizationId"));
                Assert.True(ColumnExists(conn, "Abonelik", "FaturalamaDonemi"));
                Assert.True(ColumnExists(conn, "Abonelik", "EkMusteriKredisi"));
                Assert.True(ColumnExists(conn, "Abonelik", "DonemTutari"));
                Assert.True(ColumnExists(conn, "Abonelik", "OdemeSorunuAt"));
                Assert.True(ColumnExists(conn, "Abonelik", "ToleransBitisAt"));
                Assert.True(ColumnExists(conn, "IsletmeDeneme", "HesapTipi"));
                Assert.True(ColumnExists(conn, "IsletmeDeneme", "FaturalamaDonemi"));
                Assert.True(ColumnExists(conn, "IsletmeDeneme", "EkMusteriKredisi"));
                Assert.True(ColumnExists(conn, "IsletmeDeneme", "DonemSonundaIptal"));
                Assert.True(ColumnExists(conn, "IsletmeDeneme", "YediGunHatirlatmaAt"));
                Assert.True(ColumnExists(conn, "OdemeIslemi", "SonOlayAt"));
                Assert.True(ColumnExists(conn, "OdemeIslemi", "EkMusteriKredisi"));
                Assert.True(ColumnExists(conn, "MuhasebeciMusteriTalebi", "AylikHizmetBedeli"));
                Assert.True(ColumnExists(conn, "MuhasebeciHizmetOdemesi", "HizmetDonemi"));
                Assert.True(ColumnExists(conn, "MuhasebeciHizmetOdemesi", "PlatformKomisyonOrani"));
                Assert.True(ColumnExists(conn, "AbonelikOnayi", "EkMusteriKredisi"));
                Assert.True(ColumnExists(conn, "StokHareket", "DepoId"));
                Assert.True(ColumnExists(conn, "StokHareket", "StokDefterIslemiId"));
                Assert.True(ColumnExists(conn, "StokHareket", "RezerveMiktar"));
                Assert.True(IndexExists(conn, "IX_IsletmeDeneme_IsletmeId_HesapTipi"));
                Assert.True(IndexExists(conn, "IX_AbonelikOnayi_IsletmeId_CheckoutAnahtari"));
                Assert.True(IndexExists(conn, "IX_OdemeIslemi_IsletmeId_CheckoutAnahtari"));
                Assert.True(IndexExists(conn, "IX_OdemeOlayi_OdemeSaglayici_OlayId"));
                Assert.True(IndexExists(conn, "IX_MuhasebeciHizmetOdemesi_TalepId_HizmetDonemi"));
                Assert.True(IndexExists(conn, "IX_MuhasebeciAktarimAlacagi_MuhasebeciHizmetOdemesiId"));
                Assert.True(IndexExists(conn, "IX_DestekTalebi_IsletmeId_OlusturmaAnahtari"));
                Assert.True(IndexExists(conn, "IX_BildirimKaydi_IsletmeId_KullaniciRef_KaynakAnahtari"));
                Assert.True(IndexExists(conn, "IX_BildirimTercihi_IsletmeId_KullaniciRef"));
                Assert.True(IndexExists(conn, "IX_BildirimTeslimOutbox_IsletmeId_KullaniciRef_Kanal_IdempotencyAnahtari"));
                Assert.True(IndexExists(conn, "IX_BankaHareketi_IsletmeId_KaynakHash"));
                Assert.True(IndexExists(conn, "IX_StokDepo_IsletmeId_Kod"));
                Assert.True(IndexExists(conn, "IX_StokDefterIslemi_IsletmeId_IslemAnahtari"));
                Assert.True(IndexExists(conn, "IX_GelistiriciApiAnahtari_Prefix"));
                Assert.True(IndexExists(conn, "IX_YonetimDenetimKaydi_IsletmeId"));
                Assert.True(IndexExists(conn, "IX_YonetimDenetimKaydi_CreatedAt"));
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

        [Fact]
        public void EnsureKasaSchema_EskiDenemeKayitlariniTekHesapTipineIndirger()
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"cashtracker_legacy_trial_{Guid.NewGuid():N}.db");

            try
            {
                var options = new DbContextOptionsBuilder<CashTrackerDbContext>()
                    .UseSqlite($"Data Source={dbPath}")
                    .Options;

                using var db = new CashTrackerDbContext(options);
                db.Database.OpenConnection();
                db.Database.ExecuteSqlRaw(@"
CREATE TABLE IsletmeDeneme (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IsletmeId INTEGER NOT NULL,
    PlanKodu TEXT NOT NULL DEFAULT 'isletme_baslangic',
    Durum TEXT NOT NULL DEFAULT 'Aktif',
    BaslangicAt TEXT NOT NULL,
    BitisAt TEXT NOT NULL,
    OdemeYontemiEklendi INTEGER NOT NULL DEFAULT 0,
    OdemeSaglayici TEXT NOT NULL DEFAULT '',
    SaglayiciMusteriId TEXT NOT NULL DEFAULT '',
    SaglayiciOdemeYontemiId TEXT NOT NULL DEFAULT '',
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);
INSERT INTO IsletmeDeneme
    (IsletmeId, PlanKodu, Durum, BaslangicAt, BitisAt, CreatedAt, UpdatedAt)
VALUES
    (7, 'isletme_baslangic', 'SonaErdi', '2026-01-01', '2026-01-31', '2026-01-01', '2026-02-01'),
    (7, 'isletme_buyume', 'Aktif', '2026-03-01', '2026-03-31', '2026-03-01', '2026-03-01');
CREATE UNIQUE INDEX IX_IsletmeDeneme_IsletmeId_PlanKodu
    ON IsletmeDeneme(IsletmeId, PlanKodu);");

                SchemaMigrator.EnsureKasaSchema(db);
                var conn = db.Database.GetDbConnection();

                Assert.True(ColumnExists(conn, "IsletmeDeneme", "HesapTipi"));
                Assert.True(ColumnExists(conn, "IsletmeDeneme", "FaturalamaDonemi"));
                Assert.True(IndexExists(conn, "IX_IsletmeDeneme_IsletmeId_HesapTipi"));
                Assert.False(IndexExists(conn, "IX_IsletmeDeneme_IsletmeId_PlanKodu"));
                Assert.Equal(1, ScalarInt(conn,
                    "SELECT COUNT(1) FROM IsletmeDeneme WHERE IsletmeId = 7 AND HesapTipi = 'Isletme';"));
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

        private static bool IndexExists(DbConnection conn, string indexName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type='index' AND name=$name;";
            var p = cmd.CreateParameter();
            p.ParameterName = "$name";
            p.Value = indexName;
            cmd.Parameters.Add(p);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static int ScalarInt(DbConnection conn, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }
}
