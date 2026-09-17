SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_chat_rooms_resource_type' AND definition LIKE '%PROVIDER_CONSULTATION%')
    THROW 52001, 'V120 chat room resource constraint is missing.', 1;

IF (SELECT COUNT(*) FROM dbo.notification_templates WHERE template_code = 'PROVIDER_CONSULTATION_REQUESTED' AND channel_code IN ('WEB', 'KAKAO') AND event_type_code = 'CUSTOMER_CONSULTATION_REQUESTED' AND audience_type_code = 'PROVIDER' AND is_active = 1) <> 2
    THROW 52002, 'V120 provider consultation notification templates are incomplete.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20260820170000_AddProviderAdvertisingConsultationV120')
    THROW 52003, 'V120 migration history is missing.', 1;

SELECT 'V120_OK' AS verification_result,
       (SELECT COUNT(*) FROM dbo.notification_templates WHERE template_code = 'PROVIDER_CONSULTATION_REQUESTED') AS notification_template_count,
       (SELECT COUNT(*) FROM dbo.chat_rooms WHERE resource_type = 'PROVIDER_CONSULTATION') AS consultation_room_count;
