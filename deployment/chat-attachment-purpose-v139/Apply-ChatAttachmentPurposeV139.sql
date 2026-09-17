SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.files', N'U') IS NULL
    THROW 51000, 'V139 requires dbo.files.', 1;

BEGIN TRANSACTION;

IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.files')
      AND name = N'CK_files_purpose'
)
    ALTER TABLE dbo.files DROP CONSTRAINT CK_files_purpose;

ALTER TABLE dbo.files WITH CHECK ADD CONSTRAINT CK_files_purpose CHECK (
    [purpose_code] IN (
        'PROVIDER_DOCUMENT','REQUEST_ANSWER','COMPLETION_EVIDENCE','AFTER_SERVICE',
        'DISPUTE_EVIDENCE','REVIEW','REPORT_EVIDENCE','SANCTION_APPEAL_EVIDENCE',
        'PROVIDER_PUBLIC_LOGO','PROVIDER_PUBLIC_PHOTO','CHAT_ATTACHMENT'
    )
);
ALTER TABLE dbo.files CHECK CONSTRAINT CK_files_purpose;

COMMIT TRANSACTION;

SELECT 'V139_APPLY_OK' AS result,
       OBJECT_DEFINITION(OBJECT_ID(N'dbo.CK_files_purpose')) AS purpose_constraint;
