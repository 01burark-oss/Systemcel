using System;

namespace CashTracker.Core.Models
{
    public sealed class ReceiptOcrSettings
    {
        public string Provider { get; set; } = "Gemini";
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-2.5-flash";
        public string FallbackModel { get; set; } = "gemini-2.5-pro";
        public string Language { get; set; } = "tur";
        public int SessionTimeoutMinutes { get; set; } = 30;
        public string EffectiveProvider => string.IsNullOrWhiteSpace(_licenseProvider) ? Provider : _licenseProvider;
        public string EffectiveApiKey => string.IsNullOrWhiteSpace(_licenseApiKey) ? ApiKey : _licenseApiKey;
        public string EffectiveModel => string.IsNullOrWhiteSpace(_licenseModel) ? Model : _licenseModel;
        public string EffectiveFallbackModel => string.IsNullOrWhiteSpace(FallbackModel) ? string.Empty : FallbackModel.Trim();
        public string EffectiveLanguage => string.IsNullOrWhiteSpace(Language) ? "tur" : Language.Trim();

        private string _licenseProvider = string.Empty;
        private string _licenseApiKey = string.Empty;
        private string _licenseModel = string.Empty;

        public bool IsConfigured =>
            (string.Equals(EffectiveProvider, "Gemini", StringComparison.OrdinalIgnoreCase) ||
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
