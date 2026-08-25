using CashTracker.Infrastructure.Services;

namespace Systemcel.Api;

internal sealed class BildirimDeliveryHostedService : BackgroundService
{
    private readonly BildirimDeliveryService _delivery;
    private readonly ILogger<BildirimDeliveryHostedService> _logger;

    public BildirimDeliveryHostedService(BildirimDeliveryService delivery, ILogger<BildirimDeliveryHostedService> logger)
    {
        _delivery = delivery;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await _delivery.DispatchAsync(DateTime.UtcNow, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogWarning("Bildirim teslim turu tamamlanamadi: {ErrorType}", ex.GetType().Name); }
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}
