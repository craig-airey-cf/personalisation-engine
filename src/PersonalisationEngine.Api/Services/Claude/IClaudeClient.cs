using PersonalisationEngine.Api.DTOs.Recommendations;

namespace PersonalisationEngine.Api.Services.Claude;

public interface IClaudeClient
{
    Task<ClaudeRecommendation?> GenerateRecommendationAsync(
        string playerJson,
        IReadOnlyList<string> safeOptions,
        CancellationToken ct = default);
}
