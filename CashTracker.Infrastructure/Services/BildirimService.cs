using System.Data;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;
using CashTracker.Core.Services;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services;

public sealed class BildirimService : IBildirimService, IBildirimOutboxService
{
    private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;

    public BildirimService(IDbContextFactory<CashTrackerDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<IReadOnlyList<BildirimGorunumu>> SyncAndListAsync(
        int isletmeId,
        string kullaniciRef,
        IReadOnlyCollection<BildirimSnapshot> snapshots,
        CancellationToken ct = default)
    {
        ValidateScope(isletmeId, kullaniciRef);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var userRef = kullaniciRef.Trim();
        var now = DateTime.UtcNow;
        var preferences = await db.BildirimTercihleri.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IsletmeId == isletmeId && x.KullaniciRef == userRef, ct)
            ?? new BildirimTercihi { IsletmeId = isletmeId, KullaniciRef = userRef };
        foreach (var snapshot in snapshots.GroupBy(x => NormalizeKey(x.KaynakAnahtari)).Select(x => x.First()))
            await UpsertSnapshotWithOutboxAsync(isletmeId, userRef, snapshot, preferences, now, ct);

        db.ChangeTracker.Clear();
        var rows = await db.BildirimKayitlari.AsNoTracking()
            .Where(x => x.IsletmeId == isletmeId && x.KullaniciRef == userRef)
            .OrderBy(x => x.OkunduAt != null)
            .ThenByDescending(x => x.CreatedAt)
            .Take(50)
            .ToListAsync(ct);
        return rows.Select(ToView).ToList();
    }

