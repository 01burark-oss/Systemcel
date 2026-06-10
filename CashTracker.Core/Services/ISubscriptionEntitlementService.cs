using System;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface ISubscriptionEntitlementService
    {
        Task<SubscriptionEntitlementStatus> GetIsletmeEntitlementAsync(
            int isletmeId,
            DateTime? now = null,
            CancellationToken ct = default);

        Task<SubscriptionEntitlementStatus> GetMuhasebeciEntitlementAsync(
            int muhasebeciIsletmeId,
            DateTime? now = null,
            CancellationToken ct = default);
    }
}
