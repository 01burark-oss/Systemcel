namespace CashTracker.Core.Models;

public sealed class IsletmeUyelikDavetRequest
{
    public string Eposta { get; init; } = string.Empty;
    public string Rol { get; init; } = "personel";
}

public sealed class IsletmeUyelikDavetDto
{
    public int Id { get; init; }
    public int IsletmeId { get; init; }
    public string Eposta { get; init; } = string.Empty;
    public string Rol { get; init; } = string.Empty;
    public string Durum { get; init; } = string.Empty;
    public string DavetKodu { get; init; } = string.Empty;
    public DateTime DavetAt { get; init; }
    public bool TekrarKullanildi { get; init; }
}

public sealed class IsletmeUyelikListeDto
{
    public bool SahibiMi { get; init; }
    public int IsletmeId { get; init; }
    public string IsletmeAdi { get; init; } = string.Empty;
    public List<IsletmeUyelikDto> Uyelikler { get; init; } = new();
}

public sealed class IsletmeUyelikDto
{
    public int Id { get; init; }
    public int? KullaniciId { get; init; }
    public string Eposta { get; init; } = string.Empty;
    public string AdSoyad { get; init; } = string.Empty;
    public string Rol { get; init; } = string.Empty;
    public string Durum { get; init; } = string.Empty;
    public string DavetKodu { get; init; } = string.Empty;
    public DateTime? DavetAt { get; init; }
    public DateTime? KabulAt { get; init; }
}

public sealed class IsletmeUyelikRolGuncelleRequest
{
    public string Rol { get; init; } = string.Empty;
}

public sealed class IsletmeUyelikDavetKabulRequest
{
    public string DavetKodu { get; init; } = string.Empty;
}

public sealed class EntitlementOverrideRequest
{
    public string PlanKodu { get; init; } = string.Empty;
    public bool AiAktif { get; init; }
    public int? AiMesajLimiti { get; init; }
    public int? KullaniciLimiti { get; init; }
    public int? MusteriLimiti { get; init; }
    public string Gerekce { get; init; } = string.Empty;
}

public sealed class EntitlementOverrideResult
{
    public int IsletmeId { get; init; }
    public int DenetimKaydiId { get; init; }
    public string PlanKodu { get; init; } = string.Empty;
    public int? KullaniciLimiti { get; init; }
    public int? MusteriLimiti { get; init; }
}
