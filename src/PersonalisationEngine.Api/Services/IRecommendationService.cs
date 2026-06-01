using PersonalisationEngine.Api.DTOs.Recommendations;

namespace PersonalisationEngine.Api.Services;

public interface IRecommendationService
{
    Task<RecommendationResponse> GenerateAsync(string playerId);
}
