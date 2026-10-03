namespace CashTracker.Core.Entities;

// A correction supplements the immutable receipt; it is not a provider refund or an official return invoice.
public sealed class TedarikciMalKabulDuzeltme
{
    public int Id { get; set; }
    public int TedarikciMalKabulId { get; set; }
    public int TedarikciSiparisId { get; set; }
    public int AliciIsletmeId { get; set; }
    public string IdempotencyAnahtari { get; set; } = string.Empty;
    public decimal Miktar { get; set; }
    public string Not { get; set; } = string.Empty;
    public string Durum { get; set; } = "OnayBekliyor";
    public string IslemYapanKullaniciRef { get; set; } = string.Empty;
    public string OnaylayanKullaniciRef { get; set; } = string.Empty;
    public string OnayNotu { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? OnayAt { get; set; }
    public decimal NetTutar { get; set; }
    public decimal KdvTutar { get; set; }
    public decimal BrutTutar { get; set; }
    public decimal HakEdisAzaltimi { get; set; }
    public decimal TedarikcidenGeriAlinacakTutar { get; set; }
    public string ParaDurumu { get; set; } = "OnayBekliyor";
    public string BelgeDurumu { get; set; } = "OnayBekliyor";
    public string StokDurumu { get; set; } = "OnayBekliyor";
    public int? AliciFaturaId { get; set; }
    public int? SaticiFaturaId { get; set; }
}
