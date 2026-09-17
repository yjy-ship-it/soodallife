BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE TABLE [help_posts] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [customer_user_id] bigint NOT NULL,
        [category_id] bigint NOT NULL,
        [administrative_area_id] bigint NULL,
        [region_disclosure_code] varchar(20) NOT NULL DEFAULT 'HIDDEN',
        [purpose_code] varchar(40) NOT NULL,
        [title] nvarchar(200) NOT NULL,
        [body] nvarchar(5000) NOT NULL,
        [status_code] varchar(20) NOT NULL,
        [intent_code] varchar(30) NOT NULL,
        [intent_reason] nvarchar(500) NULL,
        [converted_service_request_id] bigint NULL,
        [resolved_at] datetime2(7) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_help_posts] PRIMARY KEY ([id]),
        CONSTRAINT [CK_help_posts_intent] CHECK ([intent_code] IN ('ADVICE','QUOTE_RECOMMENDED','DANGEROUS')),
        CONSTRAINT [CK_help_posts_region] CHECK ([region_disclosure_code] IN ('HIDDEN','SIGUNGU')),
        CONSTRAINT [CK_help_posts_status] CHECK ([status_code] IN ('PUBLISHED','RESOLVED','CONVERTED','HIDDEN')),
        CONSTRAINT [FK_help_posts_administrative_areas_administrative_area_id] FOREIGN KEY ([administrative_area_id]) REFERENCES [administrative_areas] ([id]),
        CONSTRAINT [FK_help_posts_service_categories_category_id] FOREIGN KEY ([category_id]) REFERENCES [service_categories] ([id]),
        CONSTRAINT [FK_help_posts_service_requests_converted_service_request_id] FOREIGN KEY ([converted_service_request_id]) REFERENCES [service_requests] ([id]),
        CONSTRAINT [FK_help_posts_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_help_posts_users_customer_user_id] FOREIGN KEY ([customer_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_help_posts_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE TABLE [user_suggestions] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [user_id] bigint NOT NULL,
        [type_code] varchar(30) NOT NULL,
        [visibility_code] varchar(20) NOT NULL DEFAULT 'PRIVATE',
        [status_code] varchar(30) NOT NULL,
        [title] nvarchar(200) NOT NULL,
        [body] nvarchar(5000) NOT NULL,
        [page_url] nvarchar(1000) NULL,
        [device_info] nvarchar(1000) NULL,
        [app_version] nvarchar(100) NULL,
        [assigned_admin_user_id] bigint NULL,
        [admin_reply] nvarchar(5000) NULL,
        [release_version] nvarchar(100) NULL,
        [content_hash] binary(32) NOT NULL,
        [idempotency_key] varchar(100) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_user_suggestions] PRIMARY KEY ([id]),
        CONSTRAINT [FK_user_suggestions_users_assigned_admin_user_id] FOREIGN KEY ([assigned_admin_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_user_suggestions_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_user_suggestions_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_user_suggestions_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE TABLE [help_post_files] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [help_post_id] bigint NOT NULL,
        [file_id] bigint NOT NULL,
        [display_order] int NOT NULL DEFAULT 0,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_help_post_files] PRIMARY KEY ([id]),
        CONSTRAINT [FK_help_post_files_files_file_id] FOREIGN KEY ([file_id]) REFERENCES [files] ([id]),
        CONSTRAINT [FK_help_post_files_help_posts_help_post_id] FOREIGN KEY ([help_post_id]) REFERENCES [help_posts] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE TABLE [help_room_entries] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [help_post_id] bigint NOT NULL,
        [author_user_id] bigint NOT NULL,
        [provider_profile_id] bigint NULL,
        [author_role_code] varchar(20) NOT NULL,
        [entry_type_code] varchar(30) NOT NULL,
        [body] nvarchar(5000) NOT NULL,
        [cause_text] nvarchar(2000) NULL,
        [check_text] nvarchar(2000) NULL,
        [diy_steps_text] nvarchar(3000) NULL,
        [risk_text] nvarchar(2000) NULL,
        [next_step_text] nvarchar(2000) NULL,
        [requires_professional] bit NOT NULL DEFAULT CAST(0 AS bit),
        [safety_code] varchar(30) NOT NULL,
        [status_code] varchar(20) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_help_room_entries] PRIMARY KEY ([id]),
        CONSTRAINT [FK_help_room_entries_help_posts_help_post_id] FOREIGN KEY ([help_post_id]) REFERENCES [help_posts] ([id]),
        CONSTRAINT [FK_help_room_entries_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_help_room_entries_users_author_user_id] FOREIGN KEY ([author_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE TABLE [user_suggestion_events] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [user_suggestion_id] bigint NOT NULL,
        [actor_user_id] bigint NOT NULL,
        [action_code] varchar(30) NOT NULL,
        [from_status_code] varchar(30) NULL,
        [to_status_code] varchar(30) NOT NULL,
        [note] nvarchar(2000) NULL,
        [occurred_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_user_suggestion_events] PRIMARY KEY ([id]),
        CONSTRAINT [FK_user_suggestion_events_user_suggestions_user_suggestion_id] FOREIGN KEY ([user_suggestion_id]) REFERENCES [user_suggestions] ([id]),
        CONSTRAINT [FK_user_suggestion_events_users_actor_user_id] FOREIGN KEY ([actor_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE TABLE [help_post_resolutions] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [help_post_id] bigint NOT NULL,
        [resolved_by_customer_user_id] bigint NOT NULL,
        [resolution_code] varchar(30) NOT NULL,
        [summary] nvarchar(3000) NOT NULL,
        [helpful_entry_id] bigint NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_help_post_resolutions] PRIMARY KEY ([id]),
        CONSTRAINT [FK_help_post_resolutions_help_posts_help_post_id] FOREIGN KEY ([help_post_id]) REFERENCES [help_posts] ([id]),
        CONSTRAINT [FK_help_post_resolutions_help_room_entries_helpful_entry_id] FOREIGN KEY ([helpful_entry_id]) REFERENCES [help_room_entries] ([id]),
        CONSTRAINT [FK_help_post_resolutions_users_resolved_by_customer_user_id] FOREIGN KEY ([resolved_by_customer_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE INDEX [IX_help_post_files_file_id] ON [help_post_files] ([file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE INDEX [IX_help_post_files_help_post_id_display_order] ON [help_post_files] ([help_post_id], [display_order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE UNIQUE INDEX [IX_help_post_files_help_post_id_file_id] ON [help_post_files] ([help_post_id], [file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE UNIQUE INDEX [IX_help_post_files_public_id] ON [help_post_files] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE UNIQUE INDEX [IX_help_post_resolutions_help_post_id] ON [help_post_resolutions] ([help_post_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE INDEX [IX_help_post_resolutions_helpful_entry_id] ON [help_post_resolutions] ([helpful_entry_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE UNIQUE INDEX [IX_help_post_resolutions_public_id] ON [help_post_resolutions] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE INDEX [IX_help_post_resolutions_resolved_by_customer_user_id] ON [help_post_resolutions] ([resolved_by_customer_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE INDEX [IX_help_posts_administrative_area_id] ON [help_posts] ([administrative_area_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE INDEX [IX_help_posts_category_id] ON [help_posts] ([category_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE INDEX [IX_help_posts_converted_service_request_id] ON [help_posts] ([converted_service_request_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE INDEX [IX_help_posts_created_by_user_id] ON [help_posts] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE INDEX [IX_help_posts_customer_user_id] ON [help_posts] ([customer_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE UNIQUE INDEX [IX_help_posts_public_id] ON [help_posts] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE INDEX [IX_help_posts_status_code_created_at] ON [help_posts] ([status_code], [created_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE INDEX [IX_help_posts_updated_by_user_id] ON [help_posts] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE INDEX [IX_help_room_entries_author_user_id] ON [help_room_entries] ([author_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE INDEX [IX_help_room_entries_help_post_id_created_at] ON [help_room_entries] ([help_post_id], [created_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE INDEX [IX_help_room_entries_provider_profile_id] ON [help_room_entries] ([provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE UNIQUE INDEX [IX_help_room_entries_public_id] ON [help_room_entries] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE INDEX [IX_user_suggestion_events_actor_user_id] ON [user_suggestion_events] ([actor_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE UNIQUE INDEX [IX_user_suggestion_events_public_id] ON [user_suggestion_events] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE INDEX [IX_user_suggestion_events_user_suggestion_id_occurred_at] ON [user_suggestion_events] ([user_suggestion_id], [occurred_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE INDEX [IX_user_suggestions_assigned_admin_user_id] ON [user_suggestions] ([assigned_admin_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE INDEX [IX_user_suggestions_created_by_user_id] ON [user_suggestions] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE UNIQUE INDEX [IX_user_suggestions_idempotency_key] ON [user_suggestions] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE UNIQUE INDEX [IX_user_suggestions_public_id] ON [user_suggestions] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE INDEX [IX_user_suggestions_updated_by_user_id] ON [user_suggestions] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE INDEX [IX_user_suggestions_user_id_content_hash_created_at] ON [user_suggestions] ([user_id], [content_hash], [created_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    CREATE INDEX [IX_user_suggestions_user_id_created_at] ON [user_suggestions] ([user_id], [created_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830064725_AddHelpRoomAndSuggestionBoxV163'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260830064725_AddHelpRoomAndSuggestionBoxV163', N'10.0.10');
END;

COMMIT;
GO

