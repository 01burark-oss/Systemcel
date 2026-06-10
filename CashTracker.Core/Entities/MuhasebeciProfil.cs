using System;

namespace CashTracker.Core.Entities
{
    public sealed class MuhasebeciProfil
    {
        public int Id { get; set; }
        public int MuhasebeciIsletmeId { get; set; }
        public bool Yayinda { get; set; }
        public string Unvan { get; set; } = string.Empty;
        public string Konum { get; set; } = string.Empty;
        public string Telefon { get; set; } = string.Empty;
        public int DeneyimYili { get; set; }
        public string ProfilResmiUrl { get; set; } = string.Empty;
        public string UcretBilgisi { get; set; } = string.Empty;
        public string Uzmanliklar { get; set; } = string.Empty;
        public string MusteriTipleri { get; set; } = string.Empty;
        public string KisaAciklama { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
