using Microsoft.EntityFrameworkCore;
using PersonalisationEngine.Api.Models;

namespace PersonalisationEngine.Api.Data;

/// <summary>
/// Seeds demo players in the Development environment only, when the Players table is empty.
/// Not registered in production — demo data should never appear in non-dev environments.
/// </summary>
public class DevDataSeeder(IServiceProvider services, ILogger<DevDataSeeder> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.Players.AnyAsync(cancellationToken))
        {
            logger.LogInformation("DevDataSeeder: players already exist, skipping seed");
            return;
        }

        logger.LogInformation("DevDataSeeder: seeding demo players");

        db.Players.AddRange(
            new Player { PlayerId = "P001", DaysSinceJoined = 180, MostBetSport = "Football", FavouriteTeam = "Scotland", MostBetType = "Bet Builder", AverageStake = 12.50m, LastLoginDaysAgo = 6, RiskLevel = RiskLevel.Low },
            new Player { PlayerId = "P002", DaysSinceJoined = 365, MostBetSport = "Horse Racing", FavouriteTeam = "Cheltenham", MostBetType = "Each Way", AverageStake = 25.00m, LastLoginDaysAgo = 2, RiskLevel = RiskLevel.Low },
            new Player { PlayerId = "P003", DaysSinceJoined = 90, MostBetSport = "Tennis", MostBetType = "Match Winner", AverageStake = 8.00m, LastLoginDaysAgo = 1, RiskLevel = RiskLevel.Medium },
            new Player { PlayerId = "P004", DaysSinceJoined = 200, MostBetSport = "Basketball", MostBetType = "Accumulator", AverageStake = 5.00m, LastLoginDaysAgo = 20, RiskLevel = RiskLevel.Low },
            new Player { PlayerId = "P005", DaysSinceJoined = 500, MostBetSport = "Football", FavouriteTeam = "England", MostBetType = "In-Play", AverageStake = 150.00m, LastLoginDaysAgo = 3, RiskLevel = RiskLevel.High },
            new Player { PlayerId = "P006", DaysSinceJoined = 730, MostBetSport = "Football", FavouriteTeam = "Celtic", MostBetType = "Single", AverageStake = 10.00m, LastLoginDaysAgo = 30, RiskLevel = RiskLevel.Medium, IsSelfExcluded = true },
            new Player { PlayerId = "P007", DaysSinceJoined = 45, MostBetSport = "Rugby", MostBetType = "Handicap", AverageStake = 20.00m, LastLoginDaysAgo = 5, RiskLevel = RiskLevel.Low, IsInCoolingOff = true },
            new Player { PlayerId = "P008", DaysSinceJoined = 600, MostBetSport = "Golf", MostBetType = "Tournament Winner", AverageStake = 15.00m, LastLoginDaysAgo = 45, RiskLevel = RiskLevel.Low }
        );

        await db.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
