using System;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services;

public interface IBrutKarMarjiService
{
    Task<BrutKarMarjiOzeti> GetAsync(DateTime baslangic, DateTime bitis, CancellationToken ct = default);
}
