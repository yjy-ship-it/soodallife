SET NOCOUNT ON;
SET XACT_ABORT ON;
IF OBJECT_ID(N'dbo.notification_broadcasts',N'U') IS NULL THROW 51620,N'V162 verification failed: notification_broadcasts is missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_notification_broadcast_audience') THROW 51621,N'V162 verification failed: audience constraint is missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_notification_broadcast_kind') THROW 51622,N'V162 verification failed: kind constraint is missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_notification_broadcast_channels') THROW 51623,N'V162 verification failed: JSON channel constraint is missing.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260830054617_AddMemberNotificationBroadcastV162') THROW 51624,N'V162 verification failed: migration history is missing.',1;
SELECT N'V162_OK' AS verification_result,COUNT_BIG(*) AS broadcast_count,SUM(CASE WHEN status_code IN('QUEUED','PREPARING','SENDING') THEN 1 ELSE 0 END) AS active_queue_count FROM dbo.notification_broadcasts;
