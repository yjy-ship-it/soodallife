SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.help_posts',N'U') IS NULL THROW 51000,'V163 help_posts missing',1;
IF OBJECT_ID(N'dbo.help_room_entries',N'U') IS NULL THROW 51000,'V163 help_room_entries missing',1;
IF OBJECT_ID(N'dbo.help_post_files',N'U') IS NULL THROW 51000,'V163 help_post_files missing',1;
IF OBJECT_ID(N'dbo.help_post_resolutions',N'U') IS NULL THROW 51000,'V163 help_post_resolutions missing',1;
IF OBJECT_ID(N'dbo.user_suggestions',N'U') IS NULL THROW 51000,'V163 user_suggestions missing',1;
IF OBJECT_ID(N'dbo.user_suggestion_events',N'U') IS NULL THROW 51000,'V163 user_suggestion_events missing',1;
SELECT 'V163_OK' verification_result,
 (SELECT COUNT_BIG(*) FROM dbo.help_posts) help_post_count,
 (SELECT COUNT_BIG(*) FROM dbo.help_room_entries) help_entry_count,
 (SELECT COUNT_BIG(*) FROM dbo.user_suggestions) suggestion_count;
