using System;

namespace CashTracker.Core.Entities
{
    public sealed class MuhasebeciSohbetKatilimciDurumu
    {
        public int Id { get; set; }
        public int SohbetId { get; set; }
        public int IsletmeId { get; set; }
        public bool Arsivlendi { get; set; }
        public DateTime? ArsivlendiAt { get; set; }
        public DateTime? SonOkumaAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
