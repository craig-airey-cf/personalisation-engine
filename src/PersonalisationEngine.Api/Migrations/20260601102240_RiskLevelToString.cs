using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalisationEngine.Api.Migrations
{
    /// <inheritdoc />
    public partial class RiskLevelToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert integer enum values to their string names before changing the column type.
            // PostgreSQL requires an explicit USING clause because int→text is not implicit.
            migrationBuilder.Sql("""
                ALTER TABLE "Players"
                ALTER COLUMN "RiskLevel" TYPE text
                USING CASE "RiskLevel"
                    WHEN 0 THEN 'Low'
                    WHEN 1 THEN 'Medium'
                    WHEN 2 THEN 'High'
                    ELSE 'Low'
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "RiskLevel",
                table: "Players",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
