using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Persistence;

public static partial class SchemaMigrator
{
    private static partial void EnsureMarketplaceAcceptanceColumns(CashTrackerDbContext db, DbConnection conn)
    {
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
    }

    private static void AddColumn(CashTrackerDbContext db, DbConnection conn, string table, string column, string definition)
    {
        if (!TableExists(conn, table) || ColumnExists(conn, table, column))
            return;

        var allowed = table switch
        {
            "TedarikciSevkiyat" => new HashSet<string>(StringComparer.Ordinal)
                { "BelgeUuid", "BelgeDosyaYolu", "SevkAt", "VarisDeposu", "RandevuAt" },
            "TedarikciSevkiyatKalemi" => new HashSet<string>(StringComparer.Ordinal)
                { "SeriNo", "Agirlik", "PaletKoli" },
            "TedarikciMalKabul" => new HashSet<string>(StringComparer.Ordinal)
                { "SubeId", "DepoId", "IslemYapanKullaniciRef", "CihazRef", "IpAdresi", "BelgeKarmasi", "FotoKanitiYolu", "OlculenAgirlik", "OlculenSicaklik", "KabulBrutTutar", "SerbestBirakilanNetTutar", "MuhasebelestiAt", "HakEdisAktarimReferansi", "HakEdisAktarimHatasi" },
            "IsletmeUyelik" => new HashSet<string>(StringComparer.Ordinal) { "SubeId", "DepoId" },
            _ => []
        };
        if (!allowed.Contains(column))
            throw new InvalidOperationException("Beklenmeyen pazaryeri şema kolonu.");

        // Table, column and definition are selected only from the fixed lists above.
        db.Database.ExecuteSqlRaw("ALTER TABLE " + table + " ADD COLUMN " + column + " " + definition + ";");
    }
}
