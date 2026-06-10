using System;

namespace CashTracker.Core.Entities
{
    public sealed class IsletmeDeneme
    {
        public int Id { get; set; }
        public int IsletmeId { get; set; }
        public string PlanKodu { get; set; } = "isletme_baslangic";
        public string Durum { get; set; } = "Aktif";
        public DateTime BaslangicAt { get; set; } = DateTime.Now;
        public DateTime BitisAt { get; set; } = DateTime.Now.AddMonths(1);
        public bool OdemeYontemiEklendi { get; set; }
        public string OdemeSaglayici { get; set; } = string.Empty;
        public string SaglayiciMusteriId { get; set; } = string.Empty;
        public string SaglayiciOdemeYontemiId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
