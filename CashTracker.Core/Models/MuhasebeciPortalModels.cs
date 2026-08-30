using System;
using System.Collections.Generic;

namespace CashTracker.Core.Models
{
    public static class MuhasebeciYetkiSeviyeleri
    {
        public const string OkumaRapor = "OkumaRapor";
        public const string TamIslem = "TamIslem";
    }

    public static class MuhasebeciTalepDurumlari
    {
        public const string Beklemede = "Beklemede";
        public const string OdemeBekliyor = "OdemeBekliyor";
        public const string Kabul = "Kabul";
        public const string Red = "Red";
        public const string Iptal = "Iptal";
    }

    public static class MuhasebeciHizmetOdemeDurumlari
    {
        public const string OdemeBekliyor = "OdemeBekliyor";
        public const string CheckoutAcik = "CheckoutAcik";
        public const string TahsilEdildi = "TahsilEdildi";
        public const string Basarisiz = "Basarisiz";
        public const string IadeEdildi = "IadeEdildi";
        public const string IptalEdildi = "IptalEdildi";
    }

    public static class MuhasebeciAktarimDurumlari
    {
        public const string Olusmadi = "Olusmadi";
        public const string Bekliyor = "Bekliyor";
        public const string Aktarildi = "Aktarildi";
        public const string Iptal = "Iptal";
        public const string TersKayit = "TersKayit";
    }

    public sealed class MuhasebeciOdemeOptions
    {
        public decimal PlatformCommissionRate { get; init; } = 10m;
    }

    public static class MuhasebeciTalepTurleri
    {
        public const string Davet = "Davet";
        public const string MusteriDaveti = "MusteriDaveti";
        public const string Pazaryeri = "Pazaryeri";
    }

    public static class KullaniciDurumlari
    {
        public const string Aktif = "Aktif";
        public const string MuhasebeciOnayBekliyor = "MuhasebeciOnayBekliyor";
        public const string MuhasebeciReddedildi = "MuhasebeciReddedildi";
    }

    public sealed class ActiveBusinessAccess
    {
        public int IsletmeId { get; init; }
        public bool MuhasebeciMusteriBaglami { get; init; }
        public int? MuhasebeciIsletmeId { get; init; }
        public string YetkiSeviyesi { get; init; } = MuhasebeciYetkiSeviyeleri.TamIslem;
        public bool YazmaYetkisi => !MuhasebeciMusteriBaglami ||
            string.Equals(YetkiSeviyesi, MuhasebeciYetkiSeviyeleri.TamIslem, StringComparison.OrdinalIgnoreCase);
    }

    public sealed class MuhasebeciProfilKaydetRequest
    {
        public bool Yayinda { get; init; }
        public string Unvan { get; init; } = string.Empty;
        public string Konum { get; init; } = string.Empty;
        public string Telefon { get; init; } = string.Empty;
        public int DeneyimYili { get; init; }
        public string ProfilResmiUrl { get; init; } = string.Empty;
        public string UcretBilgisi { get; init; } = string.Empty;
        public string Uzmanliklar { get; init; } = string.Empty;
        public string MusteriTipleri { get; init; } = string.Empty;
        public string SektorDeneyimleri { get; init; } = string.Empty;
        public string VergiMukellefiTipleri { get; init; } = string.Empty;
        public string UygunIsletmeOlcekleri { get; init; } = string.Empty;
        public string CalismaSekilleri { get; init; } = string.Empty;
        public string KisaAciklama { get; init; } = string.Empty;
    }

    public sealed class MuhasebeciTalepOlusturRequest
    {
        public string YetkiSeviyesi { get; init; } = MuhasebeciYetkiSeviyeleri.OkumaRapor;
        public string Mesaj { get; init; } = string.Empty;
        public decimal AylikHizmetBedeli { get; init; }
        public string VergiMukellefiTipi { get; init; } = string.Empty;
        public string IsletmeOlcegi { get; init; } = string.Empty;
        public string CalismaSekli { get; init; } = string.Empty;
    }

    public sealed class MuhasebeciSohbetMesajiGonderRequest
    {
        public string Mesaj { get; init; } = string.Empty;
    }

