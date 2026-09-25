using System.Net;
using System.Text.Json;
using CashTracker.Core.Models;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Xunit;

namespace CashTracker.Tests;

public sealed class JevDecisionServiceTests
{
    private static readonly IReadOnlyDictionary<string, JevChoiceQuestion> Questions =
        new Dictionary<string, JevChoiceQuestion>
        {
            ["nextAction"] = new("Choose the next action", new Dictionary<string, string?>
            {
                ["approve"] = "Approve",
                ["review"] = "Review"
            })
        };

    [Fact]
    public void Constructor_RejectsNonHttpsBaseUrl()
    {
        var handler = new RecordingHttpMessageHandler();
        var error = Assert.Throws<ArgumentException>(() => new JevDecisionService(
            new HttpClient(handler), new JevSettings { ApiKey = "key", BaseUrl = "http://jev.test/v1" }));

        Assert.Contains("HTTPS", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Unconfigured_ReturnsUnavailableWithoutHttp()
    {
        var handler = new RecordingHttpMessageHandler();
        var service = new JevDecisionService(new HttpClient(handler), new JevSettings { ApiKey = "" });

        var result = await service.ChooseAsync(new { total = 12 }, Questions);

        Assert.False(result["nextAction"].Available);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Configured_SendsEndpointAuthAndBody_AndParsesChoiceConfidenceAndProbabilities()
    {
        var handler = new CapturingHandler(RecordingHttpMessageHandler.OkJson("""{"model":"jev-test","answers":{"nextAction":{"choice":"approve","confidence":1.4,"probabilities":{"approve":0.8,"review":-0.2,"unknown":0.9}}}}"""));
        using var client = new HttpClient(handler);
        var service = new JevDecisionService(client, new JevSettings
        {
            ApiKey = "jev-secret",
            BaseUrl = "https://jev.example/v1",
            Model = "jev-model"
        });

        var result = await service.ChooseAsync(new { total = 12 }, Questions);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://jev.example/v1/systemone", request.Url);
        Assert.Contains("\"model\":\"jev-model\"", request.Body, StringComparison.Ordinal);
        Assert.Contains("\"nextAction\"", request.Body, StringComparison.Ordinal);
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("jev-secret", handler.AuthorizationParameter);
        using var body = JsonDocument.Parse(request.Body);
        var question = body.RootElement.GetProperty("questions").GetProperty("nextAction");
        Assert.Equal("choice", question.GetProperty("type").GetString());
        Assert.Equal("Approve", question.GetProperty("criteria").GetProperty("approve").GetString());
        Assert.Equal("Review", question.GetProperty("criteria").GetProperty("review").GetString());
        Assert.False(question.TryGetProperty("options", out _));

        var choice = result["nextAction"];
        Assert.True(choice.Available);
        Assert.Equal("approve", choice.Choice);
        Assert.Equal(1, choice.Confidence);
        Assert.Equal(0.8, choice.Probabilities["approve"]);
        Assert.Equal(0, choice.Probabilities["review"]);
        Assert.DoesNotContain("unknown", choice.Probabilities.Keys);
        Assert.Equal("jev-test", choice.Model);
    }

    [Fact]
    public async Task InvalidAnswerChoice_ReturnsUnavailable()
    {
        var handler = new RecordingHttpMessageHandler((_, _) =>
            RecordingHttpMessageHandler.OkJson("""{"model":"jev-test","answers":{"nextAction":{"choice":"reject","confidence":0.9}}}"""));
        var service = new JevDecisionService(new HttpClient(handler), new JevSettings { ApiKey = "key" });

        var result = await service.ChooseAsync(new { }, Questions);

        Assert.False(result["nextAction"].Available);
        Assert.Empty(result["nextAction"].Probabilities);
    }

    [Fact]
    public async Task UpstreamFailure_ReturnsUnavailableWithoutLeakingResponse()
    {
        const string secret = "provider-internal-secret";
        var handler = new RecordingHttpMessageHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent($"upstream failure: {secret}")
            });
        var service = new JevDecisionService(new HttpClient(handler), new JevSettings { ApiKey = "key" });

        var result = await service.ChooseAsync(new { }, Questions);

        Assert.False(result["nextAction"].Available);
        Assert.DoesNotContain(secret, result["nextAction"].Choice, StringComparison.Ordinal);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task TransportFailure_ReturnsUnavailable()
    {
        var handler = new RecordingHttpMessageHandler((_, _) => throw new HttpRequestException("connection failed"));
        var service = new JevDecisionService(new HttpClient(handler), new JevSettings { ApiKey = "key" });

        var result = await service.ChooseAsync(new { }, Questions);

        Assert.False(result["nextAction"].Available);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task InvalidJson_ReturnsUnavailable()
    {
        var handler = new RecordingHttpMessageHandler((_, _) => RecordingHttpMessageHandler.OkJson("{not-json"));
        var service = new JevDecisionService(new HttpClient(handler), new JevSettings { ApiKey = "key" });

        var result = await service.ChooseAsync(new { }, Questions);

        Assert.False(result["nextAction"].Available);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task ProviderTimeout_ReturnsUnavailable()
    {
        var handler = new RecordingHttpMessageHandler((_, _) => throw new OperationCanceledException());
        var service = new JevDecisionService(new HttpClient(handler), new JevSettings { ApiKey = "key" });

        var result = await service.ChooseAsync(new { }, Questions);

        Assert.False(result["nextAction"].Available);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task CallerCancellation_IsPropagated()
    {
        using var cancellation = new CancellationTokenSource();
        var handler = new RecordingHttpMessageHandler((_, _) =>
        {
            cancellation.Cancel();
            throw new OperationCanceledException(cancellation.Token);
        });
        var service = new JevDecisionService(new HttpClient(handler), new JevSettings { ApiKey = "key" });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.ChooseAsync(new { }, Questions, cancellation.Token));
    }

    private sealed class CapturingHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public string AuthorizationScheme { get; private set; } = string.Empty;
        public string AuthorizationParameter { get; private set; } = string.Empty;
        public List<RecordedHttpRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            AuthorizationScheme = request.Headers.Authorization?.Scheme ?? string.Empty;
            AuthorizationParameter = request.Headers.Authorization?.Parameter ?? string.Empty;
            Requests.Add(new RecordedHttpRequest(request.Method, request.RequestUri?.ToString() ?? string.Empty, body));
            return response;
        }
    }
}
