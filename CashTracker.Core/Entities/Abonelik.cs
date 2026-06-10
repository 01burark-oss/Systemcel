using System;

namespace CashTracker.Core.Entities
{
    public sealed class Abonelik
    {
        public int Id { get; set; }
        public int IsletmeId { get; set; }
        public string HesapTipi { get; set; } = "Isletme";
        public string PlanKodu { get; set; } = "isletme_ucretsiz";
        public string Durum { get; set; } = "Aktif";
        public decimal AylikTutar { get; set; }
        public string ParaBirimi { get; set; } = "TRY";
        public DateTime DonemBaslangicAt { get; set; } = DateTime.Now;
        public DateTime? DonemBitisAt { get; set; }
        public bool DonemSonundaIptal { get; set; }
        public DateTime? IptalAt { get; set; }
        public string OdemeSaglayici { get; set; } = string.Empty;
        public string SaglayiciMusteriId { get; set; } = string.Empty;
        public string SaglayiciAbonelikId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
