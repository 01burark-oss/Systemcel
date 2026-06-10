using System;

namespace CashTracker.Core.Entities
{
    public sealed class MuhasebeciMusteriTalebi
    {
        public int Id { get; set; }
        public int MuhasebeciIsletmeId { get; set; }
        public int? MusteriIsletmeId { get; set; }
        public int TalepEdenIsletmeId { get; set; }
        public string Tur { get; set; } = "Pazaryeri";
        public string Durum { get; set; } = "Beklemede";
        public string YetkiSeviyesi { get; set; } = "OkumaRapor";
        public string DavetKodu { get; set; } = string.Empty;
        public string Mesaj { get; set; } = string.Empty;
        public DateTime? SonucAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
