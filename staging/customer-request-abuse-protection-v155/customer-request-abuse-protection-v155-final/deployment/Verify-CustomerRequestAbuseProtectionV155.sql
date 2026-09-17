SET NOCOUNT ON;
IF COL_LENGTH(N'dbo.service_requests',N'abuse_count_excluded') IS NULL THROW 51000,'abuse count exclusion column is missing',1;
IF COL_LENGTH(N'dbo.service_requests',N'abuse_exclusion_reason') IS NULL THROW 51000,'abuse exclusion reason column is missing',1;
IF COL_LENGTH(N'dbo.service_requests',N'abuse_fingerprint') IS NULL THROW 51000,'abuse fingerprint column is missing',1;
IF COL_LENGTH(N'dbo.service_requests',N'abuse_policy_version') IS NULL THROW 51000,'abuse policy version column is missing',1;
IF COL_LENGTH(N'dbo.service_requests',N'customer_quotes_viewed_at') IS NULL THROW 51000,'customer quote view column is missing',1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.service_requests') AND name=N'IX_service_requests_customer_profile_id_category_id_opened_at') THROW 51000,'category request limit index is missing',1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.service_requests') AND name=N'IX_service_requests_customer_profile_id_abuse_fingerprint_opened_at') THROW 51000,'duplicate request fingerprint index is missing',1;
IF EXISTS (SELECT 1 FROM dbo.service_requests WHERE abuse_count_excluded IS NULL) THROW 51000,'abuse count exclusion contains NULL',1;
IF OBJECT_ID(N'dbo.__EFMigrationsHistory',N'U') IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260828065008_AddCustomerRequestAbuseProtectionV155') THROW 51000,'V155 migration history is missing',1;
SELECT 'V155_OK' verification_result,
       COUNT_BIG(*) request_count,
       SUM(CASE WHEN abuse_policy_version=1 THEN 1 ELSE 0 END) protected_request_count,
       SUM(CASE WHEN abuse_count_excluded=1 THEN 1 ELSE 0 END) excluded_count
FROM dbo.service_requests;
