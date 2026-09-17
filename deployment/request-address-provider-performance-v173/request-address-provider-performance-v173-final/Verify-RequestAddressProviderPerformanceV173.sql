SET NOCOUNT ON;
IF COL_LENGTH(N'dbo.service_requests', N'detail_address_disclosure_code') IS NULL
    THROW 51000, 'V173 detail address disclosure column is missing.', 1;
EXEC sys.sp_executesql N'
IF EXISTS (SELECT 1 FROM dbo.service_requests WHERE detail_address_disclosure_code NOT IN (''AFTER_SELECTION'', ''BEFORE_QUOTE''))
    THROW 51000, ''V173 contains an invalid detail address disclosure value.'', 1;';
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20260831180000_AddRequestAddressDisclosureV173')
    THROW 51000, 'V173 migration history marker is missing.', 1;
EXEC sys.sp_executesql N'
SELECT ''V173_OK'' AS verification_result,
       COUNT_BIG(*) AS request_count,
       SUM(CASE WHEN detail_address_disclosure_code = ''BEFORE_QUOTE'' THEN 1 ELSE 0 END) AS before_quote_count
FROM dbo.service_requests;';
