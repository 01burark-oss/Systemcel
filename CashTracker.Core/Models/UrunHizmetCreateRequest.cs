namespace CashTracker.Core.Models
{
    public sealed class UrunHizmetCreateRequest
    {
        public string Tip { get; set; } = "Urun";
        public string Ad { get; set; } = string.Empty;
        public string Barkod { get; set; } = string.Empty;
        public string Birim { get; set; } = "Adet";
        public decimal KdvOrani { get; set; } = 20m;
        public decimal AlisFiyati { get; set; }
        public decimal SatisFiyati { get; set; }
        public string ParaBirimi { get; set; } = "TRY";
        public decimal KritikStok { get; set; }
    }
}
