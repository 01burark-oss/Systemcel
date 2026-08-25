using CashTracker.Core.Services;

namespace Systemcel.Api;

internal sealed class SubscriptionLifecycleHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
    private readonly ISubscriptionLifecycleService _lifecycle;
    private readonly IMuhasebeciOdemeService _accountantPayments;
    private readonly ILogger<SubscriptionLifecycleHostedService> _logger;

    public SubscriptionLifecycleHostedService(
        ISubscriptionLifecycleService lifecycle,
        IMuhasebeciOdemeService accountantPayments,
        ILogger<SubscriptionLifecycleHostedService> logger)
    {
        _lifecycle = lifecycle;
        _accountantPayments = accountantPayments;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await _lifecycle.ReconcileAsync(DateTime.UtcNow, stoppingToken);
                var accountantPeriods = await _accountantPayments.EnsureDuePeriodsAsync(DateTime.UtcNow, stoppingToken);
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
                if (accountantPeriods > 0)
                    _logger.LogInformation("Accountant service periods created. Count={AccountantPeriods}", accountantPeriods);
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
