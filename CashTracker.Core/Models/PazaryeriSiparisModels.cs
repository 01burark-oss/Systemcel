namespace CashTracker.Core.Models;

public static class PazaryeriSiparisDurumlari
{
    public const string OdemeBekliyor = "OdemeBekliyor";
    public const string SiparisVerildi = "SiparisVerildi";
    public const string Odendi = "Odendi";
    public const string TedarikciOnayladi = "TedarikciOnayladi";
    public const string Hazirlaniyor = "Hazirlaniyor";
    public const string SevkeHazir = "SevkeHazir";
    public const string KismenSevkEdildi = "KismenSevkEdildi";
    public const string SevkEdildi = "SevkEdildi";
    public const string KismenKabul = "KismenKabul";
    public const string MalKabulBekliyor = "MalKabulBekliyor";
    public const string TeslimEdildi = "TeslimEdildi";
    public const string HakEdisBekliyor = "HakEdisBekliyor";
    public const string CariOdemeBekliyor = "CariOdemeBekliyor";
    public const string Tamamlandi = "Tamamlandi";
    public const string IptalEdildi = "IptalEdildi";
    public const string IadeBekliyor = "IadeBekliyor";
    public const string IadeEdildi = "IadeEdildi";
    public const string KismiIade = "KismiIade";
    public const string KismiIptal = "KismiIptal";
    public const string Itirazli = "Itirazli";
}

public sealed record TedarikciOnboardingRequest(
    string Unvan,
    string Kategoriler,
    string Sehir,
    string Aciklama,
    string VergiNo,
    string MersisNo,
    string KepAdresi,
    string Iban,
    string Adres,
    string YetkiliAdSoyad,
    string VergiDurumu,
    string SevkiyatBolgeleri,
    string IadeKosullari,
    string PazaryeriSozlesmeVersiyonu,
    bool TevkifatMuaf,
    bool Yayinda);

public sealed record TedarikciDogrulamaRequest(
    bool Onaylandi,
    string Not,
    decimal KomisyonOrani,
    int OdemeVadesiGun,
    string PspAltUyeIsyeriId);

public sealed record TedarikciUrunKaydetRequest(
    int? KaynakUrunHizmetId,
    string Sku,
    string Ad,
    string Aciklama,
    string Kategori,
    string Birim,
    decimal BirimFiyat,
    decimal KdvOrani,
    string ParaBirimi,
    decimal StokMiktari,
    decimal MinimumSiparisMiktari,
    int TahminiTeslimatGun,
    bool Aktif);

public sealed record PazaryeriSepetKalemiRequest(int UrunId, decimal Miktar);

public sealed record PazaryeriSiparisOlusturRequest(
    string TeslimatAdresi,
    string IdempotencyKey,
    IReadOnlyList<PazaryeriSepetKalemiRequest> Kalemler,
    bool Vadeli = false);

public sealed record PazaryeriOdemeRequest(string IdempotencyKey);

public sealed record TedarikciSiparisDurumRequest(
    string Durum,
    string? KargoFirmasi,
    string? KargoTakipNo,
    string? Aciklama);

public sealed record PazaryeriIptalRequest(string Neden);

public sealed record PazaryeriHakEdisTamamlaRequest(string AktarimReferansi);

public static class PazaryeriItirazKararlari
{
    public const string TedarikciyeAktar = "TedarikciyeAktar";
    public const string AliciyaIade = "AliciyaIade";
    public const string KismiPaylas = "KismiPaylas";
    public const string YenidenTeslim = "YenidenTeslim";
}

public sealed record PazaryeriItirazCozRequest(
    bool DevamEt,
    string Not,
    string? Karar = null,
    decimal? TedarikciyeAktarilacakTutar = null);

public sealed record TedarikciBelgeEsleRequest(string BelgeNo, string BelgeUuid);

public sealed record TedarikciSevkiyatKalemiRequest(
    int SiparisKalemiId,
    decimal Miktar,
    int EtiketSayisi,
    string? LotNo,
    DateTime? SonKullanmaTarihi,
    decimal? SicaklikMin,
    decimal? SicaklikMax,
    string? SeriNo = null,
    decimal? Agirlik = null,
    int PaletKoli = 0);

public sealed record TedarikciSevkiyatOlusturRequest(
    string TasimaTipi,
    string? Tasiyici,
    string? BelgeNo,
    string? AracPlaka,
    string? SurucuAdi,
    string? CikisDeposu,
    DateTime? PlanlananTeslimAt,
    string? Not,
    IReadOnlyList<TedarikciSevkiyatKalemiRequest> Kalemler,
    string? BelgeUuid = null,
    DateTime? SevkAt = null,
    int? VarisDeposu = null,
    DateTime? RandevuAt = null);

