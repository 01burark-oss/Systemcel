using CashTracker.Core.Models;
using CashTracker.Core.Services;

namespace Systemcel.Api.Services
{
    internal sealed class UnsupportedBarcodeReaderService : IBarcodeReaderService
    {
        public Task<BarcodeReadResult> TryReadAsync(string imagePath, CancellationToken ct = default)
        {
            return Task.FromResult(BarcodeReadResult.Failed("Barkod okuma bu platformda aktif degil."));
        }
    }
}
