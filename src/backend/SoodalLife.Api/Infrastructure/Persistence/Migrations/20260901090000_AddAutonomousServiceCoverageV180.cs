using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SoodalLife.Api.Infrastructure.Persistence;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoodalLifeDbContext))]
[Migration("20260901090000_AddAutonomousServiceCoverageV180")]
public sealed class AddAutonomousServiceCoverageV180 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "coverage_type_code", table: "category_operation_policies", type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "LOCAL_ONLY");
        migrationBuilder.Sql("""
            UPDATE policy
            SET coverage_type_code = CASE
                WHEN category.name LIKE N'%홈페이지%' OR category.name LIKE N'%쇼핑몰%' OR category.name LIKE N'%웹사이트%'
                    OR category.name LIKE N'%앱 개발%' OR category.name LIKE N'%온라인 광고%' OR category.name LIKE N'%SNS%'
                    OR category.name LIKE N'%블로그%' OR category.name LIKE N'%로고%' OR category.name LIKE N'%디자인%'
                    OR policy.onsite_requirement_text LIKE N'%원격%' OR policy.onsite_requirement_text LIKE N'%온라인%'
                    OR policy.onsite_requirement_text LIKE N'%비대면%' THEN 'NATIONWIDE_REMOTE'
                WHEN category.name LIKE N'%배송%' OR category.name LIKE N'%택배%'
                    OR policy.onsite_requirement_text LIKE N'%배송%' OR policy.onsite_requirement_text LIKE N'%택배%' THEN 'NATIONWIDE_DELIVERY'
                WHEN policy.matching_area_rule_text LIKE N'%전국 방문%' OR policy.onsite_requirement_text LIKE N'%전국 방문%' THEN 'NATIONWIDE_NETWORK'
                WHEN policy.matching_area_rule_text LIKE N'%전국%' THEN 'FLEXIBLE'
                ELSE 'LOCAL_ONLY'
            END
            FROM category_operation_policies policy
            INNER JOIN service_categories category ON category.id = policy.category_id;
            """);
        migrationBuilder.Sql("""
            UPDATE approval
            SET approval_status_code = 'APPROVED',
                approval_decided_at = COALESCE(approval_decided_at, SYSUTCDATETIME()),
                decision_reason = N'전국 방문망 자율등록',
                updated_at = SYSUTCDATETIME()
            FROM provider_service_approvals approval
            INNER JOIN provider_service_categories provider_service ON provider_service.id = approval.provider_service_category_id
            INNER JOIN category_operation_policies policy ON policy.category_id = provider_service.category_id
            WHERE policy.is_active = 1 AND policy.coverage_type_code = 'NATIONWIDE_NETWORK'
              AND approval.approval_status_code <> 'APPROVED';
            """);
        migrationBuilder.AddCheckConstraint(name: "CK_category_operation_policies_coverage_type", table: "category_operation_policies", sql: "[coverage_type_code] IN ('LOCAL_ONLY','NATIONWIDE_REMOTE','NATIONWIDE_DELIVERY','NATIONWIDE_NETWORK','FLEXIBLE')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(name: "CK_category_operation_policies_coverage_type", table: "category_operation_policies");
        migrationBuilder.DropColumn(name: "coverage_type_code", table: "category_operation_policies");
    }
}
