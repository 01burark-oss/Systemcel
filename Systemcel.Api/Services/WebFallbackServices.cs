using System.Text;
using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace Systemcel.Api.Services
{
    internal sealed class Base64SecretProtector : ISecretProtector
    {
        private const string Prefix = "b64:";

        public string Protect(string secret)
        {
            if (string.IsNullOrEmpty(secret))
                return string.Empty;

            return Prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(secret));
        }

        public bool TryUnprotect(string protectedSecret, out string secret)
        {
            secret = string.Empty;
            if (string.IsNullOrWhiteSpace(protectedSecret))
                return true;

            if (!protectedSecret.StartsWith(Prefix, StringComparison.Ordinal))
                return false;

            try
            {
                secret = Encoding.UTF8.GetString(Convert.FromBase64String(protectedSecret[Prefix.Length..]));
                return true;
            }
            catch
            {
                secret = string.Empty;
                return false;
            }
        }
    }

    internal sealed class UnsupportedBarcodeReaderService : IBarcodeReaderService
    {
        public Task<BarcodeReadResult> TryReadAsync(string imagePath, CancellationToken ct = default)
        {
            return Task.FromResult(BarcodeReadResult.Failed("Barkod okuma bu platformda aktif degil."));
        }
    }
}
