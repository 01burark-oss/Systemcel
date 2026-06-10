using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Persistence
{
    public static partial class SchemaMigrator
    {
        private static partial int EnsureActiveBusiness(CashTrackerDbContext db, DbConnection conn)
        {
            var isletmeSayisi = ExecuteScalarInt(conn, "SELECT COUNT(1) FROM Isletme;");
            if (isletmeSayisi == 0)
            {
                var now = DateTime.Now;
                db.Database.ExecuteSqlRaw(
                    "INSERT INTO Isletme (Ad, TenantTipi, IsAktif, CreatedAt, UpdatedAt) VALUES ({0}, {1}, 1, {2}, {2});",
                    VarsayilanIsletmeAdi,
                    "Isletme",
                    now);
                return ExecuteScalarInt(conn, "SELECT Id FROM Isletme WHERE IsAktif = 1 ORDER BY Id LIMIT 1;");
            }

            var activeCount = ExecuteScalarInt(conn, "SELECT COUNT(1) FROM Isletme WHERE IsAktif = 1;");
            if (activeCount == 0)
            {
                var firstId = ExecuteScalarInt(conn, "SELECT Id FROM Isletme ORDER BY Id LIMIT 1;");
                db.Database.ExecuteSqlRaw(
                    "UPDATE Isletme SET IsAktif = CASE WHEN Id = {0} THEN 1 ELSE 0 END;",
                    firstId);
                return firstId;
            }

            var keepId = ExecuteScalarInt(conn, "SELECT Id FROM Isletme WHERE IsAktif = 1 ORDER BY Id LIMIT 1;");
            if (activeCount > 1)
            {
                db.Database.ExecuteSqlRaw(
                    "UPDATE Isletme SET IsAktif = CASE WHEN Id = {0} THEN 1 ELSE 0 END;",
                    keepId);
            }

            return keepId;
        }

        private static partial void BackfillKasaBusiness(CashTrackerDbContext db, int activeIsletmeId)
        {
            db.Database.ExecuteSqlRaw(
                "UPDATE Kasa SET IsletmeId = {0} WHERE IsletmeId IS NULL OR IsletmeId = 0;",
                activeIsletmeId);
        }

        private static partial void BackfillKasaKalem(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
UPDATE Kasa
SET Kalem = TRIM(GiderTuru)
WHERE (Kalem IS NULL OR TRIM(Kalem) = '')
  AND (Tip = 'Gider' OR Tip = 'Cikis')
  AND GiderTuru IS NOT NULL
  AND TRIM(GiderTuru) <> '';");

            db.Database.ExecuteSqlRaw(@"
UPDATE Kasa
SET Kalem = 'Genel Gider'
WHERE (Kalem IS NULL OR TRIM(Kalem) = '')
  AND (Tip = 'Gider' OR Tip = 'Cikis');");

            db.Database.ExecuteSqlRaw(@"
UPDATE Kasa
SET Kalem = 'Genel Gelir'
WHERE (Kalem IS NULL OR TRIM(Kalem) = '')
  AND (Tip = 'Gelir' OR Tip = 'Giris');");

            db.Database.ExecuteSqlRaw(@"
UPDATE Kasa
SET GiderTuru = Kalem
WHERE (Tip = 'Gider' OR Tip = 'Cikis')
  AND (GiderTuru IS NULL OR TRIM(GiderTuru) = '')
  AND Kalem IS NOT NULL
  AND TRIM(Kalem) <> '';");
        }

        private static partial void SeedKalemFromKasa(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
INSERT INTO KalemTanimi (IsletmeId, Tip, Ad, CreatedAt)
SELECT DISTINCT k.IsletmeId, 'Gelir', TRIM(k.Kalem), CURRENT_TIMESTAMP
FROM Kasa k
WHERE (k.Tip = 'Gelir' OR k.Tip = 'Giris')
  AND k.Kalem IS NOT NULL
  AND TRIM(k.Kalem) <> ''
  AND NOT EXISTS (
      SELECT 1
      FROM KalemTanimi kt
      WHERE kt.IsletmeId = k.IsletmeId
        AND kt.Tip = 'Gelir'
        AND LOWER(kt.Ad) = LOWER(TRIM(k.Kalem))
  );");

            db.Database.ExecuteSqlRaw(@"
INSERT INTO KalemTanimi (IsletmeId, Tip, Ad, CreatedAt)
SELECT DISTINCT k.IsletmeId, 'Gider', TRIM(k.Kalem), CURRENT_TIMESTAMP
FROM Kasa k
WHERE (k.Tip = 'Gider' OR k.Tip = 'Cikis')
  AND k.Kalem IS NOT NULL
  AND TRIM(k.Kalem) <> ''
  AND NOT EXISTS (
      SELECT 1
      FROM KalemTanimi kt
      WHERE kt.IsletmeId = k.IsletmeId
        AND kt.Tip = 'Gider'
        AND LOWER(kt.Ad) = LOWER(TRIM(k.Kalem))
);");
        }

        private static partial void PublishApprovedCompleteAccountantProfiles(CashTrackerDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
UPDATE MuhasebeciProfil
SET Yayinda = 1,
    UpdatedAt = CURRENT_TIMESTAMP
WHERE Yayinda = 0
  AND Telefon IS NOT NULL
  AND TRIM(Telefon) <> ''
  AND ProfilResmiUrl IS NOT NULL
  AND TRIM(ProfilResmiUrl) <> ''
  AND UcretBilgisi IS NOT NULL
  AND TRIM(UcretBilgisi) <> ''
  AND EXISTS (
      SELECT 1
      FROM Isletme i
      WHERE i.Id = MuhasebeciProfil.MuhasebeciIsletmeId
        AND i.TenantTipi = 'Muhasebeci'
        AND (
            EXISTS (
                SELECT 1
                FROM Kullanici k
                WHERE k.Id = i.SahipKullaniciId
                  AND k.HesapTipi = 'Muhasebeci'
                  AND k.Durum = 'Aktif'
            )
            OR EXISTS (
                SELECT 1
                FROM IsletmeUyelik iu
                INNER JOIN Kullanici k ON k.Id = iu.KullaniciId
                WHERE iu.IsletmeId = i.Id
                  AND iu.Durum = 'Aktif'
                  AND k.HesapTipi = 'Muhasebeci'
                  AND k.Durum = 'Aktif'
            )
        )
  );");
        }

        private static partial void EnsureDefaultKalemler(CashTrackerDbContext db, int isletmeId)
        {
            db.Database.ExecuteSqlRaw(@"
INSERT INTO KalemTanimi (IsletmeId, Tip, Ad, CreatedAt)
SELECT {0}, 'Gelir', 'Genel Gelir', CURRENT_TIMESTAMP
WHERE NOT EXISTS (
    SELECT 1 FROM KalemTanimi WHERE IsletmeId = {0} AND Tip = 'Gelir'
);", isletmeId);

            foreach (var category in DefaultKalemCatalog.DefaultExpenseCategories)
            {
                db.Database.ExecuteSqlRaw(@"
INSERT INTO KalemTanimi (IsletmeId, Tip, Ad, CreatedAt)
SELECT {0}, 'Gider', {1}, CURRENT_TIMESTAMP
WHERE NOT EXISTS (
    SELECT 1 FROM KalemTanimi WHERE IsletmeId = {0} AND Tip = 'Gider' AND LOWER(Ad) = LOWER({1})
);", isletmeId, category);
            }
        }
    }
}
