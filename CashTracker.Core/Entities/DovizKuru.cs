using System;

namespace CashTracker.Core.Entities;

public sealed class DovizKuru
{
    public int Id { get; set; }
    public int IsletmeId { get; set; }
    public string ParaBirimi { get; set; } = "TRY";
    public decimal Kur { get; set; } = 1m;
    public DateTime GecerliAt { get; set; } = DateTime.Now;
    public string OlusturmaAnahtari { get; set; } = string.Empty;
    public string IcerikOzeti { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
