SET NOCOUNT ON;

DECLARE @definition nvarchar(max) = (
    SELECT definition
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.files')
      AND name = N'CK_files_purpose'
);

IF @definition IS NULL OR @definition NOT LIKE N'%CHAT_ATTACHMENT%'
    THROW 51001, 'V139 verification failed: CHAT_ATTACHMENT is not allowed by CK_files_purpose.', 1;

SELECT 'V139_OK' AS verification_result,
       CASE WHEN @definition LIKE N'%CHAT_ATTACHMENT%' THEN 1 ELSE 0 END AS chat_attachment_allowed,
       (SELECT COUNT_BIG(*) FROM dbo.files WHERE purpose_code = 'CHAT_ATTACHMENT') AS chat_attachment_rows;
