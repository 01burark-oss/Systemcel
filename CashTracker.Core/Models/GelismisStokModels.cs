using System;
using System.Collections.Generic;

namespace CashTracker.Core.Models
{
    public static class StokDefterIslemTipleri
    {
        public const string Giris = "Giris";
        public const string Cikis = "Cikis";
        public const string Transfer = "Transfer";
        public const string SayimDuzeltme = "SayimDuzeltme";
        public const string Rezervasyon = "Rezervasyon";
        public const string RezervasyonBirakma = "RezervasyonBirakma";
        public const string TersKayit = "TersKayit";
    }

    public sealed class StokDepoOlusturRequest
    {
        public string Ad { get; init; } = string.Empty;
        public string Kod { get; init; } = string.Empty;
        public string? Konum { get; init; }
    }

    public sealed class StokHareketIslemRequest
    {
        public int UrunHizmetId { get; init; }
        public int DepoId { get; init; }
        public string IslemTipi { get; init; } = StokDefterIslemTipleri.Giris;
        public decimal Miktar { get; init; }
        public string? Aciklama { get; init; }
    }

    public sealed class StokTransferRequest
    {
        public int UrunHizmetId { get; init; }
        public int KaynakDepoId { get; init; }
        public int HedefDepoId { get; init; }
        public decimal Miktar { get; init; }
        public string? Aciklama { get; init; }
    }

    public sealed class StokSayimRequest
    {
        public int UrunHizmetId { get; init; }
        public int DepoId { get; init; }
        public decimal SayilanMiktar { get; init; }
        public bool Onaylandi { get; init; }
        public string? Aciklama { get; init; }
    }

    public sealed class StokTersKayitRequest
    {
        public string? Aciklama { get; init; }
    }

    public sealed class StokDefterIslemResult
    {
        public int IslemId { get; init; }
        public string IslemTipi { get; init; } = string.Empty;
        public bool Tekrarlandi { get; init; }
        public int? TersKayitKaynakIslemId { get; init; }
    }

    public sealed class StokDepoDto
    {
        public int Id { get; init; }
        public string Ad { get; init; } = string.Empty;
        public string Kod { get; init; } = string.Empty;
        public string? Konum { get; init; }
        public bool Varsayilan { get; init; }
    }

    public sealed class StokDefterHareketDto
    {
        public int Id { get; init; }
        public int? IslemId { get; init; }
        public int UrunHizmetId { get; init; }
        public string UrunAdi { get; init; } = string.Empty;
        public int? DepoId { get; init; }
        public string DepoAdi { get; init; } = string.Empty;
        public DateTime Tarih { get; init; }
        public decimal Miktar { get; init; }
        public decimal RezerveMiktar { get; init; }
        public string HareketTipi { get; init; } = string.Empty;
        public string? Aciklama { get; init; }
        public bool TersKayitVar { get; init; }
    }

    public sealed class StokDefteriDto
    {
        public List<StokDepoDto> Depolar { get; init; } = [];
        public List<StokDefterHareketDto> Hareketler { get; init; } = [];
        public bool NegatifStokEngelli { get; init; } = true;
    }
}
