namespace CashTracker.Core.Entities;

public sealed class BildirimTercihi
{
    public int Id { get; set; }
    public int IsletmeId { get; set; }
    public string KullaniciRef { get; set; } = string.Empty;
    public bool UygulamaAktif { get; set; } = true;
    public bool EpostaAktif { get; set; }
    public bool TelegramAktif { get; set; }
    public bool SessizSaatAktif { get; set; }
    public int SessizBaslangicDakika { get; set; } = 1320;
    public int SessizBitisDakika { get; set; } = 480;
    public string SaatDilimi { get; set; } = "Europe/Istanbul";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
