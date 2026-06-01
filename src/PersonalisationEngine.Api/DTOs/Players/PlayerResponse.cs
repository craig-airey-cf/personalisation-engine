using PersonalisationEngine.Api.Models;

namespace PersonalisationEngine.Api.DTOs.Players;

public record PlayerResponse(
    int Id,
    string PlayerId,
    int DaysSinceJoined,
    string MostBetSport,
    string? FavouriteTeam,
    string MostBetType,
    decimal AverageStake,
    int LastLoginDaysAgo,
    string RiskLevel,
    bool IsSelfExcluded,
    bool IsInCoolingOff,
    DateTime CreatedAt
)
{
    public static PlayerResponse From(Player p) => new(
        p.Id,
        p.PlayerId,
        p.DaysSinceJoined,
        p.MostBetSport,
        p.FavouriteTeam,
        p.MostBetType,
        p.AverageStake,
        p.LastLoginDaysAgo,
        p.RiskLevel.ToString(),
        p.IsSelfExcluded,
        p.IsInCoolingOff,
        p.CreatedAt
    );
}
