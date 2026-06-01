using System;
using System.Collections.Generic;

namespace CashTracker.Core.Models
{
    public sealed class SystemcelYonetimOptions
    {
        public string AdminClerkUserIds { get; init; } = string.Empty;
        public string AdminEmails { get; init; } = string.Empty;
    }

    public sealed class MuhasebeciBasvuruDto
    {
        public int KullaniciId { get; init; }
        public string ClerkUserId { get; init; } = string.Empty;
        public string Eposta { get; init; } = string.Empty;
        public string AdSoyad { get; init; } = string.Empty;
        public string Durum { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public DateTime? SonGirisAt { get; init; }
        public int? IsletmeId { get; init; }
        public string IsletmeAdi { get; init; } = string.Empty;
        public string IsletmeTuru { get; init; } = string.Empty;
        public string Konum { get; init; } = string.Empty;
        public string Telefon { get; init; } = string.Empty;
        public int DeneyimYili { get; init; }
        public string ProfilResmiUrl { get; init; } = string.Empty;
        public string UcretBilgisi { get; init; } = string.Empty;
        public string Uzmanliklar { get; init; } = string.Empty;
        public string MusteriTipleri { get; init; } = string.Empty;
        public string KisaAciklama { get; init; } = string.Empty;
        public bool ProfilTamam { get; init; }
    }

    public sealed class MuhasebeciBasvuruListeDto
    {
        public bool YoneticiMi { get; init; }
        public string DurumFiltresi { get; init; } = string.Empty;
        public int BekleyenSayisi { get; init; }
        public int OnayliSayisi { get; init; }
        public int ReddedilenSayisi { get; init; }
        public List<MuhasebeciBasvuruDto> Basvurular { get; init; } = new();
    }

    public sealed class MuhasebeciBasvuruRedRequest
    {
        public string Sebep { get; init; } = string.Empty;
    }
}
