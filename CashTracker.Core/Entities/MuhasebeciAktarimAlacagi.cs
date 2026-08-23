using System;

namespace CashTracker.Core.Entities
{
    public sealed class MuhasebeciAktarimAlacagi
    {
        public int Id { get; set; }
        public int MuhasebeciHizmetOdemesiId { get; set; }
        public int MuhasebeciIsletmeId { get; set; }
        public int MusteriIsletmeId { get; set; }
        public int TalepId { get; set; }
        public decimal TahsilEdilenTutar { get; set; }
        public decimal PlatformKomisyonTutari { get; set; }
        public decimal AktarilacakTutar { get; set; }
        public string ParaBirimi { get; set; } = "TRY";
        public string AktarimDonemi { get; set; } = string.Empty;
        public string Durum { get; set; } = "Bekliyor";
        public string AktarimReferansi { get; set; } = string.Empty;
        public DateTime TahakkukAt { get; set; }
        public DateTime? AktarildiAt { get; set; }
        public DateTime? TersKayitAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
