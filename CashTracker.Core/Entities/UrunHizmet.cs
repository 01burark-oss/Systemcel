using System;

namespace CashTracker.Core.Entities
{
    public sealed class UrunHizmet
    {
        public int Id { get; set; }
        public int IsletmeId { get; set; }
        public int? SubeId { get; set; }
        public string Tip { get; set; } = "Urun"; // Urun | Hizmet
        public string Ad { get; set; } = string.Empty;
        public string Barkod { get; set; } = string.Empty;
        public string Birim { get; set; } = "Adet";
        public decimal KdvOrani { get; set; } = 20m;
        public decimal AlisFiyati { get; set; }
        public decimal SatisFiyati { get; set; }
        public string ParaBirimi { get; set; } = "TRY";
        public decimal KurSnapshot { get; set; } = 1m;
        public decimal KritikStok { get; set; }
        public bool Aktif { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
