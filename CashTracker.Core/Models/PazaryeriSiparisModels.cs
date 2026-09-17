namespace CashTracker.Core.Models;

public static class PazaryeriSiparisDurumlari
{
    public const string OdemeBekliyor = "OdemeBekliyor";
    public const string SiparisVerildi = "SiparisVerildi";
    public const string Odendi = "Odendi";
    public const string TedarikciOnayladi = "TedarikciOnayladi";
    public const string Hazirlaniyor = "Hazirlaniyor";
    public const string SevkEdildi = "SevkEdildi";
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

public sealed record PazaryeriItirazCozRequest(bool DevamEt, string Not);

public sealed record TedarikciBelgeEsleRequest(string BelgeNo, string BelgeUuid);

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
