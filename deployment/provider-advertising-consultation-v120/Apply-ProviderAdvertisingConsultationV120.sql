BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260820170000_AddProviderAdvertisingConsultationV120'
)
BEGIN
    ALTER TABLE [chat_rooms] DROP CONSTRAINT [CK_chat_rooms_resource_type];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260820170000_AddProviderAdvertisingConsultationV120'
)
BEGIN
    EXEC(N'ALTER TABLE [chat_rooms] ADD CONSTRAINT [CK_chat_rooms_resource_type] CHECK ([resource_type] IN (''TRANSACTION'',''SUBSCRIPTION'',''INTERIOR'',''AFTER_SERVICE'',''PROVIDER_CONSULTATION''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260820170000_AddProviderAdvertisingConsultationV120'
)
BEGIN
    IF OBJECT_ID(N'dbo.notification_templates', N'U') IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.notification_templates WHERE template_code = 'PROVIDER_CONSULTATION_REQUESTED' AND channel_code = 'WEB')
        BEGIN
            INSERT INTO dbo.notification_templates
            (public_id, template_code, name, description, audience_type_code, event_type_code, channel_code,
             title_template, body_template, allowed_variables_json, is_required_business_notice, is_marketing,
             is_active, effective_from, effective_to, created_at, created_by_user_id, updated_at, updated_by_user_id)
            VALUES
            (NEWID(), 'PROVIDER_CONSULTATION_REQUESTED', N'공급자 광고 상담 신청',
             N'고객이 공급자 소개 화면에서 채팅 상담을 신청했을 때 공급자에게 안내합니다.',
             'PROVIDER', 'CUSTOMER_CONSULTATION_REQUESTED', 'WEB', N'새 채팅 상담 신청',
             N'{{customer_name}} 고객이 상담 신청하였습니다. 수달 라이프 채팅에서 확인해 주세요.',
             N'["customer_name"]', 1, 0, 1, SYSUTCDATETIME(), NULL, SYSUTCDATETIME(), NULL, SYSUTCDATETIME(), NULL);
        END;

        IF NOT EXISTS (SELECT 1 FROM dbo.notification_templates WHERE template_code = 'PROVIDER_CONSULTATION_REQUESTED' AND channel_code = 'KAKAO')
        BEGIN
            INSERT INTO dbo.notification_templates
            (public_id, template_code, name, description, audience_type_code, event_type_code, channel_code,
             title_template, body_template, allowed_variables_json, is_required_business_notice, is_marketing,
             is_active, effective_from, effective_to, created_at, created_by_user_id, updated_at, updated_by_user_id)
            VALUES
            (NEWID(), 'PROVIDER_CONSULTATION_REQUESTED', N'공급자 광고 상담 신청',
             N'알림톡 계약 및 채널 설정이 활성화되면 고객 상담 신청을 공급자에게 전송합니다.',
             'PROVIDER', 'CUSTOMER_CONSULTATION_REQUESTED', 'KAKAO', N'새 채팅 상담 신청',
             N'{{customer_name}} 고객이 상담 신청하였습니다. 수달 라이프 채팅에서 확인해 주세요.',
             N'["customer_name"]', 1, 0, 1, SYSUTCDATETIME(), NULL, SYSUTCDATETIME(), NULL, SYSUTCDATETIME(), NULL);
        END;
    END;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260820170000_AddProviderAdvertisingConsultationV120'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260820170000_AddProviderAdvertisingConsultationV120', N'10.0.10');
END;

COMMIT;
GO

