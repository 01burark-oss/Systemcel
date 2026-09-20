namespace CashTracker.Core.Models;

public sealed record AkilliKararDurumu(bool Hazir, string Model);

public sealed class UrunEslesmeIstek
{
    public string KaynakMetin { get; set; } = string.Empty;
    public string Birim { get; set; } = string.Empty;
    public string Barkod { get; set; } = string.Empty;
    public decimal? BirimFiyat { get; set; }
    public int? CariKartId { get; set; }
}

public sealed class UrunEslesmeOnerisi
{
    public bool Hazir { get; set; }
    public int? UrunHizmetId { get; set; }
    public string UrunAdi { get; set; } = string.Empty;
    public string Iliski { get; set; } = "incele";
    public double Guven { get; set; }
    public bool KayitliEslesme { get; set; }
    public string BirimUyarisi { get; set; } = string.Empty;
    public string MaliyetUyarisi { get; set; } = string.Empty;
}

public sealed class UrunEslesmeOnayIstek
{
    public string KaynakMetin { get; set; } = string.Empty;
    public int UrunHizmetId { get; set; }
    public int? CariKartId { get; set; }
}

public sealed class FaturaKontrolIstek
{
    public int CariKartId { get; set; }
    public DateTime Tarih { get; set; } = DateTime.Today;
    public string FaturaTipi { get; set; } = "Alis";
    public string Aciklama { get; set; } = string.Empty;
    public decimal GenelToplam { get; set; }
    public int? UrunHizmetId { get; set; }
    public string Birim { get; set; } = string.Empty;
    public decimal? BirimFiyat { get; set; }
}

public sealed class FaturaKontrolSonucu
{
    public bool Hazir { get; set; }
    public string Iliski { get; set; } = "yeni_kayit";
    public int? IliskiliFaturaId { get; set; }
    public string IliskiliFatura { get; set; } = string.Empty;
    public double Guven { get; set; }
    public string BirimUyarisi { get; set; } = string.Empty;
    public string MaliyetUyarisi { get; set; } = string.Empty;
}

public sealed record BugununIsi(
    string Kimlik,
    string Baslik,
    string Aciklama,
    string Oncelik,
    string Metrik,
    string AksiyonUrl,
    double Guven);

public sealed record SutunEslemeOnerisi(
    string KaynakSutun,
    string HedefAlan,
    double Guven);

public sealed record AsistanYonlendirme(
    string Niyet,
    double Guven,
    string AksiyonUrl);
