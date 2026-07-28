using System;
using System.Collections.Generic;

namespace CashTracker.Core.Models
{
    public sealed class HizliSatisCreateRequest
    {
        public string IslemAnahtari { get; set; } = string.Empty;
        public string OdemeYontemi { get; set; } = "Nakit";
        public DateTime Tarih { get; set; } = DateTime.Now;
        public List<HizliSatisSatirRequest> Satirlar { get; set; } = new();
    }

    public sealed class HizliSatisSatirRequest
    {
        public int UrunHizmetId { get; set; }
        public decimal Miktar { get; set; }
    }

    public sealed class HizliSatisResult
    {
        public int FaturaId { get; set; }
        public string FaturaNo { get; set; } = string.Empty;
        public decimal Toplam { get; set; }
        public bool Tekrarlandi { get; set; }
    }
}
