BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE TABLE [customer_proposal_area_interests] (
        [id] bigint NOT NULL IDENTITY,
        [customer_profile_id] bigint NOT NULL,
        [administrative_area_id] bigint NOT NULL,
        [source_code] varchar(30) NOT NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_customer_proposal_area_interests] PRIMARY KEY ([id]),
        CONSTRAINT [FK_customer_proposal_area_interests_administrative_areas_administrative_area_id] FOREIGN KEY ([administrative_area_id]) REFERENCES [administrative_areas] ([id]),
        CONSTRAINT [FK_customer_proposal_area_interests_customer_profiles_customer_profile_id] FOREIGN KEY ([customer_profile_id]) REFERENCES [customer_profiles] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE TABLE [customer_proposal_category_interests] (
        [id] bigint NOT NULL IDENTITY,
        [customer_profile_id] bigint NOT NULL,
        [category_id] bigint NOT NULL,
        [source_code] varchar(30) NOT NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_customer_proposal_category_interests] PRIMARY KEY ([id]),
        CONSTRAINT [FK_customer_proposal_category_interests_customer_profiles_customer_profile_id] FOREIGN KEY ([customer_profile_id]) REFERENCES [customer_profiles] ([id]),
        CONSTRAINT [FK_customer_proposal_category_interests_service_categories_category_id] FOREIGN KEY ([category_id]) REFERENCES [service_categories] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE TABLE [customer_proposal_signals] (
        [id] bigint NOT NULL IDENTITY,
        [customer_profile_id] bigint NOT NULL,
        [category_id] bigint NOT NULL,
        [signal_type_code] varchar(30) NOT NULL,
        [occurred_at] datetime2(7) NOT NULL,
        [expires_at] datetime2(7) NOT NULL,
        CONSTRAINT [PK_customer_proposal_signals] PRIMARY KEY ([id]),
        CONSTRAINT [CK_customer_proposal_signal_type] CHECK ([signal_type_code] IN ('SERVICE_DETAIL','SERVICE_SEARCH','SESSION_CATEGORY')),
        CONSTRAINT [FK_customer_proposal_signals_customer_profiles_customer_profile_id] FOREIGN KEY ([customer_profile_id]) REFERENCES [customer_profiles] ([id]),
        CONSTRAINT [FK_customer_proposal_signals_service_categories_category_id] FOREIGN KEY ([category_id]) REFERENCES [service_categories] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE TABLE [provider_proposal_campaigns] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [provider_service_category_id] bigint NOT NULL,
        [wallet_id] bigint NOT NULL,
        [proposal_type_code] varchar(30) NOT NULL,
        [scope_code] varchar(20) NOT NULL,
        [title] nvarchar(200) NOT NULL,
        [summary] nvarchar(3000) NOT NULL,
        [normal_price_amount] decimal(19,4) NULL,
        [offer_price_amount] decimal(19,4) NOT NULL,
        [minimum_participants] int NOT NULL,
        [maximum_participants] int NOT NULL,
        [confirmed_participants] int NOT NULL DEFAULT 0,
        [start_at] datetime2(7) NOT NULL,
        [end_at] datetime2(7) NOT NULL,
        [service_at] datetime2(7) NULL,
        [minimum_failure_policy_code] varchar(30) NOT NULL,
        [cancellation_policy_text] nvarchar(2000) NOT NULL,
        [status_code] varchar(30) NOT NULL,
        [fee_per_participant] decimal(19,4) NOT NULL,
        [reserved_fee_amount] decimal(19,4) NOT NULL,
        [captured_fee_amount] decimal(19,4) NOT NULL,
        [fee_status_code] varchar(30) NOT NULL,
        [reserve_ledger_entry_id] bigint NOT NULL,
        [release_ledger_entry_id] bigint NULL,
        [published_notification_queued_at] datetime2(7) NULL,
        [midpoint_notification_queued_at] datetime2(7) NULL,
        [closing_notification_queued_at] datetime2(7) NULL,
        [closed_at] datetime2(7) NULL,
        [cancelled_at] datetime2(7) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_provider_proposal_campaigns] PRIMARY KEY ([id]),
        CONSTRAINT [CK_provider_proposal_amounts] CHECK ([offer_price_amount] >= 0 AND ([normal_price_amount] IS NULL OR [normal_price_amount] >= [offer_price_amount]) AND [fee_per_participant] > 0 AND [reserved_fee_amount] >= 0 AND [captured_fee_amount] >= 0),
        CONSTRAINT [CK_provider_proposal_fee_status] CHECK ([fee_status_code] IN ('RESERVED','PARTIALLY_CAPTURED','CAPTURED','RELEASED','RESTORED')),
        CONSTRAINT [CK_provider_proposal_participants] CHECK ([minimum_participants] > 0 AND [maximum_participants] >= [minimum_participants] AND [confirmed_participants] >= 0 AND [confirmed_participants] <= [maximum_participants]),
        CONSTRAINT [CK_provider_proposal_period] CHECK ([end_at] > [start_at]),
        CONSTRAINT [CK_provider_proposal_scope] CHECK ([scope_code] IN ('LOCAL','NATIONWIDE')),
        CONSTRAINT [CK_provider_proposal_status] CHECK ([status_code] IN ('PUBLISHED','MINIMUM_MET','FULL','EXPIRED','CANCELLED')),
        CONSTRAINT [CK_provider_proposal_type] CHECK ([proposal_type_code] IN ('DISCOUNT_SERVICE','GROUP_BUY','GROUP_LESSON')),
        CONSTRAINT [FK_provider_proposal_campaigns_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_provider_proposal_campaigns_provider_service_categories_provider_service_category_id] FOREIGN KEY ([provider_service_category_id]) REFERENCES [provider_service_categories] ([id]),
        CONSTRAINT [FK_provider_proposal_campaigns_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_provider_proposal_campaigns_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_provider_proposal_campaigns_wallet_ledger_release_ledger_entry_id] FOREIGN KEY ([release_ledger_entry_id]) REFERENCES [wallet_ledger] ([id]),
        CONSTRAINT [FK_provider_proposal_campaigns_wallet_ledger_reserve_ledger_entry_id] FOREIGN KEY ([reserve_ledger_entry_id]) REFERENCES [wallet_ledger] ([id]),
        CONSTRAINT [FK_provider_proposal_campaigns_wallets_wallet_id] FOREIGN KEY ([wallet_id]) REFERENCES [wallets] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE TABLE [provider_proposal_applications] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [campaign_id] bigint NOT NULL,
        [customer_profile_id] bigint NOT NULL,
        [status_code] varchar(20) NOT NULL,
        [captured_fee_amount] decimal(19,4) NOT NULL,
        [capture_ledger_entry_id] bigint NULL,
        [applied_at] datetime2(7) NOT NULL,
        [confirmed_at] datetime2(7) NULL,
        [cancelled_at] datetime2(7) NULL,
        [declined_at] datetime2(7) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_provider_proposal_applications] PRIMARY KEY ([id]),
        CONSTRAINT [CK_provider_proposal_application_status] CHECK ([status_code] IN ('APPLIED','CONFIRMED','CANCELLED','DECLINED')),
        CONSTRAINT [FK_provider_proposal_applications_customer_profiles_customer_profile_id] FOREIGN KEY ([customer_profile_id]) REFERENCES [customer_profiles] ([id]),
        CONSTRAINT [FK_provider_proposal_applications_provider_proposal_campaigns_campaign_id] FOREIGN KEY ([campaign_id]) REFERENCES [provider_proposal_campaigns] ([id]),
        CONSTRAINT [FK_provider_proposal_applications_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_provider_proposal_applications_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_provider_proposal_applications_wallet_ledger_capture_ledger_entry_id] FOREIGN KEY ([capture_ledger_entry_id]) REFERENCES [wallet_ledger] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE TABLE [provider_proposal_areas] (
        [id] bigint NOT NULL IDENTITY,
        [campaign_id] bigint NOT NULL,
        [administrative_area_id] bigint NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_provider_proposal_areas] PRIMARY KEY ([id]),
        CONSTRAINT [FK_provider_proposal_areas_administrative_areas_administrative_area_id] FOREIGN KEY ([administrative_area_id]) REFERENCES [administrative_areas] ([id]),
        CONSTRAINT [FK_provider_proposal_areas_provider_proposal_campaigns_campaign_id] FOREIGN KEY ([campaign_id]) REFERENCES [provider_proposal_campaigns] ([id]),
        CONSTRAINT [FK_provider_proposal_areas_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE INDEX [IX_customer_proposal_area_interests_administrative_area_id_is_active] ON [customer_proposal_area_interests] ([administrative_area_id], [is_active]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE UNIQUE INDEX [IX_customer_proposal_area_interests_customer_profile_id_administrative_area_id] ON [customer_proposal_area_interests] ([customer_profile_id], [administrative_area_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE INDEX [IX_customer_proposal_category_interests_category_id_is_active] ON [customer_proposal_category_interests] ([category_id], [is_active]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE UNIQUE INDEX [IX_customer_proposal_category_interests_customer_profile_id_category_id] ON [customer_proposal_category_interests] ([customer_profile_id], [category_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE INDEX [IX_customer_proposal_signals_category_id] ON [customer_proposal_signals] ([category_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE INDEX [IX_customer_proposal_signals_customer_profile_id_category_id_expires_at] ON [customer_proposal_signals] ([customer_profile_id], [category_id], [expires_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_proposal_applications_campaign_id_customer_profile_id] ON [provider_proposal_applications] ([campaign_id], [customer_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE INDEX [IX_provider_proposal_applications_campaign_id_status_code] ON [provider_proposal_applications] ([campaign_id], [status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE INDEX [IX_provider_proposal_applications_capture_ledger_entry_id] ON [provider_proposal_applications] ([capture_ledger_entry_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE INDEX [IX_provider_proposal_applications_created_by_user_id] ON [provider_proposal_applications] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE INDEX [IX_provider_proposal_applications_customer_profile_id] ON [provider_proposal_applications] ([customer_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_proposal_applications_public_id] ON [provider_proposal_applications] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE INDEX [IX_provider_proposal_applications_updated_by_user_id] ON [provider_proposal_applications] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE INDEX [IX_provider_proposal_areas_administrative_area_id] ON [provider_proposal_areas] ([administrative_area_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_proposal_areas_campaign_id_administrative_area_id] ON [provider_proposal_areas] ([campaign_id], [administrative_area_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE INDEX [IX_provider_proposal_areas_created_by_user_id] ON [provider_proposal_areas] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE INDEX [IX_provider_proposal_campaigns_created_by_user_id] ON [provider_proposal_campaigns] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE INDEX [IX_provider_proposal_campaigns_provider_profile_id_status_code] ON [provider_proposal_campaigns] ([provider_profile_id], [status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE INDEX [IX_provider_proposal_campaigns_provider_service_category_id_status_code] ON [provider_proposal_campaigns] ([provider_service_category_id], [status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_proposal_campaigns_public_id] ON [provider_proposal_campaigns] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE INDEX [IX_provider_proposal_campaigns_release_ledger_entry_id] ON [provider_proposal_campaigns] ([release_ledger_entry_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE INDEX [IX_provider_proposal_campaigns_reserve_ledger_entry_id] ON [provider_proposal_campaigns] ([reserve_ledger_entry_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE INDEX [IX_provider_proposal_campaigns_status_code_start_at_end_at] ON [provider_proposal_campaigns] ([status_code], [start_at], [end_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE INDEX [IX_provider_proposal_campaigns_updated_by_user_id] ON [provider_proposal_campaigns] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    CREATE INDEX [IX_provider_proposal_campaigns_wallet_id] ON [provider_proposal_campaigns] ([wallet_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830020412_AddProviderProposalMarketplaceV159'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260830020412_AddProviderProposalMarketplaceV159', N'10.0.10');
END;

COMMIT;
GO

