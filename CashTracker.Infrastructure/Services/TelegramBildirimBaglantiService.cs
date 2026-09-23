using System.Data;
using System.Security.Cryptography;
using CashTracker.Core.Entities;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services;

public sealed class TelegramBildirimBaglantiService : ITelegramBildirimBaglantiService
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);
    private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;

    public TelegramBildirimBaglantiService(IDbContextFactory<CashTrackerDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public Task<TelegramPairingCode> EnsureCodeAsync(int isletmeId, string kullaniciRef, CancellationToken ct = default) =>
        GetOrRenewCodeAsync(isletmeId, kullaniciRef, false, ct);

    public Task<TelegramPairingCode> RenewCodeAsync(int isletmeId, string kullaniciRef, CancellationToken ct = default) =>
        GetOrRenewCodeAsync(isletmeId, kullaniciRef, true, ct);

    public async Task<TelegramBildirimBaglantisiDurumu> GetStateAsync(
        int isletmeId, string kullaniciRef, CancellationToken ct = default)
    {
        ValidateScope(isletmeId, kullaniciRef);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await RequireActiveMemberAsync(db, isletmeId, kullaniciRef, ct);
        var connection = await db.TelegramBildirimBaglantilari.AsNoTracking().SingleOrDefaultAsync(
            x => x.IsletmeId == isletmeId && x.KullaniciRef == kullaniciRef, ct);
        return connection is { ChatId.Length: > 0 }
            ? new(true, connection.ChatId, connection.BaglandiAt)
            : new(false, string.Empty, null);
    }

    public async Task<bool> TryCompleteAsync(
        string code, long chatId, long? telegramUserId, CancellationToken ct = default)
    {
        if (chatId <= 0 || !telegramUserId.HasValue || telegramUserId.Value != chatId ||
            string.IsNullOrWhiteSpace(code))
            return false;
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var now = DateTime.UtcNow;
        var normalizedCode = code.Trim().ToUpperInvariant();
        var connection = await db.TelegramBildirimBaglantilari.SingleOrDefaultAsync(
            x => x.EslestirmeKodu == normalizedCode && x.KodGecerliAt > now, ct);
        if (connection is null || !await IsActiveMemberAsync(db, connection.IsletmeId, connection.KullaniciRef, ct))
            return false;
        connection.ChatId = chatId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        connection.TelegramUserId = telegramUserId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        connection.EslestirmeKodu = string.Empty;
        connection.KodGecerliAt = null;
        connection.BaglandiAt = now;
        connection.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task ClearAsync(int isletmeId, string kullaniciRef, CancellationToken ct = default)
    {
        ValidateScope(isletmeId, kullaniciRef);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await RequireActiveMemberAsync(db, isletmeId, kullaniciRef, ct);
        var connection = await db.TelegramBildirimBaglantilari.SingleOrDefaultAsync(
            x => x.IsletmeId == isletmeId && x.KullaniciRef == kullaniciRef, ct);
        if (connection is null) return;
        connection.ChatId = string.Empty;
        connection.TelegramUserId = string.Empty;
        connection.EslestirmeKodu = string.Empty;
        connection.KodGecerliAt = null;
        connection.BaglandiAt = null;
        connection.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private async Task<TelegramPairingCode> GetOrRenewCodeAsync(
        int isletmeId, string kullaniciRef, bool renew, CancellationToken ct)
    {
        ValidateScope(isletmeId, kullaniciRef);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await RequireActiveMemberAsync(db, isletmeId, kullaniciRef, ct);
        var now = DateTime.UtcNow;
        var connection = await db.TelegramBildirimBaglantilari.SingleOrDefaultAsync(
            x => x.IsletmeId == isletmeId && x.KullaniciRef == kullaniciRef, ct);
        if (connection is null)
        {
            connection = new TelegramBildirimBaglantisi
            {
                IsletmeId = isletmeId,
                KullaniciRef = kullaniciRef.Trim(),
                CreatedAt = now
            };
            db.TelegramBildirimBaglantilari.Add(connection);
        }
        if (renew || string.IsNullOrWhiteSpace(connection.EslestirmeKodu) || connection.KodGecerliAt <= now)
        {
            string code;
            do
            {
                code = $"SC-{Convert.ToHexString(RandomNumberGenerator.GetBytes(6))}";
            } while (await db.TelegramBildirimBaglantilari.AnyAsync(x => x.EslestirmeKodu == code, ct));
            connection.EslestirmeKodu = code;
            connection.KodGecerliAt = now.Add(CodeLifetime);
            connection.UpdatedAt = now;
            await db.SaveChangesAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return new TelegramPairingCode(connection.EslestirmeKodu, connection.KodGecerliAt!.Value.Subtract(CodeLifetime), connection.KodGecerliAt.Value);
    }

    private static void ValidateScope(int isletmeId, string kullaniciRef)
    {
        if (isletmeId <= 0 || string.IsNullOrWhiteSpace(kullaniciRef) || kullaniciRef.Length > 160)
            throw new UnauthorizedAccessException("Telegram bağlantısı için işletme ve kullanıcı gerekir.");
    }

    private static async Task RequireActiveMemberAsync(
        CashTrackerDbContext db, int isletmeId, string kullaniciRef, CancellationToken ct)
    {
        if (!await IsActiveMemberAsync(db, isletmeId, kullaniciRef, ct))
            throw new UnauthorizedAccessException("Telegram bağlantısı için aktif işletme üyeliği gerekir.");
    }

    private static Task<bool> IsActiveMemberAsync(
        CashTrackerDbContext db, int isletmeId, string kullaniciRef, CancellationToken ct) =>
        (from user in db.Kullanicilar.AsNoTracking()
         join membership in db.IsletmeUyelikleri.AsNoTracking() on user.Id equals membership.KullaniciId
         where user.AuthProviderUserId == kullaniciRef && user.Durum == "Aktif" &&
               membership.IsletmeId == isletmeId && membership.Durum == "Aktif"
         select user.Id).AnyAsync(ct);
}
