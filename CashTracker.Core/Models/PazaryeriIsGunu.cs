namespace CashTracker.Core.Models;

public static class PazaryeriIsGunu
{
    public static DateTime SonTarih(DateTime openedAtUtc, int businessDays, IReadOnlySet<DateOnly> holidays)
    {
        if (businessDays < 1) throw new ArgumentOutOfRangeException(nameof(businessDays));
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(openedAtUtc, DateTimeKind.Utc), zone);
        for (var remaining = businessDays; remaining > 0;)
        {
            local = local.AddDays(1);
            if (local.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) &&
                !holidays.Contains(DateOnly.FromDateTime(local))) remaining--;
        }
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), zone);
    }
}
