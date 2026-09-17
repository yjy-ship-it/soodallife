using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations;

public partial class AddNotificationOperationsV114 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF COL_LENGTH('notification_preferences','night_marketing_enabled') IS NULL
    ALTER TABLE notification_preferences ADD night_marketing_enabled bit NOT NULL CONSTRAINT DF_notification_preferences_night_marketing_enabled DEFAULT(0);
IF COL_LENGTH('notification_preferences','consent_version') IS NULL
    ALTER TABLE notification_preferences ADD consent_version varchar(50) NULL;
IF COL_LENGTH('notification_preferences','consent_source_code') IS NULL
    ALTER TABLE notification_preferences ADD consent_source_code varchar(30) NOT NULL CONSTRAINT DF_notification_preferences_consent_source_code DEFAULT('MY_SOODAL');
IF COL_LENGTH('notification_preferences','consented_at') IS NULL
    ALTER TABLE notification_preferences ADD consented_at datetime2(7) NULL;
IF COL_LENGTH('notification_preferences','withdrawn_at') IS NULL
    ALTER TABLE notification_preferences ADD withdrawn_at datetime2(7) NULL;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_notification_preferences_event_group')
   AND NOT EXISTS(SELECT 1 FROM notification_preferences WHERE event_group_code NOT IN('BUSINESS','MARKETING'))
    ALTER TABLE notification_preferences ADD CONSTRAINT CK_notification_preferences_event_group CHECK(event_group_code IN('BUSINESS','MARKETING'));

IF OBJECT_ID('notification_channel_settings','U') IS NULL
BEGIN
    CREATE TABLE notification_channel_settings(
        id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_notification_channel_settings PRIMARY KEY,
        public_id uniqueidentifier NOT NULL,
        channel_code varchar(20) NOT NULL,
        display_name nvarchar(100) NOT NULL,
        is_enabled bit NOT NULL CONSTRAINT DF_notification_channel_settings_enabled DEFAULT(0),
        operation_mode_code varchar(20) NOT NULL CONSTRAINT DF_notification_channel_settings_mode DEFAULT('DISABLED'),
        provider_code varchar(50) NULL,
        sender_identity nvarchar(200) NULL,
        reply_to nvarchar(300) NULL,
        batch_size int NOT NULL CONSTRAINT DF_notification_channel_settings_batch DEFAULT(50),
        max_attempts int NOT NULL CONSTRAINT DF_notification_channel_settings_attempts DEFAULT(5),
        base_retry_seconds int NOT NULL CONSTRAINT DF_notification_channel_settings_base_retry DEFAULT(30),
        max_retry_seconds int NOT NULL CONSTRAINT DF_notification_channel_settings_max_retry DEFAULT(900),
        send_window_start_hour int NOT NULL CONSTRAINT DF_notification_channel_settings_window_start DEFAULT(0),
        send_window_end_hour int NOT NULL CONSTRAINT DF_notification_channel_settings_window_end DEFAULT(24),
        created_at datetime2(7) NOT NULL CONSTRAINT DF_notification_channel_settings_created DEFAULT SYSUTCDATETIME(),
        created_by_user_id bigint NULL,
        updated_at datetime2(7) NOT NULL CONSTRAINT DF_notification_channel_settings_updated DEFAULT SYSUTCDATETIME(),
        updated_by_user_id bigint NULL,
        row_version rowversion NOT NULL,
        CONSTRAINT UQ_notification_channel_settings_public UNIQUE(public_id),
        CONSTRAINT UQ_notification_channel_settings_channel UNIQUE(channel_code),
        CONSTRAINT FK_notification_channel_settings_created_user FOREIGN KEY(created_by_user_id) REFERENCES users(id),
        CONSTRAINT FK_notification_channel_settings_updated_user FOREIGN KEY(updated_by_user_id) REFERENCES users(id),
        CONSTRAINT CK_notification_channel_settings_channel CHECK(channel_code IN('WEB','KAKAO','SMS','EMAIL','PUSH')),
        CONSTRAINT CK_notification_channel_settings_mode CHECK(operation_mode_code IN('DISABLED','TEST','PRODUCTION')),
        CONSTRAINT CK_notification_channel_settings_attempts CHECK(batch_size BETWEEN 1 AND 500 AND max_attempts BETWEEN 1 AND 20 AND base_retry_seconds BETWEEN 5 AND 86400 AND max_retry_seconds>=base_retry_seconds),
        CONSTRAINT CK_notification_channel_settings_window CHECK(send_window_start_hour BETWEEN 0 AND 23 AND send_window_end_hour BETWEEN 1 AND 24)
    );
END;

IF OBJECT_ID('notification_preference_events','U') IS NULL
BEGIN
    CREATE TABLE notification_preference_events(
        id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_notification_preference_events PRIMARY KEY,
        public_id uniqueidentifier NOT NULL,
        notification_preference_id bigint NOT NULL,
        user_id bigint NOT NULL,
        event_group_code varchar(50) NOT NULL,
        event_type_code varchar(30) NOT NULL,
        channel_snapshot_json nvarchar(max) NOT NULL,
        consent_version varchar(50) NULL,
        source_code varchar(30) NOT NULL,
        occurred_at datetime2(7) NOT NULL,
        CONSTRAINT UQ_notification_preference_events_public UNIQUE(public_id),
        CONSTRAINT FK_notification_preference_events_preference FOREIGN KEY(notification_preference_id) REFERENCES notification_preferences(id),
        CONSTRAINT FK_notification_preference_events_user FOREIGN KEY(user_id) REFERENCES users(id),
        CONSTRAINT CK_notification_preference_events_snapshot CHECK(ISJSON(channel_snapshot_json)=1)
    );
    CREATE INDEX IX_notification_preference_events_user_occurred ON notification_preference_events(user_id,occurred_at DESC);
END;

MERGE notification_channel_settings AS target
USING(VALUES
('91000000-0000-4000-8000-000000000001','WEB',N'WEB 내부 알림',1,'PRODUCTION'),
('91000000-0000-4000-8000-000000000002','KAKAO',N'카카오 알림톡',0,'DISABLED'),
('91000000-0000-4000-8000-000000000003','SMS',N'SMS 문자',0,'DISABLED'),
('91000000-0000-4000-8000-000000000004','EMAIL',N'이메일',0,'DISABLED'),
('91000000-0000-4000-8000-000000000005','PUSH',N'Push 알림',0,'DISABLED')) AS source(public_id,channel_code,display_name,is_enabled,operation_mode_code)
ON target.channel_code=source.channel_code
WHEN NOT MATCHED THEN INSERT(public_id,channel_code,display_name,is_enabled,operation_mode_code) VALUES(CONVERT(uniqueidentifier,source.public_id),source.channel_code,source.display_name,source.is_enabled,source.operation_mode_code);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID('notification_preference_events','U') IS NOT NULL DROP TABLE notification_preference_events;
IF OBJECT_ID('notification_channel_settings','U') IS NOT NULL DROP TABLE notification_channel_settings;
IF COL_LENGTH('notification_preferences','night_marketing_enabled') IS NOT NULL
BEGIN
    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_notification_preferences_event_group') ALTER TABLE notification_preferences DROP CONSTRAINT CK_notification_preferences_event_group;
    ALTER TABLE notification_preferences DROP CONSTRAINT DF_notification_preferences_night_marketing_enabled;
    ALTER TABLE notification_preferences DROP CONSTRAINT DF_notification_preferences_consent_source_code;
    ALTER TABLE notification_preferences DROP COLUMN night_marketing_enabled,consent_version,consent_source_code,consented_at,withdrawn_at;
END;
""");
    }
}
