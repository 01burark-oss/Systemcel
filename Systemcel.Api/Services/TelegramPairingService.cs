using System;
using System.Globalization;
using System.Security.Cryptography;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace Systemcel.Api.Services
{
    internal sealed class TelegramPairingService : ITelegramPairingService
    {
        private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);

        private readonly object _gate = new();
        private readonly AppRuntimeOptions _runtimeOptions;
        private readonly TelegramSettings _settings;

        private string _code = string.Empty;
        private DateTime _createdUtc = DateTime.MinValue;

        public TelegramPairingService(AppRuntimeOptions runtimeOptions, TelegramSettings settings)
        {
            _runtimeOptions = runtimeOptions ?? throw new ArgumentNullException(nameof(runtimeOptions));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public TelegramPairingCode EnsureActiveCode()
        {
            lock (_gate)
            {
                if (string.IsNullOrWhiteSpace(_code) || IsExpiredUtc(DateTime.UtcNow))
                    RenewCodeCore();

                return CurrentCodeCore();
            }
        }

        public TelegramPairingCode RenewCode()
        {
            lock (_gate)
            {
                RenewCodeCore();
                return CurrentCodeCore();
            }
        }

        public bool TryCompletePairing(string code, long chatId, long? userId, out string message)
        {
            lock (_gate)
            {
                if (string.IsNullOrWhiteSpace(code) ||
                    string.IsNullOrWhiteSpace(_code) ||
                    IsExpiredUtc(DateTime.UtcNow) ||
                    !string.Equals(NormalizeCode(code), _code, StringComparison.OrdinalIgnoreCase))
                {
                    message = "Eşleştirme kodu bulunamadı veya süresi doldu. Uygulamadaki Telegram ekranından yeni kod al.";
                    return false;
                }

                _settings.ChatId = chatId.ToString(CultureInfo.InvariantCulture);
                _settings.AllowedUserIds = userId.HasValue
                    ? userId.Value.ToString(CultureInfo.InvariantCulture)
                    : string.Empty;
                _settings.EnableCommands = true;

                UserTelegramSetupStore.Save(_runtimeOptions.AppDataPath, new UserTelegramSetup
                {
                    ChatId = _settings.ChatId,
                    AllowedUserIds = _settings.AllowedUserIds
                });

                _code = string.Empty;
                _createdUtc = DateTime.MinValue;
                message = "Systemcel Telegram bağlantısı tamamlandı. Komutları görmek için /yardim yazabilirsin.";
                return true;
            }
        }

        public void ClearPairing()
        {
            lock (_gate)
            {
                _code = string.Empty;
                _createdUtc = DateTime.MinValue;
            }
        }

        private void RenewCodeCore()
        {
            _code = $"SC-{RandomNumberGenerator.GetInt32(100000, 999999)}";
            _createdUtc = DateTime.UtcNow;
        }

        private TelegramPairingCode CurrentCodeCore()
        {
            return new TelegramPairingCode(_code, _createdUtc, _createdUtc.Add(CodeLifetime));
        }

        private bool IsExpiredUtc(DateTime nowUtc)
        {
            return nowUtc - _createdUtc > CodeLifetime;
        }

        private static string NormalizeCode(string code)
        {
            return code.Trim();
        }
    }
}
