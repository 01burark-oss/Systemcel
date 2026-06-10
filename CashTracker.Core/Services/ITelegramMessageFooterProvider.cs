using System.Threading;
using System.Threading.Tasks;

namespace CashTracker.Core.Services
{
    public interface ITelegramMessageFooterProvider
    {
        Task<string> BuildFooterAsync(CancellationToken ct = default);
    }
}
