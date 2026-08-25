using System;

namespace CashTracker.Core.Entities
{
    public sealed class MuhasebeciHizmetOdemesi
    {
        public int Id { get; set; }
        public int TalepId { get; set; }
        public int MuhasebeciIsletmeId { get; set; }
        public int MusteriIsletmeId { get; set; }
        public int? OdemeIslemiId { get; set; }
        public string HizmetDonemi { get; set; } = string.Empty;
        public DateTime VadeAt { get; set; }
        public decimal AylikHizmetBedeli { get; set; }
        public decimal PlatformKomisyonOrani { get; set; }
        public string ParaBirimi { get; set; } = "TRY";
        public string Durum { get; set; } = "OdemeBekliyor";
        public decimal TahsilEdilenTutar { get; set; }
        public decimal PlatformKomisyonTutari { get; set; }
        public decimal AktarilacakTutar { get; set; }
        public DateTime? TahsilEdildiAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
