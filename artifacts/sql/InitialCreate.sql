IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [users] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [login_id] nvarchar(256) NOT NULL,
        [normalized_login_id] nvarchar(256) NOT NULL,
        [password_hash] nvarchar(512) NOT NULL,
        [email] nvarchar(320) NULL,
        [phone] nvarchar(32) NULL,
        [status_code] varchar(20) NOT NULL DEFAULT 'ACTIVE',
        [last_login_at] datetime2(7) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_users] PRIMARY KEY ([id]),
        CONSTRAINT [CK_users_status_code] CHECK ([status_code] IN ('ACTIVE','SUSPENDED','WITHDRAWN')),
        CONSTRAINT [FK_users_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_users_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [administrative_areas] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [source_system_code] varchar(30) NOT NULL DEFAULT 'MOIS_STANDARD_CODE',
        [area_code] varchar(20) NOT NULL,
        [area_name] nvarchar(100) NOT NULL,
        [area_level_code] varchar(20) NOT NULL,
        [parent_area_id] bigint NULL,
        [source_parent_area_code] varchar(20) NULL,
        [source_created_date] date NULL,
        [source_abolished_date] date NULL,
        [abolition_type_code] varchar(30) NULL,
        [effective_from] date NOT NULL,
        [effective_to] date NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_administrative_areas] PRIMARY KEY ([id]),
        CONSTRAINT [CK_administrative_areas_level] CHECK ([area_level_code] IN ('SIDO','SIGUNGU')),
        CONSTRAINT [FK_administrative_areas_administrative_areas_parent_area_id] FOREIGN KEY ([parent_area_id]) REFERENCES [administrative_areas] ([id]),
        CONSTRAINT [FK_administrative_areas_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_administrative_areas_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [audit_logs] (
        [id] bigint NOT NULL IDENTITY,
        [occurred_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [actor_user_id] bigint NULL,
        [actor_role_code] varchar(30) NULL,
        [action_code] varchar(50) NOT NULL,
        [entity_type] varchar(100) NOT NULL,
        [entity_public_id] uniqueidentifier NULL,
        [result_code] varchar(20) NOT NULL DEFAULT 'SUCCESS',
        [correlation_id] uniqueidentifier NULL,
        [ip_address] varchar(45) NULL,
        [user_agent] nvarchar(1000) NULL,
        [reason] nvarchar(1000) NULL,
        [before_json] nvarchar(max) NULL,
        [after_json] nvarchar(max) NULL,
        [metadata_json] nvarchar(max) NULL,
        CONSTRAINT [PK_audit_logs] PRIMARY KEY ([id]),
        CONSTRAINT [CK_audit_logs_after_json] CHECK ([after_json] IS NULL OR ISJSON([after_json]) = 1),
        CONSTRAINT [CK_audit_logs_before_json] CHECK ([before_json] IS NULL OR ISJSON([before_json]) = 1),
        CONSTRAINT [CK_audit_logs_metadata_json] CHECK ([metadata_json] IS NULL OR ISJSON([metadata_json]) = 1),
        CONSTRAINT [CK_audit_logs_result] CHECK ([result_code] IN ('SUCCESS','FAILURE')),
        CONSTRAINT [FK_audit_logs_users_actor_user_id] FOREIGN KEY ([actor_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [completion_photo_roles] (
        [id] bigint NOT NULL IDENTITY,
        [code] varchar(50) NOT NULL,
        [name] nvarchar(100) NOT NULL,
        [description] nvarchar(1000) NOT NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_completion_photo_roles] PRIMARY KEY ([id]),
        CONSTRAINT [FK_completion_photo_roles_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_completion_photo_roles_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [customer_profiles] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [user_id] bigint NOT NULL,
        [display_name] nvarchar(100) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_customer_profiles] PRIMARY KEY ([id]),
        CONSTRAINT [FK_customer_profiles_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_customer_profiles_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_customer_profiles_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [fee_policies] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [code] varchar(50) NOT NULL,
        [policy_kind_code] varchar(20) NOT NULL,
        [transaction_type_code] varchar(20) NOT NULL,
        [applies_to_text] nvarchar(200) NOT NULL,
        [calculation_method_text] nvarchar(100) NULL,
        [min_base_amount] decimal(19,4) NULL,
        [max_base_amount] decimal(19,4) NULL,
        [display_fee_amount] decimal(19,4) NULL,
        [rate] decimal(9,6) NULL,
        [monthly_amount] decimal(19,4) NULL,
        [per_visit_amount] decimal(19,4) NULL,
        [currency_code] char(3) NOT NULL DEFAULT 'KRW',
        [charge_timing_text] nvarchar(300) NOT NULL,
        [restore_rule_text] nvarchar(1000) NULL,
        [note] nvarchar(1000) NULL,
        [effective_from] date NULL,
        [effective_to] date NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_fee_policies] PRIMARY KEY ([id]),
        CONSTRAINT [CK_fee_policies_kind] CHECK ([policy_kind_code] IN ('QUOTE','SUPPORT','PROJECT','SUBSCRIPTION')),
        CONSTRAINT [CK_fee_policies_rate] CHECK ([rate] IS NULL OR ([rate] >= 0 AND [rate] <= 1)),
        CONSTRAINT [CK_fee_policies_transaction_type] CHECK ([transaction_type_code] IN ('ONE_TIME','PROJECT','SUBSCRIPTION')),
        CONSTRAINT [FK_fee_policies_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_fee_policies_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [files] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [purpose_code] varchar(30) NOT NULL,
        [storage_container] nvarchar(200) NOT NULL,
        [storage_key] nvarchar(1000) NOT NULL,
        [storage_key_hash] binary(32) NOT NULL,
        [original_file_name] nvarchar(255) NOT NULL,
        [content_type] varchar(200) NOT NULL,
        [size_bytes] bigint NOT NULL,
        [sha256_hex] char(64) NOT NULL,
        [status_code] varchar(20) NOT NULL DEFAULT 'PENDING',
        [scan_result_text] nvarchar(1000) NULL,
        [activated_at] datetime2(7) NULL,
        [deleted_at] datetime2(7) NULL,
        [uploaded_by_user_id] bigint NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_files] PRIMARY KEY ([id]),
        CONSTRAINT [CK_files_purpose] CHECK ([purpose_code] IN ('PROVIDER_DOCUMENT','REQUEST_ANSWER','COMPLETION_EVIDENCE','AFTER_SERVICE')),
        CONSTRAINT [CK_files_size_bytes] CHECK ([size_bytes] >= 0),
        CONSTRAINT [CK_files_status] CHECK ([status_code] IN ('PENDING','ACTIVE','QUARANTINED','DELETED')),
        CONSTRAINT [FK_files_users_uploaded_by_user_id] FOREIGN KEY ([uploaded_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [outbox_events] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [aggregate_type] varchar(100) NOT NULL,
        [aggregate_public_id] uniqueidentifier NOT NULL,
        [event_type] varchar(100) NOT NULL,
        [payload_json] nvarchar(max) NOT NULL,
        [status_code] varchar(20) NOT NULL DEFAULT 'PENDING',
        [occurred_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [available_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [attempt_count] int NOT NULL DEFAULT 0,
        [last_attempt_at] datetime2(7) NULL,
        [processed_at] datetime2(7) NULL,
        [error_message] nvarchar(2000) NULL,
        [idempotency_key] varchar(120) NOT NULL,
        [created_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_outbox_events] PRIMARY KEY ([id]),
        CONSTRAINT [CK_outbox_events_payload_json] CHECK (ISJSON([payload_json]) = 1),
        CONSTRAINT [CK_outbox_events_status] CHECK ([status_code] IN ('PENDING','PROCESSING','PUBLISHED','FAILED')),
        CONSTRAINT [FK_outbox_events_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [provider_profiles] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [user_id] bigint NOT NULL,
        [business_name] nvarchar(200) NOT NULL,
        [business_registration_no] nvarchar(32) NULL,
        [approval_status_code] varchar(20) NOT NULL DEFAULT 'PENDING',
        [activity_status_code] varchar(20) NOT NULL DEFAULT 'INACTIVE',
        [trust_score] decimal(9,4) NULL,
        [approval_decided_at] datetime2(7) NULL,
        [approval_decided_by_user_id] bigint NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_provider_profiles] PRIMARY KEY ([id]),
        CONSTRAINT [CK_provider_profiles_activity_status] CHECK ([activity_status_code] IN ('ACTIVE','INACTIVE')),
        CONSTRAINT [CK_provider_profiles_approval_status] CHECK ([approval_status_code] IN ('PENDING','APPROVED','REJECTED','SUSPENDED')),
        CONSTRAINT [FK_provider_profiles_users_approval_decided_by_user_id] FOREIGN KEY ([approval_decided_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_provider_profiles_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_provider_profiles_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_provider_profiles_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [roles] (
        [id] bigint NOT NULL IDENTITY,
        [code] varchar(30) NOT NULL,
        [name] nvarchar(100) NOT NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_roles] PRIMARY KEY ([id]),
        CONSTRAINT [FK_roles_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_roles_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [service_categories] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [parent_id] bigint NULL,
        [level_code] varchar(20) NOT NULL,
        [external_code] varchar(50) NULL,
        [source_record_id] varchar(50) NULL,
        [name] nvarchar(200) NOT NULL,
        [status_code] varchar(20) NOT NULL DEFAULT 'ACTIVE',
        [sort_order] int NOT NULL DEFAULT 0,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_service_categories] PRIMARY KEY ([id]),
        CONSTRAINT [CK_service_categories_level] CHECK ([level_code] IN ('MAJOR','MIDDLE','SERVICE')),
        CONSTRAINT [CK_service_categories_status] CHECK ([status_code] IN ('ACTIVE','PAUSED','REVIEW')),
        CONSTRAINT [FK_service_categories_service_categories_parent_id] FOREIGN KEY ([parent_id]) REFERENCES [service_categories] ([id]),
        CONSTRAINT [FK_service_categories_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_service_categories_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [service_assets] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [customer_profile_id] bigint NOT NULL,
        [asset_type_code] varchar(50) NOT NULL,
        [name] nvarchar(200) NOT NULL,
        [manufacturer] nvarchar(200) NULL,
        [model_name] nvarchar(200) NULL,
        [serial_number] nvarchar(200) NULL,
        [installed_at] date NULL,
        [attributes_json] nvarchar(max) NULL,
        [status_code] varchar(20) NOT NULL DEFAULT 'ACTIVE',
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_service_assets] PRIMARY KEY ([id]),
        CONSTRAINT [CK_service_assets_attributes_json] CHECK ([attributes_json] IS NULL OR ISJSON([attributes_json]) = 1),
        CONSTRAINT [CK_service_assets_status] CHECK ([status_code] IN ('ACTIVE','INACTIVE')),
        CONSTRAINT [FK_service_assets_customer_profiles_customer_profile_id] FOREIGN KEY ([customer_profile_id]) REFERENCES [customer_profiles] ([id]),
        CONSTRAINT [FK_service_assets_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_service_assets_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [provider_approval_events] (
        [id] bigint NOT NULL IDENTITY,
        [provider_profile_id] bigint NOT NULL,
        [from_status_code] varchar(20) NULL,
        [to_status_code] varchar(20) NOT NULL,
        [action_code] varchar(20) NOT NULL,
        [reason] nvarchar(1000) NULL,
        [decided_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [decided_by_user_id] bigint NOT NULL,
        [correlation_id] uniqueidentifier NULL,
        CONSTRAINT [PK_provider_approval_events] PRIMARY KEY ([id]),
        CONSTRAINT [CK_provider_approval_events_action] CHECK ([action_code] IN ('APPROVE','REJECT','SUSPEND','RESUME')),
        CONSTRAINT [CK_provider_approval_events_from_status] CHECK ([from_status_code] IS NULL OR [from_status_code] IN ('PENDING','APPROVED','REJECTED','SUSPENDED')),
        CONSTRAINT [CK_provider_approval_events_to_status] CHECK ([to_status_code] IN ('PENDING','APPROVED','REJECTED','SUSPENDED')),
        CONSTRAINT [FK_provider_approval_events_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_provider_approval_events_users_decided_by_user_id] FOREIGN KEY ([decided_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [provider_documents] (
        [id] bigint NOT NULL IDENTITY,
        [provider_profile_id] bigint NOT NULL,
        [file_id] bigint NOT NULL,
        [document_type_code] varchar(50) NOT NULL,
        [document_number] nvarchar(100) NULL,
        [issued_at] date NULL,
        [expires_at] date NULL,
        [verification_status_code] varchar(20) NOT NULL DEFAULT 'PENDING',
        [verified_at] datetime2(7) NULL,
        [verified_by_user_id] bigint NULL,
        [note] nvarchar(1000) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_provider_documents] PRIMARY KEY ([id]),
        CONSTRAINT [FK_provider_documents_files_file_id] FOREIGN KEY ([file_id]) REFERENCES [files] ([id]),
        CONSTRAINT [FK_provider_documents_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_provider_documents_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_provider_documents_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_provider_documents_users_verified_by_user_id] FOREIGN KEY ([verified_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [user_roles] (
        [id] bigint NOT NULL IDENTITY,
        [user_id] bigint NOT NULL,
        [role_id] bigint NOT NULL,
        [granted_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [granted_by_user_id] bigint NULL,
        [revoked_at] datetime2(7) NULL,
        [revoked_by_user_id] bigint NULL,
        CONSTRAINT [PK_user_roles] PRIMARY KEY ([id]),
        CONSTRAINT [FK_user_roles_roles_role_id] FOREIGN KEY ([role_id]) REFERENCES [roles] ([id]),
        CONSTRAINT [FK_user_roles_users_granted_by_user_id] FOREIGN KEY ([granted_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_user_roles_users_revoked_by_user_id] FOREIGN KEY ([revoked_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_user_roles_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [category_field_definitions] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [source_field_id] varchar(50) NOT NULL,
        [owner_middle_category_id] bigint NOT NULL,
        [field_key] varchar(100) NOT NULL,
        [label] nvarchar(200) NOT NULL,
        [field_type_code] varchar(20) NOT NULL,
        [is_required] bit NOT NULL DEFAULT CAST(0 AS bit),
        [options_or_unit_text] nvarchar(2000) NULL,
        [provider_visibility_code] varchar(20) NOT NULL DEFAULT 'FULL',
        [pre_accept_masking_code] varchar(30) NOT NULL DEFAULT 'NONE',
        [validation_rule_text] nvarchar(1000) NOT NULL,
        [display_order] int NOT NULL DEFAULT 0,
        [status_code] varchar(20) NOT NULL DEFAULT 'ACTIVE',
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_category_field_definitions] PRIMARY KEY ([id]),
        CONSTRAINT [CK_category_field_definitions_masking] CHECK ([pre_accept_masking_code] IN ('NONE','DETAIL_ADDRESS')),
        CONSTRAINT [CK_category_field_definitions_status] CHECK ([status_code] IN ('ACTIVE','INACTIVE')),
        CONSTRAINT [CK_category_field_definitions_type] CHECK ([field_type_code] IN ('LONG_TEXT','FILE','DATETIME','MONEY','TEXT','ADDRESS','SELECT','NUMBER','PERIOD','RECURRENCE')),
        CONSTRAINT [CK_category_field_definitions_visibility] CHECK ([provider_visibility_code] IN ('FULL','AREA_ONLY')),
        CONSTRAINT [FK_category_field_definitions_service_categories_owner_middle_category_id] FOREIGN KEY ([owner_middle_category_id]) REFERENCES [service_categories] ([id]),
        CONSTRAINT [FK_category_field_definitions_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_category_field_definitions_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [category_policies] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [category_id] bigint NOT NULL,
        [policy_version] varchar(30) NOT NULL,
        [transaction_type_code] varchar(20) NOT NULL,
        [request_method_text] nvarchar(300) NOT NULL,
        [onsite_requirement_text] nvarchar(30) NOT NULL,
        [is_emergency_allowed] bit NOT NULL DEFAULT CAST(0 AS bit),
        [subscription_option_text] nvarchar(30) NOT NULL,
        [standard_work_unit_text] nvarchar(100) NOT NULL,
        [base_price_amount] decimal(19,4) NOT NULL,
        [currency_code] char(3) NOT NULL DEFAULT 'KRW',
        [price_method_text] nvarchar(30) NOT NULL,
        [vat_display_rule_text] nvarchar(100) NOT NULL,
        [minimum_budget_amount] decimal(19,4) NOT NULL,
        [max_quote_count] smallint NOT NULL,
        [quote_validity_minutes] int NOT NULL,
        [fee_policy_id] bigint NOT NULL,
        [estimated_quote_fee_amount] decimal(19,4) NOT NULL,
        [fee_charge_timing_text] nvarchar(200) NOT NULL,
        [fee_restore_condition_text] nvarchar(500) NOT NULL,
        [matching_area_rule_text] nvarchar(200) NOT NULL,
        [notification_target_rule_text] nvarchar(300) NOT NULL,
        [provider_response_deadline_minutes] int NOT NULL,
        [request_field_summary_text] nvarchar(1000) NOT NULL,
        [required_completion_photo_count] smallint NOT NULL DEFAULT CAST(0 AS smallint),
        [required_qualification_summary_text] nvarchar(1000) NOT NULL,
        [insurance_requirement_text] nvarchar(100) NOT NULL,
        [safety_grade_code] varchar(20) NOT NULL,
        [completion_evidence_rule_text] nvarchar(1000) NOT NULL,
        [default_warranty_days] smallint NOT NULL DEFAULT CAST(0 AS smallint),
        [trust_score_display_text] nvarchar(100) NOT NULL,
        [default_sort_code] varchar(30) NOT NULL,
        [service_area_level_code] varchar(20) NOT NULL DEFAULT 'SIGUNGU',
        [reference_url] nvarchar(2048) NOT NULL,
        [admin_note] nvarchar(2000) NOT NULL,
        [effective_from] date NOT NULL,
        [effective_to] date NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_category_policies] PRIMARY KEY ([id]),
        CONSTRAINT [CK_category_policies_max_quotes] CHECK ([max_quote_count] > 0),
        CONSTRAINT [CK_category_policies_photo_count] CHECK ([required_completion_photo_count] >= 0),
        CONSTRAINT [CK_category_policies_quote_validity] CHECK ([quote_validity_minutes] > 0),
        CONSTRAINT [CK_category_policies_response_deadline] CHECK ([provider_response_deadline_minutes] > 0),
        CONSTRAINT [CK_category_policies_safety_grade] CHECK ([safety_grade_code] IN ('NORMAL','MEDIUM','HIGH')),
        CONSTRAINT [CK_category_policies_transaction_type] CHECK ([transaction_type_code] IN ('ONE_TIME','SUBSCRIPTION','PROJECT')),
        CONSTRAINT [CK_category_policies_warranty_days] CHECK ([default_warranty_days] >= 0),
        CONSTRAINT [FK_category_policies_fee_policies_fee_policy_id] FOREIGN KEY ([fee_policy_id]) REFERENCES [fee_policies] ([id]),
        CONSTRAINT [FK_category_policies_service_categories_category_id] FOREIGN KEY ([category_id]) REFERENCES [service_categories] ([id]),
        CONSTRAINT [FK_category_policies_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_category_policies_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [provider_service_categories] (
        [id] bigint NOT NULL IDENTITY,
        [provider_profile_id] bigint NOT NULL,
        [category_id] bigint NOT NULL,
        [status_code] varchar(20) NOT NULL DEFAULT 'ACTIVE',
        [activated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [deactivated_at] datetime2(7) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_provider_service_categories] PRIMARY KEY ([id]),
        CONSTRAINT [CK_provider_service_categories_status] CHECK ([status_code] IN ('ACTIVE','INACTIVE')),
        CONSTRAINT [FK_provider_service_categories_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_provider_service_categories_service_categories_category_id] FOREIGN KEY ([category_id]) REFERENCES [service_categories] ([id]),
        CONSTRAINT [FK_provider_service_categories_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_provider_service_categories_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [qualification_policies] (
        [id] bigint NOT NULL IDENTITY,
        [source_policy_id] varchar(50) NOT NULL,
        [middle_category_id] bigint NOT NULL,
        [identity_verification_rule_text] nvarchar(500) NOT NULL,
        [business_registration_rule_text] nvarchar(500) NOT NULL,
        [required_license_text] nvarchar(2000) NOT NULL,
        [insurance_rule_text] nvarchar(1000) NOT NULL,
        [equipment_facility_rule_text] nvarchar(1000) NOT NULL,
        [background_check_rule_text] nvarchar(1000) NOT NULL,
        [safety_grade_code] varchar(20) NOT NULL,
        [emergency_rule_text] nvarchar(1000) NOT NULL,
        [review_cycle_text] nvarchar(500) NOT NULL,
        [admin_checklist_text] nvarchar(2000) NOT NULL,
        [effective_from] date NULL,
        [effective_to] date NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_qualification_policies] PRIMARY KEY ([id]),
        CONSTRAINT [CK_qualification_policies_safety_grade] CHECK ([safety_grade_code] IN ('NORMAL','MEDIUM','HIGH')),
        CONSTRAINT [FK_qualification_policies_service_categories_middle_category_id] FOREIGN KEY ([middle_category_id]) REFERENCES [service_categories] ([id]),
        CONSTRAINT [FK_qualification_policies_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_qualification_policies_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [category_field_assignments] (
        [id] bigint NOT NULL IDENTITY,
        [field_definition_id] bigint NOT NULL,
        [target_category_id] bigint NOT NULL,
        [scope_code] varchar(20) NOT NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_category_field_assignments] PRIMARY KEY ([id]),
        CONSTRAINT [CK_category_field_assignments_scope] CHECK ([scope_code] IN ('MIDDLE','SERVICE')),
        CONSTRAINT [FK_category_field_assignments_category_field_definitions_field_definition_id] FOREIGN KEY ([field_definition_id]) REFERENCES [category_field_definitions] ([id]),
        CONSTRAINT [FK_category_field_assignments_service_categories_target_category_id] FOREIGN KEY ([target_category_id]) REFERENCES [service_categories] ([id]),
        CONSTRAINT [FK_category_field_assignments_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_category_field_assignments_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [category_completion_photo_requirements] (
        [id] bigint NOT NULL IDENTITY,
        [category_policy_id] bigint NOT NULL,
        [photo_role_id] bigint NOT NULL,
        [minimum_count] smallint NOT NULL,
        [display_order] int NOT NULL DEFAULT 0,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_category_completion_photo_requirements] PRIMARY KEY ([id]),
        CONSTRAINT [CK_category_completion_photo_requirements_minimum] CHECK ([minimum_count] >= 0),
        CONSTRAINT [FK_category_completion_photo_requirements_category_policies_category_policy_id] FOREIGN KEY ([category_policy_id]) REFERENCES [category_policies] ([id]),
        CONSTRAINT [FK_category_completion_photo_requirements_completion_photo_roles_photo_role_id] FOREIGN KEY ([photo_role_id]) REFERENCES [completion_photo_roles] ([id]),
        CONSTRAINT [FK_category_completion_photo_requirements_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_category_completion_photo_requirements_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [service_requests] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [customer_profile_id] bigint NOT NULL,
        [category_id] bigint NOT NULL,
        [category_policy_id] bigint NOT NULL,
        [administrative_area_id] bigint NOT NULL,
        [detail_address] nvarchar(500) NULL,
        [title] nvarchar(200) NOT NULL,
        [description] nvarchar(max) NULL,
        [status_code] varchar(20) NOT NULL DEFAULT 'DRAFT',
        [is_urgent] bit NOT NULL DEFAULT CAST(0 AS bit),
        [policy_snapshot_json] nvarchar(max) NOT NULL,
        [opened_at] datetime2(7) NULL,
        [expires_at] datetime2(7) NULL,
        [accepted_at] datetime2(7) NULL,
        [cancelled_at] datetime2(7) NULL,
        [cancellation_reason] nvarchar(1000) NULL,
        [idempotency_key] varchar(100) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_service_requests] PRIMARY KEY ([id]),
        CONSTRAINT [CK_service_requests_policy_json] CHECK (ISJSON([policy_snapshot_json]) = 1),
        CONSTRAINT [CK_service_requests_status] CHECK ([status_code] IN ('DRAFT','OPEN','ACCEPTED','EXPIRED','CANCELLED')),
        CONSTRAINT [FK_service_requests_administrative_areas_administrative_area_id] FOREIGN KEY ([administrative_area_id]) REFERENCES [administrative_areas] ([id]),
        CONSTRAINT [FK_service_requests_category_policies_category_policy_id] FOREIGN KEY ([category_policy_id]) REFERENCES [category_policies] ([id]),
        CONSTRAINT [FK_service_requests_customer_profiles_customer_profile_id] FOREIGN KEY ([customer_profile_id]) REFERENCES [customer_profiles] ([id]),
        CONSTRAINT [FK_service_requests_service_categories_category_id] FOREIGN KEY ([category_id]) REFERENCES [service_categories] ([id]),
        CONSTRAINT [FK_service_requests_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_service_requests_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [provider_service_areas] (
        [id] bigint NOT NULL IDENTITY,
        [provider_service_category_id] bigint NOT NULL,
        [administrative_area_id] bigint NOT NULL,
        [status_code] varchar(20) NOT NULL DEFAULT 'ACTIVE',
        [activated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [deactivated_at] datetime2(7) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_provider_service_areas] PRIMARY KEY ([id]),
        CONSTRAINT [CK_provider_service_areas_status] CHECK ([status_code] IN ('ACTIVE','INACTIVE')),
        CONSTRAINT [FK_provider_service_areas_administrative_areas_administrative_area_id] FOREIGN KEY ([administrative_area_id]) REFERENCES [administrative_areas] ([id]),
        CONSTRAINT [FK_provider_service_areas_provider_service_categories_provider_service_category_id] FOREIGN KEY ([provider_service_category_id]) REFERENCES [provider_service_categories] ([id]),
        CONSTRAINT [FK_provider_service_areas_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_provider_service_areas_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [dispatch_candidates] (
        [id] bigint NOT NULL IDENTITY,
        [service_request_id] bigint NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [status_code] varchar(20) NOT NULL,
        [category_match] bit NOT NULL,
        [area_match] bit NOT NULL,
        [approval_match] bit NOT NULL,
        [reason_code] varchar(50) NULL,
        [evaluated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [expires_at] datetime2(7) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_dispatch_candidates] PRIMARY KEY ([id]),
        CONSTRAINT [CK_dispatch_candidates_status] CHECK ([status_code] IN ('ELIGIBLE','INELIGIBLE','DISPATCHED','EXPIRED')),
        CONSTRAINT [FK_dispatch_candidates_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_dispatch_candidates_service_requests_service_request_id] FOREIGN KEY ([service_request_id]) REFERENCES [service_requests] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [request_answers] (
        [id] bigint NOT NULL IDENTITY,
        [service_request_id] bigint NOT NULL,
        [field_definition_id] bigint NOT NULL,
        [value_text] nvarchar(max) NULL,
        [value_number] decimal(19,4) NULL,
        [value_boolean] bit NULL,
        [value_date] date NULL,
        [value_datetime] datetime2(7) NULL,
        [value_json] nvarchar(max) NULL,
        [value_currency_code] char(3) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_request_answers] PRIMARY KEY ([id]),
        CONSTRAINT [CK_request_answers_value_json] CHECK ([value_json] IS NULL OR ISJSON([value_json]) = 1),
        CONSTRAINT [FK_request_answers_category_field_definitions_field_definition_id] FOREIGN KEY ([field_definition_id]) REFERENCES [category_field_definitions] ([id]),
        CONSTRAINT [FK_request_answers_service_requests_service_request_id] FOREIGN KEY ([service_request_id]) REFERENCES [service_requests] ([id]),
        CONSTRAINT [FK_request_answers_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_request_answers_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [request_dispatches] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [service_request_id] bigint NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [candidate_id] bigint NOT NULL,
        [status_code] varchar(20) NOT NULL DEFAULT 'AVAILABLE',
        [available_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [viewed_at] datetime2(7) NULL,
        [responded_at] datetime2(7) NULL,
        [expires_at] datetime2(7) NOT NULL,
        [idempotency_key] varchar(100) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_request_dispatches] PRIMARY KEY ([id]),
        CONSTRAINT [CK_request_dispatches_status] CHECK ([status_code] IN ('AVAILABLE','VIEWED','RESPONDED','EXPIRED')),
        CONSTRAINT [FK_request_dispatches_dispatch_candidates_candidate_id] FOREIGN KEY ([candidate_id]) REFERENCES [dispatch_candidates] ([id]),
        CONSTRAINT [FK_request_dispatches_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_request_dispatches_service_requests_service_request_id] FOREIGN KEY ([service_request_id]) REFERENCES [service_requests] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [request_answer_files] (
        [id] bigint NOT NULL IDENTITY,
        [request_answer_id] bigint NOT NULL,
        [file_id] bigint NOT NULL,
        [display_order] int NOT NULL DEFAULT 0,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_request_answer_files] PRIMARY KEY ([id]),
        CONSTRAINT [FK_request_answer_files_files_file_id] FOREIGN KEY ([file_id]) REFERENCES [files] ([id]),
        CONSTRAINT [FK_request_answer_files_request_answers_request_answer_id] FOREIGN KEY ([request_answer_id]) REFERENCES [request_answers] ([id]),
        CONSTRAINT [FK_request_answer_files_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [notifications] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [recipient_user_id] bigint NOT NULL,
        [request_dispatch_id] bigint NULL,
        [type_code] varchar(50) NOT NULL,
        [status_code] varchar(30) NOT NULL DEFAULT 'PENDING',
        [title] nvarchar(200) NOT NULL,
        [body] nvarchar(2000) NOT NULL,
        [data_json] nvarchar(max) NULL,
        [is_urgent] bit NOT NULL DEFAULT CAST(0 AS bit),
        [recorded_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [read_at] datetime2(7) NULL,
        [idempotency_key] varchar(100) NOT NULL,
        [created_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_notifications] PRIMARY KEY ([id]),
        CONSTRAINT [CK_notifications_data_json] CHECK ([data_json] IS NULL OR ISJSON([data_json]) = 1),
        CONSTRAINT [CK_notifications_status] CHECK ([status_code] IN ('PENDING','RECORDED','PROCESSING','SENT','PARTIALLY_FAILED','FAILED')),
        CONSTRAINT [FK_notifications_request_dispatches_request_dispatch_id] FOREIGN KEY ([request_dispatch_id]) REFERENCES [request_dispatches] ([id]),
        CONSTRAINT [FK_notifications_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_notifications_users_recipient_user_id] FOREIGN KEY ([recipient_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [quotes] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [service_request_id] bigint NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [request_dispatch_id] bigint NOT NULL,
        [status_code] varchar(20) NOT NULL DEFAULT 'DRAFT',
        [submitted_at] datetime2(7) NULL,
        [accepted_at] datetime2(7) NULL,
        [withdrawn_at] datetime2(7) NULL,
        [expires_at] datetime2(7) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_quotes] PRIMARY KEY ([id]),
        CONSTRAINT [CK_quotes_status] CHECK ([status_code] IN ('DRAFT','SUBMITTED','ACCEPTED','NOT_SELECTED','WITHDRAWN','EXPIRED','INVALIDATED')),
        CONSTRAINT [FK_quotes_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_quotes_request_dispatches_request_dispatch_id] FOREIGN KEY ([request_dispatch_id]) REFERENCES [request_dispatches] ([id]),
        CONSTRAINT [FK_quotes_service_requests_service_request_id] FOREIGN KEY ([service_request_id]) REFERENCES [service_requests] ([id]),
        CONSTRAINT [FK_quotes_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_quotes_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [notification_deliveries] (
        [id] bigint NOT NULL IDENTITY,
        [notification_id] bigint NOT NULL,
        [channel_code] varchar(20) NOT NULL,
        [attempt_no] smallint NOT NULL DEFAULT CAST(1 AS smallint),
        [status_code] varchar(20) NOT NULL DEFAULT 'PENDING',
        [provider_message_id] nvarchar(200) NULL,
        [error_code] nvarchar(100) NULL,
        [error_message] nvarchar(1000) NULL,
        [attempted_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [completed_at] datetime2(7) NULL,
        CONSTRAINT [PK_notification_deliveries] PRIMARY KEY ([id]),
        CONSTRAINT [CK_notification_deliveries_channel] CHECK ([channel_code] IN ('IN_APP','ALIMTALK')),
        CONSTRAINT [CK_notification_deliveries_status] CHECK ([status_code] IN ('PENDING','SENT','FAILED','SKIPPED')),
        CONSTRAINT [FK_notification_deliveries_notifications_notification_id] FOREIGN KEY ([notification_id]) REFERENCES [notifications] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [quote_revisions] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [quote_id] bigint NOT NULL,
        [revision_no] int NOT NULL,
        [summary] nvarchar(1000) NOT NULL,
        [terms] nvarchar(max) NULL,
        [subtotal_amount] decimal(19,4) NOT NULL,
        [vat_amount] decimal(19,4) NOT NULL DEFAULT 0.0,
        [total_amount] decimal(19,4) NOT NULL,
        [currency_code] char(3) NOT NULL DEFAULT 'KRW',
        [estimated_duration_text] nvarchar(200) NULL,
        [available_start_at] datetime2(7) NULL,
        [valid_until] datetime2(7) NOT NULL,
        [revision_reason] nvarchar(1000) NULL,
        [submitted_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [submitted_by_user_id] bigint NOT NULL,
        [idempotency_key] varchar(100) NOT NULL,
        CONSTRAINT [PK_quote_revisions] PRIMARY KEY ([id]),
        CONSTRAINT [FK_quote_revisions_quotes_quote_id] FOREIGN KEY ([quote_id]) REFERENCES [quotes] ([id]),
        CONSTRAINT [FK_quote_revisions_users_submitted_by_user_id] FOREIGN KEY ([submitted_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [quote_items] (
        [id] bigint NOT NULL IDENTITY,
        [quote_revision_id] bigint NOT NULL,
        [line_no] int NOT NULL,
        [item_name] nvarchar(200) NOT NULL,
        [description] nvarchar(1000) NULL,
        [quantity] decimal(19,4) NOT NULL DEFAULT 1.0,
        [unit_text] nvarchar(50) NULL,
        [unit_price_amount] decimal(19,4) NOT NULL,
        [line_total_amount] decimal(19,4) NOT NULL,
        [currency_code] char(3) NOT NULL DEFAULT 'KRW',
        CONSTRAINT [PK_quote_items] PRIMARY KEY ([id]),
        CONSTRAINT [CK_quote_items_quantity] CHECK ([quantity] > 0),
        CONSTRAINT [FK_quote_items_quote_revisions_quote_revision_id] FOREIGN KEY ([quote_revision_id]) REFERENCES [quote_revisions] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [transactions] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [service_request_id] bigint NOT NULL,
        [accepted_quote_revision_id] bigint NOT NULL,
        [customer_profile_id] bigint NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [category_id] bigint NOT NULL,
        [status_code] varchar(30) NOT NULL DEFAULT 'CREATED',
        [agreed_amount] decimal(19,4) NOT NULL,
        [currency_code] char(3) NOT NULL DEFAULT 'KRW',
        [quote_snapshot_json] nvarchar(max) NOT NULL,
        [category_policy_snapshot_json] nvarchar(max) NOT NULL,
        [completion_policy_snapshot_json] nvarchar(max) NOT NULL,
        [warranty_days_snapshot] smallint NOT NULL DEFAULT CAST(0 AS smallint),
        [provider_trust_score_snapshot] decimal(9,4) NULL,
        [started_at] datetime2(7) NULL,
        [completed_at] datetime2(7) NULL,
        [cancelled_at] datetime2(7) NULL,
        [cancellation_reason] nvarchar(1000) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_transactions] PRIMARY KEY ([id]),
        CONSTRAINT [CK_transactions_category_policy_json] CHECK (ISJSON([category_policy_snapshot_json]) = 1),
        CONSTRAINT [CK_transactions_completion_policy_json] CHECK (ISJSON([completion_policy_snapshot_json]) = 1),
        CONSTRAINT [CK_transactions_quote_json] CHECK (ISJSON([quote_snapshot_json]) = 1),
        CONSTRAINT [CK_transactions_status] CHECK ([status_code] IN ('CREATED','IN_PROGRESS','COMPLETION_SUBMITTED','REVISION_REQUESTED','COMPLETED','DISPUTED','CANCELLED')),
        CONSTRAINT [CK_transactions_warranty_days] CHECK ([warranty_days_snapshot] >= 0),
        CONSTRAINT [FK_transactions_customer_profiles_customer_profile_id] FOREIGN KEY ([customer_profile_id]) REFERENCES [customer_profiles] ([id]),
        CONSTRAINT [FK_transactions_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_transactions_quote_revisions_accepted_quote_revision_id] FOREIGN KEY ([accepted_quote_revision_id]) REFERENCES [quote_revisions] ([id]),
        CONSTRAINT [FK_transactions_service_categories_category_id] FOREIGN KEY ([category_id]) REFERENCES [service_categories] ([id]),
        CONSTRAINT [FK_transactions_service_requests_service_request_id] FOREIGN KEY ([service_request_id]) REFERENCES [service_requests] ([id]),
        CONSTRAINT [FK_transactions_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_transactions_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [after_service_cases] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [transaction_id] bigint NOT NULL,
        [customer_profile_id] bigint NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [status_code] varchar(20) NOT NULL DEFAULT 'RECEIVED',
        [subject] nvarchar(200) NOT NULL,
        [description] nvarchar(max) NOT NULL,
        [received_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [started_at] datetime2(7) NULL,
        [completed_at] datetime2(7) NULL,
        [idempotency_key] varchar(100) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_after_service_cases] PRIMARY KEY ([id]),
        CONSTRAINT [CK_after_service_cases_status] CHECK ([status_code] IN ('RECEIVED','IN_PROGRESS','COMPLETED')),
        CONSTRAINT [FK_after_service_cases_customer_profiles_customer_profile_id] FOREIGN KEY ([customer_profile_id]) REFERENCES [customer_profiles] ([id]),
        CONSTRAINT [FK_after_service_cases_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_after_service_cases_transactions_transaction_id] FOREIGN KEY ([transaction_id]) REFERENCES [transactions] ([id]),
        CONSTRAINT [FK_after_service_cases_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_after_service_cases_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [work_completions] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [transaction_id] bigint NOT NULL,
        [status_code] varchar(30) NOT NULL DEFAULT 'DRAFT',
        [latest_revision_no] int NOT NULL DEFAULT 0,
        [first_submitted_at] datetime2(7) NULL,
        [confirmed_at] datetime2(7) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_work_completions] PRIMARY KEY ([id]),
        CONSTRAINT [CK_work_completions_status] CHECK ([status_code] IN ('DRAFT','SUBMITTED','REVISION_REQUESTED','SUPERSEDED','CONFIRMED','DISPUTED')),
        CONSTRAINT [FK_work_completions_transactions_transaction_id] FOREIGN KEY ([transaction_id]) REFERENCES [transactions] ([id]),
        CONSTRAINT [FK_work_completions_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_work_completions_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [after_service_actions] (
        [id] bigint NOT NULL IDENTITY,
        [after_service_case_id] bigint NOT NULL,
        [from_status_code] varchar(20) NULL,
        [to_status_code] varchar(20) NOT NULL,
        [action_note] nvarchar(2000) NULL,
        [occurred_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [actor_user_id] bigint NULL,
        [idempotency_key] varchar(100) NOT NULL,
        CONSTRAINT [PK_after_service_actions] PRIMARY KEY ([id]),
        CONSTRAINT [CK_after_service_actions_from_status] CHECK ([from_status_code] IS NULL OR [from_status_code] IN ('RECEIVED','IN_PROGRESS','COMPLETED')),
        CONSTRAINT [CK_after_service_actions_to_status] CHECK ([to_status_code] IN ('RECEIVED','IN_PROGRESS','COMPLETED')),
        CONSTRAINT [FK_after_service_actions_after_service_cases_after_service_case_id] FOREIGN KEY ([after_service_case_id]) REFERENCES [after_service_cases] ([id]),
        CONSTRAINT [FK_after_service_actions_users_actor_user_id] FOREIGN KEY ([actor_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [work_completion_revisions] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [work_completion_id] bigint NOT NULL,
        [revision_no] int NOT NULL,
        [status_code] varchar(30) NOT NULL DEFAULT 'SUBMITTED',
        [work_summary] nvarchar(max) NOT NULL,
        [checklist_json] nvarchar(max) NULL,
        [provider_attestation_at] datetime2(7) NOT NULL,
        [submitted_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [submitted_by_user_id] bigint NOT NULL,
        [revision_reason] nvarchar(1000) NULL,
        [idempotency_key] varchar(100) NOT NULL,
        CONSTRAINT [PK_work_completion_revisions] PRIMARY KEY ([id]),
        CONSTRAINT [CK_work_completion_revisions_checklist_json] CHECK ([checklist_json] IS NULL OR ISJSON([checklist_json]) = 1),
        CONSTRAINT [CK_work_completion_revisions_status] CHECK ([status_code] IN ('DRAFT','SUBMITTED','REVISION_REQUESTED','SUPERSEDED','CONFIRMED','DISPUTED')),
        CONSTRAINT [FK_work_completion_revisions_users_submitted_by_user_id] FOREIGN KEY ([submitted_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_work_completion_revisions_work_completions_work_completion_id] FOREIGN KEY ([work_completion_id]) REFERENCES [work_completions] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [after_service_files] (
        [id] bigint NOT NULL IDENTITY,
        [after_service_case_id] bigint NOT NULL,
        [after_service_action_id] bigint NULL,
        [file_id] bigint NOT NULL,
        [role_code] varchar(50) NULL,
        [description] nvarchar(500) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_after_service_files] PRIMARY KEY ([id]),
        CONSTRAINT [FK_after_service_files_after_service_actions_after_service_action_id] FOREIGN KEY ([after_service_action_id]) REFERENCES [after_service_actions] ([id]),
        CONSTRAINT [FK_after_service_files_after_service_cases_after_service_case_id] FOREIGN KEY ([after_service_case_id]) REFERENCES [after_service_cases] ([id]),
        CONSTRAINT [FK_after_service_files_files_file_id] FOREIGN KEY ([file_id]) REFERENCES [files] ([id]),
        CONSTRAINT [FK_after_service_files_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [completion_evidence_files] (
        [id] bigint NOT NULL IDENTITY,
        [completion_revision_id] bigint NOT NULL,
        [file_id] bigint NOT NULL,
        [photo_role_id] bigint NOT NULL,
        [display_order] int NOT NULL DEFAULT 0,
        [description] nvarchar(500) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_completion_evidence_files] PRIMARY KEY ([id]),
        CONSTRAINT [FK_completion_evidence_files_completion_photo_roles_photo_role_id] FOREIGN KEY ([photo_role_id]) REFERENCES [completion_photo_roles] ([id]),
        CONSTRAINT [FK_completion_evidence_files_files_file_id] FOREIGN KEY ([file_id]) REFERENCES [files] ([id]),
        CONSTRAINT [FK_completion_evidence_files_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_completion_evidence_files_work_completion_revisions_completion_revision_id] FOREIGN KEY ([completion_revision_id]) REFERENCES [work_completion_revisions] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [customer_confirmations] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [transaction_id] bigint NOT NULL,
        [completion_revision_id] bigint NOT NULL,
        [result_code] varchar(30) NOT NULL,
        [comment] nvarchar(2000) NULL,
        [confirmed_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [confirmed_by_user_id] bigint NOT NULL,
        [idempotency_key] varchar(100) NOT NULL,
        CONSTRAINT [PK_customer_confirmations] PRIMARY KEY ([id]),
        CONSTRAINT [CK_customer_confirmations_result] CHECK ([result_code] IN ('COMPLETED','REVISION_REQUESTED','DISPUTED')),
        CONSTRAINT [FK_customer_confirmations_transactions_transaction_id] FOREIGN KEY ([transaction_id]) REFERENCES [transactions] ([id]),
        CONSTRAINT [FK_customer_confirmations_users_confirmed_by_user_id] FOREIGN KEY ([confirmed_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_customer_confirmations_work_completion_revisions_completion_revision_id] FOREIGN KEY ([completion_revision_id]) REFERENCES [work_completion_revisions] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [service_history_entries] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [customer_profile_id] bigint NOT NULL,
        [transaction_id] bigint NULL,
        [source_completion_revision_id] bigint NULL,
        [after_service_case_id] bigint NULL,
        [event_type_code] varchar(40) NOT NULL,
        [title] nvarchar(200) NOT NULL,
        [summary] nvarchar(2000) NOT NULL,
        [provider_name_snapshot] nvarchar(200) NULL,
        [category_name_snapshot] nvarchar(500) NULL,
        [total_amount_snapshot] decimal(19,4) NULL,
        [currency_code] char(3) NULL,
        [completed_at_snapshot] datetime2(7) NULL,
        [warranty_start_date] date NULL,
        [warranty_end_date] date NULL,
        [snapshot_json] nvarchar(max) NOT NULL,
        [occurred_at] datetime2(7) NOT NULL,
        [idempotency_key] varchar(120) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_service_history_entries] PRIMARY KEY ([id]),
        CONSTRAINT [CK_service_history_entries_event_type] CHECK ([event_type_code] IN ('COMPLETION','AFTER_SERVICE_RECEIVED','AFTER_SERVICE_STARTED','AFTER_SERVICE_COMPLETED','ASSET_LINKED','ASSET_CORRECTED')),
        CONSTRAINT [CK_service_history_entries_snapshot_json] CHECK (ISJSON([snapshot_json]) = 1),
        CONSTRAINT [FK_service_history_entries_after_service_cases_after_service_case_id] FOREIGN KEY ([after_service_case_id]) REFERENCES [after_service_cases] ([id]),
        CONSTRAINT [FK_service_history_entries_customer_profiles_customer_profile_id] FOREIGN KEY ([customer_profile_id]) REFERENCES [customer_profiles] ([id]),
        CONSTRAINT [FK_service_history_entries_transactions_transaction_id] FOREIGN KEY ([transaction_id]) REFERENCES [transactions] ([id]),
        CONSTRAINT [FK_service_history_entries_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_service_history_entries_work_completion_revisions_source_completion_revision_id] FOREIGN KEY ([source_completion_revision_id]) REFERENCES [work_completion_revisions] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [service_history_items] (
        [id] bigint NOT NULL IDENTITY,
        [service_history_entry_id] bigint NOT NULL,
        [line_no] int NOT NULL,
        [item_name] nvarchar(200) NOT NULL,
        [description] nvarchar(1000) NULL,
        [quantity] decimal(19,4) NULL,
        [unit_text] nvarchar(50) NULL,
        [amount] decimal(19,4) NULL,
        [currency_code] char(3) NULL,
        CONSTRAINT [PK_service_history_items] PRIMARY KEY ([id]),
        CONSTRAINT [FK_service_history_items_service_history_entries_service_history_entry_id] FOREIGN KEY ([service_history_entry_id]) REFERENCES [service_history_entries] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE TABLE [transaction_asset_links] (
        [id] bigint NOT NULL IDENTITY,
        [transaction_id] bigint NOT NULL,
        [service_asset_id] bigint NOT NULL,
        [source_history_entry_id] bigint NULL,
        [status_code] varchar(20) NOT NULL DEFAULT 'ACTIVE',
        [correction_reason] nvarchar(1000) NULL,
        [linked_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [linked_by_user_id] bigint NULL,
        [corrected_at] datetime2(7) NULL,
        [corrected_by_user_id] bigint NULL,
        CONSTRAINT [PK_transaction_asset_links] PRIMARY KEY ([id]),
        CONSTRAINT [CK_transaction_asset_links_status] CHECK ([status_code] IN ('ACTIVE','CORRECTED')),
        CONSTRAINT [FK_transaction_asset_links_service_assets_service_asset_id] FOREIGN KEY ([service_asset_id]) REFERENCES [service_assets] ([id]),
        CONSTRAINT [FK_transaction_asset_links_service_history_entries_source_history_entry_id] FOREIGN KEY ([source_history_entry_id]) REFERENCES [service_history_entries] ([id]),
        CONSTRAINT [FK_transaction_asset_links_transactions_transaction_id] FOREIGN KEY ([transaction_id]) REFERENCES [transactions] ([id]),
        CONSTRAINT [FK_transaction_asset_links_users_corrected_by_user_id] FOREIGN KEY ([corrected_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_transaction_asset_links_users_linked_by_user_id] FOREIGN KEY ([linked_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_administrative_areas_abolition_type_code] ON [administrative_areas] ([abolition_type_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_administrative_areas_area_code] ON [administrative_areas] ([area_code]) WHERE [is_active] = CAST(1 AS bit)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_administrative_areas_area_code_effective_from] ON [administrative_areas] ([area_code], [effective_from]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_administrative_areas_area_level_code] ON [administrative_areas] ([area_level_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_administrative_areas_area_name] ON [administrative_areas] ([area_name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_administrative_areas_created_by_user_id] ON [administrative_areas] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_administrative_areas_effective_from] ON [administrative_areas] ([effective_from]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_administrative_areas_effective_to] ON [administrative_areas] ([effective_to]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_administrative_areas_is_active] ON [administrative_areas] ([is_active]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_administrative_areas_parent_area_id] ON [administrative_areas] ([parent_area_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_administrative_areas_parent_area_id_area_level_code_is_active_area_name] ON [administrative_areas] ([parent_area_id], [area_level_code], [is_active], [area_name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_administrative_areas_public_id] ON [administrative_areas] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_administrative_areas_source_parent_area_code] ON [administrative_areas] ([source_parent_area_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_administrative_areas_source_system_code] ON [administrative_areas] ([source_system_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_administrative_areas_updated_by_user_id] ON [administrative_areas] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_after_service_actions_actor_user_id] ON [after_service_actions] ([actor_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_after_service_actions_after_service_case_id] ON [after_service_actions] ([after_service_case_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_after_service_actions_after_service_case_id_occurred_at] ON [after_service_actions] ([after_service_case_id], [occurred_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_after_service_actions_idempotency_key] ON [after_service_actions] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_after_service_actions_occurred_at] ON [after_service_actions] ([occurred_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_after_service_actions_to_status_code] ON [after_service_actions] ([to_status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_after_service_cases_completed_at] ON [after_service_cases] ([completed_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_after_service_cases_created_by_user_id] ON [after_service_cases] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_after_service_cases_customer_profile_id] ON [after_service_cases] ([customer_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_after_service_cases_idempotency_key] ON [after_service_cases] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_after_service_cases_provider_profile_id] ON [after_service_cases] ([provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_after_service_cases_provider_profile_id_status_code_received_at] ON [after_service_cases] ([provider_profile_id], [status_code], [received_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_after_service_cases_public_id] ON [after_service_cases] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_after_service_cases_received_at] ON [after_service_cases] ([received_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_after_service_cases_status_code] ON [after_service_cases] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_after_service_cases_transaction_id] ON [after_service_cases] ([transaction_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_after_service_cases_transaction_id_received_at] ON [after_service_cases] ([transaction_id], [received_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_after_service_cases_updated_by_user_id] ON [after_service_cases] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_after_service_files_after_service_action_id] ON [after_service_files] ([after_service_action_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_after_service_files_after_service_case_id] ON [after_service_files] ([after_service_case_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_after_service_files_after_service_case_id_file_id] ON [after_service_files] ([after_service_case_id], [file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_after_service_files_created_by_user_id] ON [after_service_files] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_after_service_files_file_id] ON [after_service_files] ([file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_after_service_files_role_code] ON [after_service_files] ([role_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_audit_logs_action_code] ON [audit_logs] ([action_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_audit_logs_actor_role_code] ON [audit_logs] ([actor_role_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_audit_logs_actor_user_id] ON [audit_logs] ([actor_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_audit_logs_actor_user_id_occurred_at] ON [audit_logs] ([actor_user_id], [occurred_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_audit_logs_correlation_id] ON [audit_logs] ([correlation_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_audit_logs_entity_public_id] ON [audit_logs] ([entity_public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_audit_logs_entity_type] ON [audit_logs] ([entity_type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_audit_logs_entity_type_entity_public_id_occurred_at] ON [audit_logs] ([entity_type], [entity_public_id], [occurred_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_audit_logs_occurred_at] ON [audit_logs] ([occurred_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_audit_logs_result_code] ON [audit_logs] ([result_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_completion_photo_requirements_category_policy_id] ON [category_completion_photo_requirements] ([category_policy_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_category_completion_photo_requirements_category_policy_id_photo_role_id] ON [category_completion_photo_requirements] ([category_policy_id], [photo_role_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_completion_photo_requirements_created_by_user_id] ON [category_completion_photo_requirements] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_completion_photo_requirements_display_order] ON [category_completion_photo_requirements] ([display_order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_completion_photo_requirements_photo_role_id] ON [category_completion_photo_requirements] ([photo_role_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_completion_photo_requirements_updated_by_user_id] ON [category_completion_photo_requirements] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_field_assignments_created_by_user_id] ON [category_field_assignments] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_field_assignments_field_definition_id] ON [category_field_assignments] ([field_definition_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_category_field_assignments_field_definition_id_target_category_id] ON [category_field_assignments] ([field_definition_id], [target_category_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_field_assignments_is_active] ON [category_field_assignments] ([is_active]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_field_assignments_target_category_id] ON [category_field_assignments] ([target_category_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_field_assignments_updated_by_user_id] ON [category_field_assignments] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_field_definitions_created_by_user_id] ON [category_field_definitions] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_field_definitions_field_type_code] ON [category_field_definitions] ([field_type_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_field_definitions_is_required] ON [category_field_definitions] ([is_required]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_field_definitions_owner_middle_category_id] ON [category_field_definitions] ([owner_middle_category_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_category_field_definitions_owner_middle_category_id_field_key] ON [category_field_definitions] ([owner_middle_category_id], [field_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_field_definitions_owner_middle_category_id_status_code_display_order] ON [category_field_definitions] ([owner_middle_category_id], [status_code], [display_order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_category_field_definitions_public_id] ON [category_field_definitions] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_category_field_definitions_source_field_id] ON [category_field_definitions] ([source_field_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_field_definitions_updated_by_user_id] ON [category_field_definitions] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_policies_category_id] ON [category_policies] ([category_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_policies_category_id_effective_from] ON [category_policies] ([category_id], [effective_from] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_category_policies_category_id_policy_version] ON [category_policies] ([category_id], [policy_version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_policies_created_by_user_id] ON [category_policies] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_policies_effective_from] ON [category_policies] ([effective_from]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_policies_effective_to] ON [category_policies] ([effective_to]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_policies_fee_policy_id] ON [category_policies] ([fee_policy_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_policies_is_emergency_allowed] ON [category_policies] ([is_emergency_allowed]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_category_policies_public_id] ON [category_policies] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_policies_safety_grade_code] ON [category_policies] ([safety_grade_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_policies_transaction_type_code] ON [category_policies] ([transaction_type_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_category_policies_updated_by_user_id] ON [category_policies] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_completion_evidence_files_completion_revision_id] ON [completion_evidence_files] ([completion_revision_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_completion_evidence_files_completion_revision_id_file_id] ON [completion_evidence_files] ([completion_revision_id], [file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_completion_evidence_files_completion_revision_id_photo_role_id] ON [completion_evidence_files] ([completion_revision_id], [photo_role_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_completion_evidence_files_created_by_user_id] ON [completion_evidence_files] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_completion_evidence_files_file_id] ON [completion_evidence_files] ([file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_completion_evidence_files_photo_role_id] ON [completion_evidence_files] ([photo_role_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_completion_photo_roles_code] ON [completion_photo_roles] ([code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_completion_photo_roles_created_by_user_id] ON [completion_photo_roles] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_completion_photo_roles_is_active] ON [completion_photo_roles] ([is_active]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_completion_photo_roles_updated_by_user_id] ON [completion_photo_roles] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_customer_confirmations_completion_revision_id] ON [customer_confirmations] ([completion_revision_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_customer_confirmations_confirmed_at] ON [customer_confirmations] ([confirmed_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_customer_confirmations_confirmed_by_user_id] ON [customer_confirmations] ([confirmed_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_customer_confirmations_idempotency_key] ON [customer_confirmations] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_customer_confirmations_public_id] ON [customer_confirmations] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_customer_confirmations_result_code] ON [customer_confirmations] ([result_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_customer_confirmations_transaction_id] ON [customer_confirmations] ([transaction_id]) WHERE [result_code] = ''COMPLETED''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_customer_confirmations_transaction_id_confirmed_at] ON [customer_confirmations] ([transaction_id], [confirmed_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_customer_profiles_created_by_user_id] ON [customer_profiles] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_customer_profiles_public_id] ON [customer_profiles] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_customer_profiles_updated_by_user_id] ON [customer_profiles] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_customer_profiles_user_id] ON [customer_profiles] ([user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_dispatch_candidates_evaluated_at] ON [dispatch_candidates] ([evaluated_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_dispatch_candidates_expires_at] ON [dispatch_candidates] ([expires_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_dispatch_candidates_provider_profile_id] ON [dispatch_candidates] ([provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_dispatch_candidates_reason_code] ON [dispatch_candidates] ([reason_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_dispatch_candidates_service_request_id] ON [dispatch_candidates] ([service_request_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_dispatch_candidates_service_request_id_provider_profile_id] ON [dispatch_candidates] ([service_request_id], [provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_dispatch_candidates_service_request_id_status_code] ON [dispatch_candidates] ([service_request_id], [status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_dispatch_candidates_status_code] ON [dispatch_candidates] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_fee_policies_code] ON [fee_policies] ([code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_fee_policies_created_by_user_id] ON [fee_policies] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_fee_policies_effective_from] ON [fee_policies] ([effective_from]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_fee_policies_effective_to] ON [fee_policies] ([effective_to]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_fee_policies_is_active] ON [fee_policies] ([is_active]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_fee_policies_max_base_amount] ON [fee_policies] ([max_base_amount]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_fee_policies_min_base_amount] ON [fee_policies] ([min_base_amount]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_fee_policies_policy_kind_code] ON [fee_policies] ([policy_kind_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_fee_policies_public_id] ON [fee_policies] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_fee_policies_transaction_type_code] ON [fee_policies] ([transaction_type_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_fee_policies_updated_by_user_id] ON [fee_policies] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_files_content_type] ON [files] ([content_type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_files_created_at] ON [files] ([created_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_files_public_id] ON [files] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_files_purpose_code] ON [files] ([purpose_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_files_sha256_hex] ON [files] ([sha256_hex]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_files_status_code] ON [files] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_files_storage_key_hash] ON [files] ([storage_key_hash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_files_uploaded_by_user_id] ON [files] ([uploaded_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_notification_deliveries_attempted_at] ON [notification_deliveries] ([attempted_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_notification_deliveries_channel_code] ON [notification_deliveries] ([channel_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_notification_deliveries_notification_id] ON [notification_deliveries] ([notification_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_notification_deliveries_notification_id_channel_code_attempt_no] ON [notification_deliveries] ([notification_id], [channel_code], [attempt_no]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_notification_deliveries_provider_message_id] ON [notification_deliveries] ([provider_message_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_notification_deliveries_status_code] ON [notification_deliveries] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_notifications_created_by_user_id] ON [notifications] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_notifications_idempotency_key] ON [notifications] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_notifications_is_urgent] ON [notifications] ([is_urgent]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_notifications_public_id] ON [notifications] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_notifications_read_at] ON [notifications] ([read_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_notifications_recipient_user_id] ON [notifications] ([recipient_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_notifications_recipient_user_id_read_at_recorded_at] ON [notifications] ([recipient_user_id], [read_at], [recorded_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_notifications_recorded_at] ON [notifications] ([recorded_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_notifications_request_dispatch_id] ON [notifications] ([request_dispatch_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_notifications_status_code] ON [notifications] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_notifications_type_code] ON [notifications] ([type_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_outbox_events_aggregate_public_id] ON [outbox_events] ([aggregate_public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_outbox_events_aggregate_type] ON [outbox_events] ([aggregate_type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_outbox_events_available_at] ON [outbox_events] ([available_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_outbox_events_created_by_user_id] ON [outbox_events] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_outbox_events_event_type] ON [outbox_events] ([event_type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_outbox_events_idempotency_key] ON [outbox_events] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_outbox_events_occurred_at] ON [outbox_events] ([occurred_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_outbox_events_processed_at] ON [outbox_events] ([processed_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_outbox_events_public_id] ON [outbox_events] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_outbox_events_status_code] ON [outbox_events] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_outbox_events_status_code_available_at_id] ON [outbox_events] ([status_code], [available_at], [id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_approval_events_correlation_id] ON [provider_approval_events] ([correlation_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_approval_events_decided_by_user_id] ON [provider_approval_events] ([decided_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_approval_events_provider_profile_id] ON [provider_approval_events] ([provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_approval_events_provider_profile_id_decided_at] ON [provider_approval_events] ([provider_profile_id], [decided_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_documents_created_by_user_id] ON [provider_documents] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_documents_document_type_code] ON [provider_documents] ([document_type_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_documents_expires_at] ON [provider_documents] ([expires_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_documents_file_id] ON [provider_documents] ([file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_documents_provider_profile_id] ON [provider_documents] ([provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_documents_updated_by_user_id] ON [provider_documents] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_documents_verification_status_code] ON [provider_documents] ([verification_status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_documents_verified_by_user_id] ON [provider_documents] ([verified_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_profiles_activity_status_code] ON [provider_profiles] ([activity_status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_profiles_approval_decided_by_user_id] ON [provider_profiles] ([approval_decided_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_profiles_approval_status_code] ON [provider_profiles] ([approval_status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_profiles_approval_status_code_activity_status_code_trust_score] ON [provider_profiles] ([approval_status_code], [activity_status_code], [trust_score] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_profiles_business_name] ON [provider_profiles] ([business_name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_provider_profiles_business_registration_no] ON [provider_profiles] ([business_registration_no]) WHERE [business_registration_no] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_profiles_created_by_user_id] ON [provider_profiles] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_profiles_public_id] ON [provider_profiles] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_profiles_updated_by_user_id] ON [provider_profiles] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_profiles_user_id] ON [provider_profiles] ([user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_service_areas_administrative_area_id] ON [provider_service_areas] ([administrative_area_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_service_areas_administrative_area_id_status_code_provider_service_category_id] ON [provider_service_areas] ([administrative_area_id], [status_code], [provider_service_category_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_service_areas_created_by_user_id] ON [provider_service_areas] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_service_areas_provider_service_category_id] ON [provider_service_areas] ([provider_service_category_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_service_areas_provider_service_category_id_administrative_area_id] ON [provider_service_areas] ([provider_service_category_id], [administrative_area_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_service_areas_status_code] ON [provider_service_areas] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_service_areas_updated_by_user_id] ON [provider_service_areas] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_service_categories_category_id] ON [provider_service_categories] ([category_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_service_categories_category_id_status_code_provider_profile_id] ON [provider_service_categories] ([category_id], [status_code], [provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_service_categories_created_by_user_id] ON [provider_service_categories] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_service_categories_provider_profile_id] ON [provider_service_categories] ([provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_service_categories_provider_profile_id_category_id] ON [provider_service_categories] ([provider_profile_id], [category_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_service_categories_status_code] ON [provider_service_categories] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_provider_service_categories_updated_by_user_id] ON [provider_service_categories] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_qualification_policies_created_by_user_id] ON [qualification_policies] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_qualification_policies_effective_from] ON [qualification_policies] ([effective_from]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_qualification_policies_effective_to] ON [qualification_policies] ([effective_to]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_qualification_policies_middle_category_id] ON [qualification_policies] ([middle_category_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_qualification_policies_middle_category_id_source_policy_id] ON [qualification_policies] ([middle_category_id], [source_policy_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_qualification_policies_safety_grade_code] ON [qualification_policies] ([safety_grade_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_qualification_policies_source_policy_id] ON [qualification_policies] ([source_policy_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_qualification_policies_updated_by_user_id] ON [qualification_policies] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_quote_items_quote_revision_id] ON [quote_items] ([quote_revision_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_quote_items_quote_revision_id_line_no] ON [quote_items] ([quote_revision_id], [line_no]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_quote_revisions_idempotency_key] ON [quote_revisions] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_quote_revisions_public_id] ON [quote_revisions] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_quote_revisions_quote_id] ON [quote_revisions] ([quote_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_quote_revisions_quote_id_revision_no] ON [quote_revisions] ([quote_id], [revision_no] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_quote_revisions_quote_id_submitted_at] ON [quote_revisions] ([quote_id], [submitted_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_quote_revisions_submitted_at] ON [quote_revisions] ([submitted_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_quote_revisions_submitted_by_user_id] ON [quote_revisions] ([submitted_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_quote_revisions_total_amount] ON [quote_revisions] ([total_amount]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_quote_revisions_valid_until] ON [quote_revisions] ([valid_until]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_quotes_created_by_user_id] ON [quotes] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_quotes_expires_at] ON [quotes] ([expires_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_quotes_provider_profile_id] ON [quotes] ([provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_quotes_public_id] ON [quotes] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_quotes_request_dispatch_id] ON [quotes] ([request_dispatch_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_quotes_service_request_id] ON [quotes] ([service_request_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_quotes_service_request_id_provider_profile_id] ON [quotes] ([service_request_id], [provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_quotes_service_request_id_status_code_submitted_at] ON [quotes] ([service_request_id], [status_code], [submitted_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_quotes_status_code] ON [quotes] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_quotes_submitted_at] ON [quotes] ([submitted_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_quotes_updated_by_user_id] ON [quotes] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_request_answer_files_created_by_user_id] ON [request_answer_files] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_request_answer_files_file_id] ON [request_answer_files] ([file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_request_answer_files_request_answer_id] ON [request_answer_files] ([request_answer_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_request_answer_files_request_answer_id_file_id] ON [request_answer_files] ([request_answer_id], [file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_request_answers_created_by_user_id] ON [request_answers] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_request_answers_field_definition_id] ON [request_answers] ([field_definition_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_request_answers_service_request_id] ON [request_answers] ([service_request_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_request_answers_service_request_id_field_definition_id] ON [request_answers] ([service_request_id], [field_definition_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_request_answers_updated_by_user_id] ON [request_answers] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_request_dispatches_available_at] ON [request_dispatches] ([available_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_request_dispatches_candidate_id] ON [request_dispatches] ([candidate_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_request_dispatches_expires_at] ON [request_dispatches] ([expires_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_request_dispatches_idempotency_key] ON [request_dispatches] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_request_dispatches_provider_profile_id] ON [request_dispatches] ([provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_request_dispatches_provider_profile_id_status_code_available_at] ON [request_dispatches] ([provider_profile_id], [status_code], [available_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_request_dispatches_public_id] ON [request_dispatches] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_request_dispatches_service_request_id] ON [request_dispatches] ([service_request_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_request_dispatches_service_request_id_provider_profile_id] ON [request_dispatches] ([service_request_id], [provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_request_dispatches_status_code] ON [request_dispatches] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_roles_code] ON [roles] ([code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_roles_created_by_user_id] ON [roles] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_roles_is_active] ON [roles] ([is_active]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_roles_updated_by_user_id] ON [roles] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_assets_asset_type_code] ON [service_assets] ([asset_type_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_assets_created_by_user_id] ON [service_assets] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_assets_customer_profile_id] ON [service_assets] ([customer_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_assets_customer_profile_id_status_code_name] ON [service_assets] ([customer_profile_id], [status_code], [name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_service_assets_public_id] ON [service_assets] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_assets_status_code] ON [service_assets] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_assets_updated_by_user_id] ON [service_assets] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_categories_created_by_user_id] ON [service_categories] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_service_categories_external_code] ON [service_categories] ([external_code]) WHERE [external_code] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_categories_level_code] ON [service_categories] ([level_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_categories_parent_id] ON [service_categories] ([parent_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_service_categories_parent_id_name] ON [service_categories] ([parent_id], [name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_categories_parent_id_status_code_sort_order] ON [service_categories] ([parent_id], [status_code], [sort_order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_service_categories_public_id] ON [service_categories] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_service_categories_source_record_id] ON [service_categories] ([source_record_id]) WHERE [source_record_id] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_categories_updated_by_user_id] ON [service_categories] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_history_entries_after_service_case_id] ON [service_history_entries] ([after_service_case_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_history_entries_created_by_user_id] ON [service_history_entries] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_history_entries_customer_profile_id] ON [service_history_entries] ([customer_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_history_entries_customer_profile_id_occurred_at] ON [service_history_entries] ([customer_profile_id], [occurred_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_history_entries_event_type_code] ON [service_history_entries] ([event_type_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_service_history_entries_idempotency_key] ON [service_history_entries] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_history_entries_occurred_at] ON [service_history_entries] ([occurred_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_service_history_entries_public_id] ON [service_history_entries] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_history_entries_source_completion_revision_id] ON [service_history_entries] ([source_completion_revision_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_history_entries_transaction_id] ON [service_history_entries] ([transaction_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_history_entries_transaction_id_event_type_code] ON [service_history_entries] ([transaction_id], [event_type_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_history_entries_warranty_end_date] ON [service_history_entries] ([warranty_end_date]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_history_items_service_history_entry_id] ON [service_history_items] ([service_history_entry_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_service_history_items_service_history_entry_id_line_no] ON [service_history_items] ([service_history_entry_id], [line_no]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_requests_administrative_area_id] ON [service_requests] ([administrative_area_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_requests_category_id] ON [service_requests] ([category_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_requests_category_id_administrative_area_id_status_code_opened_at] ON [service_requests] ([category_id], [administrative_area_id], [status_code], [opened_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_requests_category_policy_id] ON [service_requests] ([category_policy_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_requests_created_at] ON [service_requests] ([created_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_requests_created_by_user_id] ON [service_requests] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_requests_customer_profile_id] ON [service_requests] ([customer_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_requests_customer_profile_id_created_at] ON [service_requests] ([customer_profile_id], [created_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_requests_expires_at] ON [service_requests] ([expires_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_service_requests_idempotency_key] ON [service_requests] ([idempotency_key]) WHERE [idempotency_key] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_requests_is_urgent] ON [service_requests] ([is_urgent]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_requests_opened_at] ON [service_requests] ([opened_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_service_requests_public_id] ON [service_requests] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_requests_status_code] ON [service_requests] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_service_requests_updated_by_user_id] ON [service_requests] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_transaction_asset_links_corrected_by_user_id] ON [transaction_asset_links] ([corrected_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_transaction_asset_links_linked_at] ON [transaction_asset_links] ([linked_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_transaction_asset_links_linked_by_user_id] ON [transaction_asset_links] ([linked_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_transaction_asset_links_service_asset_id] ON [transaction_asset_links] ([service_asset_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_transaction_asset_links_service_asset_id_linked_at] ON [transaction_asset_links] ([service_asset_id], [linked_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_transaction_asset_links_source_history_entry_id] ON [transaction_asset_links] ([source_history_entry_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_transaction_asset_links_status_code] ON [transaction_asset_links] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_transaction_asset_links_transaction_id] ON [transaction_asset_links] ([transaction_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_transaction_asset_links_transaction_id_service_asset_id] ON [transaction_asset_links] ([transaction_id], [service_asset_id]) WHERE [status_code] = ''ACTIVE''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_transaction_asset_links_transaction_id_status_code] ON [transaction_asset_links] ([transaction_id], [status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_transactions_accepted_quote_revision_id] ON [transactions] ([accepted_quote_revision_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_transactions_category_id] ON [transactions] ([category_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_transactions_completed_at] ON [transactions] ([completed_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_transactions_created_at] ON [transactions] ([created_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_transactions_created_by_user_id] ON [transactions] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_transactions_customer_profile_id] ON [transactions] ([customer_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_transactions_customer_profile_id_created_at] ON [transactions] ([customer_profile_id], [created_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_transactions_provider_profile_id] ON [transactions] ([provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_transactions_provider_profile_id_status_code_created_at] ON [transactions] ([provider_profile_id], [status_code], [created_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_transactions_public_id] ON [transactions] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_transactions_service_request_id] ON [transactions] ([service_request_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_transactions_status_code] ON [transactions] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_transactions_updated_by_user_id] ON [transactions] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_user_roles_granted_by_user_id] ON [user_roles] ([granted_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_user_roles_revoked_at] ON [user_roles] ([revoked_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_user_roles_revoked_by_user_id] ON [user_roles] ([revoked_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_user_roles_role_id] ON [user_roles] ([role_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_user_roles_user_id] ON [user_roles] ([user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_user_roles_user_id_role_id] ON [user_roles] ([user_id], [role_id]) WHERE [revoked_at] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_users_created_at] ON [users] ([created_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_users_created_by_user_id] ON [users] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_users_email] ON [users] ([email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_users_normalized_login_id] ON [users] ([normalized_login_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_users_phone] ON [users] ([phone]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_users_public_id] ON [users] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_users_status_code] ON [users] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_users_updated_by_user_id] ON [users] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_work_completion_revisions_idempotency_key] ON [work_completion_revisions] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_work_completion_revisions_public_id] ON [work_completion_revisions] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_work_completion_revisions_status_code] ON [work_completion_revisions] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_work_completion_revisions_submitted_at] ON [work_completion_revisions] ([submitted_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_work_completion_revisions_submitted_by_user_id] ON [work_completion_revisions] ([submitted_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_work_completion_revisions_work_completion_id] ON [work_completion_revisions] ([work_completion_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_work_completion_revisions_work_completion_id_revision_no] ON [work_completion_revisions] ([work_completion_id], [revision_no] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_work_completion_revisions_work_completion_id_submitted_at] ON [work_completion_revisions] ([work_completion_id], [submitted_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_work_completions_confirmed_at] ON [work_completions] ([confirmed_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_work_completions_created_by_user_id] ON [work_completions] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_work_completions_public_id] ON [work_completions] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_work_completions_status_code] ON [work_completions] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_work_completions_transaction_id] ON [work_completions] ([transaction_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_work_completions_updated_by_user_id] ON [work_completions] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809073803_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260809073803_InitialCreate', N'10.0.10');
END;

COMMIT;
GO

