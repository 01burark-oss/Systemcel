namespace CashTracker.Core.Entities;

public sealed class BildirimKaydi
{
    public int Id { get; set; }
    public int IsletmeId { get; set; }
    public string KullaniciRef { get; set; } = string.Empty;
    public string KaynakAnahtari { get; set; } = string.Empty;
    public string Tur { get; set; } = string.Empty;
    public string Onem { get; set; } = "orta";
    public string Baslik { get; set; } = string.Empty;
    public string Mesaj { get; set; } = string.Empty;
    public string Aksiyon { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime? OkunduAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
