BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830054617_AddMemberNotificationBroadcastV162'
)
BEGIN
    CREATE TABLE [notification_broadcasts] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [notification_id] bigint NULL,
        [name] nvarchar(200) NOT NULL,
        [audience_code] varchar(20) NOT NULL,
        [kind_code] varchar(30) NOT NULL,
        [channels_json] varchar(1000) NOT NULL,
        [title] nvarchar(300) NOT NULL,
        [body] nvarchar(3000) NOT NULL,
        [scheduled_at] datetime2(7) NULL,
        [status_code] varchar(20) NOT NULL,
        [estimated_audience_count] int NOT NULL,
        [recipients_processed_count] int NOT NULL,
        [deliveries_created_count] int NOT NULL,
        [last_processed_user_id] bigint NULL,
        [previewed_at] datetime2(7) NULL,
        [confirmed_at] datetime2(7) NULL,
        [confirmed_by_user_id] bigint NULL,
        [cancelled_at] datetime2(7) NULL,
        [cancelled_by_user_id] bigint NULL,
        [cancellation_reason] nvarchar(1000) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_notification_broadcasts] PRIMARY KEY ([id]),
        CONSTRAINT [CK_notification_broadcast_audience] CHECK ([audience_code] IN ('CUSTOMER','PROVIDER','ALL')),
        CONSTRAINT [CK_notification_broadcast_channels] CHECK (ISJSON([channels_json])=1),
        CONSTRAINT [CK_notification_broadcast_kind] CHECK ([kind_code] IN ('BUSINESS_NOTICE','MARKETING')),
        CONSTRAINT [CK_notification_broadcast_status] CHECK ([status_code] IN ('DRAFT','QUEUED','PREPARING','SENDING','COMPLETED','CANCELLED')),
        CONSTRAINT [FK_notification_broadcasts_notifications_notification_id] FOREIGN KEY ([notification_id]) REFERENCES [notifications] ([id]),
        CONSTRAINT [FK_notification_broadcasts_users_cancelled_by_user_id] FOREIGN KEY ([cancelled_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_notification_broadcasts_users_confirmed_by_user_id] FOREIGN KEY ([confirmed_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_notification_broadcasts_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_notification_broadcasts_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830054617_AddMemberNotificationBroadcastV162'
)
BEGIN
    CREATE INDEX [IX_notification_broadcasts_cancelled_by_user_id] ON [notification_broadcasts] ([cancelled_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830054617_AddMemberNotificationBroadcastV162'
)
BEGIN
    CREATE INDEX [IX_notification_broadcasts_confirmed_by_user_id] ON [notification_broadcasts] ([confirmed_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830054617_AddMemberNotificationBroadcastV162'
)
BEGIN
    CREATE INDEX [IX_notification_broadcasts_created_at] ON [notification_broadcasts] ([created_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830054617_AddMemberNotificationBroadcastV162'
)
BEGIN
    CREATE INDEX [IX_notification_broadcasts_created_by_user_id] ON [notification_broadcasts] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830054617_AddMemberNotificationBroadcastV162'
)
BEGIN
    CREATE INDEX [IX_notification_broadcasts_notification_id] ON [notification_broadcasts] ([notification_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830054617_AddMemberNotificationBroadcastV162'
)
BEGIN
    CREATE UNIQUE INDEX [IX_notification_broadcasts_public_id] ON [notification_broadcasts] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830054617_AddMemberNotificationBroadcastV162'
)
BEGIN
    CREATE INDEX [IX_notification_broadcasts_status_code_scheduled_at] ON [notification_broadcasts] ([status_code], [scheduled_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830054617_AddMemberNotificationBroadcastV162'
)
BEGIN
    CREATE INDEX [IX_notification_broadcasts_updated_by_user_id] ON [notification_broadcasts] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830054617_AddMemberNotificationBroadcastV162'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260830054617_AddMemberNotificationBroadcastV162', N'10.0.10');
END;

COMMIT;
GO

