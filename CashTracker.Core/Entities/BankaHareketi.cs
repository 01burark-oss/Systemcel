namespace CashTracker.Core.Entities;

public sealed class BankaHareketi
{
    public int Id { get; set; }
    public int IsletmeId { get; set; }
    public DateTime Tarih { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public decimal Tutar { get; set; }
    public string ParaBirimi { get; set; } = "TRY";
    public string Durum { get; set; } = "Acik";
    public string KaynakHash { get; set; } = string.Empty;
    public string EslesenKaynakTuru { get; set; } = string.Empty;
    public int? EslesenKaynakId { get; set; }
    public DateTime? EslestiAt { get; set; }
    public DateTime? YokSayildiAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
