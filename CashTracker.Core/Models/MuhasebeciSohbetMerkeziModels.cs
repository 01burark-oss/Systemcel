using System;
using System.Collections.Generic;

namespace CashTracker.Core.Models
{
    public static class MuhasebeciSohbetMesajTipleri
    {
        public const string Metin = "Metin";
        public const string Sistem = "Sistem";
        public const string VeriIstegi = "VeriIstegi";
        public const string VeriPaylasimi = "VeriPaylasimi";
        public const string Dosya = "Dosya";
    }

    public static class MuhasebeciSohbetEkTipleri
    {
        public const string Dosya = "Dosya";
        public const string VeriKarti = "VeriKarti";
        public const string RaporPaketi = "RaporPaketi";
    }

    public static class MuhasebeciSohbetVeriIstegiDurumlari
    {
        public const string Beklemede = "Beklemede";
        public const string Paylasildi = "Paylasildi";
        public const string Reddedildi = "Reddedildi";
    }

    public sealed class MuhasebeciSohbetOzetDto
    {
        public int Id { get; init; }
        public int MuhasebeciIsletmeId { get; init; }
        public int MusteriIsletmeId { get; init; }
        public int? TalepId { get; init; }
        public int? BaglantiId { get; init; }
        public string Baslik { get; init; } = string.Empty;
        public string Konu { get; init; } = string.Empty;
        public string KarsiTarafAdi { get; init; } = string.Empty;
        public string Durum { get; init; } = string.Empty;
        public string SonMesaj { get; init; } = string.Empty;
        public DateTime? SonMesajAt { get; init; }
        public int OkunmamisMesajSayisi { get; init; }
        public bool Arsivlendi { get; init; }
        public string HedefUrl { get; init; } = string.Empty;
    }

    public sealed class MuhasebeciSohbetListeDto
    {
        public List<MuhasebeciSohbetOzetDto> Sohbetler { get; init; } = new();
        public int OkunmamisMesajSayisi { get; init; }
    }

    public sealed class MuhasebeciSohbetEkiDto
    {
        public int Id { get; init; }
        public int? MesajId { get; init; }
        public string EkTipi { get; init; } = string.Empty;
        public string DosyaAdi { get; init; } = string.Empty;
        public string IcerikTipi { get; init; } = string.Empty;
        public long Boyut { get; init; }
        public string VeriTipi { get; init; } = string.Empty;
        public string Baslik { get; init; } = string.Empty;
        public string OzetJson { get; init; } = string.Empty;
        public string IndirUrl { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }

    public sealed class MuhasebeciSohbetMerkeziMesajiDto
    {
        public int Id { get; init; }
        public int SohbetId { get; init; }
        public int GonderenIsletmeId { get; init; }
        public string GonderenAdi { get; init; } = string.Empty;
        public bool BenimMesajim { get; init; }
        public string MesajTipi { get; init; } = MuhasebeciSohbetMesajTipleri.Metin;
        public string ClientMessageId { get; init; } = string.Empty;
        public string Mesaj { get; init; } = string.Empty;
        public string Durum { get; init; } = "Gonderildi";
        public DateTime? OkunduAt { get; init; }
        public DateTime CreatedAt { get; init; }
        public List<MuhasebeciSohbetEkiDto> Ekler { get; init; } = new();
    }

    public sealed class MuhasebeciSohbetMesajSayfasiDto
    {
        public int SohbetId { get; init; }
        public MuhasebeciSohbetOzetDto? Sohbet { get; init; }
        public List<MuhasebeciSohbetMerkeziMesajiDto> Mesajlar { get; init; } = new();
        public bool HasMore { get; init; }
        public int? NextBeforeId { get; init; }
    }

    public sealed class MuhasebeciSohbetMesajiOlusturRequest
    {
        public string Mesaj { get; init; } = string.Empty;
        public string ClientMessageId { get; init; } = string.Empty;
    }

    public sealed class MuhasebeciSohbetKonuGuncelleRequest
    {
        public string Konu { get; init; } = string.Empty;
    }

    public sealed class MuhasebeciSohbetArsivRequest
    {
        public bool Arsivlendi { get; init; }
    }

    public sealed class MuhasebeciSohbetVeriIstegiRequest
    {
        public string VeriTipi { get; init; } = "GelirGiderOzeti";
        public string AralikKodu { get; init; } = "last30";
        public string Baslangic { get; init; } = string.Empty;
        public string Bitis { get; init; } = string.Empty;
        public string Mesaj { get; init; } = string.Empty;
    }

    public sealed class MuhasebeciSohbetVeriPaylasimiRequest
    {
        public string VeriTipi { get; init; } = "GelirGiderOzeti";
        public string AralikKodu { get; init; } = "last30";
        public string Baslangic { get; init; } = string.Empty;
        public string Bitis { get; init; } = string.Empty;
        public string Mesaj { get; init; } = string.Empty;
        public int? VeriIstegiId { get; init; }
    }

    public sealed class MuhasebeciSohbetVeriIstegiDto
    {
        public int Id { get; init; }
        public int SohbetId { get; init; }
        public string VeriTipi { get; init; } = string.Empty;
        public string AralikKodu { get; init; } = string.Empty;
        public DateTime Baslangic { get; init; }
        public DateTime Bitis { get; init; }
        public string Durum { get; init; } = string.Empty;
        public int? SonucEkId { get; init; }
        public string Mesaj { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }
}
