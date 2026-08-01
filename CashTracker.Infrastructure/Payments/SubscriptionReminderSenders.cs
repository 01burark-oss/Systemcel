using System.Net;
using System.Net.Mail;
using System.Text;
using CashTracker.Core.Services;

namespace CashTracker.Infrastructure.Payments;

public sealed class SubscriptionReminderEmailOptions
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 587;
    public bool EnableSsl { get; init; } = true;
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = "Systemcel";
    public string PublicBaseUrl { get; init; } = "https://systemcel.app";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);
}

public sealed class UnconfiguredSubscriptionReminderSender : ISubscriptionReminderSender
{
    public Task<bool> SendTrialEndingAsync(SubscriptionTrialReminder reminder, CancellationToken ct = default)
    {
        return Task.FromResult(false);
    }
}

public sealed class SmtpSubscriptionReminderSender : ISubscriptionReminderSender
{
    private readonly SubscriptionReminderEmailOptions _options;

    public SmtpSubscriptionReminderSender(SubscriptionReminderEmailOptions options)
    {
        _options = options;
    }

    public async Task<bool> SendTrialEndingAsync(
        SubscriptionTrialReminder reminder,
        CancellationToken ct = default)
    {
        if (!_options.IsConfigured || string.IsNullOrWhiteSpace(reminder.Email))
            return false;

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName, Encoding.UTF8),
                Subject = reminder.DaysRemaining == 3
                    ? "Systemcel denemenizin bitmesine 3 gün kaldı"
                    : "Systemcel denemenizin bitmesine 7 gün kaldı",
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = false,
                Body = BuildBody(reminder)
            };
            message.To.Add(reminder.Email);

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = string.IsNullOrWhiteSpace(_options.UserName)
                    ? CredentialCache.DefaultNetworkCredentials
                    : new NetworkCredential(_options.UserName, _options.Password)
            };
            await client.SendMailAsync(message, ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private string BuildBody(SubscriptionTrialReminder reminder)
    {
        var subscriptionUrl = Uri.TryCreate(reminder.SubscriptionUrl, UriKind.Absolute, out var absolute)
            ? absolute.AbsoluteUri
            : $"{_options.PublicBaseUrl.TrimEnd('/')}/{reminder.SubscriptionUrl.TrimStart('/')}";
        return $"""
               Merhaba,

               Systemcel {reminder.PlanName} denemeniz {reminder.TrialEndsAt:dd.MM.yyyy} tarihinde sona erecek.
               İptal etmezseniz deneme sonunda aylık {reminder.NetAmount:N2} {reminder.Currency} +
               {reminder.VatAmount:N2} {reminder.Currency} KDV, toplam {reminder.TotalAmount:N2} {reminder.Currency}
               kayıtlı ödeme yönteminizden tahsil edilir.

               Planınızı ve dönem sonu iptal yolunu görüntüleyin:
               {subscriptionUrl}

               Systemcel
               """;
    }
}
