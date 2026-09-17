SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.service_requests', N'abuse_count_excluded') IS NULL
    ALTER TABLE dbo.service_requests ADD abuse_count_excluded bit NOT NULL CONSTRAINT DF_service_requests_abuse_count_excluded DEFAULT(0) WITH VALUES;
IF COL_LENGTH(N'dbo.service_requests', N'abuse_exclusion_reason') IS NULL
    ALTER TABLE dbo.service_requests ADD abuse_exclusion_reason nvarchar(500) NULL;
IF COL_LENGTH(N'dbo.service_requests', N'abuse_fingerprint') IS NULL
    ALTER TABLE dbo.service_requests ADD abuse_fingerprint char(64) NULL;
IF COL_LENGTH(N'dbo.service_requests', N'abuse_policy_version') IS NULL
    ALTER TABLE dbo.service_requests ADD abuse_policy_version smallint NULL;
IF COL_LENGTH(N'dbo.service_requests', N'customer_quotes_viewed_at') IS NULL
    ALTER TABLE dbo.service_requests ADD customer_quotes_viewed_at datetime2(7) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.service_requests') AND name=N'IX_service_requests_customer_profile_id_category_id_opened_at')
    CREATE INDEX IX_service_requests_customer_profile_id_category_id_opened_at ON dbo.service_requests(customer_profile_id, category_id, opened_at);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.service_requests') AND name=N'IX_service_requests_customer_profile_id_abuse_fingerprint_opened_at')
    CREATE INDEX IX_service_requests_customer_profile_id_abuse_fingerprint_opened_at ON dbo.service_requests(customer_profile_id, abuse_fingerprint, opened_at);

IF OBJECT_ID(N'dbo.__EFMigrationsHistory',N'U') IS NOT NULL
   AND NOT EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260828065008_AddCustomerRequestAbuseProtectionV155')
    INSERT dbo.__EFMigrationsHistory(MigrationId,ProductVersion) VALUES(N'20260828065008_AddCustomerRequestAbuseProtectionV155',N'10.0.10');

COMMIT;
GO
SELECT 'V155_APPLY_OK' result,
       COUNT_BIG(*) request_count,
       SUM(CASE WHEN abuse_count_excluded=1 THEN 1 ELSE 0 END) excluded_count,
       SUM(CASE WHEN customer_quotes_viewed_at IS NOT NULL THEN 1 ELSE 0 END) viewed_count
FROM dbo.service_requests;
