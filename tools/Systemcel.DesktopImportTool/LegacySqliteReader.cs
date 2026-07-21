using System.Globalization;
using CashTracker.Core.Import;
using Microsoft.Data.Sqlite;

namespace Systemcel.DesktopImportTool;

internal sealed class LegacySqliteReader
{
    public DesktopImportPackageData Read(string dbPath)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var data = new DesktopImportPackageData();
        data.Isletmeler.AddRange(ReadTable(connection, "Isletme", row => new DesktopImportIsletmeRecord
        {
            LocalId = row.GetInt32("Id"),
            Ad = row.GetString("Ad", "Aktarilan Isletme"),
            TenantTipi = row.GetString("TenantTipi", "Isletme"),
            IsAktif = row.GetBool("IsAktif", true),
            CreatedAt = row.GetDateTime("CreatedAt", DateTime.Now),
            UpdatedAt = row.GetDateTime("UpdatedAt", DateTime.Now)
        }));

        var defaultIsletmeId = data.Isletmeler.FirstOrDefault(x => x.IsAktif)?.LocalId
            ?? data.Isletmeler.FirstOrDefault()?.LocalId
            ?? 1;

        data.CariKartlar.AddRange(ReadTable(connection, "CariKart", row => new DesktopImportCariKartRecord
        {
            LocalId = row.GetInt32("Id"),
            IsletmeLocalId = row.GetInt32("IsletmeId", defaultIsletmeId),
            Tip = row.GetString("Tip", "Musteri"),
            Unvan = row.GetString("Unvan"),
            Telefon = row.GetString("Telefon"),
            Eposta = row.GetString("Eposta"),
            Adres = row.GetString("Adres"),
            VergiNoTc = row.GetString("VergiNoTc"),
            VergiDairesi = row.GetString("VergiDairesi"),
            Aktif = row.GetBool("Aktif", true),
            CreatedAt = row.GetDateTime("CreatedAt", DateTime.Now),
            UpdatedAt = row.GetDateTime("UpdatedAt", DateTime.Now)
        }));

        data.CariHareketler.AddRange(ReadTable(connection, "CariHareket", row => new DesktopImportCariHareketRecord
        {
            LocalId = row.GetInt32("Id"),
            IsletmeLocalId = row.GetInt32("IsletmeId", defaultIsletmeId),
            CariKartLocalId = row.GetInt32("CariKartId"),
            Tarih = row.GetDateTime("Tarih", DateTime.Now),
            HareketTipi = row.GetString("HareketTipi", "Borc"),
            Tutar = row.GetDecimal("Tutar"),
            Kaynak = row.GetString("Kaynak", "Manuel"),
            Aciklama = row.GetNullableString("Aciklama"),
            CreatedAt = row.GetDateTime("CreatedAt", DateTime.Now)
        }));

        data.Urunler.AddRange(ReadTable(connection, "UrunHizmet", row => new DesktopImportUrunHizmetRecord
        {
            LocalId = row.GetInt32("Id"),
            IsletmeLocalId = row.GetInt32("IsletmeId", defaultIsletmeId),
            Tip = row.GetString("Tip", "Urun"),
            Ad = row.GetString("Ad"),
            Barkod = row.GetString("Barkod"),
            Birim = row.GetString("Birim", "Adet"),
            KdvOrani = row.GetDecimal("KdvOrani", 20m),
            AlisFiyati = row.GetDecimal("AlisFiyati"),
            SatisFiyati = row.GetDecimal("SatisFiyati"),
            KritikStok = row.GetDecimal("KritikStok"),
            Aktif = row.GetBool("Aktif", true),
            CreatedAt = row.GetDateTime("CreatedAt", DateTime.Now),
            UpdatedAt = row.GetDateTime("UpdatedAt", DateTime.Now)
        }));

        data.StokHareketleri.AddRange(ReadTable(connection, "StokHareket", row => new DesktopImportStokHareketRecord
        {
            LocalId = row.GetInt32("Id"),
            IsletmeLocalId = row.GetInt32("IsletmeId", defaultIsletmeId),
            UrunHizmetLocalId = row.GetInt32("UrunHizmetId"),
            Tarih = row.GetDateTime("Tarih", DateTime.Now),
            Miktar = row.GetDecimal("Miktar"),
            HareketTipi = row.GetString("HareketTipi", "Giris"),
            Kaynak = row.GetString("Kaynak", "Manuel"),
            Aciklama = row.GetNullableString("Aciklama"),
            CreatedAt = row.GetDateTime("CreatedAt", DateTime.Now)
        }));

