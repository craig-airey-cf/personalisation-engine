namespace PersonalisationEngine.Api.DTOs.Recommendations;

public record RecommendationResponse(
    int Id,
    string PlayerId,
    bool SafeToShow,
    string? BlockReason,
    IReadOnlyList<string> SafeOptions,
    string? RecommendationType,
    string? Headline,
    string? Message,
    string? Reason,
    DateTime CreatedAt
);
