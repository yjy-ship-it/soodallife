using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementTransactionDirectPaymentClosure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "transaction_direct_payments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    transaction_id = table.Column<long>(type: "bigint", nullable: false),
                    registered_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    registered_by_role_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    currency_code = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    payment_method_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    paid_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    note_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    evidence_file_id = table.Column<long>(type: "bigint", nullable: true),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "REGISTERED"),
                    registered_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    decided_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    decided_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    rejection_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    registration_idempotency_key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    decision_idempotency_key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaction_direct_payments", x => x.id);
                    table.CheckConstraint("CK_transaction_direct_payments_amount", "[amount] > 0");
                    table.CheckConstraint("CK_transaction_direct_payments_method", "[payment_method_code] IN ('BANK_TRANSFER','ON_SITE_CARD','CASH','OTHER')");
                    table.CheckConstraint("CK_transaction_direct_payments_role", "[registered_by_role_code] IN ('CUSTOMER','PROVIDER')");
                    table.CheckConstraint("CK_transaction_direct_payments_status", "[status_code] IN ('REGISTERED','COUNTERPART_CONFIRMED','REJECTED')");
                    table.ForeignKey(
                        name: "FK_transaction_direct_payments_files_evidence_file_id",
                        column: x => x.evidence_file_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transaction_direct_payments_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transaction_direct_payments_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transaction_direct_payments_users_decided_by_user_id",
                        column: x => x.decided_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transaction_direct_payments_users_registered_by_user_id",
                        column: x => x.registered_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transaction_direct_payments_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_transaction_direct_payments_created_by_user_id",
                table: "transaction_direct_payments",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_direct_payments_decided_by_user_id",
                table: "transaction_direct_payments",
                column: "decided_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_direct_payments_decision_idempotency_key",
                table: "transaction_direct_payments",
                column: "decision_idempotency_key",
                unique: true,
                filter: "[decision_idempotency_key] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_direct_payments_evidence_file_id",
                table: "transaction_direct_payments",
                column: "evidence_file_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_direct_payments_public_id",
                table: "transaction_direct_payments",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transaction_direct_payments_registered_by_user_id",
                table: "transaction_direct_payments",
                column: "registered_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_direct_payments_registration_idempotency_key",
                table: "transaction_direct_payments",
                column: "registration_idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transaction_direct_payments_transaction_id",
                table: "transaction_direct_payments",
                column: "transaction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transaction_direct_payments_updated_by_user_id",
                table: "transaction_direct_payments",
                column: "updated_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transaction_direct_payments");
        }
    }
}
