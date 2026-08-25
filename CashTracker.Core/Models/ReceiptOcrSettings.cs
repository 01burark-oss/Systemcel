using System;

namespace CashTracker.Core.Models
{
    public sealed class ReceiptOcrSettings
    {
        public string Provider { get; set; } = "OpenAI";
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";
        public string Model { get; set; } = "gpt-5-mini";
        public string FallbackModel { get; set; } = string.Empty;
        public string Language { get; set; } = "tur";
        public int SessionTimeoutMinutes { get; set; } = 30;
        public string EffectiveProvider => string.IsNullOrWhiteSpace(_licenseProvider) ? Provider : _licenseProvider;
        public string EffectiveApiKey => string.IsNullOrWhiteSpace(_licenseApiKey) ? ApiKey : _licenseApiKey;
        public string EffectiveBaseUrl => string.IsNullOrWhiteSpace(BaseUrl)
            ? "https://api.openai.com/v1"
            : BaseUrl.Trim().TrimEnd('/');
        public string EffectiveModel => string.IsNullOrWhiteSpace(_licenseModel) ? Model : _licenseModel;
        public string EffectiveFallbackModel => string.IsNullOrWhiteSpace(FallbackModel) ? string.Empty : FallbackModel.Trim();
        public string EffectiveLanguage => string.IsNullOrWhiteSpace(Language) ? "tur" : Language.Trim();

        private string _licenseProvider = string.Empty;
        private string _licenseApiKey = string.Empty;
        private string _licenseModel = string.Empty;

        public bool IsConfigured =>
            (string.Equals(EffectiveProvider, "OpenAI", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(EffectiveProvider, "OcrSpace", StringComparison.OrdinalIgnoreCase)) &&
            !string.IsNullOrWhiteSpace(EffectiveApiKey);

        public TimeSpan GetSessionTimeout()
        {
            var minutes = SessionTimeoutMinutes switch
            {
                < 1 => 30,
                > 720 => 720,
                _ => SessionTimeoutMinutes
            };

            return TimeSpan.FromMinutes(minutes);
        }

        public void ApplyLicenseOverrides(string? provider, string? apiKey, string? model)
        {
            _licenseProvider = string.IsNullOrWhiteSpace(provider) ? string.Empty : provider.Trim();
            _licenseApiKey = string.IsNullOrWhiteSpace(apiKey) ? string.Empty : apiKey.Trim();
            _licenseModel = string.IsNullOrWhiteSpace(model) ? string.Empty : model.Trim();
        }

        public void ClearLicenseOverrides()
        {
            _licenseProvider = string.Empty;
            _licenseApiKey = string.Empty;
            _licenseModel = string.Empty;
        }
    }
}
