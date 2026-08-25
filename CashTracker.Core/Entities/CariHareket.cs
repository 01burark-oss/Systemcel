using System;

namespace CashTracker.Core.Entities
{
    public sealed class CariHareket
    {
        public int Id { get; set; }
        public int IsletmeId { get; set; }
        public int? SubeId { get; set; }
        public int CariKartId { get; set; }
        public DateTime Tarih { get; set; } = DateTime.Now;
        public string HareketTipi { get; set; } = "Borc"; // Borc | Alacak | Tahsilat | Odeme
        public decimal Tutar { get; set; }
        public string ParaBirimi { get; set; } = "TRY";
        public decimal KurSnapshot { get; set; } = 1m;
        public decimal TryKarsiligi { get; set; }
        public string Kaynak { get; set; } = "Manuel";
        public string? Aciklama { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