    public sealed class MuhasebeciTalepKararRequest
    {
        public string YetkiSeviyesi { get; init; } = MuhasebeciYetkiSeviyeleri.OkumaRapor;
        public decimal AylikHizmetBedeli { get; init; }
    }

    public sealed class MuhasebeciDavetKabulRequest
    {
        public string DavetKodu { get; init; } = string.Empty;
        public string YetkiSeviyesi { get; init; } = MuhasebeciYetkiSeviyeleri.OkumaRapor;
    }

    public sealed class MuhasebeciProfilDto
    {
        public int MuhasebeciIsletmeId { get; init; }
        public bool Yayinda { get; init; }
        public string Unvan { get; init; } = string.Empty;
        public string Konum { get; init; } = string.Empty;
        public string Telefon { get; init; } = string.Empty;
        public int DeneyimYili { get; init; }
        public string ProfilResmiUrl { get; init; } = string.Empty;
        public string UcretBilgisi { get; init; } = string.Empty;
        public string Uzmanliklar { get; init; } = string.Empty;
        public string MusteriTipleri { get; init; } = string.Empty;
        public string SektorDeneyimleri { get; init; } = string.Empty;
        public string VergiMukellefiTipleri { get; init; } = string.Empty;
        public string UygunIsletmeOlcekleri { get; init; } = string.Empty;
        public string CalismaSekilleri { get; init; } = string.Empty;
        public string KisaAciklama { get; init; } = string.Empty;
        public string PlanAdi { get; init; } = string.Empty;
        public bool Pro { get; init; }
        public bool TalepVar { get; init; }
        public bool Bagli { get; init; }
        public int? EslesmeSkoru { get; init; }
        public List<string> EslesmeNedenleri { get; init; } = new();
    }

    public sealed class MuhasebeciMusteriDto
    {
        public int IsletmeId { get; init; }
        public string Ad { get; init; } = string.Empty;
        public string Konum { get; init; } = string.Empty;
        public string YetkiSeviyesi { get; init; } = MuhasebeciYetkiSeviyeleri.OkumaRapor;
        public string Durum { get; init; } = string.Empty;
        public DateTime BaslangicAt { get; init; }
        public BelgeSaglikOzeti? BelgeSagligi { get; init; }
    }

    public sealed class MuhasebeciLinkDavetOlusturRequest
    {
        public string YetkiSeviyesi { get; init; } = MuhasebeciYetkiSeviyeleri.OkumaRapor;
        public string Mesaj { get; init; } = string.Empty;
    }

    public sealed class MuhasebeciLinkDavetKabulRequest
    {
        public string Token { get; init; } = string.Empty;
        public decimal AylikHizmetBedeli { get; init; }
    }

    public sealed class MuhasebeciLinkDavetDto
    {
        public string MusteriAdi { get; init; } = string.Empty;
        public string Durum { get; init; } = string.Empty;
        public string YetkiSeviyesi { get; init; } = MuhasebeciYetkiSeviyeleri.OkumaRapor;
        public string Mesaj { get; init; } = string.Empty;
        public string DavetLinki { get; init; } = string.Empty;
        public DateTime SonGecerlilikAt { get; init; }
    }

    public sealed class MuhasebeciTalepDto
    {
        public int Id { get; init; }
        public int MuhasebeciIsletmeId { get; init; }
        public int? MusteriIsletmeId { get; init; }
        public string MuhasebeciAdi { get; init; } = string.Empty;
        public string MusteriAdi { get; init; } = string.Empty;
        public string Tur { get; init; } = string.Empty;
        public string Durum { get; init; } = string.Empty;
        public string YetkiSeviyesi { get; init; } = MuhasebeciYetkiSeviyeleri.OkumaRapor;
        public string DavetKodu { get; init; } = string.Empty;
        public string DavetLinki { get; init; } = string.Empty;
        public string Mesaj { get; init; } = string.Empty;
        public decimal AylikHizmetBedeli { get; init; }
        public string Sektor { get; init; } = string.Empty;
        public string VergiMukellefiTipi { get; init; } = string.Empty;
        public string IsletmeOlcegi { get; init; } = string.Empty;
        public string CalismaSekli { get; init; } = string.Empty;
        public string OdemeDurumu { get; init; } = string.Empty;
        public bool OdemeYapilabilir { get; init; }
        public DateTime CreatedAt { get; init; }
    }

