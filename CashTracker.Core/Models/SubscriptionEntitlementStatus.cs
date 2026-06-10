using System;

namespace CashTracker.Core.Models
{
    public sealed class SubscriptionEntitlementStatus
    {
        public int IsletmeId { get; init; }
        public string HesapTipi { get; init; } = HesapTipleri.Isletme;
        public string PlanKodu { get; init; } = PlanKodlari.IsletmeUcretsiz;
        public string PlanAdi { get; init; } = "Ücretsiz";
        public string Kaynak { get; init; } = EntitlementKaynaklari.Ucretsiz;
        public decimal AylikTutar { get; init; }
        public string ParaBirimi { get; init; } = "TRY";
        public bool OcrAktif { get; init; }
        public bool GibAktif { get; init; }
        public bool TelegramAktif { get; init; }
        public bool AiAktif { get; init; }
        public int? AiMesajLimiti { get; init; }
        public int? KullaniciLimiti { get; init; }
        public int? MusteriLimiti { get; init; }
        public bool MuhasebeciPaneliAktif { get; init; }
        public bool OneCikmaAktif { get; init; }
        public bool DonemOtomasyonuAktif { get; init; }
        public bool MusteriSaglikSkoruAktif { get; init; }
        public int? SponsorMuhasebeciIsletmeId { get; init; }
        public DateTime GecerliBaslangicAt { get; init; }
        public DateTime? GecerliBitisAt { get; init; }
        public int? AktifMusteriSayisi { get; init; }
        public decimal? MuhasebeciStandartAylikTutar { get; init; }
        public bool MuhasebeciProOnerilir { get; init; }
        public bool AiSinirsiz => AiAktif && AiMesajLimiti is null;
        public bool MusteriSinirsiz => MusteriLimiti is null;
    }
}
