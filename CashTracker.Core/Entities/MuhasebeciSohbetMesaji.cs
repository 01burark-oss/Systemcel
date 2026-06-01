using System;

namespace CashTracker.Core.Entities
{
    public sealed class MuhasebeciSohbetMesaji
    {
        public int Id { get; set; }
        public int MuhasebeciIsletmeId { get; set; }
        public int MusteriIsletmeId { get; set; }
        public int GonderenIsletmeId { get; set; }
        public int? TalepId { get; set; }
        public int? BaglantiId { get; set; }
        public string Mesaj { get; set; } = string.Empty;
        public DateTime? OkunduAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
