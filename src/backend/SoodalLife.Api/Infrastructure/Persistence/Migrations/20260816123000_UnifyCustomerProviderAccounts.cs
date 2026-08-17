using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoodalLifeDbContext))]
[Migration("20260816123000_UnifyCustomerProviderAccounts")]
public partial class UnifyCustomerProviderAccounts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DECLARE @CustomerRoleId bigint =
            (
                SELECT TOP (1) id
                FROM roles
                WHERE code = 'CUSTOMER' AND is_active = 1
            );

            IF @CustomerRoleId IS NULL
                THROW 51000, 'Active CUSTOMER role is required before account unification.', 1;

            INSERT INTO user_roles (user_id, role_id, granted_at)
            SELECT DISTINCT provider_role.user_id, @CustomerRoleId, SYSUTCDATETIME()
            FROM user_roles provider_role
            INNER JOIN roles provider_role_definition
                ON provider_role_definition.id = provider_role.role_id
               AND provider_role_definition.code = 'PROVIDER'
            INNER JOIN users account
                ON account.id = provider_role.user_id
               AND account.status_code = 'ACTIVE'
            WHERE provider_role.revoked_at IS NULL
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM user_roles customer_role
                  WHERE customer_role.user_id = provider_role.user_id
                    AND customer_role.role_id = @CustomerRoleId
                    AND customer_role.revoked_at IS NULL
              );

            INSERT INTO customer_profiles
                (public_id, user_id, display_name, created_at, updated_at)
            SELECT
                NEWID(),
                provider.user_id,
                LEFT(COALESCE(NULLIF(provider.representative_name, N''),
                              NULLIF(provider.contact_name, N''),
                              NULLIF(provider.business_name, N''),
                              account.login_id), 100),
                SYSUTCDATETIME(),
                SYSUTCDATETIME()
            FROM provider_profiles provider
            INNER JOIN users account
                ON account.id = provider.user_id
               AND account.status_code = 'ACTIVE'
            WHERE NOT EXISTS
            (
                SELECT 1
                FROM customer_profiles customer
                WHERE customer.user_id = provider.user_id
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // 역할 및 고객 프로필은 운영 중 생성되는 업무 데이터의 소유자가 될 수 있으므로 자동 삭제하지 않는다.
    }
}
