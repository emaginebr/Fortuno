using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Fortuno.Infra.Migrations
{
    /// <inheritdoc />
    public partial class DropWebhookEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fortuna_webhook_events");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fortuna_webhook_events",
                columns: table => new
                {
                    webhook_event_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    event_type = table.Column<string>(type: "varchar(40)", nullable: false),
                    invoice_id = table.Column<long>(type: "bigint", nullable: false),
                    payload_hash = table.Column<string>(type: "varchar(64)", nullable: true),
                    received_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("fortuna_webhook_events_pkey", x => x.webhook_event_id);
                });

            migrationBuilder.CreateIndex(
                name: "fortuna_webhook_events_invoice_type_uq",
                table: "fortuna_webhook_events",
                columns: new[] { "invoice_id", "event_type" },
                unique: true);
        }
    }
}
