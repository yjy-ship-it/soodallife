SET NOCOUNT ON;
IF OBJECT_ID('dbo.admin_security_profiles','U') IS NULL THROW 51161,'admin_security_profiles missing',1;
IF OBJECT_ID('dbo.admin_reauthentication_sessions','U') IS NULL THROW 51162,'admin_reauthentication_sessions missing',1;
IF OBJECT_ID('dbo.outbox_retry_requests','U') IS NULL THROW 51163,'outbox_retry_requests missing',1;
IF OBJECT_ID('dbo.data_retention_policies','U') IS NULL OR OBJECT_ID('dbo.data_retention_executions','U') IS NULL THROW 51164,'retention tables missing',1;
IF OBJECT_ID('dbo.analytics_events','U') IS NULL THROW 51165,'analytics_events missing',1;
IF EXISTS(SELECT 1 FROM dbo.data_retention_policies WHERE dry_run=0) THROW 51166,'V116 retention policies must remain dry-run until destructive processing is explicitly approved.',1;
IF EXISTS(SELECT 1 FROM dbo.users u JOIN dbo.user_roles ur ON ur.user_id=u.id AND ur.revoked_at IS NULL JOIN dbo.roles r ON r.id=ur.role_id AND r.code='ADMIN' WHERE NOT EXISTS(SELECT 1 FROM dbo.admin_security_profiles p WHERE p.user_id=u.id)) THROW 51167,'Admin security profile backfill incomplete.',1;
SELECT 'V116_OK' verification_result,(SELECT COUNT(*) FROM dbo.admin_security_profiles) admin_profiles,(SELECT COUNT(*) FROM dbo.data_retention_policies) retention_policies,(SELECT COUNT(*) FROM dbo.outbox_retry_requests) retry_requests;
