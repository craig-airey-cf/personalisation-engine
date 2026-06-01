using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PersonalisationEngine.Api.Data;
using PersonalisationEngine.Api.DTOs.Recommendations;
using PersonalisationEngine.Api.Models;
using PersonalisationEngine.Api.Services;
using PersonalisationEngine.Api.Services.Claude;
using PersonalisationEngine.Api.Services.Rules;

namespace PersonalisationEngine.Tests.Recommendations;

public class RecommendationServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IRulesEngine> _rulesEngine = new();
    private readonly Mock<IClaudeClient> _claude = new();

    public RecommendationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _db.Players.Add(new Player
        {
            PlayerId = "TEST01",
            MostBetSport = "Football",
            FavouriteTeam = "Scotland",
            MostBetType = "Bet Builder",
            RiskLevel = RiskLevel.Low,
            LastLoginDaysAgo = 5
        });
        _db.SaveChanges();
    }

    private RecommendationService CreateService() =>
        new(_db, _rulesEngine.Object, _claude.Object,
            NullLogger<RecommendationService>.Instance);

    [Fact]
    public async Task SafePlayer_CallsClaudeAndReturnsHeadline()
    {
        _rulesEngine.Setup(r => r.Evaluate(It.IsAny<Player>()))
            .Returns(new RulesResult(true, null, ["Sport: Football"]));

        _claude.Setup(c => c.GenerateRecommendationAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClaudeRecommendation(
                "Scotland are back in action",
                "Check your favourite team's latest markets.",
                "Content",
                "Favourite team matched",
                true));

        var result = await CreateService().GenerateAsync("TEST01");

        Assert.True(result.SafeToShow);
        Assert.Equal("Scotland are back in action", result.Headline);
        Assert.Equal("Content", result.RecommendationType);
        _claude.Verify(c => c.GenerateRecommendationAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BlockedPlayer_DoesNotCallClaude()
    {
        _rulesEngine.Setup(r => r.Evaluate(It.IsAny<Player>()))
            .Returns(new RulesResult(false, "High risk level", []));

        var result = await CreateService().GenerateAsync("TEST01");

        Assert.False(result.SafeToShow);
        Assert.Equal("High risk level", result.BlockReason);
        _claude.Verify(c => c.GenerateRecommendationAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ClaudeUnavailable_ReturnsResultWithNullCopy()
    {
        _rulesEngine.Setup(r => r.Evaluate(It.IsAny<Player>()))
            .Returns(new RulesResult(true, null, ["Sport: Football"]));

        _claude.Setup(c => c.GenerateRecommendationAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClaudeRecommendation?)null);

        var result = await CreateService().GenerateAsync("TEST01");

        Assert.True(result.SafeToShow);
        Assert.Null(result.Headline);
        Assert.Null(result.Message);
    }

    [Fact]
    public async Task UnknownPlayer_ThrowsNotFoundException()
    {
        _rulesEngine.Setup(r => r.Evaluate(It.IsAny<Player>()))
            .Returns(new RulesResult(true, null, []));

        await Assert.ThrowsAsync<Api.Middleware.NotFoundException>(
            () => CreateService().GenerateAsync("NOBODY"));
    }

    [Fact]
    public async Task Recommendation_IsPersistedToDatabase()
    {
        _rulesEngine.Setup(r => r.Evaluate(It.IsAny<Player>()))
            .Returns(new RulesResult(true, null, ["Sport: Football"]));

        _claude.Setup(c => c.GenerateRecommendationAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClaudeRecommendation("Headline", "Msg", "Market", "Reason", true));

        await CreateService().GenerateAsync("TEST01");

        var saved = await _db.Recommendations.FirstOrDefaultAsync(r => r.PlayerId == "TEST01");
        Assert.NotNull(saved);
        Assert.Equal("Headline", saved.Headline);
    }

    public void Dispose() => _db.Dispose();
}
