using System;

namespace CashTracker.Core.Entities
{
    public sealed class StokDefterIslemi
    {
        public int Id { get; set; }
        public int IsletmeId { get; set; }
        public string IslemAnahtari { get; set; } = string.Empty;
        public string IcerikOzeti { get; set; } = string.Empty;
        public string IslemTipi { get; set; } = string.Empty;
        public int? TersKayitKaynakIslemId { get; set; }
        public string? Aciklama { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
