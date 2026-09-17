SET NOCOUNT ON;
IF COL_LENGTH(N'dbo.category_operation_policies',N'coverage_type_code') IS NULL THROW 51000,'V180 coverage type column is missing.',1;
IF EXISTS(SELECT 1 FROM dbo.category_operation_policies WHERE coverage_type_code NOT IN ('LOCAL_ONLY','NATIONWIDE_REMOTE','NATIONWIDE_DELIVERY','NATIONWIDE_NETWORK','FLEXIBLE')) THROW 51000,'V180 invalid coverage type.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260901090000_AddAutonomousServiceCoverageV180') THROW 51000,'V180 migration marker is missing.',1;
SELECT 'V180_OK' verification_result, coverage_type_code, COUNT_BIG(*) policy_count FROM dbo.category_operation_policies GROUP BY coverage_type_code ORDER BY coverage_type_code;
