namespace PersonalisationEngine.Api.DTOs.Recommendations;

public record RulesResult(
    bool SafeToShow,
    string? BlockReason,
    IReadOnlyList<string> SafeOptions
);
