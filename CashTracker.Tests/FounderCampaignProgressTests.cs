using CashTracker.Core.Models;
using Xunit;

namespace CashTracker.Tests;

public sealed class FounderCampaignProgressTests
{
    [Fact]
    public void PaidAndReservedSlots_AreSeparatedForDisplayAndAvailability()
    {
        var progress = SubscriptionPlanCatalog.CreateFounderCampaignProgress(17, 2);

        Assert.Equal(50, progress.TotalSlots);
        Assert.Equal(17, progress.WonSlots);
        Assert.Equal(19, progress.OccupiedSlots);
        Assert.Equal(31, progress.RemainingSlots);
        Assert.Equal(34, progress.FillPercentage);
        Assert.True(progress.IsActive);
    }

    [Fact]
    public void FullCampaign_ClosesEvenWhenLastSlotsAreReserved()
    {
        var progress = SubscriptionPlanCatalog.CreateFounderCampaignProgress(48, 2);

        Assert.Equal(0, progress.RemainingSlots);
        Assert.Equal(96, progress.FillPercentage);
        Assert.False(progress.IsActive);
    }

    [Fact]
    public void InvalidCounts_AreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SubscriptionPlanCatalog.CreateFounderCampaignProgress(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SubscriptionPlanCatalog.CreateFounderCampaignProgress(0, -1));
    }
}
