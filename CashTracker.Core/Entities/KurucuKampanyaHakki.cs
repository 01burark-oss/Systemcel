using System;

namespace CashTracker.Core.Entities
{
    public sealed class KurucuKampanyaHakki
    {
        public int Id { get; set; }
        public int IsletmeId { get; set; }
        public string KampanyaKodu { get; set; } = string.Empty;
        public int SiraNo { get; set; }
        public string CheckoutAnahtari { get; set; } = string.Empty;
        public string Durum { get; set; } = "Rezerve";
        public DateTime RezerveAt { get; set; } = DateTime.UtcNow;
        public DateTime RezervasyonBitisAt { get; set; } = DateTime.UtcNow;
        public DateTime? KazanildiAt { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
