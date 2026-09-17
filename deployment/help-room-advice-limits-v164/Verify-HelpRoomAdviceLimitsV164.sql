SET NOCOUNT ON;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.help_room_entries') AND name=N'IX_help_room_entries_help_post_id_provider_profile_id_created_at') THROW 51000,'V164 provider advice index missing',1;
SELECT 'V164_OK' verification_result,
 (SELECT COUNT_BIG(*) FROM dbo.help_posts) help_post_count,
 (SELECT COUNT_BIG(*) FROM dbo.help_room_entries WHERE author_role_code='PROVIDER') provider_advice_count;
