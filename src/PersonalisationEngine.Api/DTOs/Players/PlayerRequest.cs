using System.ComponentModel.DataAnnotations;
using PersonalisationEngine.Api.Models;

namespace PersonalisationEngine.Api.DTOs.Players;

public record PlayerRequest(
    [Required, StringLength(50)] string PlayerId,
    [Range(0, int.MaxValue)] int DaysSinceJoined,
    [Required, StringLength(100)] string MostBetSport,
    [StringLength(100)] string? FavouriteTeam,
    [Required, StringLength(100)] string MostBetType,
    [Range(0, 100000)] decimal AverageStake,
    [Range(0, int.MaxValue)] int LastLoginDaysAgo,
    RiskLevel RiskLevel,
    bool IsSelfExcluded,
    bool IsInCoolingOff
);
