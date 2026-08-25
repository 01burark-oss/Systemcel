namespace CashTracker.Core.Entities;

public sealed class DestekTalebi
{
    public int Id { get; set; }
    public int IsletmeId { get; set; }
    public string OlusturanKullaniciReferansi { get; set; } = string.Empty;
    public string OlusturmaAnahtari { get; set; } = string.Empty;
    public string Konu { get; set; } = string.Empty;
    public string Kategori { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;
    public string Oncelik { get; set; } = "Standart";
    public string Durum { get; set; } = "Acik";
    public string YoneticiYaniti { get; set; } = string.Empty;
    public DateTime? CozulduAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
