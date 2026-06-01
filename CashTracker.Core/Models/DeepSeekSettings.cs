namespace CashTracker.Core.Models
{
    public sealed class DeepSeekSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.deepseek.com";
        public string ProModel { get; set; } = "deepseek-v4-pro";
        public string FlashModel { get; set; } = "deepseek-v4-flash";
        public int TimeoutSeconds { get; set; } = 60;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

        public string EffectiveApiKey => ApiKey.Trim();

        public string EffectiveBaseUrl => string.IsNullOrWhiteSpace(BaseUrl)
            ? "https://api.deepseek.com"
            : BaseUrl.Trim().TrimEnd('/');

        public string EffectiveProModel => string.IsNullOrWhiteSpace(ProModel)
            ? "deepseek-v4-pro"
            : ProModel.Trim();

        public string EffectiveFlashModel => string.IsNullOrWhiteSpace(FlashModel)
            ? "deepseek-v4-flash"
            : FlashModel.Trim();

        public int EffectiveTimeoutSeconds => TimeoutSeconds <= 0 ? 60 : TimeoutSeconds;
    }
}
