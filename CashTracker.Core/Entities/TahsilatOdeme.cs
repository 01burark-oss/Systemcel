using System;

namespace CashTracker.Core.Entities
{
    public sealed class TahsilatOdeme
    {
        public int Id { get; set; }
        public int IsletmeId { get; set; }
        public int? SubeId { get; set; }
        public int FaturaId { get; set; }
        public int CariKartId { get; set; }
        public DateTime Tarih { get; set; } = DateTime.Now;
        public string Tip { get; set; } = "Tahsilat"; // Tahsilat | Odeme
        public decimal Tutar { get; set; }
        public string ParaBirimi { get; set; } = "TRY";
        public decimal KurSnapshot { get; set; } = 1m;
        public decimal TryKarsiligi { get; set; }
        public string OdemeYontemi { get; set; } = "Nakit";
        public int? KasaId { get; set; }
        public int? CariHareketId { get; set; }
        public string? Aciklama { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
