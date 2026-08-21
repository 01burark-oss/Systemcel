using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Encodings.Web;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using Microsoft.Extensions.Logging;

namespace CashTracker.Infrastructure.Payments;

public sealed class UnconfiguredOdemeHatirlatmaSender : IOdemeHatirlatmaSender
{
    public bool IsConfigured => false;

    public Task<bool> SendAsync(OdemeHatirlatmaIcerigi reminder, CancellationToken ct = default)
    {
        return Task.FromResult(false);
    }
}

public sealed class SmtpOdemeHatirlatmaSender : IOdemeHatirlatmaSender
{
    private readonly SubscriptionReminderEmailOptions _options;
    private readonly ILogger<SmtpOdemeHatirlatmaSender> _logger;

    public SmtpOdemeHatirlatmaSender(
        SubscriptionReminderEmailOptions options,
        ILogger<SmtpOdemeHatirlatmaSender> logger)
    {
        _options = options;
        _logger = logger;
    }

    public bool IsConfigured => _options.IsConfigured;

    public async Task<bool> SendAsync(OdemeHatirlatmaIcerigi reminder, CancellationToken ct = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(reminder.AliciEposta))
            return false;

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName, Encoding.UTF8),
                Subject = OdemeHatirlatmaMetni.BuildSubject(reminder),
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = true,
                Body = BuildHtmlBody(reminder)
            };
            message.To.Add(reminder.AliciEposta);

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
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Payment reminder delivery failed. BusinessId={BusinessId} Invoice={Invoice}",
                reminder.IsletmeId,
                reminder.FaturaNo);
            return false;
        }
    }

    private string BuildHtmlBody(OdemeHatirlatmaIcerigi reminder)
    {
        var encoder = HtmlEncoder.Default;
        var body = OdemeHatirlatmaMetni.BuildMessage(reminder)
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var paragraphs = string.Join(
            string.Empty,
            body.Take(Math.Max(0, body.Length - 1)).Select(x => $"<p style=\"margin:0 0 16px;line-height:1.6\">{encoder.Encode(x)}</p>"));
        var publicUrl = _options.PublicBaseUrl.TrimEnd('/');

        return $"""
               <!doctype html>
               <html lang="tr">
               <body style="margin:0;padding:32px;background:#f4f2ea;color:#11110f;font-family:Arial,sans-serif">
                 <table role="presentation" width="100%" cellspacing="0" cellpadding="0">
                   <tr><td align="center">
                     <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;background:#fffef9;border:1px solid #dedbd0;border-radius:18px">
                       <tr><td style="padding:28px 30px">
                         <div style="margin-bottom:24px;font-size:22px;font-weight:800">systemcel<span style="color:#799500">.</span></div>
                         {paragraphs}
                         <div style="margin-top:24px;padding-top:18px;border-top:1px solid #dedbd0;color:#6f6c63;font-size:12px;line-height:1.5">
                           Systemcel ile gönderildi · <a href="{encoder.Encode(publicUrl)}" style="color:#536e00">systemcel.app</a>
                         </div>
                       </td></tr>
                     </table>
                   </td></tr>
                 </table>
               </body>
               </html>
               """;
    }
}
