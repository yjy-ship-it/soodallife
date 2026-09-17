BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    ALTER TABLE [emergency_progress_events] DROP CONSTRAINT [CK_emergency_progress_events_type];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    ALTER TABLE [provider_emergency_service_settings] ADD [additional_fee_text] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    ALTER TABLE [provider_emergency_service_settings] ADD [base_dispatch_fee_amount] decimal(19,4) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    ALTER TABLE [provider_emergency_service_settings] ADD [no_show_fee_amount] decimal(19,4) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    ALTER TABLE [provider_emergency_service_settings] ADD [no_show_wait_minutes] int NOT NULL DEFAULT 10;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    ALTER TABLE [provider_emergency_service_settings] ADD [payment_instruction_protected] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    ALTER TABLE [provider_emergency_service_settings] ADD [payment_mode_code] varchar(30) NOT NULL DEFAULT 'ON_SITE';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    ALTER TABLE [provider_emergency_service_settings] ADD [work_fee_separate] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    ALTER TABLE [emergency_responses] ADD [additional_fee_text] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    ALTER TABLE [emergency_responses] ADD [base_dispatch_fee_amount] decimal(19,4) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    ALTER TABLE [emergency_responses] ADD [no_show_fee_amount] decimal(19,4) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    ALTER TABLE [emergency_responses] ADD [no_show_wait_minutes] int NOT NULL DEFAULT 10;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    ALTER TABLE [emergency_responses] ADD [payment_mode_code] varchar(30) NOT NULL DEFAULT 'ON_SITE';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    ALTER TABLE [emergency_responses] ADD [work_fee_separate] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    CREATE TABLE [emergency_dispatch_agreements] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [transaction_id] bigint NOT NULL,
        [base_dispatch_fee_amount] decimal(19,4) NOT NULL,
        [payment_mode_code] varchar(30) NOT NULL,
        [payment_status_code] varchar(30) NOT NULL,
        [no_show_fee_amount] decimal(19,4) NOT NULL,
        [no_show_wait_minutes] int NOT NULL,
        [work_fee_separate] bit NOT NULL DEFAULT CAST(1 AS bit),
        [additional_fee_text] nvarchar(1000) NULL,
        [payment_instruction_protected] nvarchar(2000) NULL,
        [terms_accepted_at] datetime2(7) NOT NULL,
        [payment_reported_at] datetime2(7) NULL,
        [payment_confirmed_at] datetime2(7) NULL,
        [arrived_at] datetime2(7) NULL,
        [no_show_wait_until] datetime2(7) NULL,
        [no_show_status_code] varchar(30) NULL,
        [no_show_reported_at] datetime2(7) NULL,
        [no_show_reported_by_user_id] bigint NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_emergency_dispatch_agreements] PRIMARY KEY ([id]),
        CONSTRAINT [CK_emergency_agreement_no_show] CHECK ([no_show_status_code] IS NULL OR [no_show_status_code] IN ('WAITING','CUSTOMER_NO_SHOW','PROVIDER_NO_SHOW','DISPUTED')),
        CONSTRAINT [CK_emergency_agreement_payment_mode] CHECK ([payment_mode_code] IN ('NO_FEE','ON_SITE','TRANSFER_REPORTED','TRANSFER_CONFIRMED')),
        CONSTRAINT [CK_emergency_agreement_payment_status] CHECK ([payment_status_code] IN ('NOT_REQUIRED','ON_SITE_PENDING','AWAITING_TRANSFER','REPORTED','CONFIRMED','REJECTED')),
        CONSTRAINT [FK_emergency_dispatch_agreements_transactions_transaction_id] FOREIGN KEY ([transaction_id]) REFERENCES [transactions] ([id]),
        CONSTRAINT [FK_emergency_dispatch_agreements_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_emergency_dispatch_agreements_users_no_show_reported_by_user_id] FOREIGN KEY ([no_show_reported_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_emergency_dispatch_agreements_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    EXEC(N'ALTER TABLE [provider_emergency_service_settings] ADD CONSTRAINT [CK_provider_emergency_service_amounts] CHECK ([base_dispatch_fee_amount] >= 0 AND [no_show_fee_amount] >= 0 AND [no_show_fee_amount] <= [base_dispatch_fee_amount])');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    EXEC(N'ALTER TABLE [provider_emergency_service_settings] ADD CONSTRAINT [CK_provider_emergency_service_payment_mode] CHECK ([payment_mode_code] IN (''NO_FEE'',''ON_SITE'',''TRANSFER_REPORTED'',''TRANSFER_CONFIRMED''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    EXEC(N'ALTER TABLE [provider_emergency_service_settings] ADD CONSTRAINT [CK_provider_emergency_service_wait] CHECK ([no_show_wait_minutes] BETWEEN 5 AND 60)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    EXEC(N'ALTER TABLE [emergency_progress_events] ADD CONSTRAINT [CK_emergency_progress_events_type] CHECK ([event_type_code] IN (''DISPATCH_CONFIRMED'',''PAYMENT_REPORTED'',''PAYMENT_CONFIRMED'',''PAYMENT_REJECTED'',''DEPARTED'',''ARRIVED'',''COMPLETED'',''NO_SHOW_WAITING'',''CUSTOMER_NO_SHOW'',''PROVIDER_NO_SHOW'',''NO_SHOW_DISPUTED''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    CREATE INDEX [IX_emergency_dispatch_agreements_created_by_user_id] ON [emergency_dispatch_agreements] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    CREATE INDEX [IX_emergency_dispatch_agreements_no_show_reported_by_user_id] ON [emergency_dispatch_agreements] ([no_show_reported_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    CREATE UNIQUE INDEX [IX_emergency_dispatch_agreements_public_id] ON [emergency_dispatch_agreements] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    CREATE UNIQUE INDEX [IX_emergency_dispatch_agreements_transaction_id] ON [emergency_dispatch_agreements] ([transaction_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    CREATE INDEX [IX_emergency_dispatch_agreements_updated_by_user_id] ON [emergency_dispatch_agreements] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824132109_ImplementAutonomousEmergencyDispatchV131'
)
BEGIN
    INSERT INTO [emergency_dispatch_agreements]
        ([public_id],[transaction_id],[base_dispatch_fee_amount],[payment_mode_code],[payment_status_code],
         [no_show_fee_amount],[no_show_wait_minutes],[work_fee_separate],[terms_accepted_at],
         [created_at],[created_by_user_id],[updated_at],[updated_by_user_id])
    SELECT NEWID(),t.[id],0,N'ON_SITE',N'ON_SITE_PENDING',0,10,1,
           COALESCE(r.[accepted_at],t.[created_at]),t.[created_at],t.[created_by_user_id],t.[updated_at],t.[updated_by_user_id]
    FROM [transactions] t
    INNER JOIN [service_requests] r ON r.[id]=t.[service_request_id] AND r.[is_urgent]=1
    WHERE NOT EXISTS (SELECT 1 FROM [emergency_dispatch_agreements] a WHERE a.[transaction_id]=t.[id]);

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260824132109_ImplementAutonomousEmergencyDispatchV131', N'10.0.10');
END;

COMMIT;
GO

