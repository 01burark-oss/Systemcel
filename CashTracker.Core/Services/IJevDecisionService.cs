using CashTracker.Core.Models;

namespace CashTracker.Core.Services;

public interface IJevDecisionService
{
    bool IsConfigured { get; }

    Task<IReadOnlyDictionary<string, JevChoiceResult>> ChooseAsync(
        object state,
        IReadOnlyDictionary<string, JevChoiceQuestion> questions,
        CancellationToken ct = default);
}
