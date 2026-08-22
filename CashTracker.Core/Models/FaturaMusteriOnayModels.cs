using System;

namespace CashTracker.Core.Models
{
    public static class FaturaMusteriOnayDurumlari
    {
        public const string Bekliyor = "Bekliyor";
        public const string Onaylandi = "Onaylandi";
        public const string DuzeltmeIstendi = "DuzeltmeIstendi";
        public const string SuresiDoldu = "SuresiDoldu";
        public const string Iptal = "Iptal";
        public const string Gonderilemedi = "Gonderilemedi";
    }

    public sealed class MusteriSmsSettings
    {
        public string Provider { get; set; } = "Netgsm";
        public string BaseUrl { get; set; } = "https://api.netgsm.com.tr";
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Header { get; set; } = string.Empty;
        public string AppName { get; set; } = "systemcel";
        public string PublicBaseUrl { get; set; } = "https://systemcel.app";
        public int LinkExpiryHours { get; set; } = 72;
        public int ResendCooldownMinutes { get; set; } = 15;

        public bool IsConfigured =>
            string.Equals(Provider?.Trim(), "Netgsm", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(Password) &&
            !string.IsNullOrWhiteSpace(Header);

        public string EffectiveBaseUrl => string.IsNullOrWhiteSpace(BaseUrl)
            ? "https://api.netgsm.com.tr"
            : BaseUrl.Trim().TrimEnd('/');

        public string EffectivePublicBaseUrl => string.IsNullOrWhiteSpace(PublicBaseUrl)
            ? "https://systemcel.app"
            : PublicBaseUrl.Trim().TrimEnd('/');

        public int EffectiveLinkExpiryHours => Math.Clamp(LinkExpiryHours, 1, 168);
        public int EffectiveResendCooldownMinutes => Math.Clamp(ResendCooldownMinutes, 1, 1440);
    }

    public sealed record MusteriSmsGonderimSonucu(
        bool Basarili,
        string Saglayici,
        string IslemId,
        string Hata);

    public sealed record FaturaMusteriOnayGonderimSonucu(
        int OnayId,
        int FaturaId,
        string Durum,
        string AliciTelefonMaskeli,
        string OnayUrl,
        DateTime SonGecerlilikAt,
        DateTime? GonderildiAt,
        string Mesaj);

    public sealed record FaturaMusteriOnayDurumu(
        int? OnayId,
        int FaturaId,
        string Durum,
        string AliciTelefonMaskeli,
        DateTime? GonderildiAt,
        DateTime? SonGecerlilikAt,
        DateTime? YanitAt,
        string YanitNotu);

    public sealed record PublicFaturaMusteriOnayDetayi(
        string Durum,
        string IsletmeAdi,
        string CariUnvan,
        string CariVergiNoMaskeli,
        string CariAdres,
        string FaturaNo,
        DateTime FaturaTarihi,
        decimal FaturaToplami,
        string ParaBirimi,
        DateTime SonGecerlilikAt,
        DateTime? YanitAt,
        string Aciklama);

    public sealed class PublicFaturaMusteriOnayYaniti
    {
        public bool BilgilerDogru { get; set; }
        public string? Aciklama { get; set; }
    }
}
