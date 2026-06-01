using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PersonalisationEngine.Api.Data;
using PersonalisationEngine.Api.Models;
using System.Net.Http.Json;

namespace PersonalisationEngine.Tests.Infrastructure;

[Collection(nameof(IntegrationTestCollection))]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly PersonalisationEngineFactory Factory;
    protected readonly HttpClient Client;

    protected IntegrationTestBase(PostgresContainerFixture db)
    {
        Factory = new PersonalisationEngineFactory(db.ConnectionString);
        Factory.MigrateDatabase();
        Client = Factory.CreateClient();
        Client.DefaultRequestHeaders.Add("X-Api-Key", PersonalisationEngineFactory.TestApiKey);
    }

    public async Task InitializeAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Recommendations.ExecuteDeleteAsync();
        await db.Players.ExecuteDeleteAsync();
        await SeedPlayersAsync(db);
    }

    private static async Task SeedPlayersAsync(AppDbContext db)
    {
        db.Players.AddRange(
            new Player
            {
                PlayerId = "P001", MostBetSport = "Football", FavouriteTeam = "Scotland",
                MostBetType = "Bet Builder", AverageStake = 12.50m, DaysSinceJoined = 180,
                LastLoginDaysAgo = 6, RiskLevel = RiskLevel.Low
            },
            new Player
            {
                PlayerId = "P005", MostBetSport = "Football", FavouriteTeam = "England",
                MostBetType = "In-Play", AverageStake = 150m, DaysSinceJoined = 500,
                LastLoginDaysAgo = 3, RiskLevel = RiskLevel.High
            },
            new Player
            {
                PlayerId = "P006", MostBetSport = "Football", FavouriteTeam = "Celtic",
                MostBetType = "Single", AverageStake = 10m, DaysSinceJoined = 730,
                LastLoginDaysAgo = 30, RiskLevel = RiskLevel.Medium, IsSelfExcluded = true
            },
            new Player
            {
                PlayerId = "P007", MostBetSport = "Rugby", MostBetType = "Handicap",
                AverageStake = 20m, DaysSinceJoined = 45,
                LastLoginDaysAgo = 5, RiskLevel = RiskLevel.Low, IsInCoolingOff = true
            },
            new Player
            {
                PlayerId = "P008", MostBetSport = "Golf", MostBetType = "Tournament Winner",
                AverageStake = 15m, DaysSinceJoined = 600,
                LastLoginDaysAgo = 45, RiskLevel = RiskLevel.Low
            }
        );
        await db.SaveChangesAsync();
    }

    protected async Task<T> GetJsonAsync<T>(string url)
    {
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Factory.DisposeAsync().AsTask();
    }
}
