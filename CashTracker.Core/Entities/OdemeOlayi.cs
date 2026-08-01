using System;

namespace CashTracker.Core.Entities
{
    public sealed class OdemeOlayi
    {
        public int Id { get; set; }
        public string OdemeSaglayici { get; set; } = string.Empty;
        public string OlayId { get; set; } = string.Empty;
        public string OlayTipi { get; set; } = string.Empty;
        public string CheckoutAnahtari { get; set; } = string.Empty;
        public string SaglayiciIslemId { get; set; } = string.Empty;
        public string IslenmeDurumu { get; set; } = "Alindi";
        public string PayloadHash { get; set; } = string.Empty;
        public string HataMesaji { get; set; } = string.Empty;
        public DateTime SaglayiciAt { get; set; }
        public DateTime AlindiAt { get; set; } = DateTime.UtcNow;
        public DateTime? IslendiAt { get; set; }
    }
}
