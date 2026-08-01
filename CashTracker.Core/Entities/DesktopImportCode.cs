namespace CashTracker.Core.Entities;

public sealed class DesktopImportCode
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddMinutes(30);
    public DateTime? ClaimedAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public int? TargetIsletmeId { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public string PackageId { get; set; } = string.Empty;
    public string ImportedTotalsJson { get; set; } = "{}";
}
