using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteFeeReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_wallet_ledger_entry_type",
                table: "wallet_ledger");

            migrationBuilder.CreateTable(
                name: "quote_fee_reservations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    quote_id = table.Column<long>(type: "bigint", nullable: false),
                    wallet_id = table.Column<long>(type: "bigint", nullable: false),
                    category_fee_policy_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    currency_code = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false, defaultValue: "KRW"),
                    status_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "RESERVED"),
                    reserve_ledger_entry_id = table.Column<long>(type: "bigint", nullable: false),
                    capture_ledger_entry_id = table.Column<long>(type: "bigint", nullable: true),
                    release_ledger_entry_id = table.Column<long>(type: "bigint", nullable: true),
                    reserved_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    captured_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    released_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    release_reason_code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quote_fee_reservations", x => x.id);
                    table.CheckConstraint("CK_quote_fee_reservations_amount", "[amount] > 0");
                    table.CheckConstraint("CK_quote_fee_reservations_status", "[status_code] IN ('RESERVED','CAPTURED','RELEASED')");
                    table.ForeignKey(
                        name: "FK_quote_fee_reservations_category_fee_policies_category_fee_policy_id",
                        column: x => x.category_fee_policy_id,
                        principalTable: "category_fee_policies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_quote_fee_reservations_quotes_quote_id",
                        column: x => x.quote_id,
                        principalTable: "quotes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_quote_fee_reservations_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_quote_fee_reservations_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_quote_fee_reservations_wallet_ledger_capture_ledger_entry_id",
                        column: x => x.capture_ledger_entry_id,
                        principalTable: "wallet_ledger",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_quote_fee_reservations_wallet_ledger_release_ledger_entry_id",
                        column: x => x.release_ledger_entry_id,
                        principalTable: "wallet_ledger",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_quote_fee_reservations_wallet_ledger_reserve_ledger_entry_id",
                        column: x => x.reserve_ledger_entry_id,
                        principalTable: "wallet_ledger",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_quote_fee_reservations_wallets_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "wallets",
                        principalColumn: "id");
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_wallet_ledger_entry_type",
                table: "wallet_ledger",
                sql: "[entry_type_code] IN ('CHARGE','RESERVE','RELEASE','USE','RESTORE','REFUND','ADJUST')");

            migrationBuilder.CreateIndex(
                name: "IX_quote_fee_reservations_capture_ledger_entry_id",
                table: "quote_fee_reservations",
                column: "capture_ledger_entry_id",
                unique: true,
                filter: "[capture_ledger_entry_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_quote_fee_reservations_category_fee_policy_id",
                table: "quote_fee_reservations",
                column: "category_fee_policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_quote_fee_reservations_created_by_user_id",
                table: "quote_fee_reservations",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_quote_fee_reservations_public_id",
                table: "quote_fee_reservations",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quote_fee_reservations_quote_id",
                table: "quote_fee_reservations",
                column: "quote_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quote_fee_reservations_release_ledger_entry_id",
                table: "quote_fee_reservations",
                column: "release_ledger_entry_id",
                unique: true,
                filter: "[release_ledger_entry_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_quote_fee_reservations_reserve_ledger_entry_id",
                table: "quote_fee_reservations",
                column: "reserve_ledger_entry_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quote_fee_reservations_status_code_reserved_at",
                table: "quote_fee_reservations",
                columns: new[] { "status_code", "reserved_at" });

            migrationBuilder.CreateIndex(
                name: "IX_quote_fee_reservations_updated_by_user_id",
                table: "quote_fee_reservations",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_quote_fee_reservations_wallet_id",
                table: "quote_fee_reservations",
                column: "wallet_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quote_fee_reservations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_wallet_ledger_entry_type",
                table: "wallet_ledger");

            migrationBuilder.AddCheckConstraint(
                name: "CK_wallet_ledger_entry_type",
                table: "wallet_ledger",
                sql: "[entry_type_code] IN ('CHARGE','USE','RESTORE','REFUND','ADJUST')");
        }
    }
}
