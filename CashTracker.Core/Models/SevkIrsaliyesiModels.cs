namespace CashTracker.Core.Models;

public static class SevkIrsaliyesiDurumlari
{
    public const string Hazirlandi = "Hazirlandi";
    public const string Gonderildi = "Gonderildi";
    public const string TeslimEdildi = "TeslimEdildi";
    public const string KabulEdildi = "KabulEdildi";
    public const string KismenKabul = "KismenKabul";
    public const string Reddedildi = "Reddedildi";
    public const string Hatali = "Hatali";
}

public sealed record SevkIrsaliyesiKalemi(
    int SiparisKalemiId,
    string Sku,
    string Ad,
    decimal Miktar,
    string Birim,
    string? LotNo,
    string? SeriNo,
    DateTime? SonKullanmaTarihi);

public sealed record SevkIrsaliyesiGonderRequest(
    int IsletmeId,
    int SevkiyatId,
    string BelgeNo,
    string BelgeUuid,
    DateTime SevkAt,
    string GondericiVergiNo,
    string AliciVergiNo,
    string CikisDeposu,
    string VarisDeposu,
    string? AracPlaka,
    string? SurucuAdi,
    IReadOnlyList<SevkIrsaliyesiKalemi> Kalemler,
    string IdempotencyAnahtari);

public sealed record SevkIrsaliyesiYanitRequest(
    int IsletmeId,
    string SaglayiciBelgeReferansi,
    string Karar,
    string Aciklama,
    IReadOnlyDictionary<int, decimal> KabulEdilenMiktarlar,
    string IdempotencyAnahtari);

public sealed record SevkIrsaliyesiSonucu(
    bool Basarili,
    string Durum,
    string SaglayiciBelgeReferansi,
    string BelgeUuid,
    string HataKodu,
    string Mesaj,
    DateTime? SaglayiciZamani)
{
    public static SevkIrsaliyesiSonucu Yapilandirilmadi(string mesaj) =>
        new(false, SevkIrsaliyesiDurumlari.Hatali, string.Empty, string.Empty, "provider_not_configured", mesaj, null);
}
