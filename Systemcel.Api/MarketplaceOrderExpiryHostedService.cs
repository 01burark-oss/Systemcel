using CashTracker.Core.Services;

namespace Systemcel.Api;

internal sealed class MarketplaceOrderExpiryHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private readonly ITedarikciPazaryeriService _marketplace;
    private readonly ILogger<MarketplaceOrderExpiryHostedService> _logger;

    public MarketplaceOrderExpiryHostedService(
        ITedarikciPazaryeriService marketplace,
        ILogger<MarketplaceOrderExpiryHostedService> logger)
    {
        _marketplace = marketplace;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var expired = await _marketplace.ExpirePendingOrdersAsync(DateTime.UtcNow, stoppingToken);
                if (expired > 0)
                    _logger.LogInformation("Expired marketplace reservations released. Count={ExpiredCount}", expired);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Marketplace reservation expiry failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
