using System;

namespace CashTracker.Core.Entities
{
    public sealed class MuhasebeciSohbetVeriIstegi
    {
        public int Id { get; set; }
        public int SohbetId { get; set; }
        public int IsteyenIsletmeId { get; set; }
        public int HedefIsletmeId { get; set; }
        public string VeriTipi { get; set; } = "GelirGiderOzeti";
        public string AralikKodu { get; set; } = "last30";
        public DateTime Baslangic { get; set; } = DateTime.Today;
        public DateTime Bitis { get; set; } = DateTime.Today;
        public string Durum { get; set; } = "Beklemede";
        public int? SonucEkId { get; set; }
        public string Mesaj { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
