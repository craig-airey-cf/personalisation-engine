using Microsoft.AspNetCore.Mvc;
using PersonalisationEngine.Api.Services;

namespace PersonalisationEngine.Api.Controllers;

[ApiController]
[Route("api/recommendations")]
public class RecommendationsController(IRecommendationService recommendationService) : ControllerBase
{
    [HttpPost("{playerId}")]
    public async Task<IActionResult> Generate(string playerId) =>
        Ok(await recommendationService.GenerateAsync(playerId));
}
