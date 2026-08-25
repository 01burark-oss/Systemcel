namespace CashTracker.Core.Entities;

public sealed class GelistiriciApiAnahtari
{
    public int Id { get; set; }
    public int IsletmeId { get; set; }
    public string OlusturanKullaniciRef { get; set; } = string.Empty;
    public string Ad { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public byte[] AnahtarHash { get; set; } = Array.Empty<byte>();
    public string ScopeListesi { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastUsedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string RevokedByUserRef { get; set; } = string.Empty;
}
