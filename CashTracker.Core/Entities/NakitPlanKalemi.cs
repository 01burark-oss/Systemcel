using System;

namespace CashTracker.Core.Entities
{
    public sealed class NakitPlanKalemi
    {
        public int Id { get; set; }
        public int IsletmeId { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string Tip { get; set; } = "Gider"; // Gelir | Gider
        public decimal Tutar { get; set; }
        public DateTime IlkTarih { get; set; } = DateTime.Now.Date;
        public string TekrarTipi { get; set; } = "TekSefer"; // TekSefer | Haftalik | Aylik
        public int TekrarAraligi { get; set; } = 1;
        public DateTime? BitisTarihi { get; set; }
        public string Kategori { get; set; } = string.Empty;
        public string? Aciklama { get; set; }
        public bool Aktif { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
