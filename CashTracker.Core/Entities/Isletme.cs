using System;

namespace CashTracker.Core.Entities
{
    public sealed class Isletme
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string IsletmeTuru { get; set; } = "Genel";
        public string Konum { get; set; } = string.Empty;
        public bool KolayKurulumTamamlandi { get; set; }
        public bool MuhasebeciVarMi { get; set; }
        public string TenantTipi { get; set; } = "Isletme";
        public int? SahipKullaniciId { get; set; }
        public string? ClerkOrganizationId { get; set; }
        public bool IsAktif { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
