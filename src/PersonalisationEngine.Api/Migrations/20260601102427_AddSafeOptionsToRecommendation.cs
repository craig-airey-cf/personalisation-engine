using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalisationEngine.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSafeOptionsToRecommendation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SafeOptionsJson",
                table: "Recommendations",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SafeOptionsJson",
                table: "Recommendations");
        }
    }
}
