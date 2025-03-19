using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tennis_Card_Game.Migrations
{
    /// <inheritdoc />
    public partial class Drop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WinnerId",
                table: "Matches");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WinnerId",
                table: "Matches",
                type: "int",
                nullable: true);
        }
    }
}
