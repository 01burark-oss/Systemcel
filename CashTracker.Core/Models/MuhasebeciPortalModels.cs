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
        public const string Kabul = "Kabul";
        public const string Red = "Red";
        public const string Iptal = "Iptal";
    }

    public static class MuhasebeciTalepTurleri
    {
        public const string Davet = "Davet";
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
        public string KisaAciklama { get; init; } = string.Empty;
    }

    public sealed class MuhasebeciTalepOlusturRequest
    {
        public string YetkiSeviyesi { get; init; } = MuhasebeciYetkiSeviyeleri.OkumaRapor;
        public string Mesaj { get; init; } = string.Empty;
    }

    public sealed class MuhasebeciSohbetMesajiGonderRequest
    {
        public string Mesaj { get; init; } = string.Empty;
    }

    public sealed class MuhasebeciTalepKararRequest
    {
        public string YetkiSeviyesi { get; init; } = MuhasebeciYetkiSeviyeleri.OkumaRapor;
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
        public string KisaAciklama { get; init; } = string.Empty;
        public string PlanAdi { get; init; } = string.Empty;
        public bool Pro { get; init; }
        public bool TalepVar { get; init; }
        public bool Bagli { get; init; }
    }

    public sealed class MuhasebeciMusteriDto
    {
        public int IsletmeId { get; init; }
        public string Ad { get; init; } = string.Empty;
        public string Konum { get; init; } = string.Empty;
        public string YetkiSeviyesi { get; init; } = MuhasebeciYetkiSeviyeleri.OkumaRapor;
        public string Durum { get; init; } = string.Empty;
        public DateTime BaslangicAt { get; init; }
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
        public DateTime CreatedAt { get; init; }
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
