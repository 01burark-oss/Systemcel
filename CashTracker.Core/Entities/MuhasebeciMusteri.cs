using System;

namespace CashTracker.Core.Entities
{
    public sealed class MuhasebeciMusteri
    {
        public int Id { get; set; }
        public int MuhasebeciIsletmeId { get; set; }
        public int MusteriIsletmeId { get; set; }
        public string Durum { get; set; } = "Aktif";
        public string YetkiSeviyesi { get; set; } = "OkumaRapor";
        public string Kaynak { get; set; } = "Davet";
        public int? TalepId { get; set; }
        public string? DavetKodu { get; set; }
        public DateTime BaslangicAt { get; set; } = DateTime.Now;
        public DateTime? BitisAt { get; set; }
        public DateTime? KabulAt { get; set; }
        public string Notlar { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
