namespace CashTracker.Core.Entities;

public sealed class UrunEslesmeTakmaAdi
{
    public int Id { get; set; }
    public int IsletmeId { get; set; }
    public int UrunHizmetId { get; set; }
    public int? CariKartId { get; set; }
    public string KaynakMetin { get; set; } = string.Empty;
    public string Anahtar { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
