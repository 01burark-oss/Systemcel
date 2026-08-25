using System;

namespace CashTracker.Core.Entities
{
    public sealed class AbonelikOnayi
    {
        public int Id { get; set; }
        public int IsletmeId { get; set; }
        public string KullaniciRef { get; set; } = string.Empty;
        public string CheckoutAnahtari { get; set; } = string.Empty;
        public string HesapTipi { get; set; } = "Isletme";
        public string PlanKodu { get; set; } = string.Empty;
        public string FaturalamaDonemi { get; set; } = "Aylik";
        public int EkMusteriKredisi { get; set; }
        public string KampanyaKodu { get; set; } = string.Empty;
        public decimal ListeNetTutar { get; set; }
        public decimal YenilemeNetTutar { get; set; }
        public string MetinSurumu { get; set; } = string.Empty;
        public string MetinHash { get; set; } = string.Empty;
        public string IstemciIpHash { get; set; } = string.Empty;
        public string UserAgentHash { get; set; } = string.Empty;
        public decimal NetTutar { get; set; }
        public decimal TamDonemNetTutar { get; set; }
        public decimal KistKrediNetTutar { get; set; }
        public string DegisiklikTipi { get; set; } = string.Empty;
        public decimal KdvOrani { get; set; }
        public decimal KdvTutar { get; set; }
        public decimal ToplamTutar { get; set; }
        public string ParaBirimi { get; set; } = "TRY";
        public DateTime OnayAt { get; set; } = DateTime.UtcNow;
    }
}
