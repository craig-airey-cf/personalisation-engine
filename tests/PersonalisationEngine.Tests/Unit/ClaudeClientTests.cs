using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PersonalisationEngine.Api.Services.Claude;

namespace PersonalisationEngine.Tests.Unit;

public class ClaudeClientTests
{
    private static ClaudeClient BuildClient(
        HttpClient http,
        string? apiKey = "real-key",
        string model = "claude-haiku-4-5-20251001",
        int maxTokens = 512)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Anthropic:ApiKey"] = apiKey,
                ["Anthropic:Model"] = model,
                ["Anthropic:MaxTokens"] = maxTokens.ToString()
            })
            .Build();

        return new ClaudeClient(http, config, NullLogger<ClaudeClient>.Instance);
    }

    // Stub mode

    [Fact]
    public async Task StubMode_NoApiKey_ReturnsCannedRecommendation()
    {
        var client = BuildClient(new HttpClient(), apiKey: null);

        var result = await client.GenerateRecommendationAsync("{}", ["Sport: Football"]);

        Assert.NotNull(result);
        Assert.Equal("Check out today's markets", result.Headline);
        Assert.True(result.SafeToShow);
    }

    [Fact]
    public async Task StubMode_PlaceholderApiKey_ReturnsCannedRecommendation()
    {
        var client = BuildClient(new HttpClient(), apiKey: "REPLACE_ME");

        var result = await client.GenerateRecommendationAsync("{}", []);

        Assert.NotNull(result);
        Assert.Equal("Check out today's markets", result.Headline);
    }

    // Live mode — successful response

    [Fact]
    public async Task LiveMode_SuccessfulResponse_ReturnsDeserializedRecommendation()
    {
        var claudeJson = JsonSerializer.Serialize(new
        {
            headline = "Scotland are back",
            message = "Great markets today.",
            recommendationType = "Content",
            reason = "Favourite team matched",
            safeToShow = true
        });

        var apiResponse = JsonSerializer.Serialize(new
        {
            content = new[] { new { type = "text", text = claudeJson } }
        });

        var handler = new MockHttpHandler(HttpStatusCode.OK, apiResponse);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.com") };
        var client = BuildClient(http);

        var result = await client.GenerateRecommendationAsync("{}", ["Sport: Football"]);

        Assert.NotNull(result);
        Assert.Equal("Scotland are back", result.Headline);
        Assert.Equal("Content", result.RecommendationType);
        Assert.True(result.SafeToShow);
    }

    // Live mode — empty content array

    [Fact]
    public async Task LiveMode_EmptyContentArray_ReturnsNull()
    {
        var apiResponse = JsonSerializer.Serialize(new { content = Array.Empty<object>() });
        var handler = new MockHttpHandler(HttpStatusCode.OK, apiResponse);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.com") };
        var client = BuildClient(http);

        var result = await client.GenerateRecommendationAsync("{}", []);

        Assert.Null(result);
    }

    // Live mode — API returns 5xx

    [Fact]
    public async Task LiveMode_HttpError_ReturnsNull()
    {
        var handler = new MockHttpHandler(HttpStatusCode.InternalServerError, "error");
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.com") };
        var client = BuildClient(http);

        var result = await client.GenerateRecommendationAsync("{}", []);

        Assert.Null(result);
    }

    // Live mode — Claude returns malformed JSON

    [Fact]
    public async Task LiveMode_MalformedJson_ReturnsNull()
    {
        var apiResponse = JsonSerializer.Serialize(new
        {
            content = new[] { new { type = "text", text = "not valid json {{{" } }
        });

        var handler = new MockHttpHandler(HttpStatusCode.OK, apiResponse);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.com") };
        var client = BuildClient(http);

        var result = await client.GenerateRecommendationAsync("{}", []);

        Assert.Null(result);
    }

    // Live mode — network failure (handler throws)

    [Fact]
    public async Task LiveMode_NetworkFailure_ReturnsNull()
    {
        var handler = new ThrowingHttpHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.com") };
        var client = BuildClient(http);

        var result = await client.GenerateRecommendationAsync("{}", []);

        Assert.Null(result);
    }

    // Live mode — request cancelled via CancellationToken

    [Fact]
    public async Task LiveMode_CancelledRequest_ReturnsNull()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var handler = new MockHttpHandler(HttpStatusCode.OK, "{}");
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.com") };
        var client = BuildClient(http);

        var result = await client.GenerateRecommendationAsync("{}", [], cts.Token);

        Assert.Null(result);
    }

    // --- Helpers ---

    private sealed class MockHttpHandler(HttpStatusCode status, string content)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
    }

    private sealed class ThrowingHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("Simulated network failure");
    }
}
