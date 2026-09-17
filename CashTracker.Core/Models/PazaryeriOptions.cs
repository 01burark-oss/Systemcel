namespace CashTracker.Core.Models;

public sealed class PazaryeriOptions
{
    public decimal VarsayilanKomisyonOrani { get; init; } = 8m;
    public decimal KomisyonKdvOrani { get; init; } = 20m;
    public decimal TevkifatOrani { get; init; } = 1m;
    public decimal OdemeHizmetiOrani { get; init; }
    public int VarsayilanOdemeVadesiGun { get; init; } = 7;
}
