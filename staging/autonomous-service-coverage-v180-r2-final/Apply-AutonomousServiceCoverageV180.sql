SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.category_operation_policies', N'coverage_type_code') IS NULL
BEGIN
    ALTER TABLE dbo.category_operation_policies ADD coverage_type_code varchar(30) NOT NULL
        CONSTRAINT DF_category_operation_policies_coverage_type_code DEFAULT ('LOCAL_ONLY');
END;

EXEC sys.sp_executesql N'
UPDATE policy
SET coverage_type_code = CASE
    WHEN category.name LIKE N''%홈페이지%'' OR category.name LIKE N''%쇼핑몰%'' OR category.name LIKE N''%웹사이트%''
      OR category.name LIKE N''%앱 개발%'' OR category.name LIKE N''%온라인 광고%'' OR category.name LIKE N''%SNS%''
      OR category.name LIKE N''%블로그%'' OR category.name LIKE N''%로고%'' OR category.name LIKE N''%디자인%''
      OR policy.onsite_requirement_text LIKE N''%원격%'' OR policy.onsite_requirement_text LIKE N''%온라인%''
      OR policy.onsite_requirement_text LIKE N''%비대면%'' THEN ''NATIONWIDE_REMOTE''
    WHEN category.name LIKE N''%배송%'' OR category.name LIKE N''%택배%''
      OR policy.onsite_requirement_text LIKE N''%배송%'' OR policy.onsite_requirement_text LIKE N''%택배%'' THEN ''NATIONWIDE_DELIVERY''
    WHEN policy.matching_area_rule_text LIKE N''%전국 방문%'' OR policy.onsite_requirement_text LIKE N''%전국 방문%'' THEN ''NATIONWIDE_NETWORK''
    WHEN policy.matching_area_rule_text LIKE N''%전국%'' THEN ''FLEXIBLE''
    ELSE ''LOCAL_ONLY'' END
FROM dbo.category_operation_policies policy
INNER JOIN dbo.service_categories category ON category.id = policy.category_id;';

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_category_operation_policies_coverage_type')
    EXEC sys.sp_executesql N'ALTER TABLE dbo.category_operation_policies ADD CONSTRAINT CK_category_operation_policies_coverage_type
        CHECK (coverage_type_code IN (''LOCAL_ONLY'',''NATIONWIDE_REMOTE'',''NATIONWIDE_DELIVERY'',''NATIONWIDE_NETWORK'',''FLEXIBLE''));';

EXEC sys.sp_executesql N'
UPDATE approval SET approval_status_code=''APPROVED'', approval_decided_at=COALESCE(approval_decided_at,SYSUTCDATETIME()),
    decision_reason=N''전국 방문망 자율등록'', updated_at=SYSUTCDATETIME()
FROM dbo.provider_service_approvals approval
INNER JOIN dbo.provider_service_categories provider_service ON provider_service.id=approval.provider_service_category_id
INNER JOIN dbo.category_operation_policies policy ON policy.category_id=provider_service.category_id
WHERE policy.is_active=1 AND policy.coverage_type_code=''NATIONWIDE_NETWORK'' AND approval.approval_status_code<>''APPROVED'';';

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260901090000_AddAutonomousServiceCoverageV180')
    INSERT INTO dbo.__EFMigrationsHistory(MigrationId,ProductVersion) VALUES(N'20260901090000_AddAutonomousServiceCoverageV180',N'10.0.10');
COMMIT TRANSACTION;
