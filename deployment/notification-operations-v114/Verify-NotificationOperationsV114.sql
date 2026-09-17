SET NOCOUNT ON;
IF COL_LENGTH('dbo.notification_preferences','night_marketing_enabled') IS NULL THROW 51142, 'V114 preference columns are missing.', 1;
IF OBJECT_ID('dbo.notification_channel_settings','U') IS NULL THROW 51143, 'V114 channel settings table is missing.', 1;
IF OBJECT_ID('dbo.notification_preference_events','U') IS NULL THROW 51144, 'V114 preference history table is missing.', 1;
IF (SELECT COUNT(*) FROM dbo.notification_channel_settings WHERE channel_code IN('WEB','KAKAO','SMS','EMAIL','PUSH'))<>5 THROW 51145, 'V114 channel seed verification failed.', 1;
IF EXISTS(SELECT 1 FROM dbo.notification_channel_settings WHERE channel_code<>'WEB' AND is_enabled=1) THROW 51146, 'External channel was unexpectedly enabled.', 1;
SELECT 'V114_OK' AS verification_result,(SELECT COUNT(*) FROM dbo.notification_channel_settings) AS channel_count,(SELECT COUNT(*) FROM dbo.notification_preference_events) AS preference_event_count;
