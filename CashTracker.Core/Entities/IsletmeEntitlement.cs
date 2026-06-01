using System;

namespace CashTracker.Core.Entities
{
    public sealed class IsletmeEntitlement
    {
        public int Id { get; set; }
        public int IsletmeId { get; set; }
        public string PlanKodu { get; set; } = "isletme_ucretsiz";
        public string Kaynak { get; set; } = "Ucretsiz";
        public bool OcrAktif { get; set; }
        public bool GibAktif { get; set; }
        public bool TelegramAktif { get; set; }
        public bool AiAktif { get; set; }
        public int? AiMesajLimiti { get; set; }
        public int? KullaniciLimiti { get; set; } = 1;
        public int? MusteriLimiti { get; set; }
        public bool MuhasebeciPaneliAktif { get; set; }
        public bool OneCikmaAktif { get; set; }
        public bool DonemOtomasyonuAktif { get; set; }
        public bool MusteriSaglikSkoruAktif { get; set; }
        public int? SponsorMuhasebeciIsletmeId { get; set; }
        public DateTime GecerliBaslangicAt { get; set; } = DateTime.Now;
        public DateTime? GecerliBitisAt { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
