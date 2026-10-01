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
                if (result.UnavailablePayments > 0)
                    _logger.LogWarning("Payment reconciliation queries unavailable. Count={Count}", result.UnavailablePayments);
                if (result.DiscrepancyCount > 0)
                    _logger.LogWarning("Payment reconciliation requires review. Discrepancies={Discrepancies}, recorded={Recorded}", result.DiscrepancyCount, result.RecordedFindings);
                if (result.ProviderAvailable)
                    _logger.LogInformation("Payment reconciliation completed. Subscriptions={Subscriptions}, payments={Payments}, unavailable={Unavailable}, discrepancies={Discrepancies}, recorded={Recorded}", result.CheckedSubscriptions, result.CheckedPayments, result.UnavailablePayments, result.DiscrepancyCount, result.RecordedFindings);
                else
                    _logger.LogDebug("Payment reconciliation skipped: {Message}", result.Message);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Payment reconciliation failed."); }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
