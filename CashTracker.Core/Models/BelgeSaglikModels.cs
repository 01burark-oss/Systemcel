using System;
using System.Collections.Generic;

namespace CashTracker.Core.Models
{
    public static class BelgeSaglikDurumlari
    {
        public const string Hazir = "Hazir";
        public const string Dikkat = "Dikkat";
        public const string Eksik = "Eksik";
        public const string VeriYok = "VeriYok";
    }

    public sealed class BelgeSaglikOzeti
    {
        public int? Skor { get; set; }
        public string Durum { get; set; } = BelgeSaglikDurumlari.VeriYok;
        public DateTime DonemBaslangic { get; set; }
        public DateTime DonemBitis { get; set; }
        public int FaturaSayisi { get; set; }
        public int HazirBelgeSayisi { get; set; }
        public int EksikBelgeSayisi { get; set; }
        public int TaslakFaturaSayisi { get; set; }
        public int DosyasiEksikFaturaSayisi { get; set; }
        public int SatiriEksikFaturaSayisi { get; set; }
        public int CariBilgisiEksikFaturaSayisi { get; set; }
        public int VadeTarihiEksikFaturaSayisi { get; set; }
        public int BekleyenVeriIstegiSayisi { get; set; }
        public DateTime? SonBelgeAt { get; set; }
        public bool MuhasebeciBagli { get; set; }
        public List<BelgeSaglikSorunu> Sorunlar { get; set; } = [];
    }

    public sealed class BelgeSaglikSorunu
    {
        public string Kod { get; set; } = string.Empty;
        public string Baslik { get; set; } = string.Empty;
        public int Adet { get; set; }
        public int PuanEtkisi { get; set; }
        public string AksiyonUrl { get; set; } = string.Empty;
    }
}
