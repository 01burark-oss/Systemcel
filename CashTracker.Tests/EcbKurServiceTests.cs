using System.Net;
using System.Net.Http;
using System.Text;
using CashTracker.Infrastructure.Services;
using Xunit;

namespace CashTracker.Tests;

public sealed class EcbKurServiceTests
{
    [Fact]
    public async Task GetLatestAsync_ConvertsEuroBaseToTryAndCachesBulletin()
    {
        var date = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        using var handler = new FeedHandler(Feed(date, "<Cube currency='TRY' rate='55.7975'/><Cube currency='USD' rate='1.1403'/><Cube currency='JPY' rate='179.70'/>"));
        using var client = new HttpClient(handler);
        var service = new EcbKurService(client);

        var first = await service.GetLatestAsync();
        var second = await service.GetLatestAsync();

        Assert.Same(first, second);
        Assert.Equal(1, handler.RequestCount);
        Assert.Equal(55.7975m, first.Kurlar.Single(x => x.ParaBirimi == "EUR").Kur);
        Assert.Equal(48.932299m, first.Kurlar.Single(x => x.ParaBirimi == "USD").Kur);
        Assert.Equal(0.310504m, first.Kurlar.Single(x => x.ParaBirimi == "JPY").Kur);
    }

    [Fact]
    public async Task GetLatestAsync_RejectsOutdatedBulletin()
    {
        using var handler = new FeedHandler(Feed("2020-01-01", "<Cube currency='TRY' rate='55'/><Cube currency='USD' rate='1.1'/>"));
        using var client = new HttpClient(handler);

        await Assert.ThrowsAsync<InvalidDataException>(() => new EcbKurService(client).GetLatestAsync());
    }

    [Fact]
    public async Task GetLatestAsync_RejectsMissingTryRate()
    {
        var date = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        using var handler = new FeedHandler(Feed(date, "<Cube currency='USD' rate='1.1'/>"));
        using var client = new HttpClient(handler);

        await Assert.ThrowsAsync<InvalidDataException>(() => new EcbKurService(client).GetLatestAsync());
    }

    [Fact]
    public async Task GetLatestAsync_RejectsUnsafeXml()
    {
        using var handler = new FeedHandler("<!DOCTYPE x [<!ENTITY a SYSTEM 'file:///etc/passwd'>]><Envelope>&a;</Envelope>");
        using var client = new HttpClient(handler);

        await Assert.ThrowsAsync<System.Xml.XmlException>(() => new EcbKurService(client).GetLatestAsync());
    }

    private static string Feed(string date, string currencies) =>
        $"<gesmes:Envelope xmlns:gesmes='http://www.gesmes.org/xml/2002-08-01' xmlns='http://www.ecb.int/vocabulary/2002-08-01/eurofxref'><Cube><Cube time='{date}'>{currencies}</Cube></Cube></gesmes:Envelope>";

    private sealed class FeedHandler(string body) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            Assert.Equal("https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml", request.RequestUri?.ToString());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/xml")
            });
        }
    }
}
