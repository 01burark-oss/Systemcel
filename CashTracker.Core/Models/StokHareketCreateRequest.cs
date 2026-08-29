using System;

namespace CashTracker.Core.Models
{
    public sealed class StokHareketCreateRequest
    {
        public int UrunHizmetId { get; set; }
        public DateTime? Tarih { get; set; }
        public decimal Miktar { get; set; }
        public decimal? BirimMaliyet { get; set; }
        public string Kaynak { get; set; } = "Manuel";
        public string? Aciklama { get; set; }
    }
}
