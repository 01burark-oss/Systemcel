using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Payments;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services;

public interface IEmailDeliveryClient
{
    bool IsConfigured { get; }
    Task SendAsync(string recipient, string subject, string body, CancellationToken ct = default);
}

public sealed class SmtpEmailDeliveryClient : IEmailDeliveryClient
{
    private readonly SubscriptionReminderEmailOptions _options;

    public SmtpEmailDeliveryClient(SubscriptionReminderEmailOptions options) => _options = options;

    public bool IsConfigured => _options.IsConfigured;

    public async Task SendAsync(string recipient, string subject, string body, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("SMTP yapılandırılmadı.");

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName, Encoding.UTF8),
            Subject = subject,
            SubjectEncoding = Encoding.UTF8,
            Body = body,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = false
        };
        message.To.Add(recipient);

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
    }
}

public sealed class EpostaBildirimAdapter : IBildirimKanalAdapter
{
    private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
    private readonly IEmailDeliveryClient _client;
    private readonly SubscriptionReminderEmailOptions _options;

    public EpostaBildirimAdapter(
        IDbContextFactory<CashTrackerDbContext> dbFactory,
        IEmailDeliveryClient client,
        SubscriptionReminderEmailOptions options)
    {
        _dbFactory = dbFactory;
        _client = client;
        _options = options;
    }

    public string Kanal => BildirimKanallari.Eposta;
    public bool IsConfigured => _client.IsConfigured;

    public async Task SendAsync(BildirimOutboxClaim claim, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var recipient = await (
            from user in db.Kullanicilar.AsNoTracking()
            join membership in db.IsletmeUyelikleri.AsNoTracking() on user.Id equals membership.KullaniciId
            where user.AuthProviderUserId == claim.KullaniciRef &&
                  user.Durum == "Aktif" &&
                  membership.IsletmeId == claim.IsletmeId &&
                  membership.Durum == "Aktif"
            select user.Eposta).SingleOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(recipient))
            throw new InvalidOperationException("Bildirim alıcısı aktif işletme üyeliğiyle doğrulanamadı.");

        using var payload = JsonDocument.Parse(claim.PayloadJson);
        var snapshottedRecipient = Read(payload.RootElement, "AliciEposta", "recipientEmail");
        if (!string.IsNullOrWhiteSpace(snapshottedRecipient) &&
            !string.Equals(snapshottedRecipient.Trim(), recipient.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Bildirim alıcısı snapshot ile eşleşmiyor.");
        var subject = Read(payload.RootElement, "Baslik", "title");
        var message = Read(payload.RootElement, "Mesaj", "message");
        var path = Read(payload.RootElement, "Url", "url");
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
            throw new InvalidOperationException("E-posta bildirim içeriği eksik.");

        var body = message.Trim();
        if (!string.IsNullOrWhiteSpace(path))
        {
            if (path.Contains('\\') || path.StartsWith("//", StringComparison.Ordinal) || path.Any(char.IsControl))
                throw new InvalidOperationException("E-posta bildirim bağlantısı geçersiz.");
            // Check relative paths first: Unix treats /app/... as an absolute file URI.
            var url = Uri.TryCreate(path, UriKind.Relative, out _)
                ? $"{_options.PublicBaseUrl.TrimEnd('/')}/{path.TrimStart('/')}"
                : Uri.TryCreate(path, UriKind.Absolute, out var absolute) &&
                  absolute.Scheme is "https" or "http"
                    ? absolute.AbsoluteUri
                    : throw new InvalidOperationException("E-posta bildirim bağlantısı geçersiz.");
            body += $"{Environment.NewLine}{Environment.NewLine}{url}";
        }
        await _client.SendAsync(recipient.Trim(), subject.Trim(), body, ct);
    }

    private static string Read(JsonElement root, params string[] names)
    {
        foreach (var name in names)
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString() ?? string.Empty;
        return string.Empty;
    }
}
