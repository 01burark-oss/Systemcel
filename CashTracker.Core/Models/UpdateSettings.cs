namespace CashTracker.Core.Models
{
    public sealed class UpdateSettings
    {
        public string RepoOwner { get; set; } = string.Empty;
        public string RepoName { get; set; } = string.Empty;
        public int AutoCheckDelaySeconds { get; set; } = 30;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(RepoOwner) &&
            !string.IsNullOrWhiteSpace(RepoName);
    }
}
