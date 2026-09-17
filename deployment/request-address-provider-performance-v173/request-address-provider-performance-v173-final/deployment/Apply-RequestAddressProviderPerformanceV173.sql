SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.service_requests', N'detail_address_disclosure_code') IS NULL
BEGIN
    ALTER TABLE dbo.service_requests
        ADD detail_address_disclosure_code varchar(30) NOT NULL
            CONSTRAINT DF_service_requests_detail_address_disclosure_code DEFAULT ('AFTER_SELECTION');
END;

EXEC sys.sp_executesql N'
UPDATE dbo.service_requests
SET detail_address_disclosure_code = ''AFTER_SELECTION''
WHERE detail_address_disclosure_code IS NULL
   OR detail_address_disclosure_code NOT IN (''AFTER_SELECTION'', ''BEFORE_QUOTE'');';

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20260831180000_AddRequestAddressDisclosureV173')
    INSERT INTO dbo.__EFMigrationsHistory(MigrationId, ProductVersion)
    VALUES (N'20260831180000_AddRequestAddressDisclosureV173', N'10.0.10');

COMMIT TRANSACTION;
