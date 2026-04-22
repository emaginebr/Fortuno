using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Fortuno.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fortuna_ticket_orders",
                columns: table => new
                {
                    ticket_order_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    invoice_id = table.Column<long>(type: "bigint", nullable: false),
                    invoice_number = table.Column<string>(type: "varchar(40)", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    lottery_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    mode = table.Column<int>(type: "integer", nullable: false),
                    picked_numbers_json = table.Column<string>(type: "varchar(4000)", nullable: true),
                    referral_code = table.Column<string>(type: "varchar(8)", nullable: true),
                    referral_percent_at_purchase = table.Column<float>(type: "real", nullable: false, defaultValue: 0f),
                    total_amount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    br_code = table.Column<string>(type: "varchar(2000)", nullable: true),
                    br_code_base64 = table.Column<string>(type: "text", nullable: true),
                    expired_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("fortuna_ticket_orders_pkey", x => x.ticket_order_id);
                    table.ForeignKey(
                        name: "fk_ticket_order_lottery",
                        column: x => x.lottery_id,
                        principalTable: "fortuna_lotteries",
                        principalColumn: "lottery_id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_ticket_orders_invoice_id",
                table: "fortuna_ticket_orders",
                column: "invoice_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ticket_orders_lottery_id",
                table: "fortuna_ticket_orders",
                column: "lottery_id");

            migrationBuilder.CreateIndex(
                name: "ix_ticket_orders_user_id",
                table: "fortuna_ticket_orders",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fortuna_ticket_orders");
        }
    }
}
