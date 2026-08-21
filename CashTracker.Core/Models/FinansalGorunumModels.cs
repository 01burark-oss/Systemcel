using System;
using System.Collections.Generic;

namespace CashTracker.Core.Models
{
    public sealed class FinansalGorunum
    {
        public DateTime ReferansTarihi { get; set; }
        public string ParaBirimi { get; set; } = "TRY";
        public decimal KasaBakiyesi { get; set; }
        public decimal AcikAlacakToplami { get; set; }
        public decimal VadesiGecmisAlacakToplami { get; set; }
        public List<AlacakYaslandirmaDilimi> Yaslandirma { get; set; } = [];
        public List<CariAlacakYaslandirma> CariYaslandirma { get; set; } = [];
        public AlacakYogunlasmaOzeti Yogunlasma { get; set; } = new();
        public List<CariOdemeRitmi> CariRiskleri { get; set; } = [];
        public List<NakitProjeksiyonHaftasi> NakitProjeksiyonu { get; set; } = [];
        public int? IlkNegatifHafta { get; set; }
        public List<FinansalVeriUyarisi> VeriUyarilari { get; set; } = [];
    }

    public sealed class AlacakYaslandirmaDilimi
    {
        public string Kod { get; set; } = string.Empty;
        public string Etiket { get; set; } = string.Empty;
        public decimal Tutar { get; set; }
        public int FaturaAdedi { get; set; }
        public decimal Oran { get; set; }
    }

    public sealed class CariAlacakYaslandirma
    {
        public int CariKartId { get; set; }
        public string Unvan { get; set; } = string.Empty;
        public decimal Toplam { get; set; }
        public decimal VadesiGelmemis { get; set; }
        public decimal Gun1Ila30 { get; set; }
        public decimal Gun31Ila60 { get; set; }
        public decimal Gun61Ila90 { get; set; }
        public decimal Gun91VeUzeri { get; set; }
        public int AcikFaturaAdedi { get; set; }
        public int EnUzunGecikmeGunu { get; set; }
        public decimal ToplamdakiOrani { get; set; }
    }

    public sealed class AlacakYogunlasmaOzeti
    {
        public decimal EnBuyukCariOrani { get; set; }
        public decimal IlkUcCariOrani { get; set; }
        public decimal IlkBesCariOrani { get; set; }
        public decimal Hhi { get; set; }
        public string RiskSeviyesi { get; set; } = "VeriYok";
    }

    public sealed class CariOdemeRitmi
    {
        public int CariKartId { get; set; }
        public string Unvan { get; set; } = string.Empty;
        public decimal AcikAlacak { get; set; }
        public decimal VadesiGecmisAlacak { get; set; }
        public int EnUzunGecikmeGunu { get; set; }
        public decimal AcikAlacakOrani { get; set; }
        public decimal? OrtalamaOdemeSapmasiGunu { get; set; }
        public decimal? OrtancaOdemeSapmasiGunu { get; set; }
        public decimal? OrtalamaOdemeSuresiGunu { get; set; }
        public decimal? OrtancaOdemeSuresiGunu { get; set; }
        public decimal? ZamanindaOdemeOrani { get; set; }
        public decimal? OdemeAraligiOrtancasiGunu { get; set; }
        public decimal? SonDonemDegisimiGunu { get; set; }
        public int SonDonemOrnekAdedi { get; set; }
        public int OncekiDonemOrnekAdedi { get; set; }
        public int TamamlananOdemeAdedi { get; set; }
        public string RitimDurumu { get; set; } = "YetersizVeri";
        public string RiskSeviyesi { get; set; } = "Dusuk";
    }

    public sealed class NakitProjeksiyonHaftasi
    {
        public int Hafta { get; set; }
        public DateTime Baslangic { get; set; }
        public DateTime Bitis { get; set; }
        public decimal AcilisBakiyesi { get; set; }
        public decimal BeklenenTahsilat { get; set; }
        public decimal PlanlananGelir { get; set; }
        public decimal BeklenenOdeme { get; set; }
        public decimal PlanlananGider { get; set; }
        public decimal NetDegisim { get; set; }
        public decimal KapanisBakiyesi { get; set; }
    }

    public sealed class FinansalVeriUyarisi
    {
        public string Kod { get; set; } = string.Empty;
        public string Mesaj { get; set; } = string.Empty;
        public int KayitAdedi { get; set; }
    }

    public sealed class NakitPlanKalemiKaydetRequest
    {
        public string Ad { get; set; } = string.Empty;
        public string Tip { get; set; } = "Gider";
        public decimal Tutar { get; set; }
        public DateTime IlkTarih { get; set; } = DateTime.Now.Date;
        public string TekrarTipi { get; set; } = "TekSefer";
        public int TekrarAraligi { get; set; } = 1;
        public DateTime? BitisTarihi { get; set; }
        public string Kategori { get; set; } = string.Empty;
        public string? Aciklama { get; set; }
        public bool Aktif { get; set; } = true;
    }
}
