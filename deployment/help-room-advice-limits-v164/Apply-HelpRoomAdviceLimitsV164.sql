BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830082014_AddHelpRoomProviderAdviceLimitsV164'
)
BEGIN
    CREATE INDEX [IX_help_room_entries_help_post_id_provider_profile_id_created_at] ON [help_room_entries] ([help_post_id], [provider_profile_id], [created_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830082014_AddHelpRoomProviderAdviceLimitsV164'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260830082014_AddHelpRoomProviderAdviceLimitsV164', N'10.0.10');
END;

COMMIT;
GO

