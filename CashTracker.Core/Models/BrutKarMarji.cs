using System;

namespace CashTracker.Core.Models;

public sealed class BrutKarMarjiOzeti
{
    public DateTime Baslangic { get; init; }
    public DateTime Bitis { get; init; }
    public decimal SatisGeliriTry { get; init; }
    public decimal SatisMaliyetiTry { get; init; }
    public decimal BrutKarTry { get; init; }
    public decimal? BrutKarOrani { get; init; }
    public int SatisSatiri { get; init; }
    public int EksikMaliyetliSatisSatiri { get; init; }
    public bool Guvenilir { get; init; }
    public string Durum { get; init; } = "VeriYok";
    public string Aciklama { get; init; } = string.Empty;
}
