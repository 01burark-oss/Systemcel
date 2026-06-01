using System;

namespace CashTracker.Core.Entities
{
    public sealed class AiKullanimDonemi
    {
        public int Id { get; set; }
        public int IsletmeId { get; set; }
        public string DonemAnahtari { get; set; } = string.Empty;
        public int? MesajLimiti { get; set; }
        public int KullanilanMesaj { get; set; }
        public DateTime DonemBaslangicAt { get; set; } = DateTime.Now;
        public DateTime DonemBitisAt { get; set; } = DateTime.Now.AddMonths(1);
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
