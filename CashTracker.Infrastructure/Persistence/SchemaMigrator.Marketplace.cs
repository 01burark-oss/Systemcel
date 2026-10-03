using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Persistence;

public static partial class SchemaMigrator
{
    private static partial void EnsureMarketplaceAcceptanceColumns(CashTrackerDbContext db, DbConnection conn)
    {
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "TedarikciMalKabulDuzeltme" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_TedarikciMalKabulDuzeltme" PRIMARY KEY AUTOINCREMENT,
                "TedarikciMalKabulId" INTEGER NOT NULL REFERENCES "TedarikciMalKabul"("Id"),
                "TedarikciSiparisId" INTEGER NOT NULL REFERENCES "TedarikciSiparis"("Id"),
                "AliciIsletmeId" INTEGER NOT NULL REFERENCES "Isletme"("Id"),
                "IdempotencyAnahtari" TEXT NOT NULL, "Miktar" NUMERIC(18,3) NOT NULL, "Not" TEXT NOT NULL,
                "Durum" TEXT NOT NULL, "IslemYapanKullaniciRef" TEXT NOT NULL, "OnaylayanKullaniciRef" TEXT NOT NULL,
                "OnayNotu" TEXT NOT NULL, "CreatedAt" TEXT NOT NULL, "OnayAt" TEXT NULL,
                "NetTutar" NUMERIC(18,2) NOT NULL, "KdvTutar" NUMERIC(18,2) NOT NULL, "BrutTutar" NUMERIC(18,2) NOT NULL,
                "HakEdisAzaltimi" NUMERIC(18,2) NOT NULL, "TedarikcidenGeriAlinacakTutar" NUMERIC(18,2) NOT NULL,
                "ParaDurumu" TEXT NOT NULL, "BelgeDurumu" TEXT NOT NULL, "StokDurumu" TEXT NOT NULL,
                "AliciFaturaId" INTEGER NULL REFERENCES "Fatura"("Id"), "SaticiFaturaId" INTEGER NULL REFERENCES "Fatura"("Id")
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_TedarikciMalKabulDuzeltme_AliciIsletmeId_IdempotencyAnahtari"
                ON "TedarikciMalKabulDuzeltme" ("AliciIsletmeId", "IdempotencyAnahtari");
            CREATE INDEX IF NOT EXISTS "IX_TedarikciMalKabulDuzeltme_TedarikciSiparisId_Durum" ON "TedarikciMalKabulDuzeltme" ("TedarikciSiparisId", "Durum");
            CREATE INDEX IF NOT EXISTS "IX_TedarikciMalKabulDuzeltme_TedarikciMalKabulId" ON "TedarikciMalKabulDuzeltme" ("TedarikciMalKabulId");
            CREATE INDEX IF NOT EXISTS "IX_TedarikciMalKabulDuzeltme_AliciFaturaId" ON "TedarikciMalKabulDuzeltme" ("AliciFaturaId");
            CREATE INDEX IF NOT EXISTS "IX_TedarikciMalKabulDuzeltme_SaticiFaturaId" ON "TedarikciMalKabulDuzeltme" ("SaticiFaturaId");
            """);
        foreach (var table in new[] { "StokHareket", "CariHareket", "PazaryeriDefterKaydi" })
        {
            AddColumn(db, conn, table, "TedarikciMalKabulDuzeltmeId", "INTEGER NULL REFERENCES TedarikciMalKabulDuzeltme(Id)");
            if (TableExists(conn, table))
                db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_" + table + "_TedarikciMalKabulDuzeltmeId ON " + table + " (TedarikciMalKabulDuzeltmeId);");
        }
        AddColumn(db, conn, "TedarikciSiparis", "PlatformOdemeHizmetiBedeli", "NUMERIC NOT NULL DEFAULT 0");
        foreach (var column in new[] { "ItirazAcildiAt", "ItirazIlkYanitAt", "ItirazKanitToplandiAt", "ItirazYukseltildiAt" })
            AddColumn(db, conn, "TedarikciSiparis", column, "TEXT NULL");
        foreach (var column in new[] { ("OnayDurumu", "TEXT NOT NULL DEFAULT 'Onaylandi'"), ("OnayNedeni", "TEXT NOT NULL DEFAULT ''"), ("IkinciOnaylayanKullaniciRef", "TEXT NOT NULL DEFAULT ''"), ("IkinciOnayNotu", "TEXT NOT NULL DEFAULT ''"), ("IkinciOnayAt", "TEXT NULL"), ("BelgeUyusmazligi", "INTEGER NOT NULL DEFAULT 0"), ("MiktarDegisikligi", "INTEGER NOT NULL DEFAULT 0") })
            AddColumn(db, conn, "TedarikciMalKabul", column.Item1, column.Item2);
        AddColumn(db, conn, "TedarikciSevkiyat", "BelgeUuid", "TEXT NOT NULL DEFAULT ''");
        AddColumn(db, conn, "TedarikciSevkiyat", "BelgeDosyaYolu", "TEXT NOT NULL DEFAULT ''");
        AddColumn(db, conn, "TedarikciSevkiyat", "SevkAt", "TEXT NULL");
        AddColumn(db, conn, "TedarikciSevkiyat", "VarisDeposu", "INTEGER NULL");
        AddColumn(db, conn, "TedarikciSevkiyat", "RandevuAt", "TEXT NULL");
        AddColumn(db, conn, "TedarikciSevkiyatKalemi", "SeriNo", "TEXT NOT NULL DEFAULT ''");
        AddColumn(db, conn, "TedarikciSevkiyatKalemi", "Agirlik", "NUMERIC NULL");
        AddColumn(db, conn, "TedarikciSevkiyatKalemi", "PaletKoli", "INTEGER NOT NULL DEFAULT 0");
        foreach (var column in new[] { ("SubeId", "INTEGER NULL"), ("DepoId", "INTEGER NULL"), ("IslemYapanKullaniciRef", "TEXT NOT NULL DEFAULT ''"), ("CihazRef", "TEXT NOT NULL DEFAULT ''"), ("IpAdresi", "TEXT NOT NULL DEFAULT ''"), ("BelgeKarmasi", "TEXT NOT NULL DEFAULT ''"), ("FotoKanitiYolu", "TEXT NOT NULL DEFAULT ''"), ("OlculenAgirlik", "NUMERIC NULL"), ("OlculenSicaklik", "NUMERIC NULL"), ("KabulBrutTutar", "NUMERIC NOT NULL DEFAULT 0"), ("SerbestBirakilanNetTutar", "NUMERIC NOT NULL DEFAULT 0"), ("MuhasebelestiAt", "TEXT NULL"), ("HakEdisAktarimReferansi", "TEXT NOT NULL DEFAULT ''"), ("HakEdisAktarimHatasi", "TEXT NOT NULL DEFAULT ''") })
            AddColumn(db, conn, "TedarikciMalKabul", column.Item1, column.Item2);
        AddColumn(db, conn, "IsletmeUyelik", "SubeId", "INTEGER NULL");
        AddColumn(db, conn, "IsletmeUyelik", "DepoId", "INTEGER NULL");
        if (TableExists(conn, "TedarikciMalKabul"))
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_TedarikciMalKabul_TedarikciSiparisId_OnayDurumu ON TedarikciMalKabul (TedarikciSiparisId, OnayDurumu);");
        if (TableExists(conn, "TedarikciSiparis") && TableExists(conn, "TedarikciSiparisDurumKaydi"))
            db.Database.ExecuteSqlRaw("""
                UPDATE "TedarikciSiparis" AS orders SET "ItirazAcildiAt" = COALESCE(
                    (SELECT history."CreatedAt" FROM "TedarikciSiparisDurumKaydi" AS history
                     WHERE history."TedarikciSiparisId" = orders."Id" AND history."YeniDurum" = 'Itirazli'
                       AND history."OncekiDurum" <> history."YeniDurum"
                     ORDER BY history."Id" DESC LIMIT 1), orders."UpdatedAt")
                WHERE orders."Durum" = 'Itirazli' AND orders."ItirazAcildiAt" IS NULL;
                """);
        if (TableExists(conn, "TedarikciSiparis") && TableExists(conn, "TedarikciSiparisSikayeti"))
            db.Database.ExecuteSqlRaw("""
                UPDATE "TedarikciSiparis" AS orders SET "ItirazIlkYanitAt" =
                    (SELECT MIN(complaint."YanitlandiAt") FROM "TedarikciSiparisSikayeti" AS complaint
                     WHERE complaint."TedarikciSiparisId" = orders."Id" AND complaint."YanitlandiAt" >= orders."ItirazAcildiAt")
                WHERE orders."Durum" = 'Itirazli' AND orders."ItirazIlkYanitAt" IS NULL;
                """);
    }

    private static void AddColumn(CashTrackerDbContext db, DbConnection conn, string table, string column, string definition)
    {
        if (!TableExists(conn, table) || ColumnExists(conn, table, column))
            return;

        var allowed = table switch
        {
            "StokHareket" or "CariHareket" or "PazaryeriDefterKaydi" => new HashSet<string>(StringComparer.Ordinal) { "TedarikciMalKabulDuzeltmeId" },
            "TedarikciSiparis" => new HashSet<string>(StringComparer.Ordinal) { "PlatformOdemeHizmetiBedeli", "ItirazAcildiAt", "ItirazIlkYanitAt", "ItirazKanitToplandiAt", "ItirazYukseltildiAt" },
            "TedarikciSevkiyat" => new HashSet<string>(StringComparer.Ordinal)
                { "BelgeUuid", "BelgeDosyaYolu", "SevkAt", "VarisDeposu", "RandevuAt" },
            "TedarikciSevkiyatKalemi" => new HashSet<string>(StringComparer.Ordinal)
                { "SeriNo", "Agirlik", "PaletKoli" },
            "TedarikciMalKabul" => new HashSet<string>(StringComparer.Ordinal)
                { "SubeId", "DepoId", "IslemYapanKullaniciRef", "CihazRef", "IpAdresi", "BelgeKarmasi", "FotoKanitiYolu", "OlculenAgirlik", "OlculenSicaklik", "KabulBrutTutar", "SerbestBirakilanNetTutar", "MuhasebelestiAt", "HakEdisAktarimReferansi", "HakEdisAktarimHatasi", "OnayDurumu", "OnayNedeni", "IkinciOnaylayanKullaniciRef", "IkinciOnayNotu", "IkinciOnayAt", "BelgeUyusmazligi", "MiktarDegisikligi" },
            "IsletmeUyelik" => new HashSet<string>(StringComparer.Ordinal) { "SubeId", "DepoId" },
            _ => []
        };
        if (!allowed.Contains(column))
            throw new InvalidOperationException("Beklenmeyen pazaryeri şema kolonu.");

        // Table, column and definition are selected only from the fixed lists above.
        db.Database.ExecuteSqlRaw("ALTER TABLE " + table + " ADD COLUMN " + column + " " + definition + ";");
    }
}