        data.Faturalar.AddRange(ReadTable(connection, "Fatura", row => new DesktopImportFaturaRecord
        {
            LocalId = row.GetInt32("Id"),
            IsletmeLocalId = row.GetInt32("IsletmeId", defaultIsletmeId),
            CariKartLocalId = row.GetInt32("CariKartId"),
            Tarih = row.GetDateTime("Tarih", DateTime.Now),
            VadeTarihi = row.GetNullableDateTime("VadeTarihi"),
            FaturaTipi = row.GetString("FaturaTipi", "Satis"),
            Durum = row.GetString("Durum", "YerelTaslak"),
            YerelFaturaNo = row.GetString("YerelFaturaNo"),
            PortalBelgeNo = row.GetString("PortalBelgeNo"),
            PortalUuid = row.GetString("PortalUuid"),
            AraToplam = row.GetDecimal("AraToplam"),
            IskontoToplam = row.GetDecimal("IskontoToplam"),
            KdvToplam = row.GetDecimal("KdvToplam"),
            GenelToplam = row.GetDecimal("GenelToplam"),
            OdenenTutar = row.GetDecimal("OdenenTutar"),
            OdemeYontemi = row.GetString("OdemeYontemi", "Nakit"),
            Aciklama = row.GetNullableString("Aciklama"),
            KesildiAt = row.GetNullableDateTime("KesildiAt"),
            CreatedAt = row.GetDateTime("CreatedAt", DateTime.Now),
            UpdatedAt = row.GetDateTime("UpdatedAt", DateTime.Now)
        }));

        data.FaturaSatirlari.AddRange(ReadTable(connection, "FaturaSatir", row => new DesktopImportFaturaSatirRecord
        {
            LocalId = row.GetInt32("Id"),
            IsletmeLocalId = row.GetInt32("IsletmeId", defaultIsletmeId),
            FaturaLocalId = row.GetInt32("FaturaId"),
            UrunHizmetLocalId = row.GetNullableInt32("UrunHizmetId"),
            Aciklama = row.GetString("Aciklama"),
            Birim = row.GetString("Birim", "Adet"),
            Miktar = row.GetDecimal("Miktar"),
            BirimFiyat = row.GetDecimal("BirimFiyat"),
            IskontoOrani = row.GetDecimal("IskontoOrani"),
            IskontoTutar = row.GetDecimal("IskontoTutar"),
            KdvOrani = row.GetDecimal("KdvOrani", 20m),
            KdvTutar = row.GetDecimal("KdvTutar"),
            SatirNetTutar = row.GetDecimal("SatirNetTutar"),
            SatirToplam = row.GetDecimal("SatirToplam"),
            StokEtkilesin = row.GetBool("StokEtkilesin", true)
        }));

        data.TahsilatOdemeler.AddRange(ReadTable(connection, "TahsilatOdeme", row => new DesktopImportTahsilatOdemeRecord
        {
            LocalId = row.GetInt32("Id"),
            IsletmeLocalId = row.GetInt32("IsletmeId", defaultIsletmeId),
            FaturaLocalId = row.GetInt32("FaturaId"),
            CariKartLocalId = row.GetInt32("CariKartId"),
            Tarih = row.GetDateTime("Tarih", DateTime.Now),
            Tip = row.GetString("Tip", "Tahsilat"),
            Tutar = row.GetDecimal("Tutar"),
            OdemeYontemi = row.GetString("OdemeYontemi", "Nakit"),
            KasaLocalId = row.GetNullableInt32("KasaId"),
            CariHareketLocalId = row.GetNullableInt32("CariHareketId"),
            Aciklama = row.GetNullableString("Aciklama"),
            CreatedAt = row.GetDateTime("CreatedAt", DateTime.Now)
        }));

        data.KasaHareketleri.AddRange(ReadTable(connection, "Kasa", row => new DesktopImportKasaHareketRecord
        {
            LocalId = row.GetInt32("Id"),
            IsletmeLocalId = row.GetInt32("IsletmeId", defaultIsletmeId),
            Tarih = row.GetDateTime("Tarih", DateTime.Now),
            Tip = row.GetString("Tip", "Gelir"),
            Tutar = row.GetDecimal("Tutar"),
            OdemeYontemi = row.GetString("OdemeYontemi", "Nakit"),
            Kalem = row.GetNullableString("Kalem"),
            GiderTuru = row.GetNullableString("GiderTuru"),
            Aciklama = row.GetNullableString("Aciklama"),
            CreatedAt = row.GetDateTime("CreatedAt", DateTime.Now)
        }));

