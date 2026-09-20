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
    public decimal SevkEdilenMiktar { get; set; }
    public decimal KabulEdilenMiktar { get; set; }
    public decimal ReddedilenMiktar { get; set; }
}

public sealed class TedarikciSevkiyat
{
    public int Id { get; set; }
    public int TedarikciSiparisId { get; set; }
    public int AliciIsletmeId { get; set; }
    public int TedarikciIsletmeId { get; set; }
    public string SevkiyatNo { get; set; } = string.Empty;
    public string TasimaTipi { get; set; } = string.Empty;
    public string Tasiyici { get; set; } = string.Empty;
    public string BelgeNo { get; set; } = string.Empty;
    public string BelgeUuid { get; set; } = string.Empty;
    public string BelgeDosyaYolu { get; set; } = string.Empty;
    public string AracPlaka { get; set; } = string.Empty;
    public string SurucuAdi { get; set; } = string.Empty;
    public string CikisDeposu { get; set; } = string.Empty;
    public string Not { get; set; } = string.Empty;
    public string Durum { get; set; } = "Hazir";
    public DateTime? SevkAt { get; set; }
    public int? VarisDeposu { get; set; }
    public DateTime? RandevuAt { get; set; }
    public DateTime? PlanlananTeslimAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class TedarikciSevkiyatKalemi
{
    public int Id { get; set; }
    public int TedarikciSevkiyatId { get; set; }
    public int TedarikciSiparisKalemiId { get; set; }
    public decimal Miktar { get; set; }
    public int EtiketSayisi { get; set; }
    public string LotNo { get; set; } = string.Empty;
    public DateTime? SonKullanmaTarihi { get; set; }
    public decimal? SicaklikMin { get; set; }
    public decimal? SicaklikMax { get; set; }
    public string SeriNo { get; set; } = string.Empty;
    public decimal? Agirlik { get; set; }
    public int PaletKoli { get; set; }
}

public sealed class TedarikciSevkiyatEtiketi
{
    public int Id { get; set; }
    public int TedarikciSevkiyatKalemiId { get; set; }
    public string KodHash { get; set; } = string.Empty;
    public decimal Miktar { get; set; }
    public string Durum { get; set; } = "Hazir";
    public DateTime? OkutulduAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class TedarikciMalKabul
{
    public int Id { get; set; }
    public int TedarikciSevkiyatEtiketiId { get; set; }
    public int TedarikciSiparisId { get; set; }
    public int AliciIsletmeId { get; set; }
    public string IdempotencyAnahtari { get; set; } = string.Empty;
    public decimal KabulEdilenMiktar { get; set; }
    public decimal ReddedilenMiktar { get; set; }
    public string RedNedeni { get; set; } = string.Empty;
    public string Not { get; set; } = string.Empty;
    public int? SubeId { get; set; }
    public int? DepoId { get; set; }
    public string IslemYapanKullaniciRef { get; set; } = string.Empty;
    public string CihazRef { get; set; } = string.Empty;
    public string IpAdresi { get; set; } = string.Empty;
    public string BelgeKarmasi { get; set; } = string.Empty;
    public string FotoKanitiYolu { get; set; } = string.Empty;
    public decimal? OlculenAgirlik { get; set; }
    public decimal? OlculenSicaklik { get; set; }
    public decimal KabulBrutTutar { get; set; }
    public decimal SerbestBirakilanNetTutar { get; set; }
    public DateTime? MuhasebelestiAt { get; set; }
    public string HakEdisAktarimReferansi { get; set; } = string.Empty;
    public string HakEdisAktarimHatasi { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class TedarikciSiparisSikayeti
{
    public int Id { get; set; }
    public int TedarikciSiparisId { get; set; }
    public int AliciIsletmeId { get; set; }
    public int TedarikciIsletmeId { get; set; }
    public string Kategori { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;
    public string Talep { get; set; } = string.Empty;
    public string Durum { get; set; } = "Acik";
    public string TedarikciYaniti { get; set; } = string.Empty;
    public string KapanisNotu { get; set; } = string.Empty;
    public DateTime? YanitlandiAt { get; set; }
    public DateTime? KapatildiAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class TedarikciDegerlendirmesi
{
    public int Id { get; set; }
    public int TedarikciSiparisId { get; set; }
    public int AliciIsletmeId { get; set; }
    public int TedarikciIsletmeId { get; set; }
    public int UrunUygunluguPuani { get; set; }
    public int EksiksizTeslimatPuani { get; set; }
    public int HasarsizTeslimatPuani { get; set; }
    public int ZamanindaTeslimatPuani { get; set; }
    public int? SorunCozmePuani { get; set; }
    public decimal OrtalamaPuan { get; set; }
    public string Yorum { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
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
