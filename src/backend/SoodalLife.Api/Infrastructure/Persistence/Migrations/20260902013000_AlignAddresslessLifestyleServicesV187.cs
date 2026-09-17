using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SoodalLife.Api.Infrastructure.Persistence;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoodalLifeDbContext))]
[Migration("20260902013000_AlignAddresslessLifestyleServicesV187")]
public sealed class AlignAddresslessLifestyleServicesV187 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE policy
            SET coverage_type_code = CASE
                    WHEN service.name LIKE N'%배송%' OR service.name LIKE N'%택배%' OR service.name LIKE N'%수거%'
                        OR policy.onsite_requirement_text LIKE N'%배송%' OR policy.onsite_requirement_text LIKE N'%택배%'
                        OR policy.onsite_requirement_text LIKE N'%수거%' THEN 'NATIONWIDE_DELIVERY'
                    ELSE 'NATIONWIDE_REMOTE'
                END,
                updated_at = SYSUTCDATETIME()
            FROM category_operation_policies policy
            INNER JOIN service_categories service ON service.id = policy.category_id AND service.level_code = 'SERVICE'
            INNER JOIN service_categories middle ON middle.id = service.parent_id AND middle.level_code = 'MIDDLE'
            INNER JOIN service_categories major ON major.id = middle.parent_id AND major.level_code = 'MAJOR'
            WHERE major.name = N'생활서비스' AND policy.is_active = 1
              AND (
                    service.name LIKE N'%홈페이지%' OR service.name LIKE N'%쇼핑몰%' OR service.name LIKE N'%웹사이트%'
                    OR service.name LIKE N'%앱 개발%' OR service.name LIKE N'%온라인 광고%' OR service.name LIKE N'%SNS%'
                    OR service.name LIKE N'%블로그%' OR service.name LIKE N'%로고%' OR service.name LIKE N'%디자인%'
                    OR service.name LIKE N'%배송%' OR service.name LIKE N'%택배%' OR service.name LIKE N'%수거%'
                    OR policy.onsite_requirement_text LIKE N'%원격%' OR policy.onsite_requirement_text LIKE N'%온라인%'
                    OR policy.onsite_requirement_text LIKE N'%비대면%' OR policy.onsite_requirement_text LIKE N'%방문 불필요%'
                    OR policy.onsite_requirement_text LIKE N'%출장 불필요%'
                  );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Data alignment is intentionally not reversed because the previous value cannot be
        // reconstructed safely after later policy edits.
    }
}
