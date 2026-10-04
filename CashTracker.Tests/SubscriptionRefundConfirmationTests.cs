using System.Text;
using Systemcel.Api.Api;
using Xunit;

namespace CashTracker.Tests;

public sealed class SubscriptionRefundConfirmationTests
{
    [Theory]
    [InlineData("", true, false)]
    [InlineData("{\"canliIadeOnayi\":true}", true, true)]
    [InlineData("{\"canliIadeOnayi\":false}", false, false)]
    [InlineData("{\"canliIadeOnayi\":\"true\"}", false, false)]
    [InlineData("{\"canliIadeOnayi\":true,\"canliIadeOnayi\":false}", false, false)]
    [InlineData("{\"canliIadeOnayi\":true,\"tutar\":1}", false, false)]
    [InlineData("{}", false, false)]
    [InlineData("null", false, false)]
    [InlineData("[]", false, false)]
    [InlineData("{", false, false)]
    [InlineData(null, false, false)]
    public void ConfirmationBodyRejectsCoercionDuplicatesAndClientAmounts(string? body, bool valid, bool confirmed)
    {
        Assert.Equal(valid, SubscriptionRefundApi.TryParseLiveConfirmation(body, out var result));
        Assert.Equal(confirmed, result);
    }

    [Fact]
    public async Task ConfirmationRequestUsesBoundedBody()
    {
        await using var body = new MemoryStream(Encoding.UTF8.GetBytes(new string(' ', 4097)));
        Assert.False(SubscriptionRefundApi.TryParseLiveConfirmation(await BillingApi.ReadBoundedPaytrBodyAsync(body, default), out _));
    }
}
