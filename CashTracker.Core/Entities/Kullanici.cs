using System;

namespace CashTracker.Core.Entities
{
    public sealed class Kullanici
    {
        public int Id { get; set; }
        public string AuthProvider { get; set; } = "clerk";
        public string AuthProviderUserId { get; set; } = string.Empty;
        public string Eposta { get; set; } = string.Empty;
        public string AdSoyad { get; set; } = string.Empty;
        public string HesapTipi { get; set; } = "Isletme";
        public string Durum { get; set; } = "Aktif";
        public DateTime? SonGirisAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
