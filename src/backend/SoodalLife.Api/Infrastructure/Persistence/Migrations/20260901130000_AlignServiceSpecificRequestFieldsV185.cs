using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SoodalLife.Api.Infrastructure.Persistence;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoodalLifeDbContext))]
[Migration("20260901130000_AlignServiceSpecificRequestFieldsV185")]
public sealed class AlignServiceSpecificRequestFieldsV185 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE category_field_definitions
            SET field_key = 'target_quantity',
                label = CASE source_field_id
                    WHEN 'FLD-00047' THEN N'교체·설치 대상 수량'
                    WHEN 'FLD-00058' THEN N'수리·교체 대상 수량'
                    WHEN 'FLD-00089' THEN N'수리·교체 대상 수량'
                    ELSE N'청소 대상 수량'
                END,
                unit_text = CASE WHEN source_field_id IN ('FLD-00394', 'FLD-00404', 'FLD-00414') THEN N'대' ELSE N'개' END,
                options_or_unit_text = CASE WHEN source_field_id IN ('FLD-00394', 'FLD-00404', 'FLD-00414') THEN N'대' ELSE N'개' END,
                validation_rule_text = N'1 이상',
                updated_at = SYSUTCDATETIME()
            WHERE source_field_id IN ('FLD-00047', 'FLD-00058', 'FLD-00089', 'FLD-00394', 'FLD-00404', 'FLD-00414', 'FLD-00444');

            DECLARE @FieldTargets TABLE (
                source_field_id varchar(50) NOT NULL,
                major_name nvarchar(200) NOT NULL,
                middle_name nvarchar(200) NOT NULL,
                service_names nvarchar(max) NOT NULL
            );

            INSERT INTO @FieldTargets VALUES
                ('FLD-00047', N'집수리', N'욕실 수리', N'변기 교체|변기 부속 교체|세면대 교체|샤워기 교체|욕실장 설치|환풍기 교체|욕실 액세서리 설치'),
                ('FLD-00058', N'집수리', N'주방 수리', N'싱크대 수리|싱크볼 교체|주방 수전 교체|후드 교체|가스레인지 설치|상·하부장 수리|주방 배수 수리'),
                ('FLD-00089', N'집수리', N'창호·샷시', N'창문 수리|샷시 수리|창문 잠금장치 교체|창호 롤러 교체|유리 교체|창문 틈새 보수'),
                ('FLD-00394', N'청소·위생', N'에어컨 청소', N'벽걸이형|스탠드형|천장형|시스템에어컨|실외기 청소'),
                ('FLD-00404', N'청소·위생', N'세탁기 청소', N'통돌이 세탁기|드럼 세탁기|건조기|세탁기 배수구 청소'),
                ('FLD-00414', N'청소·위생', N'주방가전 청소', N'냉장고 청소|김치냉장고 청소|식기세척기 청소|오븐·전자레인지 청소'),
                ('FLD-00444', N'청소·위생', N'침구·가구 청소', N'매트리스 청소|소파 청소|카펫 청소|의자 청소|커튼 청소'),
                ('FLD-00724', N'정기구독', N'집관리 구독', N'전기 정기점검|수도·배관 정기점검|계절별 주택점검'),
                ('FLD-00814', N'정기구독', N'사업장 관리', N'정기청소|방역|시설점검'),
                ('FLD-00824', N'정기구독', N'빈집·장기부재 관리', N'환기|누수·시설 확인');

            UPDATE assignment
            SET is_active = 0, updated_at = SYSUTCDATETIME()
            FROM category_field_assignments assignment
            INNER JOIN category_field_definitions field ON field.id = assignment.field_definition_id
            INNER JOIN @FieldTargets requested ON requested.source_field_id = field.source_field_id;

            MERGE category_field_assignments AS target
            USING (
                SELECT field.id AS field_definition_id, service.id AS target_category_id,
                       field.is_required, field.display_order
                FROM @FieldTargets requested
                CROSS APPLY STRING_SPLIT(requested.service_names, N'|') service_name
                INNER JOIN category_field_definitions field ON field.source_field_id = requested.source_field_id
                INNER JOIN service_categories major ON major.parent_id IS NULL AND major.level_code = 'MAJOR' AND major.name = requested.major_name
                INNER JOIN service_categories middle ON middle.parent_id = major.id AND middle.level_code = 'MIDDLE' AND middle.name = requested.middle_name
                INNER JOIN service_categories service ON service.parent_id = middle.id AND service.level_code = 'SERVICE' AND service.name = service_name.value
            ) AS source
            ON target.field_definition_id = source.field_definition_id AND target.target_category_id = source.target_category_id
            WHEN MATCHED THEN UPDATE SET
                scope_code = 'SERVICE', is_required = source.is_required, display_order = source.display_order,
                is_active = 1, updated_at = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN
                INSERT (field_definition_id, target_category_id, scope_code, is_required, display_order, is_active, created_at, updated_at)
                VALUES (source.field_definition_id, source.target_category_id, 'SERVICE', source.is_required, source.display_order, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE assignment
            SET is_active = CASE WHEN assignment.scope_code = 'MIDDLE' THEN 1 ELSE 0 END,
                updated_at = SYSUTCDATETIME()
            FROM category_field_assignments assignment
            INNER JOIN category_field_definitions field ON field.id = assignment.field_definition_id
            WHERE field.source_field_id IN ('FLD-00047', 'FLD-00058', 'FLD-00089', 'FLD-00394', 'FLD-00404', 'FLD-00414', 'FLD-00444', 'FLD-00724', 'FLD-00814', 'FLD-00824');

            UPDATE category_field_definitions
            SET field_key = 'area', label = N'면적', unit_text = N'평 또는 ㎡',
                options_or_unit_text = N'평 또는 ㎡', validation_rule_text = N'평 또는 ㎡',
                updated_at = SYSUTCDATETIME()
            WHERE source_field_id IN ('FLD-00047', 'FLD-00058', 'FLD-00089', 'FLD-00394', 'FLD-00404', 'FLD-00414', 'FLD-00444');
            """);
    }
}
