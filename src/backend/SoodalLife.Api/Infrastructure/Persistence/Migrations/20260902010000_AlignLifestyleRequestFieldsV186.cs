using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SoodalLife.Api.Infrastructure.Persistence;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoodalLifeDbContext))]
[Migration("20260902010000_AlignLifestyleRequestFieldsV186")]
public sealed class AlignLifestyleRequestFieldsV186 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DECLARE @Fields TABLE (
                source_field_id varchar(50) NOT NULL,
                field_key varchar(100) NOT NULL,
                label nvarchar(200) NOT NULL,
                validation_text nvarchar(1000) NOT NULL,
                is_required bit NULL
            );

            INSERT INTO @Fields VALUES
                ('FLD-00633','task_detail',N'필요한 심부름 내용',N'구매·수령·전달 등 필요한 업무와 완료 기준',NULL),
                ('FLD-00634','item_or_task_count',N'품목·건수',N'대상 품목 수 또는 처리할 업무 건수',NULL),
                ('FLD-00635','pickup_delivery_condition',N'수령·전달 조건',N'대리 수령 여부, 출입 방법, 전달 시 유의사항',NULL),
                ('FLD-00643','support_detail',N'필요한 도움 내용',N'동행·돌봄 중 필요한 지원과 우선 확인 사항',NULL),
                ('FLD-00644','participant_count',N'대상 인원',N'서비스를 함께 이용하거나 돌봄이 필요한 인원',NULL),
                ('FLD-00645','care_precautions',N'이동·돌봄 유의사항',N'건강 상태, 이동 보조, 보호자 인계 등 주의사항',NULL),
                ('FLD-00653','pet_service_detail',N'반려동물 상태·요청',N'반려동물의 상태와 원하는 돌봄·관리 내용',NULL),
                ('FLD-00654','pet_profile',N'반려동물 수·종류',N'마릿수, 동물 종류, 품종·크기 등 기본 정보',NULL),
                ('FLD-00655','care_environment',N'돌봄 환경·주의사항',N'낯가림, 공격성, 알레르기, 출입 방법 등 유의사항',NULL),
                ('FLD-00679','vehicle_request_detail',N'차량 서비스 요청 내용',N'차량 상태와 필요한 작업을 구체적으로 입력',NULL),
                ('FLD-00683','vehicle_location',N'차량 현재 위치',N'현재 차량이 있는 위치 또는 인계 가능한 장소',0),
                ('FLD-00684','destination',N'이동·입고 목적지',N'견인·탁송·입출고 서비스에서만 입력',0),
                ('FLD-00685','vehicle_info',N'차량 정보',N'제조사·차종·연식·연료 등 작업에 필요한 정보',1),
                ('FLD-00686','vehicle_condition',N'차량 상태·운행 가능 여부',N'시동·주행 가능 여부와 경고등·파손 상태',0),
                ('FLD-00687','access_condition',N'차량 위치·진입 조건',N'지하주차장 높이, 도로 폭, 견인차 진입 등 조건',0),
                ('FLD-00693','event_request_detail',N'행사·서비스 요청',N'행사 목적과 필요한 준비·진행 범위',NULL),
                ('FLD-00694','guest_or_item_count',N'참석 인원·필요 수량',N'예상 참석 인원 또는 준비할 품목 수량',NULL),
                ('FLD-00695','venue_condition',N'행사 장소·설치 조건',N'장소 규모, 반입·설치 시간, 전기·주차 등 조건',NULL),
                ('FLD-00703','care_request_detail',N'원하는 관리·상담 내용',N'원하는 관리 부위·스타일·상담 목적',NULL),
                ('FLD-00704','participant_count',N'이용 인원',N'서비스를 받을 인원',NULL),
                ('FLD-00705','care_precautions',N'건강·시술 유의사항',N'알레르기, 피부 상태, 복용 약 등 사전 전달 사항',NULL),
                ('FLD-00713','project_requirements',N'프로젝트 요구사항',N'필요 기능, 대상 고객, 참고 사례와 완료 기준',NULL),
                ('FLD-00714','deliverables',N'업무 범위·산출물',N'필요 페이지·콘텐츠·디자인·문서 등 산출 범위',NULL),
                ('FLD-00715','work_environment',N'자료·시스템 환경',N'보유 자료, 도메인·호스팅·계정, 기존 시스템 환경',NULL);

            UPDATE field
            SET field_key = source.field_key,
                label = source.label,
                validation_rule_text = source.validation_text,
                options_or_unit_text = source.validation_text,
                is_required = COALESCE(source.is_required, field.is_required),
                updated_at = SYSUTCDATETIME()
            FROM category_field_definitions field
            INNER JOIN @Fields source ON source.source_field_id = field.source_field_id;

            UPDATE assignment
            SET is_required = source.is_required, updated_at = SYSUTCDATETIME()
            FROM category_field_assignments assignment
            INNER JOIN category_field_definitions field ON field.id = assignment.field_definition_id
            INNER JOIN @Fields source ON source.source_field_id = field.source_field_id
            WHERE source.is_required IS NOT NULL;

            UPDATE assignment
            SET is_active = 0, updated_at = SYSUTCDATETIME()
            FROM category_field_assignments assignment
            INNER JOIN category_field_definitions field ON field.id = assignment.field_definition_id
            WHERE field.source_field_id = 'FLD-00684';

            MERGE category_field_assignments AS target
            USING (
                SELECT field.id AS field_definition_id, service.id AS target_category_id, field.display_order
                FROM category_field_definitions field
                CROSS APPLY STRING_SPLIT(N'견인|차량 탁송|정비소 입·출고|공항 차량 전달|단기렌트 연계|장기렌트 연계|사고대차 연계', N'|') service_name
                INNER JOIN service_categories major ON major.parent_id IS NULL AND major.level_code = 'MAJOR' AND major.name = N'생활서비스'
                INNER JOIN service_categories middle ON middle.parent_id = major.id AND middle.level_code = 'MIDDLE' AND middle.name = N'자동차'
                INNER JOIN service_categories service ON service.parent_id = middle.id AND service.level_code = 'SERVICE' AND service.name = service_name.value
                WHERE field.source_field_id = 'FLD-00684'
            ) AS source
            ON target.field_definition_id = source.field_definition_id AND target.target_category_id = source.target_category_id
            WHEN MATCHED THEN UPDATE SET scope_code = 'SERVICE', is_required = 0, display_order = source.display_order, is_active = 1, updated_at = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN INSERT (field_definition_id,target_category_id,scope_code,is_required,display_order,is_active,created_at,updated_at)
                VALUES (source.field_definition_id,source.target_category_id,'SERVICE',0,source.display_order,1,SYSUTCDATETIME(),SYSUTCDATETIME());
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE assignment SET is_active = CASE WHEN assignment.scope_code = 'MIDDLE' THEN 1 ELSE 0 END, updated_at = SYSUTCDATETIME()
            FROM category_field_assignments assignment
            INNER JOIN category_field_definitions field ON field.id = assignment.field_definition_id
            WHERE field.source_field_id = 'FLD-00684';

            UPDATE field SET
                field_key = CASE source_field_id
                    WHEN 'FLD-00679' THEN 'request_detail' WHEN 'FLD-00683' THEN 'origin' WHEN 'FLD-00684' THEN 'destination'
                    WHEN 'FLD-00685' THEN 'volume' WHEN 'FLD-00686' THEN 'floor_elevator' WHEN 'FLD-00687' THEN 'parking'
                    WHEN 'FLD-00633' THEN 'issue' WHEN 'FLD-00643' THEN 'issue' WHEN 'FLD-00653' THEN 'issue' WHEN 'FLD-00693' THEN 'issue' WHEN 'FLD-00703' THEN 'issue' WHEN 'FLD-00713' THEN 'issue'
                    WHEN 'FLD-00634' THEN 'quantity' WHEN 'FLD-00644' THEN 'quantity' WHEN 'FLD-00654' THEN 'quantity' WHEN 'FLD-00694' THEN 'quantity' WHEN 'FLD-00704' THEN 'quantity' WHEN 'FLD-00714' THEN 'quantity'
                    ELSE 'site_condition' END,
                label = CASE source_field_id
                    WHEN 'FLD-00679' THEN N'요청 내용' WHEN 'FLD-00683' THEN N'출발지' WHEN 'FLD-00684' THEN N'도착지'
                    WHEN 'FLD-00685' THEN N'물량·차량정보' WHEN 'FLD-00686' THEN N'층수·엘리베이터' WHEN 'FLD-00687' THEN N'주차·진입조건'
                    WHEN 'FLD-00633' THEN N'요청·증상 상세' WHEN 'FLD-00643' THEN N'요청·증상 상세' WHEN 'FLD-00653' THEN N'요청·증상 상세' WHEN 'FLD-00693' THEN N'요청·증상 상세' WHEN 'FLD-00703' THEN N'요청·증상 상세' WHEN 'FLD-00713' THEN N'요청·증상 상세'
                    WHEN 'FLD-00634' THEN N'수량·규모' WHEN 'FLD-00644' THEN N'수량·규모' WHEN 'FLD-00654' THEN N'수량·규모' WHEN 'FLD-00694' THEN N'수량·규모' WHEN 'FLD-00704' THEN N'수량·규모' WHEN 'FLD-00714' THEN N'수량·규모'
                    ELSE N'현장 조건' END,
                updated_at = SYSUTCDATETIME()
            FROM category_field_definitions field
            WHERE source_field_id IN ('FLD-00633','FLD-00634','FLD-00635','FLD-00643','FLD-00644','FLD-00645','FLD-00653','FLD-00654','FLD-00655','FLD-00679','FLD-00683','FLD-00684','FLD-00685','FLD-00686','FLD-00687','FLD-00693','FLD-00694','FLD-00695','FLD-00703','FLD-00704','FLD-00705','FLD-00713','FLD-00714','FLD-00715');
            """);
    }
}
