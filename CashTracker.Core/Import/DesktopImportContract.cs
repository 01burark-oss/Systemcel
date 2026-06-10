namespace CashTracker.Core.Import;

public static class DesktopImportContract
{
    public const string ManifestVersion = "1.0";
    public const string ManifestFileName = "manifest.json";
    public const string IsletmelerFileName = "isletmeler.json";
    public const string CariKartlarFileName = "cari-kartlar.json";
    public const string CariHareketlerFileName = "cari-hareketler.json";
    public const string UrunlerFileName = "urunler.json";
    public const string StokHareketleriFileName = "stok-hareketleri.json";
    public const string FaturalarFileName = "faturalar.json";
    public const string FaturaSatirlariFileName = "fatura-satirlari.json";
    public const string TahsilatOdemelerFileName = "tahsilat-odemeler.json";
    public const string KasaHareketleriFileName = "kasa-hareketleri.json";

    public static readonly string[] RequiredDataFiles =
    [
        IsletmelerFileName,
        CariKartlarFileName,
        CariHareketlerFileName,
        UrunlerFileName,
        StokHareketleriFileName,
        FaturalarFileName,
        FaturaSatirlariFileName,
        TahsilatOdemelerFileName,
        KasaHareketleriFileName
    ];
}

public sealed class DesktopImportManifest
{
    public string ManifestVersion { get; set; } = DesktopImportContract.ManifestVersion;
    public string PackageId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DesktopImportSourceInfo Source { get; set; } = new();
    public DesktopImportTransferInfo Transfer { get; set; } = new();
    public DesktopImportTotals Totals { get; set; } = new();
    public List<DesktopImportFileEntry> Files { get; set; } = new();
}

public sealed class DesktopImportSourceInfo
{
    public string AppName { get; set; } = "CashTracker Desktop";
    public string AppVersion { get; set; } = string.Empty;
    public string DatabaseProvider { get; set; } = "SQLite";
    public string DatabaseFileName { get; set; } = string.Empty;
    public long DatabaseSizeBytes { get; set; }
    public string DatabaseSha256 { get; set; } = string.Empty;
    public string SchemaVersion { get; set; } = string.Empty;
    public string ExportToolVersion { get; set; } = string.Empty;
}

public sealed class DesktopImportTransferInfo
{
    public string Code { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public int? TargetIsletmeId { get; set; }
}

public sealed class DesktopImportTotals
{
    public int Isletme { get; set; }
    public int CariKart { get; set; }
    public int CariHareket { get; set; }
    public int UrunHizmet { get; set; }
    public int StokHareket { get; set; }
    public int Fatura { get; set; }
    public int FaturaSatir { get; set; }
    public int TahsilatOdeme { get; set; }
    public int KasaHareket { get; set; }
}

public sealed class DesktopImportFileEntry
{
    public string Entity { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public bool Required { get; set; } = true;
}

public sealed class DesktopImportIsletmeRecord
{
    public int LocalId { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string TenantTipi { get; set; } = "Isletme";
    public bool IsAktif { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public sealed class DesktopImportCariKartRecord
{
    public int LocalId { get; set; }
    public int IsletmeLocalId { get; set; }
    public string Tip { get; set; } = "Musteri";
    public string Unvan { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public string Eposta { get; set; } = string.Empty;
    public string Adres { get; set; } = string.Empty;
    public string VergiNoTc { get; set; } = string.Empty;
    public string VergiDairesi { get; set; } = string.Empty;
    public bool Aktif { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public sealed class DesktopImportCariHareketRecord
{
    public int LocalId { get; set; }
    public int IsletmeLocalId { get; set; }
    public int CariKartLocalId { get; set; }
    public DateTime Tarih { get; set; } = DateTime.Now;
    public string HareketTipi { get; set; } = "Borc";
    public decimal Tutar { get; set; }
    public string Kaynak { get; set; } = "Manuel";
    public string? Aciklama { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public sealed class DesktopImportUrunHizmetRecord
{
    public int LocalId { get; set; }
    public int IsletmeLocalId { get; set; }
    public string Tip { get; set; } = "Urun";
    public string Ad { get; set; } = string.Empty;
    public string Barkod { get; set; } = string.Empty;
    public string Birim { get; set; } = "Adet";
    public decimal KdvOrani { get; set; } = 20m;
    public decimal AlisFiyati { get; set; }
    public decimal SatisFiyati { get; set; }
    public decimal KritikStok { get; set; }
    public bool Aktif { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public sealed class DesktopImportStokHareketRecord
{
    public int LocalId { get; set; }
    public int IsletmeLocalId { get; set; }
    public int UrunHizmetLocalId { get; set; }
    public DateTime Tarih { get; set; } = DateTime.Now;
    public decimal Miktar { get; set; }
    public string HareketTipi { get; set; } = "Giris";
    public string Kaynak { get; set; } = "Manuel";
    public string? Aciklama { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public sealed class DesktopImportFaturaRecord
{
    public int LocalId { get; set; }
    public int IsletmeLocalId { get; set; }
    public int CariKartLocalId { get; set; }
    public DateTime Tarih { get; set; } = DateTime.Now;
    public DateTime? VadeTarihi { get; set; }
    public string FaturaTipi { get; set; } = "Satis";
    public string Durum { get; set; } = "YerelTaslak";
    public string YerelFaturaNo { get; set; } = string.Empty;
    public string PortalBelgeNo { get; set; } = string.Empty;
    public string PortalUuid { get; set; } = string.Empty;
    public decimal AraToplam { get; set; }
    public decimal IskontoToplam { get; set; }
    public decimal KdvToplam { get; set; }
    public decimal GenelToplam { get; set; }
    public decimal OdenenTutar { get; set; }
    public string OdemeYontemi { get; set; } = "Nakit";
    public string? Aciklama { get; set; }
    public DateTime? KesildiAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public sealed class DesktopImportFaturaSatirRecord
{
    public int LocalId { get; set; }
    public int IsletmeLocalId { get; set; }
    public int FaturaLocalId { get; set; }
    public int? UrunHizmetLocalId { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public string Birim { get; set; } = "Adet";
    public decimal Miktar { get; set; }
    public decimal BirimFiyat { get; set; }
    public decimal IskontoOrani { get; set; }
    public decimal IskontoTutar { get; set; }
    public decimal KdvOrani { get; set; } = 20m;
    public decimal KdvTutar { get; set; }
    public decimal SatirNetTutar { get; set; }
    public decimal SatirToplam { get; set; }
    public bool StokEtkilesin { get; set; } = true;
}

public sealed class DesktopImportTahsilatOdemeRecord
{
    public int LocalId { get; set; }
    public int IsletmeLocalId { get; set; }
    public int FaturaLocalId { get; set; }
    public int CariKartLocalId { get; set; }
    public DateTime Tarih { get; set; } = DateTime.Now;
    public string Tip { get; set; } = "Tahsilat";
    public decimal Tutar { get; set; }
    public string OdemeYontemi { get; set; } = "Nakit";
    public int? KasaLocalId { get; set; }
    public int? CariHareketLocalId { get; set; }
    public string? Aciklama { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public sealed class DesktopImportKasaHareketRecord
{
    public int LocalId { get; set; }
    public int IsletmeLocalId { get; set; }
    public DateTime Tarih { get; set; } = DateTime.Now;
    public string Tip { get; set; } = "Gelir";
    public decimal Tutar { get; set; }
    public string OdemeYontemi { get; set; } = "Nakit";
    public string? Kalem { get; set; }
    public string? GiderTuru { get; set; }
    public string? Aciklama { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
