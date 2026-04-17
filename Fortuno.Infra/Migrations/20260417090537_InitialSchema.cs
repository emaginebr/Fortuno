using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Fortuno.Infra.Migrations;

/// <inheritdoc />
public partial class InitialSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "fortuna_invoice_referrers",
            columns: table => new
            {
                invoice_referrer_id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                invoice_id = table.Column<long>(type: "bigint", nullable: false),
                referrer_user_id = table.Column<long>(type: "bigint", nullable: false),
                lottery_id = table.Column<long>(type: "bigint", nullable: false),
                referral_percent_at_purchase = table.Column<float>(type: "real", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("fortuna_invoice_referrers_pkey", x => x.invoice_referrer_id);
            });

        migrationBuilder.CreateTable(
            name: "fortuna_lotteries",
            columns: table => new
            {
                lottery_id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                store_id = table.Column<long>(type: "bigint", nullable: false),
                name = table.Column<string>(type: "varchar(160)", nullable: false),
                slug = table.Column<string>(type: "varchar(200)", nullable: false),
                description_md = table.Column<string>(type: "text", nullable: false),
                rules_md = table.Column<string>(type: "text", nullable: false),
                privacy_policy_md = table.Column<string>(type: "text", nullable: false),
                ticket_price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                total_prize_value = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                ticket_min = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                ticket_max = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                ticket_num_ini = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                ticket_num_end = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                number_type = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                number_value_min = table.Column<int>(type: "integer", nullable: false),
                number_value_max = table.Column<int>(type: "integer", nullable: false),
                referral_percent = table.Column<float>(type: "real", nullable: false, defaultValue: 0f),
                status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                cancel_reason = table.Column<string>(type: "varchar(1000)", nullable: true),
                cancelled_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                cancelled_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("fortuna_lotteries_pkey", x => x.lottery_id);
            });

        migrationBuilder.CreateTable(
            name: "fortuna_number_reservations",
            columns: table => new
            {
                reservation_id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                lottery_id = table.Column<long>(type: "bigint", nullable: false),
                user_id = table.Column<long>(type: "bigint", nullable: false),
                invoice_id = table.Column<long>(type: "bigint", nullable: true),
                ticket_number = table.Column<long>(type: "bigint", nullable: false),
                expires_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("fortuna_number_reservations_pkey", x => x.reservation_id);
            });

        migrationBuilder.CreateTable(
            name: "fortuna_user_referrers",
            columns: table => new
            {
                user_referrer_id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                user_id = table.Column<long>(type: "bigint", nullable: false),
                referral_code = table.Column<string>(type: "varchar(8)", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("fortuna_user_referrers_pkey", x => x.user_referrer_id);
            });

        migrationBuilder.CreateTable(
            name: "fortuna_webhook_events",
            columns: table => new
            {
                webhook_event_id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                invoice_id = table.Column<long>(type: "bigint", nullable: false),
                event_type = table.Column<string>(type: "varchar(40)", nullable: false),
                received_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                payload_hash = table.Column<string>(type: "varchar(64)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("fortuna_webhook_events_pkey", x => x.webhook_event_id);
            });

        migrationBuilder.CreateTable(
            name: "fortuna_lottery_combos",
            columns: table => new
            {
                lottery_combo_id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                lottery_id = table.Column<long>(type: "bigint", nullable: false),
                name = table.Column<string>(type: "varchar(120)", nullable: false),
                discount_value = table.Column<float>(type: "real", nullable: false, defaultValue: 0f),
                discount_label = table.Column<string>(type: "varchar(80)", nullable: false),
                quantity_start = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                quantity_end = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("fortuna_lottery_combos_pkey", x => x.lottery_combo_id);
                table.ForeignKey(
                    name: "fk_lottery_lottery_combo",
                    column: x => x.lottery_id,
                    principalTable: "fortuna_lotteries",
                    principalColumn: "lottery_id");
            });

        migrationBuilder.CreateTable(
            name: "fortuna_lottery_images",
            columns: table => new
            {
                lottery_image_id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                lottery_id = table.Column<long>(type: "bigint", nullable: false),
                image_url = table.Column<string>(type: "varchar(500)", nullable: false),
                description = table.Column<string>(type: "varchar(260)", nullable: true),
                display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("fortuna_lottery_images_pkey", x => x.lottery_image_id);
                table.ForeignKey(
                    name: "fk_lottery_lottery_image",
                    column: x => x.lottery_id,
                    principalTable: "fortuna_lotteries",
                    principalColumn: "lottery_id");
            });

        migrationBuilder.CreateTable(
            name: "fortuna_raffles",
            columns: table => new
            {
                raffle_id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                lottery_id = table.Column<long>(type: "bigint", nullable: false),
                name = table.Column<string>(type: "varchar(160)", nullable: false),
                description_md = table.Column<string>(type: "text", nullable: true),
                raffle_datetime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                video_url = table.Column<string>(type: "varchar(500)", nullable: true),
                include_previous_winners = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("fortuna_raffles_pkey", x => x.raffle_id);
                table.ForeignKey(
                    name: "fk_lottery_raffle",
                    column: x => x.lottery_id,
                    principalTable: "fortuna_lotteries",
                    principalColumn: "lottery_id");
            });

        migrationBuilder.CreateTable(
            name: "fortuna_tickets",
            columns: table => new
            {
                ticket_id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                lottery_id = table.Column<long>(type: "bigint", nullable: false),
                user_id = table.Column<long>(type: "bigint", nullable: false),
                invoice_id = table.Column<long>(type: "bigint", nullable: false),
                ticket_number = table.Column<long>(type: "bigint", nullable: false),
                refund_state = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("fortuna_tickets_pkey", x => x.ticket_id);
                table.ForeignKey(
                    name: "fk_lottery_ticket",
                    column: x => x.lottery_id,
                    principalTable: "fortuna_lotteries",
                    principalColumn: "lottery_id");
            });

        migrationBuilder.CreateTable(
            name: "fortuna_raffle_awards",
            columns: table => new
            {
                raffle_award_id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                raffle_id = table.Column<long>(type: "bigint", nullable: false),
                position = table.Column<int>(type: "integer", nullable: false),
                description = table.Column<string>(type: "varchar(300)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("fortuna_raffle_awards_pkey", x => x.raffle_award_id);
                table.ForeignKey(
                    name: "fk_raffle_raffle_award",
                    column: x => x.raffle_id,
                    principalTable: "fortuna_raffles",
                    principalColumn: "raffle_id");
            });

        migrationBuilder.CreateTable(
            name: "fortuna_refund_logs",
            columns: table => new
            {
                refund_log_id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                ticket_id = table.Column<long>(type: "bigint", nullable: false),
                executed_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                reference_value = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                external_reference = table.Column<string>(type: "varchar(160)", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("fortuna_refund_logs_pkey", x => x.refund_log_id);
                table.ForeignKey(
                    name: "fk_ticket_refund_log",
                    column: x => x.ticket_id,
                    principalTable: "fortuna_tickets",
                    principalColumn: "ticket_id");
            });

        migrationBuilder.CreateTable(
            name: "fortuna_raffle_winners",
            columns: table => new
            {
                raffle_winner_id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                raffle_id = table.Column<long>(type: "bigint", nullable: false),
                raffle_award_id = table.Column<long>(type: "bigint", nullable: false),
                ticket_id = table.Column<long>(type: "bigint", nullable: true),
                user_id = table.Column<long>(type: "bigint", nullable: true),
                position = table.Column<int>(type: "integer", nullable: false),
                prize_text = table.Column<string>(type: "varchar(300)", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("fortuna_raffle_winners_pkey", x => x.raffle_winner_id);
                table.ForeignKey(
                    name: "fk_raffle_award_raffle_winner",
                    column: x => x.raffle_award_id,
                    principalTable: "fortuna_raffle_awards",
                    principalColumn: "raffle_award_id");
                table.ForeignKey(
                    name: "fk_raffle_raffle_winner",
                    column: x => x.raffle_id,
                    principalTable: "fortuna_raffles",
                    principalColumn: "raffle_id");
                table.ForeignKey(
                    name: "fk_ticket_raffle_winner",
                    column: x => x.ticket_id,
                    principalTable: "fortuna_tickets",
                    principalColumn: "ticket_id");
            });

        migrationBuilder.CreateIndex(
            name: "fortuna_invoice_referrers_invoice_uq",
            table: "fortuna_invoice_referrers",
            column: "invoice_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "fortuna_invoice_referrers_referrer_lottery_ix",
            table: "fortuna_invoice_referrers",
            columns: new[] { "referrer_user_id", "lottery_id" });

        migrationBuilder.CreateIndex(
            name: "fortuna_lotteries_slug_uq",
            table: "fortuna_lotteries",
            column: "slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "fortuna_lotteries_status_ix",
            table: "fortuna_lotteries",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "IX_fortuna_lottery_combos_lottery_id",
            table: "fortuna_lottery_combos",
            column: "lottery_id");

        migrationBuilder.CreateIndex(
            name: "IX_fortuna_lottery_images_lottery_id",
            table: "fortuna_lottery_images",
            column: "lottery_id");

        migrationBuilder.CreateIndex(
            name: "fortuna_number_reservations_lottery_expires_ix",
            table: "fortuna_number_reservations",
            columns: new[] { "lottery_id", "expires_at" });

        migrationBuilder.CreateIndex(
            name: "fortuna_number_reservations_user_lottery_ix",
            table: "fortuna_number_reservations",
            columns: new[] { "user_id", "lottery_id" });

        migrationBuilder.CreateIndex(
            name: "fortuna_raffle_awards_raffle_position_uq",
            table: "fortuna_raffle_awards",
            columns: new[] { "raffle_id", "position" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "fortuna_raffle_winners_raffle_award_uq",
            table: "fortuna_raffle_winners",
            columns: new[] { "raffle_id", "raffle_award_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_fortuna_raffle_winners_raffle_award_id",
            table: "fortuna_raffle_winners",
            column: "raffle_award_id");

        migrationBuilder.CreateIndex(
            name: "IX_fortuna_raffle_winners_ticket_id",
            table: "fortuna_raffle_winners",
            column: "ticket_id");

        migrationBuilder.CreateIndex(
            name: "IX_fortuna_raffles_lottery_id",
            table: "fortuna_raffles",
            column: "lottery_id");

        migrationBuilder.CreateIndex(
            name: "IX_fortuna_refund_logs_ticket_id",
            table: "fortuna_refund_logs",
            column: "ticket_id");

        migrationBuilder.CreateIndex(
            name: "fortuna_tickets_invoice_ix",
            table: "fortuna_tickets",
            column: "invoice_id");

        migrationBuilder.CreateIndex(
            name: "fortuna_tickets_lottery_number_uq",
            table: "fortuna_tickets",
            columns: new[] { "lottery_id", "ticket_number" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "fortuna_tickets_lottery_refund_ix",
            table: "fortuna_tickets",
            columns: new[] { "lottery_id", "refund_state" });

        migrationBuilder.CreateIndex(
            name: "fortuna_tickets_user_created_ix",
            table: "fortuna_tickets",
            columns: new[] { "user_id", "created_at" });

        migrationBuilder.CreateIndex(
            name: "fortuna_user_referrers_code_uq",
            table: "fortuna_user_referrers",
            column: "referral_code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "fortuna_user_referrers_user_uq",
            table: "fortuna_user_referrers",
            column: "user_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "fortuna_webhook_events_invoice_type_uq",
            table: "fortuna_webhook_events",
            columns: new[] { "invoice_id", "event_type" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "fortuna_invoice_referrers");

        migrationBuilder.DropTable(
            name: "fortuna_lottery_combos");

        migrationBuilder.DropTable(
            name: "fortuna_lottery_images");

        migrationBuilder.DropTable(
            name: "fortuna_number_reservations");

        migrationBuilder.DropTable(
            name: "fortuna_raffle_winners");

        migrationBuilder.DropTable(
            name: "fortuna_refund_logs");

        migrationBuilder.DropTable(
            name: "fortuna_user_referrers");

        migrationBuilder.DropTable(
            name: "fortuna_webhook_events");

        migrationBuilder.DropTable(
            name: "fortuna_raffle_awards");

        migrationBuilder.DropTable(
            name: "fortuna_tickets");

        migrationBuilder.DropTable(
            name: "fortuna_raffles");

        migrationBuilder.DropTable(
            name: "fortuna_lotteries");
    }
}
