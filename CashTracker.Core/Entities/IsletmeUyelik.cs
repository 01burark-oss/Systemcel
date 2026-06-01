using System;

namespace CashTracker.Core.Entities
{
    public sealed class IsletmeUyelik
    {
        public int Id { get; set; }
        public int IsletmeId { get; set; }
        public int? KullaniciId { get; set; }
        public string Rol { get; set; } = "isletme_sahibi";
        public string Durum { get; set; } = "Aktif";
        public string DavetEposta { get; set; } = string.Empty;
        public string? DavetKodu { get; set; }
        public int? DavetEdenKullaniciId { get; set; }
        public DateTime? DavetAt { get; set; }
        public DateTime? KabulAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
