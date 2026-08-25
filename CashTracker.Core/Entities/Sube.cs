using System;

namespace CashTracker.Core.Entities;

public sealed class Sube
{
    public int Id { get; set; }
    public int IsletmeId { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Kod { get; set; } = string.Empty;
    public bool Varsayilan { get; set; }
    public bool Aktif { get; set; } = true;
    public string OlusturmaAnahtari { get; set; } = string.Empty;
    public string IcerikOzeti { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
