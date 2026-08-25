using System;

namespace CashTracker.Core.Entities
{
    public sealed class StokHareket
    {
        public int Id { get; set; }
        public int IsletmeId { get; set; }
        public int? SubeId { get; set; }
        public int UrunHizmetId { get; set; }
        public int? DepoId { get; set; }
        public int? StokDefterIslemiId { get; set; }
        public DateTime Tarih { get; set; } = DateTime.Now;
        public decimal Miktar { get; set; }
        public decimal RezerveMiktar { get; set; }
        public string HareketTipi { get; set; } = "Giris"; // Giris | Cikis
        public string Kaynak { get; set; } = "Manuel";
        public string? Aciklama { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
