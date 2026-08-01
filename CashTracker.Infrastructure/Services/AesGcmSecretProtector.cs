using System.Security.Cryptography;
using System.Text;
using CashTracker.Core.Services;

namespace CashTracker.Infrastructure.Services;

public sealed class AesGcmSecretProtector : ISecretProtector
{
    private const string Prefix = "aesgcm1:";
    private const string LegacyPrefix = "b64:";
    private static readonly byte[] AssociatedData = Encoding.UTF8.GetBytes("systemcel.secret.v1");
    private readonly byte[] _key;

    public AesGcmSecretProtector(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length != 32)
            throw new ArgumentException("AES-GCM anahtari tam olarak 32 bayt olmalidir.", nameof(key));

        _key = key.ToArray();
    }

    public string Protect(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            return string.Empty;

        var clearBytes = Encoding.UTF8.GetBytes(secret.Trim());
        var nonce = RandomNumberGenerator.GetBytes(12);
        var cipherBytes = new byte[clearBytes.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(_key, tag.Length))
            aes.Encrypt(nonce, clearBytes, cipherBytes, tag, AssociatedData);

        var payload = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, payload, nonce.Length + tag.Length, cipherBytes.Length);
        CryptographicOperations.ZeroMemory(clearBytes);
        return Prefix + Convert.ToBase64String(payload);
    }

    public bool TryUnprotect(string protectedSecret, out string secret)
    {
        secret = string.Empty;
        if (string.IsNullOrWhiteSpace(protectedSecret))
            return false;

        var raw = protectedSecret.Trim();
        if (raw.StartsWith(LegacyPrefix, StringComparison.Ordinal))
            return TryDecodeLegacy(raw, out secret);
        if (!raw.StartsWith(Prefix, StringComparison.Ordinal))
            return false;

        try
        {
            var payload = Convert.FromBase64String(raw[Prefix.Length..]);
            if (payload.Length < 29)
                return false;

            var nonce = payload.AsSpan(0, 12);
            var tag = payload.AsSpan(12, 16);
            var cipherBytes = payload.AsSpan(28);
            var clearBytes = new byte[cipherBytes.Length];
            using (var aes = new AesGcm(_key, tag.Length))
                aes.Decrypt(nonce, cipherBytes, tag, clearBytes, AssociatedData);

            secret = Encoding.UTF8.GetString(clearBytes).Trim();
            CryptographicOperations.ZeroMemory(clearBytes);
            return !string.IsNullOrWhiteSpace(secret);
        }
        catch (CryptographicException)
        {
            secret = string.Empty;
            return false;
        }
        catch (FormatException)
        {
            secret = string.Empty;
            return false;
        }
    }

    public static bool IsLegacyCipherText(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Trim().StartsWith(LegacyPrefix, StringComparison.Ordinal);
    }

    private static bool TryDecodeLegacy(string value, out string secret)
    {
        secret = string.Empty;
        try
        {
            secret = Encoding.UTF8.GetString(Convert.FromBase64String(value[LegacyPrefix.Length..])).Trim();
            return !string.IsNullOrWhiteSpace(secret);
        }
        catch (FormatException)
        {
            secret = string.Empty;
            return false;
        }
    }
}
