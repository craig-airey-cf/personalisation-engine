using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PersonalisationEngine.Api.Data;
using PersonalisationEngine.Api.DTOs.Recommendations;
using PersonalisationEngine.Api.Middleware;
using PersonalisationEngine.Api.Models;
using PersonalisationEngine.Api.Services.Claude;
using PersonalisationEngine.Api.Services.Rules;

namespace PersonalisationEngine.Api.Services;

public class RecommendationService(
    AppDbContext db,
    IRulesEngine rulesEngine,
    IClaudeClient claudeClient,
    ILogger<RecommendationService> logger) : IRecommendationService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<RecommendationResponse> GenerateAsync(string playerId)
    {
        var player = await db.Players.AsNoTracking()
            .SingleOrDefaultAsync(p => p.PlayerId == playerId)
            ?? throw new NotFoundException($"Player '{playerId}' not found");

        var rules = rulesEngine.Evaluate(player);

        var recommendation = new Recommendation
        {
            PlayerId = player.PlayerId,
            SafeToShow = rules.SafeToShow,
            BlockReason = rules.BlockReason
        };

        if (rules.SafeToShow)
        {
            logger.LogInformation("Player {PlayerId} passed guardrails — calling Claude", playerId);
            var playerJson = JsonSerializer.Serialize(player, SerializerOptions);
            var claude = await claudeClient.GenerateRecommendationAsync(playerJson, rules.SafeOptions);

            if (claude is not null)
            {
                recommendation.Headline = claude.Headline;
                recommendation.Message = claude.Message;
                recommendation.RecommendationType = claude.RecommendationType;
                recommendation.Reason = claude.Reason;
            }
            else
            {
                logger.LogWarning("Claude returned null for player {PlayerId} — degraded mode", playerId);
            }
        }
        else
        {
            logger.LogInformation("Player {PlayerId} blocked by guardrails: {Reason}",
                playerId, rules.BlockReason);
        }

        db.Recommendations.Add(recommendation);
        await db.SaveChangesAsync();

        return ToResponse(recommendation, rules.SafeOptions);
    }

    private static RecommendationResponse ToResponse(
        Recommendation r, IReadOnlyList<string> safeOptions) => new(
        r.Id,
        r.PlayerId,
        r.SafeToShow,
        r.BlockReason,
        safeOptions,
        r.RecommendationType,
        r.Headline,
        r.Message,
        r.Reason,
        r.CreatedAt
    );
}
