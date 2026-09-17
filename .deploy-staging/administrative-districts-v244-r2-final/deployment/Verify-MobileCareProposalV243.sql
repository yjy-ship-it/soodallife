SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.provider_quote_templates', N'U') IS NULL
    THROW 52410, 'V240 verification failed: provider_quote_templates is missing.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260909134137_AddProviderQuoteTemplatesV240')
    THROW 52411, 'V240 verification failed: migration history is missing.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.provider_quote_templates') AND name=N'IX_provider_quote_templates_provider_profile_id_name' AND is_unique=1)
    THROW 52412, 'V240 verification failed: provider/name unique index is missing.', 1;
IF COL_LENGTH(N'dbo.provider_quote_templates', N'items_json') IS NULL OR COL_LENGTH(N'dbo.provider_quote_templates', N'row_version') IS NULL
    THROW 52413, 'V240 verification failed: required columns are missing.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260910103000_AllowDeclinedDispatchV241')
    THROW 52414, 'V241 verification failed: migration history is missing.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.dispatch_candidates') AND name=N'CK_dispatch_candidates_status' AND definition LIKE '%DECLINED%')
    THROW 52415, 'V241 verification failed: candidate decline status is unavailable.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.request_dispatches') AND name=N'CK_request_dispatches_status' AND definition LIKE '%DECLINED%')
    THROW 52416, 'V241 verification failed: dispatch decline status is unavailable.', 1;

SELECT 'V241_OK' AS verification_result, COUNT_BIG(*) AS saved_template_count FROM dbo.provider_quote_templates;
