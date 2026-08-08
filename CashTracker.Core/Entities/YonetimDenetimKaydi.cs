namespace CashTracker.Core.Entities;

public sealed class YonetimDenetimKaydi
{
    public int Id { get; set; }
    public int IsletmeId { get; set; }
    public string AktorProviderKullaniciId { get; set; } = string.Empty;
    public string Islem { get; set; } = string.Empty;
    public string KaynakTuru { get; set; } = string.Empty;
    public string OncekiDeger { get; set; } = string.Empty;
    public string YeniDeger { get; set; } = string.Empty;
    public string Gerekce { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
