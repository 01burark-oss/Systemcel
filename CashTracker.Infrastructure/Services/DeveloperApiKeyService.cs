using System.Security.Cryptography;
using System.Text;
using CashTracker.Core.Models;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashTracker.Infrastructure.Services;

public sealed class DeveloperApiKeyService
{
    private const string KeyMarker = "sys_live_";
    private const int PrefixByteCount = 6;
    private const int SecretByteCount = 32;
    private static readonly byte[] DummyHash = SHA256.HashData(Encoding.UTF8.GetBytes("systemcel-developer-api-dummy-key"));
    private readonly IDbContextFactory<CashTrackerDbContext> _dbFactory;

    public DeveloperApiKeyService(IDbContextFactory<CashTrackerDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<DeveloperApiKeyCreated> CreateAsync(
        int businessId,
        string creatorUserRef,
        DeveloperApiKeyCreateRequest request,
        DateTime? now = null,
        CancellationToken ct = default)
    {
        var name = NormalizeName(request.Name);
        var scopes = NormalizeScopes(request.Scopes);
        if (request.ExpiresInDays is < 1 or > 365)
            throw new ArgumentOutOfRangeException(nameof(request.ExpiresInDays), "Anahtar süresi 1 ile 365 gün arasında olmalıdır.");
        if (string.IsNullOrWhiteSpace(creatorUserRef) || creatorUserRef.Length > 200)
            throw new ArgumentException("Geçerli bir kullanıcı referansı gereklidir.", nameof(creatorUserRef));

        var current = now ?? DateTime.Now;
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var prefix = KeyMarker + Convert.ToHexString(RandomNumberGenerator.GetBytes(PrefixByteCount)).ToLowerInvariant();
            var secret = Base64UrlEncode(RandomNumberGenerator.GetBytes(SecretByteCount));
            var plaintext = prefix + "_" + secret;
            var row = new Core.Entities.GelistiriciApiAnahtari
            {
                IsletmeId = businessId,
                OlusturanKullaniciRef = creatorUserRef,
                Ad = name,
                Prefix = prefix,
                AnahtarHash = SHA256.HashData(Encoding.UTF8.GetBytes(plaintext)),
                ScopeListesi = string.Join(' ', scopes),
                CreatedAt = current,
                ExpiresAt = current.AddDays(request.ExpiresInDays)
            };
            db.GelistiriciApiAnahtarlari.Add(row);
            try
            {
                await db.SaveChangesAsync(ct);
                return new DeveloperApiKeyCreated(row.Id, row.Ad, row.Prefix, scopes, row.CreatedAt, row.ExpiresAt, plaintext);
            }
            catch (DbUpdateException) when (attempt < 2)
            {
                db.Entry(row).State = EntityState.Detached;
            }
        }

        throw new InvalidOperationException("API anahtarı üretilemedi.");
    }

