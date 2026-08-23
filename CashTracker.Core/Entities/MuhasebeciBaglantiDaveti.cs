using System;

namespace CashTracker.Core.Entities
{
    public sealed class MuhasebeciBaglantiDaveti
    {
        public int Id { get; set; }
        public int MusteriIsletmeId { get; set; }
        public int? MuhasebeciIsletmeId { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public string Durum { get; set; } = "Beklemede";
        public string YetkiSeviyesi { get; set; } = "OkumaRapor";
        public string Mesaj { get; set; } = string.Empty;
        public DateTime SonGecerlilikAt { get; set; }
        public DateTime? KabulAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
