using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoodalLifeDbContext))]
[Migration("20260906130000_StandardizeSubscriptionFeeEightPercentV195")]
public partial class StandardizeSubscriptionFeeEightPercentV195 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DECLARE @EffectiveDate date = '2026-09-06';
            DECLARE @Now datetime2 = SYSUTCDATETIME();

            INSERT INTO category_fee_policies(
              public_id,category_id,legacy_category_policy_id,source_fee_policy_id,policy_version,
              policy_kind_code,transaction_type_code,calculation_method_text,fee_amount,min_base_amount,
              max_base_amount,rate,monthly_amount,per_visit_amount,currency_code,charge_timing_text,
              restore_rule_text,effective_from,effective_to,is_active,created_at,created_by_user_id,
              updated_at,updated_by_user_id)
            SELECT NEWID(),source.category_id,NULL,source.source_fee_policy_id,'SUB-8P-V195',
              'SUBSCRIPTION','SUBSCRIPTION',N'회차 결제금액의 8%',NULL,NULL,NULL,CAST(0.08 AS decimal(9,6)),
              NULL,NULL,'KRW',N'고객의 회차 완료 확인 및 결제 확인 후 8% 산정',
              N'채택되지 않은 견적 예약금은 종료 상태 확인 시 해제',@EffectiveDate,NULL,1,@Now,NULL,@Now,NULL
            FROM (
              SELECT operation.category_id,MAX(fee.source_fee_policy_id) AS source_fee_policy_id
              FROM category_operation_policies operation
              LEFT JOIN category_fee_policies fee ON fee.category_id=operation.category_id AND fee.policy_kind_code='SUBSCRIPTION'
              WHERE operation.subscription_option_text=N'허용'
              GROUP BY operation.category_id
            ) source
            WHERE NOT EXISTS (
              SELECT 1 FROM category_fee_policies target
              WHERE target.category_id=source.category_id AND target.policy_version='SUB-8P-V195');

            UPDATE category_fee_policies
            SET is_active=0,effective_to=CASE WHEN effective_from<@EffectiveDate THEN @EffectiveDate ELSE DATEADD(day,1,effective_from) END,
                updated_at=@Now
            WHERE policy_kind_code='SUBSCRIPTION' AND policy_version<>'SUB-8P-V195' AND is_active=1;

            UPDATE subscription_contracts
            SET fee_policy_snapshot_json=JSON_MODIFY(
                  JSON_MODIFY(
                    JSON_MODIFY(
                      JSON_MODIFY(
                        JSON_MODIFY(CASE WHEN ISJSON(fee_policy_snapshot_json)=1 THEN fee_policy_snapshot_json ELSE N'{}' END,'$.policyCode','SUB-RATE'),
                      '$.rate',CAST(0.08 AS decimal(9,6))),
                    '$.ratePercent',8),
                  '$.monthlyAmount',NULL),
                '$.perVisitAmount',NULL),
                updated_at=@Now
            WHERE status_code IN ('PAYMENT_PENDING','ACTIVE','PAUSED','TERMINATION_REQUESTED');

            IF EXISTS (
              SELECT 1 FROM category_fee_policies
              WHERE policy_kind_code='SUBSCRIPTION' AND is_active=1
              GROUP BY category_id
              HAVING COUNT(*)<>1 OR MAX(rate)<>CAST(0.08 AS decimal(9,6)) OR MAX(monthly_amount) IS NOT NULL OR MAX(per_visit_amount) IS NOT NULL)
              THROW 51000,'V195 subscription fee policy verification failed.',1;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE category_fee_policies SET is_active=0,effective_to=CAST(SYSUTCDATETIME() AS date),updated_at=SYSUTCDATETIME()
            WHERE policy_version='SUB-8P-V195';
            """);
    }
}
