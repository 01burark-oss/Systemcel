using CashTracker.Core.Services;

namespace Systemcel.Api;

internal sealed class SubscriptionLifecycleHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
    private readonly ISubscriptionLifecycleService _lifecycle;
    private readonly ILogger<SubscriptionLifecycleHostedService> _logger;

    public SubscriptionLifecycleHostedService(
        ISubscriptionLifecycleService lifecycle,
        ILogger<SubscriptionLifecycleHostedService> logger)
    {
        _lifecycle = lifecycle;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await _lifecycle.ReconcileAsync(DateTime.UtcNow, stoppingToken);
                if (result.ExpiredTrials + result.ExpiredSubscriptions +
                    result.CancelledSubscriptions + result.GracePeriodsEnded +
                    result.SevenDayReminders + result.ThreeDayReminders > 0)
                {
                    _logger.LogInformation(
                        "Subscription states reconciled. Trials={ExpiredTrials}, expired={ExpiredSubscriptions}, cancelled={CancelledSubscriptions}, graceEnded={GracePeriodsEnded}, reminder7={SevenDayReminders}, reminder3={ThreeDayReminders}",
                        result.ExpiredTrials,
                        result.ExpiredSubscriptions,
                        result.CancelledSubscriptions,
                        result.GracePeriodsEnded,
                        result.SevenDayReminders,
                        result.ThreeDayReminders);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscription state reconciliation failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
