using CashTracker.Core.Services;

namespace Systemcel.Api;

internal sealed class MarketplaceMoneyInstructionHostedService(
    ITedarikciPazaryeriService marketplace,
    ILogger<MarketplaceMoneyInstructionHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await marketplace.ProcessReadyMoneyInstructionsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Marketplace money instruction processing failed.");
            }
        }
    }
}
