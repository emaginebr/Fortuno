using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Fortuno.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketOrderNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "picked_numbers_json",
                table: "fortuna_ticket_orders");

            migrationBuilder.CreateTable(
                name: "fortuna_ticket_order_numbers",
                columns: table => new
                {
                    ticket_order_number_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    ticket_order_id = table.Column<long>(type: "bigint", nullable: false),
                    ticket_number = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("fortuna_ticket_order_numbers_pkey", x => x.ticket_order_number_id);
                    table.ForeignKey(
                        name: "fk_ticket_order_number_order",
                        column: x => x.ticket_order_id,
                        principalTable: "fortuna_ticket_orders",
                        principalColumn: "ticket_order_id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_ticket_order_numbers_order_id",
                table: "fortuna_ticket_order_numbers",
                column: "ticket_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_ticket_order_numbers_order_number_uq",
                table: "fortuna_ticket_order_numbers",
                columns: new[] { "ticket_order_id", "ticket_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fortuna_ticket_order_numbers");

            migrationBuilder.AddColumn<string>(
                name: "picked_numbers_json",
                table: "fortuna_ticket_orders",
                type: "varchar(4000)",
                nullable: true);
        }
    }
}
