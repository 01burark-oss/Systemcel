namespace CashTracker.Core.Entities;

public sealed class OdemeIadeTalimati
{
    public long Id { get; set; }
    public int OdemeIslemiId { get; set; }
    public string IdempotencyAnahtari { get; set; } = string.Empty;
    public string ReferansNo { get; set; } = string.Empty;
    public decimal Tutar { get; set; }
    public string Durum { get; set; } = "Hazir";
    public string SonHataKodu { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
