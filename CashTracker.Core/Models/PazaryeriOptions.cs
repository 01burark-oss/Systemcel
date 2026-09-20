namespace CashTracker.Core.Models;

public sealed class PazaryeriOptions
{
    public bool Aktif { get; init; } = true;
    public IReadOnlySet<int> PilotIsletmeIdleri { get; init; } = new HashSet<int>();
    public decimal VarsayilanKomisyonOrani { get; init; } = 8m;
    public decimal KomisyonKdvOrani { get; init; } = 20m;
    public decimal TevkifatOrani { get; init; } = 1m;
    public decimal OdemeHizmetiOrani { get; init; }
    public int VarsayilanOdemeVadesiGun { get; init; } = 7;
    public int StokRezervasyonSuresiDakika { get; init; } = 30;

    public bool IsBusinessAllowed(int businessId) =>
        Aktif && (PilotIsletmeIdleri.Count == 0 || PilotIsletmeIdleri.Contains(businessId));
}
