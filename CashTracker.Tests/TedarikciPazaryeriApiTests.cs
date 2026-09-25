using Systemcel.Api.Api;
using Xunit;

namespace CashTracker.Tests;

public sealed class TedarikciPazaryeriApiTests
{
    [Fact]
    public void DisputeDurations_UsesLatestDisputeStartAndComplaintTimeForFirstReply()
    {
        var complaintCreatedAt = new DateTime(2026, 9, 24, 8, 0, 0, DateTimeKind.Utc);
        var latestDisputeAt = complaintCreatedAt.AddHours(2);
        var repliedAt = complaintCreatedAt.AddHours(4.26);
        var now = complaintCreatedAt.AddHours(5.24);
        var complaints = new[] { new TedarikciPazaryeriApi.SikayetZamanSatiri(7, 12, complaintCreatedAt, repliedAt) };

        var result = TedarikciPazaryeriApi.HesaplaItirazSureleri(
            latestDisputeAt, complaints, 7, now);

        Assert.Equal(3.2m, result.ItirazYasiSaat);
        Assert.Equal(4.3m, result.TedarikciIlkYanitSuresiSaat);
    }

    [Fact]
    public void DisputeDurations_FallsBackToComplaintCreatedAtWhenNoDisputeHistoryExists()
    {
        var complaintCreatedAt = new DateTime(2026, 9, 24, 8, 0, 0, DateTimeKind.Utc);

        var complaints = new[] { new TedarikciPazaryeriApi.SikayetZamanSatiri(7, 12, complaintCreatedAt, null) };
        var result = TedarikciPazaryeriApi.HesaplaItirazSureleri(
            null, complaints, 7, complaintCreatedAt.AddHours(1.56));

        Assert.Equal(1.6m, result.ItirazYasiSaat);
        Assert.Null(result.TedarikciIlkYanitSuresiSaat);
    }

    [Fact]
    public void DisputeDurations_ClampsNegativeValuesAndLeavesMissingStartNull()
    {
        var future = new DateTime(2026, 9, 25, 8, 0, 0, DateTimeKind.Utc);
        var now = future.AddHours(-2);

        var noMatchingComplaint = new[] { new TedarikciPazaryeriApi.SikayetZamanSatiri(8, 1, future.AddHours(-3), future.AddHours(-1)) };
        var result = TedarikciPazaryeriApi.HesaplaItirazSureleri(future, noMatchingComplaint, 7, now);
        var missing = TedarikciPazaryeriApi.HesaplaItirazSureleri(null, [], 7, now);

        Assert.Equal(0m, result.ItirazYasiSaat);
        Assert.Null(result.TedarikciIlkYanitSuresiSaat);
        Assert.Null(missing.ItirazYasiSaat);
        Assert.Null(missing.TedarikciIlkYanitSuresiSaat);
    }

    [Fact]
    public void DisputeDurations_FallsBackToOldestComplaintAndUsesEarliestAnsweredComplaintForReplyTime()
    {
        var firstCreatedAt = new DateTime(2026, 9, 24, 8, 0, 0, DateTimeKind.Utc);
        var complaints = new[]
        {
            new TedarikciPazaryeriApi.SikayetZamanSatiri(7, 22, firstCreatedAt.AddHours(-1), null),
            new TedarikciPazaryeriApi.SikayetZamanSatiri(7, 23, firstCreatedAt.AddHours(1), firstCreatedAt.AddHours(5)),
            new TedarikciPazaryeriApi.SikayetZamanSatiri(7, 21, firstCreatedAt.AddHours(2), firstCreatedAt.AddHours(3)),
            new TedarikciPazaryeriApi.SikayetZamanSatiri(8, 10, firstCreatedAt.AddHours(-4), firstCreatedAt.AddHours(-3))
        };

        var result = TedarikciPazaryeriApi.HesaplaItirazSureleri(
            null, complaints, 7, firstCreatedAt.AddHours(6));

        Assert.Equal(7m, result.ItirazYasiSaat);
        Assert.Equal(1m, result.TedarikciIlkYanitSuresiSaat);
    }
}
