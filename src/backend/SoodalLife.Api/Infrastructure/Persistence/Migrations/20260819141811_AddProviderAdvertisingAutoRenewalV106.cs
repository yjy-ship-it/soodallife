using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderAdvertisingAutoRenewalV106 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "auto_renew_disabled_at",
                table: "provider_advertising_applications",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "auto_renew_enabled",
                table: "provider_advertising_applications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "auto_renew_status_code",
                table: "provider_advertising_applications",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "OFF");

            migrationBuilder.AddColumn<DateTime>(
                name: "last_renewed_at",
                table: "provider_advertising_applications",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_renewal_at",
                table: "provider_advertising_applications",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "renewal_consent_at",
                table: "provider_advertising_applications",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "renewal_consent_fee_amount",
                table: "provider_advertising_applications",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "renewal_consent_policy_fingerprint",
                table: "provider_advertising_applications",
                type: "varchar(128)",
                unicode: false,
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "renewal_consent_required",
                table: "provider_advertising_applications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "renewal_cycle_no",
                table: "provider_advertising_applications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "renewal_notice_sent_at",
                table: "provider_advertising_applications",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "provider_advertising_renewal_history",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider_advertising_application_id = table.Column<long>(type: "bigint", nullable: false),
                    cycle_no = table.Column<int>(type: "int", nullable: false),
                    due_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    base_fee_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    regional_fee_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    fee_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    currency_code = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    status_code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    reserve_ledger_entry_id = table.Column<long>(type: "bigint", nullable: true),
                    capture_ledger_entry_id = table.Column<long>(type: "bigint", nullable: true),
                    notice_sent_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    processed_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    failure_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    policy_fingerprint = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_advertising_renewal_history", x => x.id);
                    table.CheckConstraint("CK_provider_ad_renewal_amount", "[fee_amount] > 0 AND [base_fee_amount] >= 0 AND [regional_fee_amount] >= 0");
                    table.CheckConstraint("CK_provider_ad_renewal_cycle", "[cycle_no] > 0");
                    table.CheckConstraint("CK_provider_ad_renewal_status", "[status_code] IN ('PENDING','NOTICE_SENT','RENEWED','PAUSED_INSUFFICIENT','CONSENT_REQUIRED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_provider_advertising_renewal_history_provider_advertising_applications_provider_advertising_application_id",
                        column: x => x.provider_advertising_application_id,
                        principalTable: "provider_advertising_applications",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_advertising_renewal_history_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_advertising_renewal_history_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_advertising_renewal_history_wallet_ledger_capture_ledger_entry_id",
                        column: x => x.capture_ledger_entry_id,
                        principalTable: "wallet_ledger",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_provider_advertising_renewal_history_wallet_ledger_reserve_ledger_entry_id",
                        column: x => x.reserve_ledger_entry_id,
                        principalTable: "wallet_ledger",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_provider_advertising_applications_auto_renew_enabled_auto_renew_status_code_next_renewal_at",
                table: "provider_advertising_applications",
                columns: new[] { "auto_renew_enabled", "auto_renew_status_code", "next_renewal_at" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_provider_ad_app_auto_renew_status",
                table: "provider_advertising_applications",
                sql: "[auto_renew_status_code] IN ('OFF','PENDING_PUBLICATION','ACTIVE','CONSENT_REQUIRED','PAUSED_INSUFFICIENT','CANCELLED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_provider_ad_app_renewal_cycle",
                table: "provider_advertising_applications",
                sql: "[renewal_cycle_no] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_provider_advertising_renewal_history_capture_ledger_entry_id",
                table: "provider_advertising_renewal_history",
                column: "capture_ledger_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_advertising_renewal_history_created_by_user_id",
                table: "provider_advertising_renewal_history",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_advertising_renewal_history_provider_advertising_application_id_cycle_no",
                table: "provider_advertising_renewal_history",
                columns: new[] { "provider_advertising_application_id", "cycle_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_advertising_renewal_history_public_id",
                table: "provider_advertising_renewal_history",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_advertising_renewal_history_reserve_ledger_entry_id",
                table: "provider_advertising_renewal_history",
                column: "reserve_ledger_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_advertising_renewal_history_status_code_due_at",
                table: "provider_advertising_renewal_history",
                columns: new[] { "status_code", "due_at" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_advertising_renewal_history_updated_by_user_id",
                table: "provider_advertising_renewal_history",
                column: "updated_by_user_id");

            migrationBuilder.Sql("""
                DECLARE @templates TABLE(template_code varchar(100), [name] nvarchar(200), event_type_code varchar(100), title_template nvarchar(300), body_template nvarchar(3000));
                INSERT INTO @templates VALUES
                ('PROVIDER_AD_RENEWAL_NOTICE_WEB',N'광고 자동 갱신 사전 안내','PROVIDER_AD_RENEWAL_NOTICE',N'다음 달 광고비를 안내드립니다',N'광고 자동 갱신 {{cycle}}회차 예정 금액은 {{amount}}원입니다. 갱신 전 충전금 잔액을 확인해 주세요.'),
                ('PROVIDER_AD_RENEWAL_RECONSENT_WEB',N'광고 자동 갱신 정책 재동의','PROVIDER_AD_RENEWAL_RECONSENT_REQUIRED',N'광고 자동 갱신 재동의가 필요합니다',N'노출 지역·위치 또는 요금 정책이 변경되었습니다. 변경된 월 광고비 {{amount}}원을 확인하고 재동의해 주세요.'),
                ('PROVIDER_AD_RENEWAL_BALANCE_WEB',N'광고 자동 갱신 잔액 부족','PROVIDER_AD_RENEWAL_PAUSED_INSUFFICIENT',N'충전금 부족으로 광고 게시가 중지되었습니다',N'자동 갱신 광고비 {{amount}}원보다 충전금 잔액이 부족하여 게시를 중지했습니다. 충전 후 자동 갱신을 다시 설정해 주세요.'),
                ('PROVIDER_AD_RENEWED_WEB',N'광고 자동 갱신 완료','PROVIDER_AD_RENEWED',N'광고가 자동 갱신되었습니다',N'광고 자동 갱신 {{cycle}}회차가 완료되어 충전금에서 {{amount}}원이 차감되었습니다.');
                INSERT INTO notification_templates(public_id,template_code,[name],[description],audience_type_code,event_type_code,channel_code,title_template,body_template,allowed_variables_json,is_required_business_notice,is_marketing,is_active,effective_from,created_at,updated_at)
                SELECT NEWID(),t.template_code,t.[name],N'공급자 광고 월 자동 갱신 운영 알림','PROVIDER',t.event_type_code,'WEB',t.title_template,t.body_template,N'["source_no","amount","cycle"]',1,0,1,SYSUTCDATETIME(),SYSUTCDATETIME(),SYSUTCDATETIME()
                FROM @templates t WHERE NOT EXISTS(SELECT 1 FROM notification_templates n WHERE n.template_code=t.template_code AND n.channel_code='WEB');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM notification_templates WHERE template_code IN ('PROVIDER_AD_RENEWAL_NOTICE_WEB','PROVIDER_AD_RENEWAL_RECONSENT_WEB','PROVIDER_AD_RENEWAL_BALANCE_WEB','PROVIDER_AD_RENEWED_WEB');");

            migrationBuilder.DropTable(
                name: "provider_advertising_renewal_history");

            migrationBuilder.DropIndex(
                name: "IX_provider_advertising_applications_auto_renew_enabled_auto_renew_status_code_next_renewal_at",
                table: "provider_advertising_applications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_provider_ad_app_auto_renew_status",
                table: "provider_advertising_applications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_provider_ad_app_renewal_cycle",
                table: "provider_advertising_applications");

            migrationBuilder.DropColumn(
                name: "auto_renew_disabled_at",
                table: "provider_advertising_applications");

            migrationBuilder.DropColumn(
                name: "auto_renew_enabled",
                table: "provider_advertising_applications");

            migrationBuilder.DropColumn(
                name: "auto_renew_status_code",
                table: "provider_advertising_applications");

            migrationBuilder.DropColumn(
                name: "last_renewed_at",
                table: "provider_advertising_applications");

            migrationBuilder.DropColumn(
                name: "next_renewal_at",
                table: "provider_advertising_applications");

            migrationBuilder.DropColumn(
                name: "renewal_consent_at",
                table: "provider_advertising_applications");

            migrationBuilder.DropColumn(
                name: "renewal_consent_fee_amount",
                table: "provider_advertising_applications");

            migrationBuilder.DropColumn(
                name: "renewal_consent_policy_fingerprint",
                table: "provider_advertising_applications");

            migrationBuilder.DropColumn(
                name: "renewal_consent_required",
                table: "provider_advertising_applications");

            migrationBuilder.DropColumn(
                name: "renewal_cycle_no",
                table: "provider_advertising_applications");

            migrationBuilder.DropColumn(
                name: "renewal_notice_sent_at",
                table: "provider_advertising_applications");
        }
    }
}
