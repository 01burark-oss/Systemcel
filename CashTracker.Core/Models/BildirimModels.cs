namespace CashTracker.Core.Models;

public static class BildirimKanallari
{
    public const string Uygulama = "Uygulama";
    public const string Eposta = "Eposta";
    public const string Telegram = "Telegram";
}

public static class BildirimTeslimDurumlari
{
    public const string Bekliyor = "Bekliyor";
    public const string Isleniyor = "Isleniyor";
    public const string TeslimEdildi = "TeslimEdildi";
    public const string Yapilandirilmadi = "Yapilandirilmadi";
    public const string DeadLetter = "DeadLetter";
}

public sealed record BildirimSnapshot(
    string KaynakAnahtari,
    string Tur,
    string Onem,
    string Baslik,
    string Mesaj,
    string Aksiyon,
    string Url);

public sealed record BildirimGorunumu(
    int Id,
    string KaynakAnahtari,
    string Tur,
    string Onem,
    string Baslik,
    string Mesaj,
    string Aksiyon,
    string Url,
    bool Okundu,
    DateTime CreatedAt);

public sealed record BildirimTercihModeli(
    bool UygulamaAktif,
    bool EpostaAktif,
    bool TelegramAktif,
    bool SessizSaatAktif,
    int SessizBaslangicDakika,
    int SessizBitisDakika,
    string SaatDilimi);

public sealed record BildirimOutboxClaim(
    long Id,
    int IsletmeId,
    string KullaniciRef,
    string Kanal,
    string PayloadJson,
    string ClaimToken,
    int DenemeSayisi);
