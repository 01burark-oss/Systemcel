using System;

namespace CashTracker.Core.Entities
{
    public sealed class StokDepo
    {
        public int Id { get; set; }
        public int IsletmeId { get; set; }
        public int? SubeId { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string Kod { get; set; } = string.Empty;
        public string? Konum { get; set; }
        public bool Varsayilan { get; set; }
        public bool Aktif { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
