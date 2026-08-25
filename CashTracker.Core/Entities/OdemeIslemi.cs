using System;

namespace CashTracker.Core.Entities
{
    public sealed class OdemeIslemi
    {
        public int Id { get; set; }
        public int IsletmeId { get; set; }
        public string CheckoutAnahtari { get; set; } = string.Empty;
        public string HesapTipi { get; set; } = "Isletme";
        public string PlanKodu { get; set; } = string.Empty;
        public string FaturalamaDonemi { get; set; } = "Aylik";
        public int EkMusteriKredisi { get; set; }
        public string KampanyaKodu { get; set; } = string.Empty;
        public decimal ListeNetTutar { get; set; }
        public decimal YenilemeNetTutar { get; set; }
        public int IndirimliDonemSayisi { get; set; }
        public string IslemTipi { get; set; } = "DenemeKartYetkilendirme";
        public string Durum { get; set; } = "Hazirlaniyor";
        public string OdemeSaglayici { get; set; } = string.Empty;
        public string SaglayiciOturumId { get; set; } = string.Empty;
        public string SaglayiciIslemId { get; set; } = string.Empty;
        public string CheckoutUrl { get; set; } = string.Empty;
        public DateTime? CheckoutExpiresAt { get; set; }
        public decimal NetTutar { get; set; }
        public decimal KdvOrani { get; set; }
        public decimal KdvTutar { get; set; }
        public decimal ToplamTutar { get; set; }
        public decimal TamDonemNetTutar { get; set; }
        public decimal KistKrediNetTutar { get; set; }
        public string DegisiklikTipi { get; set; } = string.Empty;
        public DateTime? HedefDonemBitisAt { get; set; }
        public string ParaBirimi { get; set; } = "TRY";
        public string HataKodu { get; set; } = string.Empty;
        public string HataMesaji { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? TamamlandiAt { get; set; }
        public DateTime? SonOlayAt { get; set; }
    }
}
