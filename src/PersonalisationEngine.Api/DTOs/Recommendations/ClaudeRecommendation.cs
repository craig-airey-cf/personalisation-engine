using System.Text.Json.Serialization;

namespace PersonalisationEngine.Api.DTOs.Recommendations;

public record ClaudeRecommendation(
    [property: JsonPropertyName("headline")] string Headline,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("recommendationType")] string RecommendationType,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("safeToShow")] bool SafeToShow
);
