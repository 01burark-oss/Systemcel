namespace CashTracker.Core.Entities;

public sealed class TedarikciProfil
{
    public int Id { get; set; }
    public int IsletmeId { get; set; }
    public string Unvan { get; set; } = string.Empty;
    public string Kategoriler { get; set; } = string.Empty;
    public string Sehir { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;
    public string VergiNo { get; set; } = string.Empty;
    public string MersisNo { get; set; } = string.Empty;
    public string KepAdresi { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
    public string Adres { get; set; } = string.Empty;
    public string YetkiliAdSoyad { get; set; } = string.Empty;
    public string VergiDurumu { get; set; } = string.Empty;
    public string SevkiyatBolgeleri { get; set; } = string.Empty;
    public string IadeKosullari { get; set; } = string.Empty;
    public string PazaryeriSozlesmeVersiyonu { get; set; } = string.Empty;
    public string PspAltUyeIsyeriId { get; set; } = string.Empty;
    public decimal KomisyonOrani { get; set; } = 8m;
    public int OdemeVadesiGun { get; set; } = 7;
    public bool TevkifatMuaf { get; set; }
    public string DogrulamaDurumu { get; set; } = "Taslak";
    public string DogrulamaNotu { get; set; } = string.Empty;
    public DateTime? DogrulandiAt { get; set; }
    public bool Dogrulandi { get; set; }
    public bool Yayinda { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class TedarikAlimTalebi
{
    public int Id { get; set; }
    public int AliciIsletmeId { get; set; }
    public string Baslik { get; set; } = string.Empty;
    public string Kategori { get; set; } = string.Empty;
    public string UrunHizmet { get; set; } = string.Empty;
    public decimal Miktar { get; set; }
    public string Birim { get; set; } = string.Empty;
    public string TeslimatSehri { get; set; } = string.Empty;
    public DateTime SonTeklifAt { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public string Durum { get; set; } = "Acik";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class TedarikTeklifi
{
    public int Id { get; set; }
    public int TalepId { get; set; }
    public int TedarikciIsletmeId { get; set; }
    public decimal BirimFiyat { get; set; }
    public string ParaBirimi { get; set; } = "TRY";
    public int TerminGun { get; set; }
    public decimal MinimumSiparis { get; set; }
    public string Not { get; set; } = string.Empty;
    public string Durum { get; set; } = "Gonderildi";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
