SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH(N'dbo.provider_service_categories',N'is_nationwide') IS NULL
BEGIN
 ALTER TABLE dbo.provider_service_categories ADD is_nationwide bit NOT NULL CONSTRAINT DF_provider_service_categories_is_nationwide DEFAULT(0) WITH VALUES;
END;
IF OBJECT_ID(N'dbo.__EFMigrationsHistory',N'U') IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260827234117_AddProviderCoverageLimitsV152')
 INSERT dbo.__EFMigrationsHistory(MigrationId,ProductVersion) VALUES(N'20260827234117_AddProviderCoverageLimitsV152',N'10.0.10');
COMMIT;
GO
SELECT 'V152_APPLY_OK' result,COUNT_BIG(*) provider_service_count,SUM(CASE WHEN is_nationwide=1 THEN 1 ELSE 0 END) nationwide_count FROM dbo.provider_service_categories;
