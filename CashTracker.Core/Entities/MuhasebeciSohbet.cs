using System;

namespace CashTracker.Core.Entities
{
    public sealed class MuhasebeciSohbet
    {
        public int Id { get; set; }
        public int MuhasebeciIsletmeId { get; set; }
        public int MusteriIsletmeId { get; set; }
        public int? TalepId { get; set; }
        public int? BaglantiId { get; set; }
        public string Konu { get; set; } = string.Empty;
        public string Durum { get; set; } = "Aktif";
        public DateTime? SonMesajAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
