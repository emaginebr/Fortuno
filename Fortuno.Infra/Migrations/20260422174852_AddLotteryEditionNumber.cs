using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fortuno.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddLotteryEditionNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "edition_number",
                table: "fortuna_lotteries",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "edition_number",
                table: "fortuna_lotteries");
        }
    }
}
