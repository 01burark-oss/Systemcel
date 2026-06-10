using System.Text;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace CashTracker.Infrastructure.Services
{
    public sealed class TelegramAccountantApplicationNotifier : IAccountantApplicationNotifier
    {
        private readonly HttpClient _httpClient;
        private readonly TelegramSettings _settings;

        public TelegramAccountantApplicationNotifier(HttpClient httpClient, TelegramSettings settings)
        {
            _httpClient = httpClient;
            _settings = settings;
        }

        public async Task NotifyApplicationCreatedAsync(AccountantApplicationNotification notification, CancellationToken ct = default)
        {
            if (!_settings.IsEnabled)
                return;

            var text = BuildText(notification);
            var telegram = new TelegramBotService(_httpClient, _settings);
            await telegram.SendTextAsync(_settings.ChatId, text, ct);
        }

        private static string BuildText(AccountantApplicationNotification notification)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Yeni muhasebeci başvurusu");
            sb.AppendLine($"Ad: {Display(notification.AdSoyad)}");
            sb.AppendLine($"E-posta: {Display(notification.Eposta)}");
            sb.AppendLine($"Ofis: {Display(notification.IsletmeAdi)}");
            sb.AppendLine($"Tür: {Display(notification.IsletmeTuru)}");
            sb.AppendLine($"Konum: {Display(notification.Konum)}");
            sb.AppendLine();
            sb.AppendLine("Yönetim > Muhasebeci Başvuruları ekranından onaylayabilir veya reddedebilirsin.");
            return sb.ToString().Trim();
        }

        private static string Display(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }
    }
}
