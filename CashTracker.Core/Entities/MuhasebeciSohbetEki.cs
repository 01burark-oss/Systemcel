using System;

namespace CashTracker.Core.Entities
{
    public sealed class MuhasebeciSohbetEki
    {
        public int Id { get; set; }
        public int SohbetId { get; set; }
        public int? MesajId { get; set; }
        public int YukleyenIsletmeId { get; set; }
        public string EkTipi { get; set; } = "Dosya";
        public string DosyaAdi { get; set; } = string.Empty;
        public string IcerikTipi { get; set; } = string.Empty;
        public string DosyaYolu { get; set; } = string.Empty;
        public long Boyut { get; set; }
        public string VeriTipi { get; set; } = string.Empty;
        public string Baslik { get; set; } = string.Empty;
        public string OzetJson { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
