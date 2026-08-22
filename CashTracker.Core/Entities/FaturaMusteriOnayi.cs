using System;

namespace CashTracker.Core.Entities
{
    public sealed class FaturaMusteriOnayi
    {
        public int Id { get; set; }
        public int IsletmeId { get; set; }
        public int FaturaId { get; set; }
        public int CariKartId { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public string Durum { get; set; } = "Bekliyor";
        public string IsletmeAdi { get; set; } = string.Empty;
        public string CariUnvan { get; set; } = string.Empty;
        public string CariVergiNoMaskeli { get; set; } = string.Empty;
        public string CariAdres { get; set; } = string.Empty;
        public string AliciTelefonMaskeli { get; set; } = string.Empty;
        public string FaturaNo { get; set; } = string.Empty;
        public DateTime FaturaTarihi { get; set; }
        public decimal FaturaToplami { get; set; }
        public string ParaBirimi { get; set; } = "TRY";
        public string Saglayici { get; set; } = string.Empty;
        public string SaglayiciIslemId { get; set; } = string.Empty;
        public string Hata { get; set; } = string.Empty;
        public DateTime? GonderildiAt { get; set; }
        public DateTime SonGecerlilikAt { get; set; }
        public DateTime? YanitAt { get; set; }
        public string YanitNotu { get; set; } = string.Empty;
        public string IstemciIpHash { get; set; } = string.Empty;
        public string UserAgentHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
