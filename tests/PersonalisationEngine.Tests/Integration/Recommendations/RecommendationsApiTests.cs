using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PersonalisationEngine.Api.Data;
using PersonalisationEngine.Api.DTOs.Recommendations;
using PersonalisationEngine.Tests.Infrastructure;

namespace PersonalisationEngine.Tests.Integration.Recommendations;

public sealed class RecommendationsApiTests(PostgresContainerFixture db) : IntegrationTestBase(db)
{
    // Safe player — guardrails pass, stub Claude returns copy

    [Fact]
    public async Task Generate_SafePlayer_Returns200WithCopy()
    {
        var response = await Client.PostAsync("/api/recommendations/P001", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rec = await response.Content.ReadFromJsonAsync<RecommendationResponse>();

        Assert.NotNull(rec);
        Assert.True(rec.SafeToShow);
        Assert.Null(rec.BlockReason);
        Assert.Equal(StubClaudeClient.DefaultResponse.Headline, rec.Headline);
        Assert.Equal(StubClaudeClient.DefaultResponse.Message, rec.Message);
        Assert.Equal(StubClaudeClient.DefaultResponse.RecommendationType, rec.RecommendationType);
        Assert.NotEmpty(rec.SafeOptions);
    }

    [Fact]
    public async Task Generate_SafePlayer_SafeOptionsContainSportAndBetType()
    {
        var rec = await PostRecommendationAsync("P001");

        Assert.Contains("Sport: Football", rec.SafeOptions);
        Assert.Contains("Bet type: Bet Builder", rec.SafeOptions);
        Assert.Contains("Team: Scotland", rec.SafeOptions);
    }

    [Fact]
    public async Task Generate_SafePlayer_RecommendationIsPersistedToDatabase()
    {
        await Client.PostAsync("/api/recommendations/P001", null);

        var saved = await QueryLastRecommendationAsync("P001");
        Assert.NotNull(saved);
        Assert.True(saved.SafeToShow);
        Assert.Equal(StubClaudeClient.DefaultResponse.Headline, saved.Headline);
    }

    // Guardrail: high-risk player

    [Fact]
    public async Task Generate_HighRiskPlayer_Returns200Blocked()
    {
        var rec = await PostRecommendationAsync("P005");

        Assert.False(rec.SafeToShow);
        Assert.Equal("High risk level", rec.BlockReason);
        Assert.Null(rec.Headline);
        Assert.Empty(rec.SafeOptions);
    }

    // Guardrail: self-excluded player

    [Fact]
    public async Task Generate_SelfExcludedPlayer_Returns200Blocked()
    {
        var rec = await PostRecommendationAsync("P006");

        Assert.False(rec.SafeToShow);
        Assert.Equal("Player self-excluded", rec.BlockReason);
    }

    // Guardrail: cooling-off player

    [Fact]
    public async Task Generate_CoolingOffPlayer_Returns200Blocked()
    {
        var rec = await PostRecommendationAsync("P007");

        Assert.False(rec.SafeToShow);
        Assert.Equal("Player in cooling-off period", rec.BlockReason);
    }

    // Guardrail: inactive player

    [Fact]
    public async Task Generate_InactivePlayer_Returns200Blocked()
    {
        var rec = await PostRecommendationAsync("P008");

        Assert.False(rec.SafeToShow);
        Assert.Equal("Player inactive for more than 30 days", rec.BlockReason);
    }

    // Blocked recommendations are also persisted

    [Fact]
    public async Task Generate_BlockedPlayer_IsStillPersistedToDatabase()
    {
        await Client.PostAsync("/api/recommendations/P005", null);

        var saved = await QueryLastRecommendationAsync("P005");
        Assert.NotNull(saved);
        Assert.False(saved.SafeToShow);
        Assert.Equal("High risk level", saved.BlockReason);
        Assert.Null(saved.Headline);
    }

    // Unknown player

    [Fact]
    public async Task Generate_UnknownPlayer_Returns404()
    {
        var response = await Client.PostAsync("/api/recommendations/NOBODY", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Unauthenticated request

    [Fact]
    public async Task Generate_MissingApiKey_Returns401()
    {
        using var unauthClient = Factory.CreateClient();
        var response = await unauthClient.PostAsync("/api/recommendations/P001", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Multiple recommendations for the same player are all stored

    [Fact]
    public async Task Generate_CalledTwice_CreatesTwoRecords()
    {
        await Client.PostAsync("/api/recommendations/P001", null);
        await Client.PostAsync("/api/recommendations/P001", null);

        using var scope = Factory.Services.CreateScope();
        var dbCtx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await dbCtx.Recommendations.CountAsync(r => r.PlayerId == "P001");

        Assert.Equal(2, count);
    }

    private async Task<RecommendationResponse> PostRecommendationAsync(string playerId)
    {
        var response = await Client.PostAsync($"/api/recommendations/{playerId}", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RecommendationResponse>())!;
    }

    private async Task<Api.Models.Recommendation?> QueryLastRecommendationAsync(string playerId)
    {
        using var scope = Factory.Services.CreateScope();
        var dbCtx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbCtx.Recommendations
            .Where(r => r.PlayerId == playerId)
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync();
    }
}
