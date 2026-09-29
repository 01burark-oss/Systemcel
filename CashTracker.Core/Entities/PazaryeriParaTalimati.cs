namespace CashTracker.Core.Entities;

public static class PazaryeriParaTalimatiDurumlari
{
    public const string Hazir = "Hazir";
    public const string Gonderiliyor = "Gonderiliyor";
    public const string SonucBekliyor = "SonucBekliyor";
    public const string Tamamlandi = "Tamamlandi";
    public const string KesinBasarisiz = "KesinBasarisiz";
    public const string IncelemeGerekli = "IncelemeGerekli";
}

public static class PazaryeriParaTalimatiTurleri
{
    public const string Tahsilat = "Tahsilat";
    public const string Iade = "Iade";
    public const string Aktarim = "Aktarim";
}

public sealed class PazaryeriParaTalimati
{
    public long Id { get; set; }
    public int AliciIsletmeId { get; set; }
    public int? TedarikciSiparisId { get; set; }
    public int? PazaryeriOdemeId { get; set; }
    public string Tur { get; set; } = string.Empty;
    public string Saglayici { get; set; } = string.Empty;
    public string IdempotencyAnahtari { get; set; } = string.Empty;
    public string KaynakRef { get; set; } = string.Empty;
    public decimal Tutar { get; set; }
    public string ParaBirimi { get; set; } = "TRY";
    public string Durum { get; set; } = PazaryeriParaTalimatiDurumlari.Hazir;
    public string SaglayiciIslemId { get; set; } = string.Empty;
    public string SonHataKodu { get; set; } = string.Empty;
    public int DenemeSayisi { get; set; }
    public DateTime? GonderimBasladiAt { get; set; }
    public DateTime? SonuclandiAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