public sealed record TedarikciSevkiyatEtiketiDto(
    int Id,
    string Kod,
    string QrIcerigi,
    decimal Miktar,
    string UrunAdi,
    string Sku,
    string Birim,
    string LotNo,
    DateTime? SonKullanmaTarihi);

public sealed record TedarikciSevkiyatSonucu(
    int Id,
    string SevkiyatNo,
    string Durum,
    IReadOnlyList<TedarikciSevkiyatEtiketiDto> Etiketler);

public sealed record TedarikciQrCozumDto(
    string Kod,
    int SiparisId,
    string SiparisNo,
    string UrunAdi,
    string Sku,
    string Birim,
    decimal Miktar,
    string LotNo,
    DateTime? SonKullanmaTarihi,
    string Durum,
    decimal? SicaklikMin = null,
    decimal? SicaklikMax = null);

public sealed record TedarikciMalKabulRequest(
    string IdempotencyKey,
    decimal KabulEdilenMiktar,
    decimal ReddedilenMiktar,
    string? RedNedeni,
    string? Not,
    int? SubeId = null,
    int? DepoId = null,
    string? IslemYapanKullaniciRef = null,
    string? CihazRef = null,
    string? IpAdresi = null,
    string? BelgeKarmasi = null,
    string? FotoKanitiYolu = null,
    decimal? OlculenAgirlik = null,
    decimal? OlculenSicaklik = null,
    string? IkinciRedNedeni = null);

public sealed record TedarikciMalKabulSonucu(int Id, string SiparisDurumu, bool TekrarKullanildi = false);

public static class TedarikciStokUzlastirmaDurumlari
{
    public const string Uygulanmaz = "Uygulanmaz";
    public const string Bekliyor = "Bekliyor";
    public const string ManuelInceleme = "ManuelInceleme";
    public const string Kayip = "Kayip";
    public const string TedarikciyeIade = "TedarikciyeIade";
    public const string YenidenSevk = "YenidenSevk";
}

public sealed record TedarikciMalKabulStokUzlastirmaRequest(
    string IdempotencyKey,
    string Karar,
    string Not);

public sealed record TedarikciMalKabulStokUzlastirmaSonucu(
    int MalKabulId,
    string Durum,
    decimal UzlastirilanMiktar,
    bool TekrarKullanildi = false);

public static class TedarikciSikayetKategorileri
{
    public const string Eksik = "Eksik";
    public const string Hasarli = "Hasarli";
    public const string YanlisUrun = "YanlisUrun";
    public const string Kalite = "Kalite";
    public const string Diger = "Diger";
    public const string TeslimEdilmedi = "TeslimEdilmedi";
    public const string Sicaklik = "Sicaklik";
    public const string BelgeUyusmazligi = "BelgeUyusmazligi";
}

public static class TedarikciSikayetDurumlari
{
    public const string Acik = "Acik";
    public const string Yanitlandi = "Yanitlandi";
    public const string Cozuldu = "Cozuldu";
    public const string Cozulemedi = "Cozulemedi";
}

public sealed record TedarikciSikayetOlusturRequest(
    string Kategori,
    string Aciklama,
    string Talep);

public sealed record TedarikciSikayetYanitRequest(string Yanit);

public sealed record TedarikciSikayetKapatRequest(bool Cozuldu, string? Not);

public sealed record TedarikciDegerlendirmeKaydetRequest(
    int UrunUygunluguPuani,
    int EksiksizTeslimatPuani,
    int HasarsizTeslimatPuani,
    int ZamanindaTeslimatPuani,
    int? SorunCozmePuani,
    string? Yorum);

public sealed record MarketplacePaymentCommand(
    string OrderReference,
    string IdempotencyKey,
    decimal Amount,
    string Currency,
    int BuyerBusinessId,
    IReadOnlyList<MarketplacePaymentAllocation> Allocations);

public sealed record MarketplacePaymentAllocation(
    int SupplierBusinessId,
    decimal GrossAmount,
    decimal SupplierPayable);

public sealed record MarketplacePaymentResult(
    string Provider,
    string ProviderTransactionId,
    bool Succeeded,
    string Error = "");

public sealed record MarketplacePayoutCommand(
    string ProviderTransactionId,
    string IdempotencyKey,
    int SupplierBusinessId,
    decimal Amount,
    string Currency);

public sealed record PazaryeriIslemSonucu(int Id, string Mesaj, bool TekrarKullanildi = false);
