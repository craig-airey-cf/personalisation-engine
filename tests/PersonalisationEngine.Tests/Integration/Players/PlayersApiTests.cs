using System.Net;
using System.Net.Http.Json;
using PersonalisationEngine.Api.DTOs.Players;
using PersonalisationEngine.Tests.Infrastructure;

namespace PersonalisationEngine.Tests.Integration.Players;

public sealed class PlayersApiTests(PostgresContainerFixture db) : IntegrationTestBase(db)
{
    // GET /api/players

    [Fact]
    public async Task GetAll_ReturnsAllSeededPlayers()
    {
        var players = await GetJsonAsync<List<PlayerResponse>>("/api/players");

        Assert.Equal(5, players.Count);
        Assert.Contains(players, p => p.PlayerId == "P001");
    }

    // GET /api/players/{playerId}

    [Fact]
    public async Task GetById_KnownPlayer_ReturnsCorrectProfile()
    {
        var player = await GetJsonAsync<PlayerResponse>("/api/players/P001");

        Assert.Equal("P001", player.PlayerId);
        Assert.Equal("Football", player.MostBetSport);
        Assert.Equal("Scotland", player.FavouriteTeam);
        Assert.Equal("Low", player.RiskLevel);
    }

    [Fact]
    public async Task GetById_UnknownPlayer_Returns404()
    {
        var response = await Client.GetAsync("/api/players/NOBODY");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Contains("NOBODY", body!.Error);
    }

    // POST /api/players

    [Fact]
    public async Task Create_ValidPlayer_Returns201WithLocation()
    {
        var request = new PlayerRequest(
            "NEW01", 30, "Tennis", null, "Match Winner",
            8.00m, 2, Api.Models.RiskLevel.Low, false, false);

        var response = await Client.PostAsJsonAsync("/api/players", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<PlayerResponse>();
        Assert.Equal("NEW01", created!.PlayerId);
        Assert.Equal("Tennis", created.MostBetSport);
    }

    [Fact]
    public async Task Create_DuplicatePlayerId_Returns409()
    {
        var request = new PlayerRequest(
            "P001", 30, "Tennis", null, "Match Winner",
            8.00m, 2, Api.Models.RiskLevel.Low, false, false);

        var response = await Client.PostAsJsonAsync("/api/players", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_MissingRequiredField_Returns400()
    {
        // Send JSON with missing PlayerId
        var response = await Client.PostAsJsonAsync("/api/players",
            new { DaysSinceJoined = 10, MostBetType = "Single", AverageStake = 5 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // PUT /api/players/{playerId}

    [Fact]
    public async Task Update_ExistingPlayer_ReturnsUpdatedProfile()
    {
        var request = new PlayerRequest(
            "P001", 200, "Rugby", "Scotland", "Handicap",
            20.00m, 1, Api.Models.RiskLevel.Medium, false, false);

        var response = await Client.PutAsJsonAsync("/api/players/P001", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<PlayerResponse>();
        Assert.Equal("Rugby", updated!.MostBetSport);
        Assert.Equal("Medium", updated.RiskLevel);
    }

    [Fact]
    public async Task Update_UnknownPlayer_Returns404()
    {
        var request = new PlayerRequest(
            "NOBODY", 30, "Tennis", null, "Single",
            5m, 1, Api.Models.RiskLevel.Low, false, false);

        var response = await Client.PutAsJsonAsync("/api/players/NOBODY", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_PlayerIdMismatch_Returns400()
    {
        var request = new PlayerRequest(
            "P999", 200, "Rugby", null, "Handicap",
            20m, 1, Api.Models.RiskLevel.Low, false, false);

        var response = await Client.PutAsJsonAsync("/api/players/P001", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // DELETE /api/players/{playerId}

    [Fact]
    public async Task Delete_ExistingPlayer_Returns204AndRemovesPlayer()
    {
        var deleteResponse = await Client.DeleteAsync("/api/players/P008");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await Client.GetAsync("/api/players/P008");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownPlayer_Returns404()
    {
        var response = await Client.DeleteAsync("/api/players/NOBODY");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ConcurrentDuplicatePlayerId_Returns409NotAnd500()
    {
        // Bypass the application-level AnyAsync guard by racing two requests simultaneously.
        // The unique DB constraint should catch the duplicate and the middleware must map it to 409.
        var request = new PlayerRequest(
            "RACE01", 10, "Tennis", null, "Single",
            5m, 1, Api.Models.RiskLevel.Low, false, false);

        var tasks = Enumerable.Range(0, 5)
            .Select(_ => Client.PostAsJsonAsync("/api/players", request))
            .ToList();

        var responses = await Task.WhenAll(tasks);

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.Created),
            r => Assert.True(
                r.StatusCode == HttpStatusCode.Conflict,
                $"Expected 409 Conflict but got {(int)r.StatusCode}"));
    }

    private sealed record ErrorResponse(string Error);
}
