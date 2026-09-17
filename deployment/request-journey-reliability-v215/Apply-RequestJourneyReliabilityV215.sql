SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

UPDATE category_field_definitions
SET validation_rule_text=N'',updated_at=SYSUTCDATETIME()
WHERE REPLACE(REPLACE(REPLACE(validation_rule_text,N' ',N''),N'.',N'·'),N'ㆍ',N'·')
      IN (N'형식·길이검증',N'형식·길기검증');

UPDATE category_field_definitions
SET unit_text=NULL,updated_at=SYSUTCDATETIME()
WHERE unit_text LIKE N'%NOT_INTEGRATED%';

DECLARE @BoilerServices TABLE(service_id bigint PRIMARY KEY,middle_id bigint NOT NULL);
INSERT @BoilerServices(service_id,middle_id)
SELECT id,parent_id FROM service_categories
WHERE level_code='SERVICE' AND status_code='ACTIVE' AND name LIKE N'%보일러%';

IF NOT EXISTS(SELECT 1 FROM @BoilerServices)
    THROW 51000,N'보일러 서비스 카테고리를 찾을 수 없습니다.',1;

DECLARE @OldBoilerFields TABLE(field_definition_id bigint NOT NULL,service_id bigint NOT NULL,PRIMARY KEY(field_definition_id,service_id));
INSERT @OldBoilerFields(field_definition_id,service_id)
SELECT DISTINCT field.id,boiler.service_id
FROM @BoilerServices boiler
JOIN category_field_assignments assignment ON assignment.target_category_id IN (boiler.middle_id,boiler.service_id)
JOIN category_field_definitions field ON field.id=assignment.field_definition_id
WHERE field.label LIKE N'%오류%코드%' OR field.label=N'설치 환경';

MERGE category_field_assignments AS target
USING @OldBoilerFields AS source
ON target.field_definition_id=source.field_definition_id AND target.target_category_id=source.service_id
WHEN MATCHED THEN UPDATE SET scope_code='SERVICE',is_required=0,is_active=0,updated_at=SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT(field_definition_id,target_category_id,scope_code,is_required,display_order,is_active,created_at,updated_at)
VALUES(source.field_definition_id,source.service_id,'SERVICE',0,900,0,SYSUTCDATETIME(),SYSUTCDATETIME());

MERGE category_field_definitions AS target
USING (SELECT DISTINCT middle_id FROM @BoilerServices) AS source
ON target.owner_middle_category_id=source.middle_id AND target.field_key='boiler_error_code_v215'
WHEN MATCHED THEN UPDATE SET label=N'보일러에 표시되는 오류 번호',field_type_code='TEXT',is_required=0,
    options_or_unit_text=N'오류 번호가 없거나 확인하기 어려우면 비워 두셔도 됩니다.',unit_text=N'오류 번호가 없거나 확인하기 어려우면 비워 두셔도 됩니다.',
    validation_rule_text=N'',display_order=31,status_code='ACTIVE',updated_at=SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT(public_id,source_field_id,owner_middle_category_id,field_key,label,field_type_code,is_required,options_or_unit_text,unit_text,provider_visibility_code,pre_accept_masking_code,validation_rule_text,display_order,status_code,created_at,updated_at)
VALUES(NEWID(),CONCAT('V215-BLR-ERR-',source.middle_id),source.middle_id,'boiler_error_code_v215',N'보일러에 표시되는 오류 번호','TEXT',0,N'오류 번호가 없거나 확인하기 어려우면 비워 두셔도 됩니다.',N'오류 번호가 없거나 확인하기 어려우면 비워 두셔도 됩니다.','FULL','NONE',N'',31,'ACTIVE',SYSUTCDATETIME(),SYSUTCDATETIME());

MERGE category_field_definitions AS target
USING (SELECT DISTINCT middle_id FROM @BoilerServices) AS source
ON target.owner_middle_category_id=source.middle_id AND target.field_key='boiler_install_location_v215'
WHEN MATCHED THEN UPDATE SET label=N'보일러 설치 장소',field_type_code='SELECT',is_required=0,
    options_or_unit_text=N'실내|실외|베란다|보일러실|기타|잘 모름',unit_text=N'현재 보일러가 설치된 장소를 선택해 주세요.',
    validation_rule_text=N'',display_order=32,status_code='ACTIVE',updated_at=SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT(public_id,source_field_id,owner_middle_category_id,field_key,label,field_type_code,is_required,options_or_unit_text,unit_text,provider_visibility_code,pre_accept_masking_code,validation_rule_text,display_order,status_code,created_at,updated_at)
VALUES(NEWID(),CONCAT('V215-BLR-LOC-',source.middle_id),source.middle_id,'boiler_install_location_v215',N'보일러 설치 장소','SELECT',0,N'실내|실외|베란다|보일러실|기타|잘 모름',N'현재 보일러가 설치된 장소를 선택해 주세요.','FULL','NONE',N'',32,'ACTIVE',SYSUTCDATETIME(),SYSUTCDATETIME());

MERGE category_field_assignments AS target
USING (
    SELECT field.id AS field_definition_id,boiler.service_id,field.display_order
    FROM @BoilerServices boiler
    JOIN category_field_definitions field ON field.owner_middle_category_id=boiler.middle_id
      AND field.field_key IN ('boiler_error_code_v215','boiler_install_location_v215')
) AS source
ON target.field_definition_id=source.field_definition_id AND target.target_category_id=source.service_id
WHEN MATCHED THEN UPDATE SET scope_code='SERVICE',is_required=0,display_order=source.display_order,is_active=1,updated_at=SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT(field_definition_id,target_category_id,scope_code,is_required,display_order,is_active,created_at,updated_at)
VALUES(source.field_definition_id,source.service_id,'SERVICE',0,source.display_order,1,SYSUTCDATETIME(),SYSUTCDATETIME());

DECLARE @BoilerOptions TABLE(value nvarchar(2000) NOT NULL,label nvarchar(2000) NOT NULL,display_order int NOT NULL);
INSERT @BoilerOptions VALUES(N'실내',N'실내',1),(N'실외',N'실외',2),(N'베란다',N'베란다',3),(N'보일러실',N'보일러실',4),(N'기타',N'기타',5),(N'잘 모름',N'잘 모름',6);

UPDATE optionRow SET is_active=0,updated_at=SYSUTCDATETIME()
FROM category_field_options optionRow
JOIN category_field_definitions field ON field.id=optionRow.field_definition_id
WHERE field.field_key='boiler_install_location_v215';

MERGE category_field_options AS target
USING (
    SELECT field.id AS field_definition_id,optionRow.value,optionRow.label,optionRow.display_order
    FROM category_field_definitions field CROSS JOIN @BoilerOptions optionRow
    WHERE field.field_key='boiler_install_location_v215'
) AS source
ON target.field_definition_id=source.field_definition_id AND target.value=source.value
WHEN MATCHED THEN UPDATE SET label=source.label,display_order=source.display_order,is_active=1,updated_at=SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT(public_id,field_definition_id,value,label,display_order,is_active,created_at,updated_at)
VALUES(NEWID(),source.field_definition_id,source.value,source.label,source.display_order,1,SYSUTCDATETIME(),SYSUTCDATETIME());

IF OBJECT_ID('__EFMigrationsHistory','U') IS NOT NULL
   AND NOT EXISTS(SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId='20260907190000_HardenCustomerRequestJourneyV215')
    INSERT __EFMigrationsHistory(MigrationId,ProductVersion) VALUES('20260907190000_HardenCustomerRequestJourneyV215','10.0.4');

COMMIT TRANSACTION;
