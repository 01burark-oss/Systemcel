using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Systemcel.Api
{
    internal sealed class UserTelegramSetup
    {
        public string BotToken { get; set; } = string.Empty;
        public string BotUsername { get; set; } = string.Empty;
        public string ChatId { get; set; } = string.Empty;
        public string AllowedUserIds { get; set; } = string.Empty;

        // Legacy field for previous telegram-setup.json files.
        public string UserId { get; set; } = string.Empty;
    }

    internal static class UserTelegramSetupStore
    {
        private const string FileName = "telegram-setup.json";
        private const string EncryptedPrefix = "enc:";

        public static UserTelegramSetup Load(string appDataPath)
        {
            var settingsPath = GetSettingsPath(appDataPath);
            if (!File.Exists(settingsPath))
                return new UserTelegramSetup();

            try
            {
                var json = File.ReadAllText(settingsPath);
                if (string.IsNullOrWhiteSpace(json))
                    return new UserTelegramSetup();

                var data = JsonSerializer.Deserialize<UserTelegramSetup>(json);
                if (data == null)
                    return new UserTelegramSetup();

                return new UserTelegramSetup
                {
                    BotToken = UnprotectIfNeeded(data.BotToken),
                    BotUsername = data.BotUsername?.Trim() ?? string.Empty,
                    ChatId = FirstNonEmpty(data.ChatId, data.UserId),
                    AllowedUserIds = data.AllowedUserIds?.Trim() ?? string.Empty,
                    UserId = data.UserId?.Trim() ?? string.Empty
                };
            }
            catch
            {
                return new UserTelegramSetup();
            }
        }

        public static void Save(string appDataPath, UserTelegramSetup setup)
        {
            if (setup is null)
                throw new ArgumentNullException(nameof(setup));

            Directory.CreateDirectory(appDataPath);

            var normalized = new UserTelegramSetup
            {
                BotToken = Protect(setup.BotToken.Trim()),
                BotUsername = setup.BotUsername.Trim().TrimStart('@'),
                ChatId = setup.ChatId.Trim(),
                AllowedUserIds = setup.AllowedUserIds.Trim(),
                UserId = setup.ChatId.Trim()
            };

            var json = JsonSerializer.Serialize(
                normalized,
                new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(GetSettingsPath(appDataPath), json);
        }

        private static string GetSettingsPath(string appDataPath)
        {
            return Path.Combine(appDataPath, FileName);
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }

            return string.Empty;
        }

        private static string Protect(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            if (!OperatingSystem.IsWindows())
                return value;

            try
            {
                var clearBytes = Encoding.UTF8.GetBytes(value);
                var cipherBytes = ProtectedData.Protect(clearBytes, null, DataProtectionScope.CurrentUser);
                return EncryptedPrefix + Convert.ToBase64String(cipherBytes);
            }
            catch
            {
                // Fail-safe fallback: keep previous behavior if encryption is unavailable.
                return value;
            }
        }

        private static string UnprotectIfNeeded(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            if (!value.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
                return value.Trim();

            if (!OperatingSystem.IsWindows())
                return string.Empty;

            try
            {
                var payload = value[EncryptedPrefix.Length..];
                var cipherBytes = Convert.FromBase64String(payload);
                var clearBytes = ProtectedData.Unprotect(cipherBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(clearBytes).Trim();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