    public sealed record MuhasebeciOdemeCheckoutCommand(
        int TalepId,
        int MusteriIsletmeId,
        string IdempotencyKey,
        string KullaniciReferansi,
        string Eposta,
        Uri BasariliUrl,
        Uri BasarisizUrl,
        Uri CallbackUrl);

    public sealed record MuhasebeciOdemeCheckoutResult(
        int OdemeIslemiId,
        Uri CheckoutUrl,
        DateTime ExpiresAt,
        bool Reused,
        string HizmetDonemi,
        decimal AylikHizmetBedeli,
        string ParaBirimi);

    public sealed class MuhasebeciOdemeOzetiDto
    {
        public int TalepId { get; init; }
        public int MuhasebeciIsletmeId { get; init; }
        public int MusteriIsletmeId { get; init; }
        public decimal AylikHizmetBedeli { get; init; }
        public string HizmetDonemi { get; init; } = string.Empty;
        public DateTime VadeAt { get; init; }
        public decimal PlatformKomisyonOrani { get; init; }
        public string ParaBirimi { get; init; } = "TRY";
        public string OdemeDurumu { get; init; } = string.Empty;
        public bool OdemeYapilabilir { get; init; }
        public decimal AktarilacakTutar { get; init; }
        public string AktarimDonemi { get; init; } = string.Empty;
        public string AktarimDurumu { get; init; } = string.Empty;
    }

    public sealed class MuhasebeciSohbetMesajiDto
    {
        public int Id { get; init; }
        public int GonderenIsletmeId { get; init; }
        public string GonderenAdi { get; init; } = string.Empty;
        public bool BenimMesajim { get; init; }
        public string Mesaj { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }

    public sealed class MuhasebeciSohbetDto
    {
        public int MuhasebeciIsletmeId { get; init; }
        public int MusteriIsletmeId { get; init; }
        public int? TalepId { get; init; }
        public int? BaglantiId { get; init; }
        public string MuhasebeciAdi { get; init; } = string.Empty;
        public string MusteriAdi { get; init; } = string.Empty;
        public string Durum { get; init; } = string.Empty;
        public string BilgiMesaji { get; init; } = string.Empty;
        public List<MuhasebeciSohbetMesajiDto> Mesajlar { get; init; } = new();
    }

    public sealed class MuhasebeciSohbetBildirimDto
    {
        public int MuhasebeciIsletmeId { get; init; }
        public int MusteriIsletmeId { get; init; }
        public int? TalepId { get; init; }
        public int? BaglantiId { get; init; }
        public string Baslik { get; init; } = string.Empty;
        public string SonMesaj { get; init; } = string.Empty;
        public DateTime SonMesajAt { get; init; }
        public int OkunmamisMesajSayisi { get; init; }
        public string HedefUrl { get; init; } = string.Empty;
    }

    public sealed class MuhasebeciSohbetBildirimDurumuDto
    {
        public int OkunmamisMesajSayisi { get; init; }
        public List<MuhasebeciSohbetBildirimDto> Sohbetler { get; init; } = new();
    }

    public sealed class MuhasebeciPanelDto
    {
        public bool Hazir { get; init; }
        public int MuhasebeciIsletmeId { get; init; }
        public string MuhasebeciAdi { get; init; } = string.Empty;
        public string Mesaj { get; init; } = string.Empty;
        public SubscriptionEntitlementStatus? Entitlement { get; init; }
        public MuhasebeciProfilDto? Profil { get; init; }
        public List<MuhasebeciMusteriDto> Musteriler { get; init; } = new();
        public List<MuhasebeciTalepDto> BekleyenTalepler { get; init; } = new();
        public List<MuhasebeciTalepDto> Davetler { get; init; } = new();
    }

    public sealed class MuhasebeciPazaryeriDto
    {
        public string Mesaj { get; init; } = string.Empty;
        public List<MuhasebeciProfilDto> Profiller { get; init; } = new();
    }
}
