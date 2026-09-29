using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace CashTracker.Infrastructure.Payments;

public sealed record PaytrIframeTokenInput(
    string MerchantId,
    string UserIp,
    string MerchantOrderId,
    string Email,
    long AmountKurus,
    string BasketBase64,
    bool NoInstallment,
    int MaxInstallment,
    string Currency,
    bool TestMode);

public sealed record PaytrIframeCallback(
    string MerchantOrderId,
    string Status,
    string TotalAmountKurus,
    string Hash);

/// <summary>
/// PayTR iFrame step 1 token and step 2 callback signatures. This protocol helper
/// does not initiate or acknowledge a payment; the signed callback must still be
/// matched to the server-side order and committed exactly once before replying OK.
/// </summary>
public static class PaytrIframeProtocol
{
    public static long ToKurus(decimal amount)
    {
        var scaled = amount * 100m;
        if (amount <= 0m || decimal.Truncate(scaled) != scaled)
            throw new ArgumentOutOfRangeException(nameof(amount), "The amount must be positive and have at most two decimal places.");

        return checked((long)scaled);
    }

    public static string CreateToken(PaytrIframeTokenInput input, string merchantKey, string merchantSalt)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidateSecret(merchantKey, nameof(merchantKey));
        ValidateSecret(merchantSalt, nameof(merchantSalt));
        if (string.IsNullOrWhiteSpace(input.MerchantId) || !input.MerchantId.All(char.IsAsciiDigit))
            throw new ArgumentException("Merchant ID must contain digits only.", nameof(input));
        if (!ValidOrderId(input.MerchantOrderId))
            throw new ArgumentException("Merchant order ID must be 1-64 ASCII letters or digits.", nameof(input));
        if (!IPAddress.TryParse(input.UserIp, out _) || input.UserIp.Length > 39)
            throw new ArgumentException("User IP must be a valid IP address of at most 39 characters.", nameof(input));
        if (string.IsNullOrWhiteSpace(input.Email) || input.Email.Length > 100 ||
            !input.Email.Contains('@') || input.Email.Any(c => c > 127 || char.IsControl(c)))
            throw new ArgumentException("Customer email must be an ASCII address of at most 100 characters.", nameof(input));
        if (input.AmountKurus <= 0)
            throw new ArgumentOutOfRangeException(nameof(input));
        if (string.IsNullOrWhiteSpace(input.BasketBase64) || !TryDecodeBase64(input.BasketBase64))
            throw new ArgumentException("Basket must be a nonempty Base64 value.", nameof(input));
        if (input.MaxInstallment != 0 && input.MaxInstallment is < 2 or > 12)
            throw new ArgumentOutOfRangeException(nameof(input));
        if (input.Currency is not ("TL" or "TRY" or "EUR" or "USD" or "GBP" or "RUB"))
            throw new ArgumentException("Currency is not supported by the iFrame API.", nameof(input));

        var hashInput = string.Concat(
            input.MerchantId,
            input.UserIp,
            input.MerchantOrderId,
            input.Email,
            input.AmountKurus.ToString(CultureInfo.InvariantCulture),
            input.BasketBase64,
            input.NoInstallment ? "1" : "0",
            input.MaxInstallment.ToString(CultureInfo.InvariantCulture),
            input.Currency,
            input.TestMode ? "1" : "0",
            merchantSalt);
        return Sign(hashInput, merchantKey);
    }

    public static bool VerifyCallback(PaytrIframeCallback callback, string merchantKey, string merchantSalt)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ValidateSecret(merchantKey, nameof(merchantKey));
        ValidateSecret(merchantSalt, nameof(merchantSalt));
        if (!ValidOrderId(callback.MerchantOrderId) ||
            callback.Status is not ("success" or "failed") ||
            !long.TryParse(callback.TotalAmountKurus, NumberStyles.None, CultureInfo.InvariantCulture, out _) ||
            callback.Hash?.Length != 44)
            return false;

        byte[] suppliedHash;
        try { suppliedHash = Convert.FromBase64String(callback.Hash); }
        catch (FormatException) { return false; }

        var hashInput = string.Concat(
            callback.MerchantOrderId,
            merchantSalt,
            callback.Status,
            callback.TotalAmountKurus);
        var expectedHash = Convert.FromBase64String(Sign(hashInput, merchantKey));
        return suppliedHash.Length == expectedHash.Length &&
               CryptographicOperations.FixedTimeEquals(suppliedHash, expectedHash);
    }

    private static string Sign(string input, string merchantKey)
    {
        var key = Encoding.UTF8.GetBytes(merchantKey);
        var payload = Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(HMACSHA256.HashData(key, payload));
    }

    private static void ValidateSecret(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("PayTR credential is required.", name);
    }

    private static bool ValidOrderId(string value) =>
        !string.IsNullOrEmpty(value) && value.Length <= 64 &&
        value.All(c => char.IsAsciiLetterOrDigit(c));

    private static bool TryDecodeBase64(string value)
    {
        try { return Convert.FromBase64String(value).Length > 0; }
        catch (FormatException) { return false; }
    }
}
