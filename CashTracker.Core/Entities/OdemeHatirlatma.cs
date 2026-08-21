namespace CashTracker.Core.Entities;

public sealed class OdemeHatirlatma
{
    public int Id { get; set; }
    public int IsletmeId { get; set; }
    public int FaturaId { get; set; }
    public int CariKartId { get; set; }
    public string AliciEposta { get; set; } = string.Empty;
    public string Konu { get; set; } = string.Empty;
    public string Durum { get; set; } = string.Empty;
    public string Hata { get; set; } = string.Empty;
    public DateTime? GonderildiAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
