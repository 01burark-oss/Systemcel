using System;

namespace CashTracker.Core.Entities
{
    public sealed class Kasa
    {
        public int Id { get; set; }
        public int IsletmeId { get; set; }
        public int? SubeId { get; set; }
        public DateTime Tarih { get; set; } = DateTime.Now;
        public string Tip { get; set; } = "Gelir"; // Gelir | Gider
        public decimal Tutar { get; set; }
        public decimal OrijinalTutar { get; set; }
        public string ParaBirimi { get; set; } = "TRY";
        public decimal KurSnapshot { get; set; } = 1m;
        public decimal TryKarsiligi { get; set; }
        public string OdemeYontemi { get; set; } = "Nakit"; // Nakit | KrediKarti | OnlineOdeme | Havale
        public string? Kalem { get; set; }
        public string? GiderTuru { get; set; }
        public string? Aciklama { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
