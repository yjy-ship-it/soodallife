SET NOCOUNT ON;

IF EXISTS(
    SELECT 1 FROM category_field_definitions
    WHERE REPLACE(REPLACE(REPLACE(validation_rule_text,N' ',N''),N'.',N'·'),N'ㆍ',N'·')
          IN (N'형식·길이검증',N'형식·길기검증')
) THROW 51000,N'고객용 요청 항목에 일반 형식·길이 검증 문구가 남아 있습니다.',1;

IF EXISTS(SELECT 1 FROM category_field_definitions WHERE unit_text LIKE N'%NOT_INTEGRATED%')
    THROW 51000,N'고객용 요청 안내에 내부 연동 코드가 남아 있습니다.',1;

IF NOT EXISTS(SELECT 1 FROM service_categories WHERE level_code='SERVICE' AND status_code='ACTIVE' AND name LIKE N'%보일러%')
    THROW 51000,N'보일러 서비스가 없습니다.',1;

IF EXISTS(
    SELECT 1
    FROM service_categories boiler
    WHERE boiler.level_code='SERVICE' AND boiler.status_code='ACTIVE' AND boiler.name LIKE N'%보일러%'
      AND NOT EXISTS(
          SELECT 1 FROM category_field_assignments assignment
          JOIN category_field_definitions field ON field.id=assignment.field_definition_id
          WHERE assignment.target_category_id=boiler.id AND assignment.is_active=1
            AND field.status_code='ACTIVE' AND field.field_key='boiler_error_code_v215'
            AND assignment.is_required=0 AND field.label=N'보일러에 표시되는 오류 번호'
      )
) THROW 51000,N'보일러 오류 번호 선택 항목 적용에 실패했습니다.',1;

IF EXISTS(
    SELECT 1
    FROM service_categories boiler
    WHERE boiler.level_code='SERVICE' AND boiler.status_code='ACTIVE' AND boiler.name LIKE N'%보일러%'
      AND NOT EXISTS(
          SELECT 1 FROM category_field_assignments assignment
          JOIN category_field_definitions field ON field.id=assignment.field_definition_id
          WHERE assignment.target_category_id=boiler.id AND assignment.is_active=1
            AND field.status_code='ACTIVE' AND field.field_key='boiler_install_location_v215'
            AND assignment.is_required=0 AND field.label=N'보일러 설치 장소'
      )
) THROW 51000,N'보일러 설치 장소 항목 적용에 실패했습니다.',1;

IF EXISTS(
    SELECT field.id FROM category_field_definitions field
    WHERE field.field_key='boiler_install_location_v215'
      AND (SELECT COUNT(*) FROM category_field_options optionRow WHERE optionRow.field_definition_id=field.id AND optionRow.is_active=1)<>6
) THROW 51000,N'보일러 설치 장소 선택값 검증에 실패했습니다.',1;

SELECT 'V215_OK' AS verification_result,
       (SELECT COUNT(*) FROM service_categories WHERE level_code='SERVICE' AND status_code='ACTIVE' AND name LIKE N'%보일러%') AS boiler_service_count,
       (SELECT COUNT(*) FROM category_field_definitions WHERE field_key IN ('boiler_error_code_v215','boiler_install_location_v215')) AS boiler_field_count,
       (SELECT COUNT(*) FROM category_field_definitions WHERE validation_rule_text<>N'') AS active_validation_rule_count;
