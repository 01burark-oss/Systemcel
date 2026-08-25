using System;

namespace CashTracker.Core.Entities
{
    public sealed class Fatura
    {
        public int Id { get; set; }
        public int IsletmeId { get; set; }
        public int? SubeId { get; set; }
        public int CariKartId { get; set; }
        public DateTime Tarih { get; set; } = DateTime.Now;
        public DateTime? VadeTarihi { get; set; }
        public string FaturaTipi { get; set; } = "Satis"; // Satis | Alis
        public string Durum { get; set; } = "YerelTaslak";
        public string YerelFaturaNo { get; set; } = string.Empty;
        public string PortalBelgeNo { get; set; } = string.Empty;
        public string PortalUuid { get; set; } = string.Empty;
        public string? HizliSatisAnahtari { get; set; }
        public decimal AraToplam { get; set; }
        public decimal IskontoToplam { get; set; }
        public decimal KdvToplam { get; set; }
        public decimal GenelToplam { get; set; }
        public string ParaBirimi { get; set; } = "TRY";
        public decimal KurSnapshot { get; set; } = 1m;
        public decimal GenelToplamTry { get; set; }
        public decimal OdenenTutar { get; set; }
        public string OdemeYontemi { get; set; } = "Nakit";
        public string? Aciklama { get; set; }
        public DateTime? KesildiAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
