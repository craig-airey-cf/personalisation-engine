using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalisationEngine.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedPlayers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Players",
                columns: ["PlayerId", "DaysSinceJoined", "MostBetSport", "FavouriteTeam",
                          "MostBetType", "AverageStake", "LastLoginDaysAgo", "RiskLevel",
                          "IsSelfExcluded", "IsInCoolingOff"],
                values: new object[,]
                {
                    // Low-risk active football fan — the happy-path demo player
                    { "P001", 180, "Football", "Scotland", "Bet Builder", 12.50m, 6, 0, false, false },
                    // Low-risk horse racing player
                    { "P002", 365, "Horse Racing", "Cheltenham", "Each Way", 25.00m, 2, 0, false, false },
                    // Medium-risk tennis player, recently active
                    { "P003", 90, "Tennis", null, "Match Winner", 8.00m, 1, 1, false, false },
                    // Low-risk basketball player, inactive for 20 days
                    { "P004", 200, "Basketball", null, "Accumulator", 5.00m, 20, 0, false, false },
                    // High-risk player — guardrail should block
                    { "P005", 500, "Football", "England", "In-Play", 150.00m, 3, 2, false, false },
                    // Self-excluded player — guardrail should block
                    { "P006", 730, "Football", "Celtic", "Single", 10.00m, 30, 1, true, false },
                    // Cooling-off player — guardrail should block
                    { "P007", 45, "Rugby", null, "Handicap", 20.00m, 5, 0, false, true },
                    // Inactive player (>30 days) — guardrail should block
                    { "P008", 600, "Golf", null, "Tournament Winner", 15.00m, 45, 0, false, false }
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Players",
                keyColumn: "PlayerId",
                keyValues: ["P001", "P002", "P003", "P004", "P005", "P006", "P007", "P008"]
            );
        }
    }
}
