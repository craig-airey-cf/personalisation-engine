using PersonalisationEngine.Api.Models;
using PersonalisationEngine.Api.Services.Rules;

namespace PersonalisationEngine.Tests.Rules;

public class RulesEngineTests
{
    private readonly RulesEngine _engine = new();

    private static Player LowRiskActive() => new()
    {
        PlayerId = "P001",
        MostBetSport = "Football",
        FavouriteTeam = "Scotland",
        MostBetType = "Bet Builder",
        LastLoginDaysAgo = 6,
        RiskLevel = RiskLevel.Low,
        IsSelfExcluded = false,
        IsInCoolingOff = false
    };

    [Fact]
    public void LowRiskActivePlayer_IsAllowed()
    {
        var result = _engine.Evaluate(LowRiskActive());

        Assert.True(result.SafeToShow);
        Assert.Null(result.BlockReason);
        Assert.NotEmpty(result.SafeOptions);
    }

    [Fact]
    public void SelfExcludedPlayer_IsBlocked()
    {
        var player = LowRiskActive();
        player.IsSelfExcluded = true;

        var result = _engine.Evaluate(player);

        Assert.False(result.SafeToShow);
        Assert.Equal("Player self-excluded", result.BlockReason);
        Assert.Empty(result.SafeOptions);
    }

    [Fact]
    public void CoolingOffPlayer_IsBlocked()
    {
        var player = LowRiskActive();
        player.IsInCoolingOff = true;

        var result = _engine.Evaluate(player);

        Assert.False(result.SafeToShow);
        Assert.Equal("Player in cooling-off period", result.BlockReason);
    }

    [Fact]
    public void HighRiskPlayer_IsBlocked()
    {
        var player = LowRiskActive();
        player.RiskLevel = RiskLevel.High;

        var result = _engine.Evaluate(player);

        Assert.False(result.SafeToShow);
        Assert.Equal("High risk level", result.BlockReason);
    }

    [Fact]
    public void InactivePlayer_Over30Days_IsBlocked()
    {
        var player = LowRiskActive();
        player.LastLoginDaysAgo = 31;

        var result = _engine.Evaluate(player);

        Assert.False(result.SafeToShow);
        Assert.Equal("Player inactive for more than 30 days", result.BlockReason);
    }

    [Fact]
    public void InactivePlayer_Exactly30Days_IsAllowed()
    {
        var player = LowRiskActive();
        player.LastLoginDaysAgo = 30;

        var result = _engine.Evaluate(player);

        Assert.True(result.SafeToShow);
    }

    [Fact]
    public void SafeOptions_IncludeSportAndBetType()
    {
        var result = _engine.Evaluate(LowRiskActive());

        Assert.Contains("Sport: Football", result.SafeOptions);
        Assert.Contains("Bet type: Bet Builder", result.SafeOptions);
    }

    [Fact]
    public void SafeOptions_IncludeFavouriteTeamWhenSet()
    {
        var result = _engine.Evaluate(LowRiskActive());

        Assert.Contains("Team: Scotland", result.SafeOptions);
    }

    [Fact]
    public void SafeOptions_ExcludesTeamWhenNull()
    {
        var player = LowRiskActive();
        player.FavouriteTeam = null;

        var result = _engine.Evaluate(player);

        Assert.DoesNotContain(result.SafeOptions, o => o.StartsWith("Team:"));
    }

    [Fact]
    public void SelfExclusion_TakesPrecedenceOverHighRisk()
    {
        var player = LowRiskActive();
        player.IsSelfExcluded = true;
        player.RiskLevel = RiskLevel.High;

        var result = _engine.Evaluate(player);

        Assert.Equal("Player self-excluded", result.BlockReason);
    }
}
