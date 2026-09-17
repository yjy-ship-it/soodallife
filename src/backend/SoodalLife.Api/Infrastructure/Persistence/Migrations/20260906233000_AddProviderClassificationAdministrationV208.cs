using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoodalLifeDbContext))]
[Migration("20260906233000_AddProviderClassificationAdministrationV208")]
public partial class AddProviderClassificationAdministrationV208 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "provider_type_code",
            table: "provider_profiles",
            type: "varchar(20)",
            unicode: false,
            maxLength: 20,
            nullable: false,
            defaultValue: "BUSINESS");

        migrationBuilder.Sql("UPDATE provider_profiles SET provider_type_code = CASE WHEN business_registration_no IS NULL THEN 'INDIVIDUAL' ELSE 'BUSINESS' END;");
        migrationBuilder.CreateIndex(name: "IX_provider_profiles_provider_type_code", table: "provider_profiles", column: "provider_type_code");
        migrationBuilder.AddCheckConstraint(name: "CK_provider_profiles_provider_type", table: "provider_profiles", sql: "[provider_type_code] IN ('BUSINESS','INDIVIDUAL')");
        migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM provider_document_types WHERE code = 'BUSINESS_REGISTRATION_CERTIFICATE')
INSERT provider_document_types(public_id,code,name,supports_expiry,is_active,created_at,updated_at)
VALUES(NEWID(),'BUSINESS_REGISTRATION_CERTIFICATE',N'사업자등록증',0,1,SYSUTCDATETIME(),SYSUTCDATETIME());
IF NOT EXISTS (SELECT 1 FROM provider_document_types WHERE code = 'IDENTITY_CARD')
INSERT provider_document_types(public_id,code,name,supports_expiry,is_active,created_at,updated_at)
VALUES(NEWID(),'IDENTITY_CARD',N'신분증 앞면',0,1,SYSUTCDATETIME(),SYSUTCDATETIME());
UPDATE a SET approval_status_code='APPROVED', approval_decided_at=COALESCE(approval_decided_at,SYSUTCDATETIME()),
decision_reason=N'전문가 등록 카테고리 시스템 자동 승인', updated_at=SYSUTCDATETIME()
FROM provider_service_approvals a
JOIN provider_service_categories s ON s.id=a.provider_service_category_id
WHERE s.status_code='ACTIVE' AND a.approval_status_code<>'APPROVED';
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(name: "CK_provider_profiles_provider_type", table: "provider_profiles");
        migrationBuilder.DropIndex(name: "IX_provider_profiles_provider_type_code", table: "provider_profiles");
        migrationBuilder.DropColumn(name: "provider_type_code", table: "provider_profiles");
    }
}
