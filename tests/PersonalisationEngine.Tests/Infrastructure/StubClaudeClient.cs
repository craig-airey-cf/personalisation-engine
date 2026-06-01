using PersonalisationEngine.Api.DTOs.Recommendations;
using PersonalisationEngine.Api.Services.Claude;

namespace PersonalisationEngine.Tests.Infrastructure;

/// <summary>
/// Returns a deterministic recommendation so integration tests are not flaky
/// and don't require a real Anthropic API key.
/// </summary>
public sealed class StubClaudeClient : IClaudeClient
{
    public static readonly ClaudeRecommendation DefaultResponse = new(
        "Test Headline",
        "Test message body.",
        "Content",
        "Stub reason",
        true
    );

    public Task<ClaudeRecommendation?> GenerateRecommendationAsync(
        string playerJson, IReadOnlyList<string> safeOptions, CancellationToken ct = default) =>
        Task.FromResult<ClaudeRecommendation?>(DefaultResponse);
}
