using PersonalisationEngine.Api.DTOs.Recommendations;
using PersonalisationEngine.Api.Models;

namespace PersonalisationEngine.Api.Services.Rules;

public class RulesEngine : IRulesEngine
{
    public RulesResult Evaluate(Player player)
    {
        if (player.IsSelfExcluded)
            return Blocked("Player self-excluded");

        if (player.IsInCoolingOff)
            return Blocked("Player in cooling-off period");

        if (player.RiskLevel == RiskLevel.High)
            return Blocked("High risk level");

        if (player.LastLoginDaysAgo > 30)
            return Blocked("Player inactive for more than 30 days");

        return new RulesResult(true, null, BuildSafeOptions(player));
    }

    private static RulesResult Blocked(string reason) =>
        new(false, reason, []);

    private static IReadOnlyList<string> BuildSafeOptions(Player player)
    {
        var options = new List<string>
        {
            $"Sport: {player.MostBetSport}",
            $"Bet type: {player.MostBetType}"
        };

        if (!string.IsNullOrWhiteSpace(player.FavouriteTeam))
            options.Add($"Team: {player.FavouriteTeam}");

        return options;
    }
}
