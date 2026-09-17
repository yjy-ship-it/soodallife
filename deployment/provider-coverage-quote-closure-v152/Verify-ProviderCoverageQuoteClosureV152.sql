SET NOCOUNT ON;
IF COL_LENGTH(N'dbo.provider_service_categories',N'is_nationwide') IS NULL THROW 51000,'provider service nationwide column is missing',1;
IF EXISTS(SELECT 1 FROM dbo.provider_service_categories WHERE is_nationwide IS NULL) THROW 51000,'provider service nationwide value is invalid',1;
IF OBJECT_ID(N'dbo.__EFMigrationsHistory',N'U') IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260827234117_AddProviderCoverageLimitsV152') THROW 51000,'V152 migration history is missing',1;
SELECT 'V152_OK' verification_result,COUNT_BIG(*) provider_service_count,SUM(CASE WHEN is_nationwide=1 THEN 1 ELSE 0 END) nationwide_count FROM dbo.provider_service_categories;
