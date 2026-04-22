using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fortuno.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddLotteryCascadeDeletes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_lottery_lottery_combo",
                table: "fortuna_lottery_combos");

            migrationBuilder.DropForeignKey(
                name: "fk_lottery_lottery_image",
                table: "fortuna_lottery_images");

            migrationBuilder.DropForeignKey(
                name: "fk_raffle_raffle_award",
                table: "fortuna_raffle_awards");

            migrationBuilder.DropForeignKey(
                name: "fk_raffle_raffle_winner",
                table: "fortuna_raffle_winners");

            migrationBuilder.DropForeignKey(
                name: "fk_lottery_raffle",
                table: "fortuna_raffles");

            migrationBuilder.DropForeignKey(
                name: "fk_ticket_refund_log",
                table: "fortuna_refund_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_ticket_order_number_order",
                table: "fortuna_ticket_order_numbers");

            migrationBuilder.DropForeignKey(
                name: "fk_ticket_order_lottery",
                table: "fortuna_ticket_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_lottery_ticket",
                table: "fortuna_tickets");

            migrationBuilder.CreateIndex(
                name: "IX_fortuna_invoice_referrers_lottery_id",
                table: "fortuna_invoice_referrers",
                column: "lottery_id");

            migrationBuilder.AddForeignKey(
                name: "fk_lottery_invoice_referrer",
                table: "fortuna_invoice_referrers",
                column: "lottery_id",
                principalTable: "fortuna_lotteries",
                principalColumn: "lottery_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_lottery_lottery_combo",
                table: "fortuna_lottery_combos",
                column: "lottery_id",
                principalTable: "fortuna_lotteries",
                principalColumn: "lottery_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_lottery_lottery_image",
                table: "fortuna_lottery_images",
                column: "lottery_id",
                principalTable: "fortuna_lotteries",
                principalColumn: "lottery_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_lottery_number_reservation",
                table: "fortuna_number_reservations",
                column: "lottery_id",
                principalTable: "fortuna_lotteries",
                principalColumn: "lottery_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_raffle_raffle_award",
                table: "fortuna_raffle_awards",
                column: "raffle_id",
                principalTable: "fortuna_raffles",
                principalColumn: "raffle_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_raffle_raffle_winner",
                table: "fortuna_raffle_winners",
                column: "raffle_id",
                principalTable: "fortuna_raffles",
                principalColumn: "raffle_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_lottery_raffle",
                table: "fortuna_raffles",
                column: "lottery_id",
                principalTable: "fortuna_lotteries",
                principalColumn: "lottery_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_ticket_refund_log",
                table: "fortuna_refund_logs",
                column: "ticket_id",
                principalTable: "fortuna_tickets",
                principalColumn: "ticket_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_ticket_order_number_order",
                table: "fortuna_ticket_order_numbers",
                column: "ticket_order_id",
                principalTable: "fortuna_ticket_orders",
                principalColumn: "ticket_order_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_ticket_order_lottery",
                table: "fortuna_ticket_orders",
                column: "lottery_id",
                principalTable: "fortuna_lotteries",
                principalColumn: "lottery_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_lottery_ticket",
                table: "fortuna_tickets",
                column: "lottery_id",
                principalTable: "fortuna_lotteries",
                principalColumn: "lottery_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_lottery_invoice_referrer",
                table: "fortuna_invoice_referrers");

            migrationBuilder.DropForeignKey(
                name: "fk_lottery_lottery_combo",
                table: "fortuna_lottery_combos");

            migrationBuilder.DropForeignKey(
                name: "fk_lottery_lottery_image",
                table: "fortuna_lottery_images");

            migrationBuilder.DropForeignKey(
                name: "fk_lottery_number_reservation",
                table: "fortuna_number_reservations");

            migrationBuilder.DropForeignKey(
                name: "fk_raffle_raffle_award",
                table: "fortuna_raffle_awards");

            migrationBuilder.DropForeignKey(
                name: "fk_raffle_raffle_winner",
                table: "fortuna_raffle_winners");

            migrationBuilder.DropForeignKey(
                name: "fk_lottery_raffle",
                table: "fortuna_raffles");

            migrationBuilder.DropForeignKey(
                name: "fk_ticket_refund_log",
                table: "fortuna_refund_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_ticket_order_number_order",
                table: "fortuna_ticket_order_numbers");

            migrationBuilder.DropForeignKey(
                name: "fk_ticket_order_lottery",
                table: "fortuna_ticket_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_lottery_ticket",
                table: "fortuna_tickets");

            migrationBuilder.DropIndex(
                name: "IX_fortuna_invoice_referrers_lottery_id",
                table: "fortuna_invoice_referrers");

            migrationBuilder.AddForeignKey(
                name: "fk_lottery_lottery_combo",
                table: "fortuna_lottery_combos",
                column: "lottery_id",
                principalTable: "fortuna_lotteries",
                principalColumn: "lottery_id");

            migrationBuilder.AddForeignKey(
                name: "fk_lottery_lottery_image",
                table: "fortuna_lottery_images",
                column: "lottery_id",
                principalTable: "fortuna_lotteries",
                principalColumn: "lottery_id");

            migrationBuilder.AddForeignKey(
                name: "fk_raffle_raffle_award",
                table: "fortuna_raffle_awards",
                column: "raffle_id",
                principalTable: "fortuna_raffles",
                principalColumn: "raffle_id");

            migrationBuilder.AddForeignKey(
                name: "fk_raffle_raffle_winner",
                table: "fortuna_raffle_winners",
                column: "raffle_id",
                principalTable: "fortuna_raffles",
                principalColumn: "raffle_id");

            migrationBuilder.AddForeignKey(
                name: "fk_lottery_raffle",
                table: "fortuna_raffles",
                column: "lottery_id",
                principalTable: "fortuna_lotteries",
                principalColumn: "lottery_id");

            migrationBuilder.AddForeignKey(
                name: "fk_ticket_refund_log",
                table: "fortuna_refund_logs",
                column: "ticket_id",
                principalTable: "fortuna_tickets",
                principalColumn: "ticket_id");

            migrationBuilder.AddForeignKey(
                name: "fk_ticket_order_number_order",
                table: "fortuna_ticket_order_numbers",
                column: "ticket_order_id",
                principalTable: "fortuna_ticket_orders",
                principalColumn: "ticket_order_id");

            migrationBuilder.AddForeignKey(
                name: "fk_ticket_order_lottery",
                table: "fortuna_ticket_orders",
                column: "lottery_id",
                principalTable: "fortuna_lotteries",
                principalColumn: "lottery_id");

            migrationBuilder.AddForeignKey(
                name: "fk_lottery_ticket",
                table: "fortuna_tickets",
                column: "lottery_id",
                principalTable: "fortuna_lotteries",
                principalColumn: "lottery_id");
        }
    }
}
