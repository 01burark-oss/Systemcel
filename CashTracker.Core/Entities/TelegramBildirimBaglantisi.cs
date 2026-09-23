namespace CashTracker.Core.Entities;

public sealed class TelegramBildirimBaglantisi
{
    public int Id { get; set; }
    public int IsletmeId { get; set; }
    public string KullaniciRef { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
    public string TelegramUserId { get; set; } = string.Empty;
    public string EslestirmeKodu { get; set; } = string.Empty;
    public DateTime? KodGecerliAt { get; set; }
    public DateTime? BaglandiAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