    private async Task UpsertSnapshotWithOutboxAsync(
        int businessId,
        string userRef,
        BildirimSnapshot snapshot,
        BildirimTercihi preferences,
        DateTime now,
        CancellationToken ct)
    {
        var key = NormalizeKey(snapshot.KaynakAnahtari);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await using var transaction = db.Database.IsRelational()
                ? await db.Database.BeginTransactionAsync(ct)
                : null;
            try
            {
                var row = await db.BildirimKayitlari.SingleOrDefaultAsync(x =>
                    x.IsletmeId == businessId && x.KullaniciRef == userRef && x.KaynakAnahtari == key, ct);
                if (row is null)
                {
                    row = new BildirimKaydi
                    {
                        IsletmeId = businessId,
                        KullaniciRef = userRef,
                        KaynakAnahtari = key,
                        CreatedAt = now
                    };
                    db.BildirimKayitlari.Add(row);
                }

                row.Tur = Limit(snapshot.Tur, 30);
                row.Onem = Limit(snapshot.Onem, 20);
                row.Baslik = Limit(snapshot.Baslik, 200);
                row.Mesaj = Limit(snapshot.Mesaj, 1000);
                row.Aksiyon = Limit(snapshot.Aksiyon, 120);
                row.Url = SafeRelativeUrl(snapshot.Url);
                row.UpdatedAt = now;
                await db.SaveChangesAsync(ct);

                if (preferences.UygulamaAktif)
                    await EnqueueInternalAsync(db, businessId, userRef, row, BildirimKanallari.Uygulama, now, ct);
                if (preferences.EpostaAktif)
                    await EnqueueInternalAsync(db, businessId, userRef, row, BildirimKanallari.Eposta, now, ct);
                if (preferences.TelegramAktif)
                    await EnqueueInternalAsync(db, businessId, userRef, row, BildirimKanallari.Telegram, now, ct);

                await db.SaveChangesAsync(ct);
                if (transaction is not null) await transaction.CommitAsync(ct);
                return;
            }
            catch (DbUpdateException) when (attempt == 0)
            {
                if (transaction is not null) await transaction.RollbackAsync(CancellationToken.None);
                // A concurrent insert may have won. A fresh context retries the full
                // notification + enabled-channel outbox operation and repairs any gap.
            }
        }
    }

    public async Task<int> MarkReadAsync(int isletmeId, string kullaniciRef, int bildirimId, CancellationToken ct = default)
    {
        ValidateScope(isletmeId, kullaniciRef);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var row = await db.BildirimKayitlari.SingleOrDefaultAsync(x =>
            x.Id == bildirimId && x.IsletmeId == isletmeId && x.KullaniciRef == kullaniciRef.Trim(), ct);
        if (row is null) return -1;
        row.OkunduAt ??= DateTime.UtcNow;
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await UnreadCountAsync(db, isletmeId, kullaniciRef.Trim(), ct);
    }

    public async Task<int> MarkAllReadAsync(int isletmeId, string kullaniciRef, CancellationToken ct = default)
    {
        ValidateScope(isletmeId, kullaniciRef);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var rows = await db.BildirimKayitlari.Where(x =>
            x.IsletmeId == isletmeId && x.KullaniciRef == kullaniciRef.Trim() && x.OkunduAt == null).ToListAsync(ct);
        var now = DateTime.UtcNow;
        foreach (var row in rows) { row.OkunduAt = now; row.UpdatedAt = now; }
        await db.SaveChangesAsync(ct);
        return 0;
    }

    public async Task<BildirimTercihModeli> GetPreferencesAsync(int isletmeId, string kullaniciRef, CancellationToken ct = default)
    {
        ValidateScope(isletmeId, kullaniciRef);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var row = await GetOrCreatePreferencesAsync(db, isletmeId, kullaniciRef.Trim(), ct);
        await db.SaveChangesAsync(ct);
        return ToModel(row);
    }

    public async Task<BildirimTercihModeli> SavePreferencesAsync(int isletmeId, string kullaniciRef, BildirimTercihModeli model, CancellationToken ct = default)
    {
        ValidateScope(isletmeId, kullaniciRef);
        if (model.SessizBaslangicDakika is < 0 or > 1439 || model.SessizBitisDakika is < 0 or > 1439)
            throw new ArgumentException("Sessiz saat dakika değeri 0-1439 arasında olmalıdır.");
        if (!string.Equals(model.SaatDilimi, "Europe/Istanbul", StringComparison.Ordinal))
            throw new ArgumentException("Desteklenmeyen saat dilimi.");
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var row = await GetOrCreatePreferencesAsync(db, isletmeId, kullaniciRef.Trim(), ct);
        row.UygulamaAktif = model.UygulamaAktif;
        row.EpostaAktif = model.EpostaAktif;
        row.TelegramAktif = model.TelegramAktif;
        row.SessizSaatAktif = model.SessizSaatAktif;
        row.SessizBaslangicDakika = model.SessizBaslangicDakika;
        row.SessizBitisDakika = model.SessizBitisDakika;
        row.SaatDilimi = model.SaatDilimi;
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToModel(row);
    }

    public async Task EnqueueAsync(int isletmeId, string kullaniciRef, int? bildirimId, string idempotencyAnahtari, string kanal, string payloadJson, DateTime nowUtc, CancellationToken ct = default)
    {
        ValidateScope(isletmeId, kullaniciRef);
        ValidateChannel(kanal);
        if (string.IsNullOrWhiteSpace(idempotencyAnahtari)) throw new ArgumentException("Idempotency anahtarı zorunludur.");
        var idempotencyKey = Limit(idempotencyAnahtari.Trim(), 160);
        if (string.IsNullOrWhiteSpace(payloadJson) || payloadJson.Length > 4000)
            throw new ArgumentException("Bildirim payload boyutu geçersiz.");
        _ = JsonDocument.Parse(payloadJson);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var exists = await db.BildirimTeslimOutboxlari.AnyAsync(x => x.IsletmeId == isletmeId && x.KullaniciRef == kullaniciRef.Trim() && x.Kanal == kanal && x.IdempotencyAnahtari == idempotencyKey, ct);
        if (exists) return;
        db.BildirimTeslimOutboxlari.Add(new BildirimTeslimOutbox
        {
            IsletmeId = isletmeId, KullaniciRef = kullaniciRef.Trim(), BildirimId = bildirimId,
            IdempotencyAnahtari = idempotencyKey, Kanal = kanal,
            PayloadJson = payloadJson, SonrakiDenemeAt = EnsureUtc(nowUtc), CreatedAt = EnsureUtc(nowUtc), UpdatedAt = EnsureUtc(nowUtc)
        });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            if (!await db.BildirimTeslimOutboxlari.AsNoTracking().AnyAsync(x => x.IsletmeId == isletmeId && x.KullaniciRef == kullaniciRef.Trim() && x.Kanal == kanal && x.IdempotencyAnahtari == idempotencyKey, ct))
                throw;
        }
    }

    public async Task<IReadOnlyList<BildirimOutboxClaim>> ClaimAsync(int batchSize, DateTime nowUtc, TimeSpan lease, CancellationToken ct = default)
    {
        if (batchSize is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(batchSize));
        if (lease <= TimeSpan.Zero || lease > TimeSpan.FromMinutes(15)) throw new ArgumentOutOfRangeException(nameof(lease));
        var now = EnsureUtc(nowUtc);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        var candidates = await db.BildirimTeslimOutboxlari
            .Where(x => (x.Durum == BildirimTeslimDurumlari.Bekliyor || (x.Durum == BildirimTeslimDurumlari.Isleniyor && x.ClaimBitisAt <= now)) &&
                        x.SonrakiDenemeAt <= now && x.DeadLetterAt == null)
            .OrderBy(x => x.SonrakiDenemeAt).ThenBy(x => x.Id).Take(batchSize).ToListAsync(ct);
        var result = new List<BildirimOutboxClaim>();
        foreach (var row in candidates)
        {
            if (row.Kanal != BildirimKanallari.Uygulama && await IsQuietAsync(db, row.IsletmeId, row.KullaniciRef, now, ct))
            {
                row.SonrakiDenemeAt = await NextQuietEndUtcAsync(db, row.IsletmeId, row.KullaniciRef, now, ct);
                row.UpdatedAt = now;
                continue;
            }
            row.ClaimToken = Guid.NewGuid().ToString("N");
            row.ClaimBitisAt = now.Add(lease);
            row.Durum = BildirimTeslimDurumlari.Isleniyor;
            row.UpdatedAt = now;
            result.Add(new(row.Id, row.IsletmeId, row.KullaniciRef, row.Kanal, row.PayloadJson, row.ClaimToken, row.DenemeSayisi));
        }
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return result;
    }

    public Task CompleteAsync(long id, string claimToken, DateTime nowUtc, CancellationToken ct = default) => FinishAsync(id, claimToken, null, nowUtc, 5, ct);
    public async Task MarkUnconfiguredAsync(long id, string claimToken, DateTime nowUtc, CancellationToken ct = default)
    {
        var now = EnsureUtc(nowUtc);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var row = await db.BildirimTeslimOutboxlari.SingleOrDefaultAsync(x => x.Id == id && x.ClaimToken == claimToken && x.Durum == BildirimTeslimDurumlari.Isleniyor, ct)
            ?? throw new InvalidOperationException("Teslim claim kaydı bulunamadı veya süresi değişti.");
        row.Durum = BildirimTeslimDurumlari.Yapilandirilmadi;
        row.ClaimToken = string.Empty;
        row.ClaimBitisAt = null;
        row.SonHataKodu = "channel_not_configured";
        row.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
    }
    public Task FailAsync(long id, string claimToken, string errorCode, DateTime nowUtc, int maxAttempts = 5, CancellationToken ct = default) => FinishAsync(id, claimToken, errorCode, nowUtc, maxAttempts, ct);

    private async Task FinishAsync(long id, string token, string? errorCode, DateTime nowUtc, int maxAttempts, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Claim token zorunludur.");
        var now = EnsureUtc(nowUtc);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var row = await db.BildirimTeslimOutboxlari.SingleOrDefaultAsync(x => x.Id == id && x.ClaimToken == token && x.Durum == BildirimTeslimDurumlari.Isleniyor, ct)
            ?? throw new InvalidOperationException("Teslim claim kaydı bulunamadı veya süresi değişti.");
        row.ClaimToken = string.Empty;
        row.ClaimBitisAt = null;
        row.UpdatedAt = now;
        if (errorCode is null)
        {
            row.Durum = BildirimTeslimDurumlari.TeslimEdildi;
            row.TeslimEdildiAt = now;
        }
        else
        {
            row.DenemeSayisi++;
            row.SonHataKodu = Limit(errorCode, 80);
            if (row.DenemeSayisi >= Math.Clamp(maxAttempts, 1, 20))
            {
                row.Durum = BildirimTeslimDurumlari.DeadLetter;
                row.DeadLetterAt = now;
            }
            else
            {
                row.Durum = BildirimTeslimDurumlari.Bekliyor;
                row.SonrakiDenemeAt = now.AddMinutes(Math.Min(60, 1 << Math.Min(6, row.DenemeSayisi - 1)));
            }
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task EnqueueInternalAsync(CashTrackerDbContext db, int businessId, string userRef, BildirimKaydi row, string channel, DateTime now, CancellationToken ct)
    {
        var rawKey = $"bildirim:{businessId}:{userRef}:{row.KaynakAnahtari}";
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey))).ToLowerInvariant();
        if (await db.BildirimTeslimOutboxlari.AnyAsync(x => x.IsletmeId == businessId && x.KullaniciRef == userRef && x.Kanal == channel && x.IdempotencyAnahtari == key, ct)) return;
        db.BildirimTeslimOutboxlari.Add(new BildirimTeslimOutbox
        {
            IsletmeId = businessId, KullaniciRef = userRef, BildirimId = row.Id, IdempotencyAnahtari = key,
            Kanal = channel, PayloadJson = JsonSerializer.Serialize(new { row.Baslik, row.Mesaj, row.Url }),
            SonrakiDenemeAt = now, CreatedAt = now, UpdatedAt = now
        });
    }

    private static async Task<BildirimTercihi> GetOrCreatePreferencesAsync(CashTrackerDbContext db, int businessId, string userRef, CancellationToken ct)
    {
        var row = await db.BildirimTercihleri.SingleOrDefaultAsync(x => x.IsletmeId == businessId && x.KullaniciRef == userRef, ct);
        if (row is not null) return row;
        row = new BildirimTercihi { IsletmeId = businessId, KullaniciRef = userRef };
        db.BildirimTercihleri.Add(row);
        return row;
    }

    private static async Task<bool> IsQuietAsync(CashTrackerDbContext db, int businessId, string userRef, DateTime nowUtc, CancellationToken ct)
    {
        var p = await db.BildirimTercihleri.AsNoTracking().SingleOrDefaultAsync(x => x.IsletmeId == businessId && x.KullaniciRef == userRef, ct);
        if (p?.SessizSaatAktif != true) return false;
        var local = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, ResolveIstanbul());
        var minute = local.Hour * 60 + local.Minute;
        return p.SessizBaslangicDakika <= p.SessizBitisDakika
            ? minute >= p.SessizBaslangicDakika && minute < p.SessizBitisDakika
            : minute >= p.SessizBaslangicDakika || minute < p.SessizBitisDakika;
    }

    private static async Task<DateTime> NextQuietEndUtcAsync(CashTrackerDbContext db, int businessId, string userRef, DateTime nowUtc, CancellationToken ct)
    {
        var preferences = await db.BildirimTercihleri.AsNoTracking().SingleAsync(x => x.IsletmeId == businessId && x.KullaniciRef == userRef, ct);
        var zone = ResolveIstanbul();
        var local = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, zone);
        var end = local.Date.AddMinutes(preferences.SessizBitisDakika);
        if (end <= local) end = end.AddDays(1);
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(end, DateTimeKind.Unspecified), zone);
    }

    private static TimeZoneInfo ResolveIstanbul()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
    }

    private static Task<int> UnreadCountAsync(CashTrackerDbContext db, int businessId, string userRef, CancellationToken ct) =>
        db.BildirimKayitlari.CountAsync(x => x.IsletmeId == businessId && x.KullaniciRef == userRef && x.OkunduAt == null, ct);
    private static BildirimGorunumu ToView(BildirimKaydi x) => new(x.Id, x.KaynakAnahtari, x.Tur, x.Onem, x.Baslik, x.Mesaj, x.Aksiyon, x.Url, x.OkunduAt != null, x.CreatedAt);
    private static BildirimTercihModeli ToModel(BildirimTercihi x) => new(x.UygulamaAktif, x.EpostaAktif, x.TelegramAktif, x.SessizSaatAktif, x.SessizBaslangicDakika, x.SessizBitisDakika, x.SaatDilimi);
    private static string SafeRelativeUrl(string? value) => string.IsNullOrWhiteSpace(value) || value.Contains('\\') || !Uri.TryCreate(value, UriKind.Relative, out _) || !value.StartsWith('/') || value.StartsWith("//", StringComparison.Ordinal) ? string.Empty : Limit(value, 500);
    private static string NormalizeKey(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Bildirim kaynak anahtarı zorunludur.") : Limit(value.Trim(), 160);
    private static string Limit(string? value, int max)
    {
        var normalized = (value ?? string.Empty).Trim();
        return normalized.Length > max ? normalized[..max] : normalized;
    }
    private static DateTime EnsureUtc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    private static void ValidateScope(int id, string user) { if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id)); if (string.IsNullOrWhiteSpace(user)) throw new ArgumentException("Kullanıcı kapsamı zorunludur."); }
    private static void ValidateChannel(string channel) { if (channel is not (BildirimKanallari.Uygulama or BildirimKanallari.Eposta or BildirimKanallari.Telegram)) throw new ArgumentException("Desteklenmeyen bildirim kanalı."); }
}
