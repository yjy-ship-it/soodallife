SET NOCOUNT ON;
DECLARE @definition nvarchar(max)=OBJECT_DEFINITION(OBJECT_ID(N'dbo.CK_files_purpose'));
IF @definition IS NULL OR @definition NOT LIKE N'%HELP_ROOM_PHOTO%' THROW 51000,'V165 HELP_ROOM_PHOTO purpose constraint missing',1;
SELECT 'V165_OK' verification_result,
 (SELECT COUNT_BIG(*) FROM dbo.files WHERE purpose_code='HELP_ROOM_PHOTO') help_photo_count,
 (SELECT COUNT_BIG(*) FROM dbo.files WHERE purpose_code='HELP_ROOM_PHOTO' AND malware_scan_status_code='CLEAN' AND privacy_inspection_status_code='SAFE' AND sanitization_status_code='COMPLETED') locally_safe_count,
 (SELECT COUNT_BIG(*) FROM dbo.files WHERE purpose_code='HELP_ROOM_PHOTO' AND status_code='QUARANTINED') review_queue_count;
