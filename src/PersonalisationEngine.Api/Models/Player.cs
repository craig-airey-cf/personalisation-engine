namespace PersonalisationEngine.Api.Models;

public class Player
{
    public int Id { get; set; }
    public string PlayerId { get; set; } = null!;
    public int DaysSinceJoined { get; set; }
    public string MostBetSport { get; set; } = null!;
    public string? FavouriteTeam { get; set; }
    public string MostBetType { get; set; } = null!;
    public decimal AverageStake { get; set; }
    public int LastLoginDaysAgo { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public bool IsSelfExcluded { get; set; }
    public bool IsInCoolingOff { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum RiskLevel { Low, Medium, High }
