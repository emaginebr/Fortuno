using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fortuno.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ticket_value",
                table: "fortuna_tickets",
                type: "varchar(64)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "fortuna_tickets_lottery_value_ix",
                table: "fortuna_tickets",
                columns: new[] { "lottery_id", "ticket_value" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "fortuna_tickets_lottery_value_ix",
                table: "fortuna_tickets");

            migrationBuilder.DropColumn(
                name: "ticket_value",
                table: "fortuna_tickets");
        }
    }
}
