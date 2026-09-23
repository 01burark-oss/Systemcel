using System.Text.Json;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services;

public sealed class TelegramBildirimAdapter : IBildirimKanalAdapter
{
    private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;
    private readonly TelegramBotService _bot;
    private readonly TelegramSettings _settings;

    public TelegramBildirimAdapter(
        IDbContextFactory<CashTrackerDbContext> dbFactory,
        TelegramBotService bot,
        TelegramSettings settings)
    {
        _dbFactory = dbFactory;
        _bot = bot;
        _settings = settings;
    }

    public string Kanal => BildirimKanallari.Telegram;
    public bool IsConfigured => _settings.HasBotToken;

    public async Task SendAsync(BildirimOutboxClaim claim, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var chatId = await (
            from link in db.TelegramBildirimBaglantilari.AsNoTracking()
            join user in db.Kullanicilar.AsNoTracking() on link.KullaniciRef equals user.AuthProviderUserId
            join membership in db.IsletmeUyelikleri.AsNoTracking() on user.Id equals membership.KullaniciId
            where link.IsletmeId == claim.IsletmeId &&
                  link.KullaniciRef == claim.KullaniciRef &&
                  link.ChatId != "" && user.Durum == "Aktif" &&
                  membership.IsletmeId == claim.IsletmeId && membership.Durum == "Aktif"
            select link.ChatId).SingleOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(chatId))
            throw new InvalidOperationException("Telegram alıcısı aktif işletme üyeliğiyle doğrulanamadı.");

        using var payload = JsonDocument.Parse(claim.PayloadJson);
        var title = Read(payload.RootElement, "Baslik", "title");
        var message = Read(payload.RootElement, "Mesaj", "message");
        var path = Read(payload.RootElement, "Url", "url");
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
            throw new InvalidOperationException("Telegram bildirim içeriği eksik.");
        var body = $"{title.Trim()}\n{message.Trim()}";
        if (!string.IsNullOrWhiteSpace(path))
        {
            if (!Uri.TryCreate(path, UriKind.Relative, out _) || path.StartsWith("//", StringComparison.Ordinal))
                throw new InvalidOperationException("Telegram bildirim bağlantısı geçersiz.");
            body += $"\nhttps://systemcel.app/{path.TrimStart('/')}";
        }
        await _bot.SendTextAsync(chatId, body, ct);
    }

    private static string Read(JsonElement root, params string[] names)
    {
        foreach (var name in names)
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString() ?? string.Empty;
        return string.Empty;
    }
}
