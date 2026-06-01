using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface IAiAssistantService
    {
        Task<AiAssistantStatus> GetStatusAsync(CancellationToken ct = default);

        Task<AiAssistantChatResponse> ChatAsync(
            AiAssistantChatRequest request,
            CancellationToken ct = default);

        Task<AiBusinessSuggestionsResponse> GetSuggestionsAsync(
            CancellationToken ct = default);

    }
}