        EnsureSyntheticBusiness(data);
        return data;
    }

    public static string? FindDefaultDatabasePath()
    {
        var candidates = new List<string>();
        AddIfNotEmpty(candidates, Environment.GetEnvironmentVariable("SYSTEMCEL_IMPORT_SQLITE_PATH"));

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        AddIfNotEmpty(candidates, Path.Combine(localAppData, "Systemcel", "systemcel.db"));
        AddIfNotEmpty(candidates, Path.Combine(localAppData, "Systemcel", "Web", "systemcel.db"));
        AddIfNotEmpty(candidates, Path.Combine(localAppData, "CashTracker", "cashtracker.db"));
        AddIfNotEmpty(candidates, Path.Combine(localAppData, "CashTracker", "systemcel.db"));

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        AddIfNotEmpty(candidates, Path.Combine(appData, "CashTracker", "cashtracker.db"));

        AddIfNotEmpty(candidates, Path.Combine(AppContext.BaseDirectory, "AppData", "systemcel.db"));
        return candidates.FirstOrDefault(File.Exists);
    }

    private static List<T> ReadTable<T>(SqliteConnection connection, string tableName, Func<RowReader, T> map)
    {
        var columns = GetColumns(connection, tableName);
        if (columns.Count == 0)
            return new List<T>();

        using var command = connection.CreateCommand();
        var orderBy = columns.Contains("Id") ? " ORDER BY \"Id\"" : string.Empty;
        command.CommandText = $"SELECT * FROM {QuoteIdentifier(tableName)}{orderBy};";

        using var reader = command.ExecuteReader();
        var rows = new List<T>();
        while (reader.Read())
            rows.Add(map(new RowReader(reader)));

        return rows;
    }

    private static HashSet<string> GetColumns(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({QuoteIdentifier(tableName)});";

        using var reader = command.ExecuteReader();
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
            columns.Add(reader.GetString(1));

        return columns;
    }

    private static string QuoteIdentifier(string value)
    {
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static void EnsureSyntheticBusiness(DesktopImportPackageData data)
    {
        if (data.Isletmeler.Count > 0 || !HasAnyBusinessScopedRows(data))
            return;

        data.Isletmeler.Add(new DesktopImportIsletmeRecord
        {
            LocalId = 1,
            Ad = "Aktarilan Isletme",
            IsAktif = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        });
    }

    private static bool HasAnyBusinessScopedRows(DesktopImportPackageData data)
    {
        return data.CariKartlar.Count > 0 ||
            data.CariHareketler.Count > 0 ||
            data.Urunler.Count > 0 ||
            data.StokHareketleri.Count > 0 ||
            data.Faturalar.Count > 0 ||
            data.FaturaSatirlari.Count > 0 ||
            data.TahsilatOdemeler.Count > 0 ||
            data.KasaHareketleri.Count > 0;
    }

    private static void AddIfNotEmpty(List<string> values, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            values.Add(value);
    }

    private sealed class RowReader
    {
        private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");
        private readonly SqliteDataReader _reader;
        private readonly Dictionary<string, int> _ordinals;

        public RowReader(SqliteDataReader reader)
        {
            _reader = reader;
            _ordinals = Enumerable.Range(0, reader.FieldCount)
                .ToDictionary(reader.GetName, x => x, StringComparer.OrdinalIgnoreCase);
        }

        public string GetString(string name, string fallback = "")
        {
            var value = GetValue(name);
            return value is null ? fallback : Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback;
        }

        public string? GetNullableString(string name)
        {
            var value = GetValue(name);
            return value is null ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        public int GetInt32(string name, int fallback = 0)
        {
            var value = GetValue(name);
            return value is null ? fallback : Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        public int? GetNullableInt32(string name)
        {
            var value = GetValue(name);
            return value is null ? null : Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        public decimal GetDecimal(string name, decimal fallback = 0m)
        {
            var value = GetValue(name);
            if (value is null)
                return fallback;

            if (value is decimal d)
                return d;

            if (value is double or float or int or long)
                return Convert.ToDecimal(value, CultureInfo.InvariantCulture);

            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out d) ||
                decimal.TryParse(text, NumberStyles.Any, TurkishCulture, out d))
                return d;

            return fallback;
        }

        public bool GetBool(string name, bool fallback = false)
        {
            var value = GetValue(name);
            if (value is null)
                return fallback;

            if (value is bool b)
                return b;

            if (value is int or long)
                return Convert.ToInt64(value, CultureInfo.InvariantCulture) != 0;

            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (bool.TryParse(text, out b))
                return b;

            return string.Equals(text, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "evet", StringComparison.OrdinalIgnoreCase);
        }

        public DateTime GetDateTime(string name, DateTime fallback)
        {
            return GetNullableDateTime(name) ?? fallback;
        }

        public DateTime? GetNullableDateTime(string name)
        {
            var value = GetValue(name);
            if (value is null)
                return null;

            if (value is DateTime dt)
                return dt;

            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt) ||
                DateTime.TryParse(text, TurkishCulture, DateTimeStyles.AssumeLocal, out dt) ||
                DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out dt))
                return dt;

            return null;
        }

        private object? GetValue(string name)
        {
            if (!_ordinals.TryGetValue(name, out var ordinal) || _reader.IsDBNull(ordinal))
                return null;

            return _reader.GetValue(ordinal);
        }
    }
}
