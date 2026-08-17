using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementInteriorFinalSelectionFee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_interior_projects_fee",
                table: "interior_projects");

            migrationBuilder.AlterColumn<string>(
                name: "fee_assessment_status_code",
                table: "interior_projects",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "PENDING_SELECTION",
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldUnicode: false,
                oldMaxLength: 30,
                oldDefaultValue: "POLICY_PENDING");

            migrationBuilder.Sql("""
                UPDATE [interior_projects]
                SET [fee_assessment_status_code] = CASE
                    WHEN [selected_contractor_provider_id] IS NULL AND [current_quote_revision_id] IS NULL THEN 'PENDING_SELECTION'
                    ELSE 'NOT_APPLICABLE'
                END
                WHERE [fee_assessment_status_code] = 'POLICY_PENDING';

                UPDATE [fee_policies]
                SET [policy_kind_code] = 'PROJECT',
                    [transaction_type_code] = 'PROJECT',
                    [applies_to_text] = N'인테리어 최종 시공업체 선택',
                    [calculation_method_text] = N'SELECTED_QUOTE_TIER:FEE-Q1-FEE-Q7',
                    [min_base_amount] = 0,
                    [max_base_amount] = 999999999,
                    [display_fee_amount] = 0,
                    [currency_code] = 'KRW',
                    [charge_timing_text] = N'고객이 최종 시공업체의 유효한 최신 견적을 선택할 때',
                    [restore_rule_text] = N'법정 청약철회·업무 시작 전 고객 취소·허위/중복 요청·시스템 오류·관리자 승인 사유 시 FeeRestore',
                    [note] = N'선택 견적금액에 따라 FEE-Q1~FEE-Q7 구간 적용; 현장실측·예비견적·미선택 공급자에는 차감하지 않음',
                    [is_active] = 1,
                    [updated_at] = SYSUTCDATETIME()
                WHERE [code] = 'FEE-I1';

                UPDATE category_policy
                SET category_policy.[policy_kind_code] = 'PROJECT',
                    category_policy.[transaction_type_code] = 'PROJECT',
                    category_policy.[calculation_method_text] = N'SELECTED_QUOTE_TIER:FEE-Q1-FEE-Q7',
                    category_policy.[fee_amount] = NULL,
                    category_policy.[min_base_amount] = 0,
                    category_policy.[max_base_amount] = 999999999,
                    category_policy.[currency_code] = 'KRW',
                    category_policy.[charge_timing_text] = N'고객이 최종 시공업체의 유효한 최신 견적을 선택할 때',
                    category_policy.[restore_rule_text] = N'법정 청약철회·업무 시작 전 고객 취소·허위/중복 요청·시스템 오류·관리자 승인 사유 시 FeeRestore',
                    category_policy.[is_active] = 1,
                    category_policy.[updated_at] = SYSUTCDATETIME()
                FROM [category_fee_policies] AS category_policy
                INNER JOIN [fee_policies] AS source_policy ON source_policy.[id] = category_policy.[source_fee_policy_id]
                WHERE source_policy.[code] = 'FEE-I1';
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_interior_projects_fee",
                table: "interior_projects",
                sql: "[fee_assessment_status_code] IN ('POLICY_PENDING','PENDING_SELECTION','NOT_APPLICABLE','ASSESSED')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_interior_projects_fee",
                table: "interior_projects");

            migrationBuilder.AlterColumn<string>(
                name: "fee_assessment_status_code",
                table: "interior_projects",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "POLICY_PENDING",
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldUnicode: false,
                oldMaxLength: 30,
                oldDefaultValue: "PENDING_SELECTION");

            migrationBuilder.Sql("""
                UPDATE [interior_projects]
                SET [fee_assessment_status_code] = 'POLICY_PENDING'
                WHERE [fee_assessment_status_code] = 'PENDING_SELECTION';

                UPDATE [fee_policies]
                SET [applies_to_text] = N'인테리어 현장실측',
                    [calculation_method_text] = NULL,
                    [display_fee_amount] = 30000,
                    [charge_timing_text] = N'수요자 업체 채택 시',
                    [restore_rule_text] = N'실측 전 취소·본사승인 사유 시 복원',
                    [note] = N'본계약 중개수수료는 별도 정책 가능',
                    [updated_at] = SYSUTCDATETIME()
                WHERE [code] = 'FEE-I1';
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_interior_projects_fee",
                table: "interior_projects",
                sql: "[fee_assessment_status_code] IN ('POLICY_PENDING','NOT_APPLICABLE','ASSESSED')");
        }
    }
}
