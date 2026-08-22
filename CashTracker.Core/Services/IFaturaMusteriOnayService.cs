using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface IMusteriSmsSender
    {
        bool IsConfigured { get; }
        Task<MusteriSmsGonderimSonucu> SendAsync(string phoneNumber, string message, CancellationToken ct = default);
    }

    public interface IFaturaMusteriOnayService
    {
        Task<FaturaMusteriOnayGonderimSonucu> SendAsync(int faturaId, CancellationToken ct = default);
        Task<FaturaMusteriOnayDurumu> GetLatestAsync(int faturaId, CancellationToken ct = default);
        Task<PublicFaturaMusteriOnayDetayi?> GetPublicAsync(string token, CancellationToken ct = default);
        Task<PublicFaturaMusteriOnayDetayi?> RespondAsync(
            string token,
            PublicFaturaMusteriOnayYaniti response,
            string clientIp,
            string userAgent,
            CancellationToken ct = default);
    }
}