    public async Task<IReadOnlyList<DeveloperApiKeyListItem>> ListAsync(int businessId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var rows = await db.GelistiriciApiAnahtarlari.AsNoTracking()
            .Where(x => x.IsletmeId == businessId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(x => new DeveloperApiKeyListItem(
            x.Id, x.Ad, x.Prefix, ParseScopes(x.ScopeListesi), x.CreatedAt, x.LastUsedAt, x.ExpiresAt, x.RevokedAt)).ToList();
    }

    public async Task<bool> RevokeAsync(int businessId, int keyId, string actorUserRef, CancellationToken ct = default)
    {
        if (keyId <= 0 || string.IsNullOrWhiteSpace(actorUserRef))
            return false;
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var row = await db.GelistiriciApiAnahtarlari
            .SingleOrDefaultAsync(x => x.Id == keyId && x.IsletmeId == businessId, ct);
        if (row is null)
            return false;
        if (row.RevokedAt is null)
        {
            row.RevokedAt = DateTime.Now;
            row.RevokedByUserRef = actorUserRef.Length <= 200 ? actorUserRef : actorUserRef[..200];
            await db.SaveChangesAsync(ct);
        }
        return true;
    }

    public async Task<DeveloperApiIdentity?> AuthenticateAsync(string? plaintext, DateTime? now = null, CancellationToken ct = default)
    {
        var suppliedHash = HashSuppliedKey(plaintext);
        var prefix = TryReadPrefix(plaintext);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var row = prefix is null
            ? null
            : await db.GelistiriciApiAnahtarlari.SingleOrDefaultAsync(x => x.Prefix == prefix, ct);
        var expectedHash = row?.AnahtarHash is { Length: 32 } stored ? stored : DummyHash;
        var hashMatches = CryptographicOperations.FixedTimeEquals(suppliedHash, expectedHash);
        var current = now ?? DateTime.Now;
        if (!hashMatches || row is null || row.RevokedAt is not null || row.ExpiresAt <= current)
            return null;

        // Last-used bilgisi denetim amacıyla tutulur; bir dakikadan sık yazmayarak okuma API'sini yazma yüküne çevirmeyiz.
        if (row.LastUsedAt is null || row.LastUsedAt < current.AddMinutes(-1))
        {
            row.LastUsedAt = current;
            await db.SaveChangesAsync(ct);
        }
        return new DeveloperApiIdentity(row.Id, row.IsletmeId, row.Prefix, ParseScopes(row.ScopeListesi).ToHashSet(StringComparer.Ordinal));
    }

    public async Task<bool> IsOwnerAsync(int businessId, string providerUserRef, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(providerUserRef))
            return false;
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await (
            from membership in db.IsletmeUyelikleri.AsNoTracking()
            join user in db.Kullanicilar.AsNoTracking() on membership.KullaniciId equals user.Id
            where membership.IsletmeId == businessId &&
                  membership.Durum == "Aktif" && membership.Rol == "isletme_sahibi" &&
                  user.AuthProvider == "clerk" && user.AuthProviderUserId == providerUserRef
            select membership.Id).AnyAsync(ct);
    }

    private static string NormalizeName(string value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length is < 2 or > 100)
            throw new ArgumentException("Anahtar adı 2 ile 100 karakter arasında olmalıdır.", nameof(value));
        return normalized;
    }

    private static IReadOnlyList<string> NormalizeScopes(IReadOnlyList<string>? values)
    {
        if (values is null)
            throw new ArgumentException("En az bir okuma kapsamı seçilmelidir.", nameof(values));
        var scopes = values.Select(x => (x ?? string.Empty).Trim())
            .Where(x => x.Length > 0).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();
        if (scopes.Count == 0 || scopes.Any(x => !DeveloperApiScopes.Allowed.Contains(x)))
            throw new ArgumentException("Yalnız desteklenen salt-okunur kapsamlar kullanılabilir.", nameof(values));
        return scopes;
    }

    private static IReadOnlyList<string> ParseScopes(string value) =>
        value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(DeveloperApiScopes.Allowed.Contains).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();

    private static byte[] HashSuppliedKey(string? plaintext)
    {
        if (plaintext is null || plaintext.Length > 128)
            return DummyHash.ToArray();
        return SHA256.HashData(Encoding.UTF8.GetBytes(plaintext));
    }

    private static string? TryReadPrefix(string? plaintext)
    {
        const int prefixLength = 21;
        if (plaintext is null || plaintext.Length != 65 || !plaintext.StartsWith(KeyMarker, StringComparison.Ordinal))
            return null;
        if (plaintext[prefixLength] != '_')
            return null;
        var hex = plaintext.AsSpan(KeyMarker.Length, PrefixByteCount * 2);
        foreach (var character in hex)
            if (!((character >= '0' && character <= '9') || (character >= 'a' && character <= 'f')))
                return null;
        return plaintext[..prefixLength];
    }

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
