using CashTracker.Core.Services;

namespace Systemcel.Api;

internal sealed class PaymentReconciliationHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);
    private readonly IPaymentReconciliationService _reconciliation;
    private readonly ILogger<PaymentReconciliationHostedService> _logger;

    public PaymentReconciliationHostedService(IPaymentReconciliationService reconciliation, ILogger<PaymentReconciliationHostedService> logger)
    {
        _reconciliation = reconciliation;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await _reconciliation.ReconcileAsync(DateTime.UtcNow, stoppingToken);
                if (result.ProviderAvailable)
                    _logger.LogInformation("Payment reconciliation completed. Checked={Checked}, discrepancies={Discrepancies}, recorded={Recorded}", result.CheckedSubscriptions, result.DiscrepancyCount, result.RecordedFindings);
                else
                    _logger.LogDebug("Payment reconciliation skipped: {Message}", result.Message);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Payment reconciliation failed."); }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
