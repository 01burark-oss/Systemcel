namespace CashTracker.Core.Entities;

public sealed class BildirimTeslimOutbox
{
    public long Id { get; set; }
    public int IsletmeId { get; set; }
    public string KullaniciRef { get; set; } = string.Empty;
    public int? BildirimId { get; set; }
    public string IdempotencyAnahtari { get; set; } = string.Empty;
    public string Kanal { get; set; } = "Uygulama";
    public string Durum { get; set; } = "Bekliyor";
    public string PayloadJson { get; set; } = "{}";
    public int DenemeSayisi { get; set; }
    public DateTime SonrakiDenemeAt { get; set; } = DateTime.UtcNow;
    public string ClaimToken { get; set; } = string.Empty;
    public DateTime? ClaimBitisAt { get; set; }
    public string SonHataKodu { get; set; } = string.Empty;
    public DateTime? TeslimEdildiAt { get; set; }
    public DateTime? DeadLetterAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
