using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface IEntitlementGuard
    {
        Task<SubscriptionEntitlementStatus> GetAsync(
            int businessId,
            string accountType,
            CancellationToken ct = default);

        void EnsureLimit(
            SubscriptionEntitlementStatus entitlement,
            string limitName,
            int currentCount,
            int requestedCount = 1);

        void EnsureWritable(SubscriptionEntitlementStatus entitlement);

        void EnsureFeature(SubscriptionEntitlementStatus entitlement, string featureName);
    }
}
