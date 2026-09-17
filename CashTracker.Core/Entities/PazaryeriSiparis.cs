namespace CashTracker.Core.Entities;

public sealed class TedarikciUrun
{
    public int Id { get; set; }
    public int TedarikciProfilId { get; set; }
    public int TedarikciIsletmeId { get; set; }
    public int? KaynakUrunHizmetId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Ad { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;
    public string Kategori { get; set; } = string.Empty;
    public string Birim { get; set; } = "Adet";
    public decimal BirimFiyat { get; set; }
    public decimal KdvOrani { get; set; } = 20m;
    public string ParaBirimi { get; set; } = "TRY";
    public decimal StokMiktari { get; set; }
    public decimal RezerveMiktar { get; set; }
    public decimal MinimumSiparisMiktari { get; set; } = 1m;
    public int TahminiTeslimatGun { get; set; }
    public bool Aktif { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PazaryeriAnaSiparis
{
    public int Id { get; set; }
    public int AliciIsletmeId { get; set; }
    public string SiparisNo { get; set; } = string.Empty;
    public string OlusturmaAnahtari { get; set; } = string.Empty;
    public string TeslimatAdresi { get; set; } = string.Empty;
    public string ParaBirimi { get; set; } = "TRY";
    public decimal AraToplam { get; set; }
    public decimal KdvToplam { get; set; }
    public decimal GenelToplam { get; set; }
    public string Durum { get; set; } = "OdemeBekliyor";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class TedarikciSiparis
{
    public int Id { get; set; }
    public int AnaSiparisId { get; set; }
    public int AliciIsletmeId { get; set; }
    public int TedarikciIsletmeId { get; set; }
    public int TedarikciProfilId { get; set; }
    public string SiparisNo { get; set; } = string.Empty;
    public string ParaBirimi { get; set; } = "TRY";
    public decimal AraToplam { get; set; }
    public decimal KdvToplam { get; set; }
    public decimal GenelToplam { get; set; }
    public decimal KomisyonMatrahi { get; set; }
    public decimal KomisyonOrani { get; set; }
    public decimal KomisyonTutari { get; set; }
    public decimal KomisyonKdvTutari { get; set; }
    public decimal TevkifatMatrahi { get; set; }
    public decimal TevkifatTutari { get; set; }
    public decimal OdemeHizmetiBedeli { get; set; }
    public decimal TedarikciHakEdisi { get; set; }
    public string Durum { get; set; } = "OdemeBekliyor";
    public string KargoFirmasi { get; set; } = string.Empty;
    public string KargoTakipNo { get; set; } = string.Empty;
    public DateTime? OdendiAt { get; set; }
    public DateTime? TeslimEdildiAt { get; set; }
    public DateTime? HakEdisTarihi { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class TedarikciSiparisKalemi
{
    public int Id { get; set; }
    public int TedarikciSiparisId { get; set; }
    public int TedarikciUrunId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Ad { get; set; } = string.Empty;
    public string Birim { get; set; } = "Adet";
    public decimal Miktar { get; set; }
    public decimal BirimFiyat { get; set; }
    public decimal KdvOrani { get; set; }
    public decimal NetTutar { get; set; }
    public decimal KdvTutari { get; set; }
    public decimal ToplamTutar { get; set; }
}

public sealed class PazaryeriOdeme
{
    public int Id { get; set; }
    public int AnaSiparisId { get; set; }
    public int AliciIsletmeId { get; set; }
    public string IdempotencyAnahtari { get; set; } = string.Empty;
    public string Saglayici { get; set; } = string.Empty;
    public string SaglayiciIslemId { get; set; } = string.Empty;
    public decimal Tutar { get; set; }
    public string ParaBirimi { get; set; } = "TRY";
    public string Durum { get; set; } = "Hazirlaniyor";
    public decimal IadeTutari { get; set; }
    public DateTime? OdendiAt { get; set; }
    public DateTime? IadeEdildiAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PazaryeriOdemeDagitimi
{
    public int Id { get; set; }
    public int PazaryeriOdemeId { get; set; }
    public int TedarikciSiparisId { get; set; }
    public int TedarikciIsletmeId { get; set; }
    public decimal BrutTutar { get; set; }
    public decimal TedarikciHakEdisi { get; set; }
    public decimal IadeTutari { get; set; }
    public string ParaBirimi { get; set; } = "TRY";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class TedarikciHakEdis
{
    public int Id { get; set; }
    public int TedarikciSiparisId { get; set; }
    public int TedarikciIsletmeId { get; set; }
    public decimal BrutTutar { get; set; }
    public decimal KomisyonTutari { get; set; }
    public decimal KomisyonKdvTutari { get; set; }
    public decimal TevkifatTutari { get; set; }
    public decimal OdemeHizmetiBedeli { get; set; }
    public decimal IadeTutari { get; set; }
    public decimal NetTutar { get; set; }
    public decimal OdenenTutar { get; set; }
    public string ParaBirimi { get; set; } = "TRY";
    public string Durum { get; set; } = "Bekliyor";
    public string AktarimReferansi { get; set; } = string.Empty;
    public DateTime PlanlananAt { get; set; }
    public DateTime? TamamlandiAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PazaryeriDefterKaydi
{
    public int Id { get; set; }
    public int TedarikciSiparisId { get; set; }
    public string Hesap { get; set; } = string.Empty;
    public string Yon { get; set; } = string.Empty;
    public decimal Tutar { get; set; }
    public string ParaBirimi { get; set; } = "TRY";
    public string Aciklama { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class TedarikciSiparisDurumKaydi
{
    public int Id { get; set; }
    public int TedarikciSiparisId { get; set; }
    public string OncekiDurum { get; set; } = string.Empty;
    public string YeniDurum { get; set; } = string.Empty;
    public int IslemYapanIsletmeId { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class TedarikciFaturaEslesmesi
{
    public int Id { get; set; }
    public int TedarikciSiparisId { get; set; }
    public int AliciFaturaId { get; set; }
    public int SaticiFaturaId { get; set; }
    public string TedarikciBelgeNo { get; set; } = string.Empty;
    public string TedarikciBelgeUuid { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
