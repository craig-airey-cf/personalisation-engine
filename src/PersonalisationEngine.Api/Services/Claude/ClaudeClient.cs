using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PersonalisationEngine.Api.DTOs.Recommendations;

namespace PersonalisationEngine.Api.Services.Claude;

public class ClaudeClient(
    HttpClient http,
    IConfiguration config,
    ILogger<ClaudeClient> logger) : IClaudeClient
{
    private readonly string _model = config["Anthropic:Model"] ?? "claude-haiku-4-5-20251001";
    private readonly int _maxTokens =
        int.TryParse(config["Anthropic:MaxTokens"], out var t) ? t : 512;
    private readonly bool _stubMode =
        string.IsNullOrWhiteSpace(config["Anthropic:ApiKey"]) ||
        config["Anthropic:ApiKey"] == "REPLACE_ME";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<ClaudeRecommendation?> GenerateRecommendationAsync(
        string playerJson,
        IReadOnlyList<string> safeOptions,
        CancellationToken ct = default)
    {
        if (_stubMode)
        {
            logger.LogInformation("Claude stub mode — returning canned recommendation");
            return new ClaudeRecommendation(
                "Check out today's markets",
                "We've got great markets lined up based on your betting history.",
                "Content",
                "Stub mode — no API key configured",
                true
            );
        }

        var systemPrompt = """
            You are a personalisation engine for an iGaming platform.
            Return ONLY valid JSON matching this exact schema, with no markdown or extra text:
            {
              "headline": "string",
              "message": "string",
              "recommendationType": "Content" | "Promotion" | "Market",
              "reason": "string",
              "safeToShow": true
            }
            """;

        var userPrompt = $"""
            Player profile: {playerJson}
            Safe recommendation options: {string.Join(", ", safeOptions)}
            Generate a personalised, friendly recommendation for this player.
            """;

        var request = new
        {
            model = _model,
            max_tokens = _maxTokens,
            system = systemPrompt,
            messages = new[] { new { role = "user", content = userPrompt } }
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await http.PostAsJsonAsync("/v1/messages", request, ct);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Claude API returned {StatusCode} after {ElapsedMs}ms — returning null",
                    (int)response.StatusCode, sw.ElapsedMilliseconds);
                return null;
            }

            var body = await response.Content.ReadFromJsonAsync<ClaudeApiResponse>(cancellationToken: ct)
                ?? throw new InvalidOperationException("Empty response from Claude API");

            if (body.Content.Count == 0)
                return null;

            var jsonText = body.Content[0].Text;
            return JsonSerializer.Deserialize<ClaudeRecommendation>(jsonText, JsonOptions);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Claude API call cancelled after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return null;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "Claude API call failed after {ElapsedMs}ms — returning null for graceful degradation",
                sw.ElapsedMilliseconds);
            return null;
        }
    }

    private sealed record ClaudeApiResponse(
        [property: JsonPropertyName("content")] List<ContentBlock> Content);

    private sealed record ContentBlock(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string Text);
}
