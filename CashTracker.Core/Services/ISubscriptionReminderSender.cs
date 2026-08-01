using System;
using System.Threading;
using System.Threading.Tasks;

namespace CashTracker.Core.Services
{
    public sealed record SubscriptionTrialReminder(
        int BusinessId,
        string AccountType,
        string Email,
        string PlanName,
        int DaysRemaining,
        DateTime TrialEndsAt,
        decimal NetAmount,
        decimal VatAmount,
        decimal TotalAmount,
        string Currency,
        string SubscriptionUrl);

    public interface ISubscriptionReminderSender
    {
        Task<bool> SendTrialEndingAsync(
            SubscriptionTrialReminder reminder,
            CancellationToken ct = default);
    }
}
