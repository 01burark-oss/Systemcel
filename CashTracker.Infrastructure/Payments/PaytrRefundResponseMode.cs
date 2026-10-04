namespace CashTracker.Infrastructure.Payments;

public enum PaytrRefundResponseMode
{
    Unconfirmed,
    TestOne,
    LiveZero,
    LiveAbsent
}

public static class PaytrRefundResponseContract
{
    public static bool IsLive(PaytrRefundResponseMode mode) =>
        mode is PaytrRefundResponseMode.LiveZero or PaytrRefundResponseMode.LiveAbsent;

    public static PaytrRefundResponseMode ParseLive(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "zero" => PaytrRefundResponseMode.LiveZero,
        "absent" => PaytrRefundResponseMode.LiveAbsent,
        _ => PaytrRefundResponseMode.Unconfirmed
    };
}
