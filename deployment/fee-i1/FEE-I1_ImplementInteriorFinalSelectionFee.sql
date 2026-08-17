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

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809121223_AddSourceFieldIdentity'
)
BEGIN
    DROP INDEX [IX_category_field_definitions_owner_middle_category_id_field_key] ON [category_field_definitions];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809121223_AddSourceFieldIdentity'
)
BEGIN
    CREATE INDEX [IX_category_field_definitions_owner_middle_category_id_field_key] ON [category_field_definitions] ([owner_middle_category_id], [field_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260809121223_AddSourceFieldIdentity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260809121223_AddSourceFieldIdentity', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810044919_AddServiceRequestFieldManagement'
)
BEGIN
    ALTER TABLE [category_field_definitions] ADD [unit_text] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810044919_AddServiceRequestFieldManagement'
)
BEGIN
    ALTER TABLE [category_field_assignments] ADD [display_order] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810044919_AddServiceRequestFieldManagement'
)
BEGIN
    ALTER TABLE [category_field_assignments] ADD [is_required] bit NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810044919_AddServiceRequestFieldManagement'
)
BEGIN
    CREATE TABLE [category_field_options] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [field_definition_id] bigint NOT NULL,
        [value] nvarchar(450) NOT NULL,
        [label] nvarchar(1000) NOT NULL,
        [display_order] int NOT NULL DEFAULT 0,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_category_field_options] PRIMARY KEY ([id]),
        CONSTRAINT [CK_category_field_options_display_order] CHECK ([display_order] >= 0),
        CONSTRAINT [FK_category_field_options_category_field_definitions_field_definition_id] FOREIGN KEY ([field_definition_id]) REFERENCES [category_field_definitions] ([id]),
        CONSTRAINT [FK_category_field_options_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_category_field_options_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810044919_AddServiceRequestFieldManagement'
)
BEGIN
    IF EXISTS (
        SELECT source_field_id FROM category_field_definitions
        GROUP BY source_field_id HAVING COUNT(*) > 1)
        THROW 51000, 'Duplicate source_field_id prevents request-field migration.', 1;

    IF EXISTS (
        SELECT field_definition_id, target_category_id FROM category_field_assignments
        GROUP BY field_definition_id, target_category_id HAVING COUNT(*) > 1)
        THROW 51000, 'Duplicate field assignment prevents request-field migration.', 1;

    IF EXISTS (
        SELECT 1 FROM category_field_assignments a
        LEFT JOIN category_field_definitions f ON f.id = a.field_definition_id
        LEFT JOIN service_categories c ON c.id = a.target_category_id
        WHERE f.id IS NULL OR c.id IS NULL)
        THROW 51000, 'Invalid assignment foreign key prevents request-field migration.', 1;

    CREATE TABLE #parsed_options (
        field_definition_id bigint NOT NULL,
        option_value nvarchar(2000) NOT NULL,
        display_order int NOT NULL
    );

    ;WITH option_parts AS (
        SELECT id AS field_definition_id,
               CAST(options_or_unit_text AS nvarchar(2000)) AS raw_value,
               1 AS display_order,
               1 AS start_position,
               CHARINDEX('/', options_or_unit_text) AS slash_position
        FROM category_field_definitions
        WHERE field_type_code = 'SELECT' AND NULLIF(LTRIM(RTRIM(options_or_unit_text)), '') IS NOT NULL
        UNION ALL
        SELECT field_definition_id, raw_value, display_order + 1, slash_position + 1,
               CHARINDEX('/', raw_value, slash_position + 1)
        FROM option_parts
        WHERE slash_position > 0
    )
    INSERT INTO #parsed_options (field_definition_id, option_value, display_order)
    SELECT field_definition_id,
           LTRIM(RTRIM(SUBSTRING(raw_value, start_position,
               CASE WHEN slash_position = 0 THEN LEN(raw_value) + 1 ELSE slash_position END - start_position))),
           display_order
    FROM option_parts
    OPTION (MAXRECURSION 0);

    IF EXISTS (SELECT 1 FROM #parsed_options WHERE option_value = '')
        THROW 51000, 'Empty SELECT option prevents request-field migration.', 1;
    IF EXISTS (SELECT 1 FROM #parsed_options WHERE LEN(option_value) > 450)
        THROW 51000, 'SELECT option longer than 450 characters prevents request-field migration.', 1;
    IF EXISTS (
        SELECT field_definition_id, option_value FROM #parsed_options
        GROUP BY field_definition_id, option_value HAVING COUNT(*) > 1)
        THROW 51000, 'Duplicate SELECT option prevents request-field migration.', 1;

    UPDATE a
    SET a.is_required = f.is_required,
        a.display_order = f.display_order
    FROM category_field_assignments a
    INNER JOIN category_field_definitions f ON f.id = a.field_definition_id;

    UPDATE category_field_definitions
    SET unit_text = options_or_unit_text
    WHERE field_type_code <> 'SELECT';

    INSERT INTO category_field_options
        (public_id, field_definition_id, value, label, display_order, is_active, created_at, updated_at)
    SELECT NEWID(), field_definition_id, option_value, option_value, display_order, 1,
           SYSUTCDATETIME(), SYSUTCDATETIME()
    FROM #parsed_options;

    IF EXISTS (SELECT 1 FROM category_field_assignments WHERE is_required IS NULL OR display_order IS NULL)
        THROW 51000, 'Assignment backfill validation failed.', 1;
    IF (SELECT COUNT(*) FROM category_field_options) <> (SELECT COUNT(*) FROM #parsed_options)
        THROW 51000, 'SELECT option conversion validation failed.', 1;
    IF (SELECT COUNT(*) FROM category_field_definitions
        WHERE source_field_id IN (
            'FLD-00046','FLD-00057','FLD-00088','FLD-00119','FLD-00190','FLD-00201',
            'FLD-00212','FLD-00223','FLD-00234','FLD-00245','FLD-00256','FLD-00267',
            'FLD-00278','FLD-00289','FLD-00300','FLD-00311','FLD-00362','FLD-00664')
        AND field_type_code = 'TEXT'
        AND NULLIF(LTRIM(RTRIM(options_or_unit_text)), '') IS NULL) <> 18
        THROW 51000, 'The 18 approved empty-option exceptions were not preserved.', 1;

    DROP TABLE #parsed_options;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810044919_AddServiceRequestFieldManagement'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[category_field_assignments]') AND [c].[name] = N'display_order');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [category_field_assignments] DROP CONSTRAINT ' + @var + ';');
    EXEC(N'UPDATE [category_field_assignments] SET [display_order] = 0 WHERE [display_order] IS NULL');
    ALTER TABLE [category_field_assignments] ALTER COLUMN [display_order] int NOT NULL;
    ALTER TABLE [category_field_assignments] ADD DEFAULT 0 FOR [display_order];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810044919_AddServiceRequestFieldManagement'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[category_field_assignments]') AND [c].[name] = N'is_required');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [category_field_assignments] DROP CONSTRAINT ' + @var1 + ';');
    EXEC(N'UPDATE [category_field_assignments] SET [is_required] = CAST(0 AS bit) WHERE [is_required] IS NULL');
    ALTER TABLE [category_field_assignments] ALTER COLUMN [is_required] bit NOT NULL;
    ALTER TABLE [category_field_assignments] ADD DEFAULT CAST(0 AS bit) FOR [is_required];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810044919_AddServiceRequestFieldManagement'
)
BEGIN
    CREATE INDEX [IX_category_field_assignments_target_category_id_is_active_display_order] ON [category_field_assignments] ([target_category_id], [is_active], [display_order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810044919_AddServiceRequestFieldManagement'
)
BEGIN
    EXEC(N'ALTER TABLE [category_field_assignments] ADD CONSTRAINT [CK_category_field_assignments_display_order] CHECK ([display_order] >= 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810044919_AddServiceRequestFieldManagement'
)
BEGIN
    CREATE INDEX [IX_category_field_options_created_by_user_id] ON [category_field_options] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810044919_AddServiceRequestFieldManagement'
)
BEGIN
    CREATE INDEX [IX_category_field_options_field_definition_id_display_order] ON [category_field_options] ([field_definition_id], [display_order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810044919_AddServiceRequestFieldManagement'
)
BEGIN
    CREATE INDEX [IX_category_field_options_field_definition_id_is_active_display_order] ON [category_field_options] ([field_definition_id], [is_active], [display_order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810044919_AddServiceRequestFieldManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_category_field_options_field_definition_id_value] ON [category_field_options] ([field_definition_id], [value]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810044919_AddServiceRequestFieldManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_category_field_options_public_id] ON [category_field_options] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810044919_AddServiceRequestFieldManagement'
)
BEGIN
    CREATE INDEX [IX_category_field_options_updated_by_user_id] ON [category_field_options] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810044919_AddServiceRequestFieldManagement'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810044919_AddServiceRequestFieldManagement', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE TABLE [category_fee_policies] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [category_id] bigint NOT NULL,
        [legacy_category_policy_id] bigint NULL,
        [source_fee_policy_id] bigint NULL,
        [policy_version] varchar(30) NOT NULL,
        [policy_kind_code] varchar(20) NOT NULL,
        [transaction_type_code] varchar(20) NOT NULL,
        [calculation_method_text] nvarchar(100) NULL,
        [fee_amount] decimal(19,4) NULL,
        [min_base_amount] decimal(19,4) NULL,
        [max_base_amount] decimal(19,4) NULL,
        [rate] decimal(9,6) NULL,
        [monthly_amount] decimal(19,4) NULL,
        [per_visit_amount] decimal(19,4) NULL,
        [currency_code] char(3) NOT NULL DEFAULT 'KRW',
        [charge_timing_text] nvarchar(300) NOT NULL,
        [restore_rule_text] nvarchar(1000) NULL,
        [effective_from] date NOT NULL,
        [effective_to] date NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_category_fee_policies] PRIMARY KEY ([id]),
        CONSTRAINT [CK_category_fee_policies_amounts] CHECK (([fee_amount] IS NULL OR [fee_amount] >= 0) AND ([min_base_amount] IS NULL OR [min_base_amount] >= 0) AND ([max_base_amount] IS NULL OR [max_base_amount] >= 0) AND ([monthly_amount] IS NULL OR [monthly_amount] >= 0) AND ([per_visit_amount] IS NULL OR [per_visit_amount] >= 0)),
        CONSTRAINT [CK_category_fee_policies_base_range] CHECK ([min_base_amount] IS NULL OR [max_base_amount] IS NULL OR [min_base_amount] <= [max_base_amount]),
        CONSTRAINT [CK_category_fee_policies_period] CHECK ([effective_to] IS NULL OR [effective_to] > [effective_from]),
        CONSTRAINT [CK_category_fee_policies_rate] CHECK ([rate] IS NULL OR ([rate] >= 0 AND [rate] <= 1)),
        CONSTRAINT [FK_category_fee_policies_category_policies_legacy_category_policy_id] FOREIGN KEY ([legacy_category_policy_id]) REFERENCES [category_policies] ([id]),
        CONSTRAINT [FK_category_fee_policies_fee_policies_source_fee_policy_id] FOREIGN KEY ([source_fee_policy_id]) REFERENCES [fee_policies] ([id]),
        CONSTRAINT [FK_category_fee_policies_service_categories_category_id] FOREIGN KEY ([category_id]) REFERENCES [service_categories] ([id]),
        CONSTRAINT [FK_category_fee_policies_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_category_fee_policies_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE TABLE [category_operation_policies] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [category_id] bigint NOT NULL,
        [legacy_category_policy_id] bigint NULL,
        [policy_version] varchar(30) NOT NULL,
        [request_method_text] nvarchar(300) NOT NULL,
        [onsite_requirement_text] nvarchar(30) NOT NULL,
        [is_emergency_allowed] bit NOT NULL DEFAULT CAST(0 AS bit),
        [subscription_option_text] nvarchar(30) NOT NULL,
        [max_quote_count] smallint NOT NULL,
        [quote_validity_minutes] int NOT NULL,
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
        [service_area_level_code] varchar(20) NOT NULL,
        [reference_url] nvarchar(2048) NOT NULL,
        [admin_note] nvarchar(2000) NOT NULL,
        [effective_from] date NOT NULL,
        [effective_to] date NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_category_operation_policies] PRIMARY KEY ([id]),
        CONSTRAINT [CK_category_operation_policies_counts] CHECK ([max_quote_count] > 0 AND [quote_validity_minutes] > 0 AND [provider_response_deadline_minutes] > 0 AND [required_completion_photo_count] >= 0 AND [default_warranty_days] >= 0),
        CONSTRAINT [CK_category_operation_policies_period] CHECK ([effective_to] IS NULL OR [effective_to] > [effective_from]),
        CONSTRAINT [FK_category_operation_policies_category_policies_legacy_category_policy_id] FOREIGN KEY ([legacy_category_policy_id]) REFERENCES [category_policies] ([id]),
        CONSTRAINT [FK_category_operation_policies_service_categories_category_id] FOREIGN KEY ([category_id]) REFERENCES [service_categories] ([id]),
        CONSTRAINT [FK_category_operation_policies_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_category_operation_policies_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE TABLE [category_price_policies] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [category_id] bigint NOT NULL,
        [legacy_category_policy_id] bigint NULL,
        [policy_version] varchar(30) NOT NULL,
        [legacy_price_method_text] nvarchar(30) NOT NULL,
        [price_type_code] varchar(30) NULL,
        [base_amount] decimal(19,4) NOT NULL,
        [minimum_budget_amount] decimal(19,4) NULL,
        [recommended_min_amount] decimal(19,4) NULL,
        [recommended_max_amount] decimal(19,4) NULL,
        [unit_text] nvarchar(100) NULL,
        [unit_price_amount] decimal(19,4) NULL,
        [minimum_charge_amount] decimal(19,4) NULL,
        [currency_code] char(3) NOT NULL DEFAULT 'KRW',
        [legacy_vat_display_rule_text] nvarchar(100) NOT NULL,
        [vat_policy_code] varchar(20) NULL,
        [effective_from] date NOT NULL,
        [effective_to] date NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_category_price_policies] PRIMARY KEY ([id]),
        CONSTRAINT [CK_category_price_policies_amounts] CHECK ([base_amount] >= 0 AND ([minimum_budget_amount] IS NULL OR [minimum_budget_amount] >= 0) AND ([recommended_min_amount] IS NULL OR [recommended_min_amount] >= 0) AND ([recommended_max_amount] IS NULL OR [recommended_max_amount] >= 0) AND ([unit_price_amount] IS NULL OR [unit_price_amount] >= 0) AND ([minimum_charge_amount] IS NULL OR [minimum_charge_amount] >= 0)),
        CONSTRAINT [CK_category_price_policies_period] CHECK ([effective_to] IS NULL OR [effective_to] > [effective_from]),
        CONSTRAINT [CK_category_price_policies_range] CHECK ([recommended_min_amount] IS NULL OR [recommended_max_amount] IS NULL OR [recommended_min_amount] <= [recommended_max_amount]),
        CONSTRAINT [FK_category_price_policies_category_policies_legacy_category_policy_id] FOREIGN KEY ([legacy_category_policy_id]) REFERENCES [category_policies] ([id]),
        CONSTRAINT [FK_category_price_policies_service_categories_category_id] FOREIGN KEY ([category_id]) REFERENCES [service_categories] ([id]),
        CONSTRAINT [FK_category_price_policies_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_category_price_policies_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE TABLE [category_price_policy_options] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [price_policy_id] bigint NOT NULL,
        [option_name] nvarchar(200) NOT NULL,
        [additional_amount] decimal(19,4) NOT NULL DEFAULT 0.0,
        [display_order] int NOT NULL DEFAULT 0,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_category_price_policy_options] PRIMARY KEY ([id]),
        CONSTRAINT [CK_category_price_policy_options_order] CHECK ([display_order] >= 0),
        CONSTRAINT [FK_category_price_policy_options_category_price_policies_price_policy_id] FOREIGN KEY ([price_policy_id]) REFERENCES [category_price_policies] ([id]),
        CONSTRAINT [FK_category_price_policy_options_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_category_price_policy_options_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE TABLE [category_price_policy_surcharges] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [price_policy_id] bigint NOT NULL,
        [surcharge_name] nvarchar(200) NOT NULL,
        [calculation_type_code] varchar(20) NOT NULL,
        [amount] decimal(19,4) NULL,
        [rate] decimal(9,6) NULL,
        [display_order] int NOT NULL DEFAULT 0,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_category_price_policy_surcharges] PRIMARY KEY ([id]),
        CONSTRAINT [CK_category_price_policy_surcharges_order] CHECK ([display_order] >= 0),
        CONSTRAINT [CK_category_price_policy_surcharges_value] CHECK ((([amount] IS NOT NULL AND [amount] >= 0 AND [rate] IS NULL) OR ([amount] IS NULL AND [rate] IS NOT NULL AND [rate] >= 0 AND [rate] <= 1))),
        CONSTRAINT [FK_category_price_policy_surcharges_category_price_policies_price_policy_id] FOREIGN KEY ([price_policy_id]) REFERENCES [category_price_policies] ([id]),
        CONSTRAINT [FK_category_price_policy_surcharges_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_category_price_policy_surcharges_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    IF EXISTS (SELECT category_id, policy_version FROM category_policies GROUP BY category_id, policy_version HAVING COUNT(*) > 1)
        THROW 51000, 'Duplicate legacy category policy version prevents policy normalization.', 1;
    IF EXISTS (SELECT 1 FROM category_policies p LEFT JOIN service_categories c ON c.id = p.category_id WHERE c.id IS NULL)
        THROW 51000, 'Invalid legacy category foreign key prevents policy normalization.', 1;
    IF EXISTS (SELECT 1 FROM category_policies p LEFT JOIN fee_policies f ON f.id = p.fee_policy_id WHERE f.id IS NULL)
        THROW 51000, 'Invalid legacy fee policy foreign key prevents policy normalization.', 1;
    IF EXISTS (SELECT 1 FROM category_policies p JOIN fee_policies f ON f.id = p.fee_policy_id WHERE p.estimated_quote_fee_amount <> ISNULL(f.display_fee_amount, 0))
        THROW 51000, 'Legacy estimated fee differs from source fee display amount.', 1;
    IF EXISTS (SELECT 1 FROM category_policies WHERE effective_to IS NOT NULL AND effective_to <= effective_from)
        THROW 51000, 'Invalid legacy policy period prevents policy normalization.', 1;
    IF EXISTS (SELECT 1 FROM category_policies WHERE base_price_amount < 0 OR minimum_budget_amount < 0 OR max_quote_count <= 0 OR quote_validity_minutes <= 0 OR provider_response_deadline_minutes <= 0 OR required_completion_photo_count < 0 OR default_warranty_days < 0)
        THROW 51000, 'Invalid legacy policy values prevent policy normalization.', 1;
    IF EXISTS (SELECT 1 FROM category_price_policies) OR EXISTS (SELECT 1 FROM category_fee_policies) OR EXISTS (SELECT 1 FROM category_operation_policies)
        THROW 51000, 'Policy normalization target tables must be empty.', 1;

    INSERT INTO category_price_policies
        (public_id, category_id, legacy_category_policy_id, policy_version, legacy_price_method_text, price_type_code,
         base_amount, minimum_budget_amount, recommended_min_amount, recommended_max_amount, unit_text, unit_price_amount,
         minimum_charge_amount, currency_code, legacy_vat_display_rule_text, vat_policy_code, effective_from, effective_to,
         is_active, created_at, created_by_user_id, updated_at, updated_by_user_id)
    SELECT NEWID(), p.category_id, p.id, p.policy_version, p.price_method_text, NULL,
           p.base_price_amount, p.minimum_budget_amount, NULL, NULL, p.standard_work_unit_text, NULL,
           NULL, p.currency_code, p.vat_display_rule_text, NULL, p.effective_from, p.effective_to,
           1, p.created_at, p.created_by_user_id, p.updated_at, p.updated_by_user_id
    FROM category_policies p;

    INSERT INTO category_fee_policies
        (public_id, category_id, legacy_category_policy_id, source_fee_policy_id, policy_version, policy_kind_code,
         transaction_type_code, calculation_method_text, fee_amount, min_base_amount, max_base_amount, rate,
         monthly_amount, per_visit_amount, currency_code, charge_timing_text, restore_rule_text, effective_from,
         effective_to, is_active, created_at, created_by_user_id, updated_at, updated_by_user_id)
    SELECT NEWID(), p.category_id, p.id, f.id, p.policy_version, f.policy_kind_code,
           f.transaction_type_code, f.calculation_method_text, p.estimated_quote_fee_amount, f.min_base_amount,
           f.max_base_amount, f.rate, f.monthly_amount, f.per_visit_amount, f.currency_code,
           p.fee_charge_timing_text, p.fee_restore_condition_text, p.effective_from, p.effective_to,
           f.is_active, p.created_at, p.created_by_user_id, p.updated_at, p.updated_by_user_id
    FROM category_policies p
    INNER JOIN fee_policies f ON f.id = p.fee_policy_id;

    INSERT INTO category_operation_policies
        (public_id, category_id, legacy_category_policy_id, policy_version, request_method_text,
         onsite_requirement_text, is_emergency_allowed, subscription_option_text, max_quote_count,
         quote_validity_minutes, matching_area_rule_text, notification_target_rule_text,
         provider_response_deadline_minutes, request_field_summary_text, required_completion_photo_count,
         required_qualification_summary_text, insurance_requirement_text, safety_grade_code,
         completion_evidence_rule_text, default_warranty_days, trust_score_display_text, default_sort_code,
         service_area_level_code, reference_url, admin_note, effective_from, effective_to, is_active,
         created_at, created_by_user_id, updated_at, updated_by_user_id)
    SELECT NEWID(), p.category_id, p.id, p.policy_version, p.request_method_text,
           p.onsite_requirement_text, p.is_emergency_allowed, p.subscription_option_text, p.max_quote_count,
           p.quote_validity_minutes, p.matching_area_rule_text, p.notification_target_rule_text,
           p.provider_response_deadline_minutes, p.request_field_summary_text, p.required_completion_photo_count,
           p.required_qualification_summary_text, p.insurance_requirement_text, p.safety_grade_code,
           p.completion_evidence_rule_text, p.default_warranty_days, p.trust_score_display_text, p.default_sort_code,
           p.service_area_level_code, p.reference_url, p.admin_note, p.effective_from, p.effective_to, 1,
           p.created_at, p.created_by_user_id, p.updated_at, p.updated_by_user_id
    FROM category_policies p;

    DECLARE @legacy_count bigint = (SELECT COUNT_BIG(*) FROM category_policies);
    IF (SELECT COUNT_BIG(*) FROM category_price_policies) <> @legacy_count
        THROW 51000, 'Price policy row count validation failed.', 1;
    IF (SELECT COUNT_BIG(*) FROM category_fee_policies) <> @legacy_count
        THROW 51000, 'Fee policy row count validation failed.', 1;
    IF (SELECT COUNT_BIG(*) FROM category_operation_policies) <> @legacy_count
        THROW 51000, 'Operation policy row count validation failed.', 1;

    IF EXISTS (
        SELECT id, category_id, policy_version, price_method_text, base_price_amount, minimum_budget_amount,
               standard_work_unit_text, currency_code, vat_display_rule_text, effective_from, effective_to
        FROM category_policies
        EXCEPT
        SELECT legacy_category_policy_id, category_id, policy_version, legacy_price_method_text, base_amount,
               minimum_budget_amount, unit_text, currency_code, legacy_vat_display_rule_text, effective_from, effective_to
        FROM category_price_policies)
        THROW 51000, 'Price policy value validation failed.', 1;
    IF EXISTS (SELECT 1 FROM category_price_policies WHERE price_type_code IS NOT NULL OR recommended_min_amount IS NOT NULL OR recommended_max_amount IS NOT NULL OR unit_price_amount IS NOT NULL OR minimum_charge_amount IS NOT NULL OR vat_policy_code IS NOT NULL)
        THROW 51000, 'Unapproved inferred price values were created.', 1;

    IF EXISTS (
        SELECT p.id, p.category_id, p.policy_version, f.id, f.policy_kind_code, f.transaction_type_code,
               f.calculation_method_text, p.estimated_quote_fee_amount, f.min_base_amount, f.max_base_amount,
               f.rate, f.monthly_amount, f.per_visit_amount, f.currency_code, p.fee_charge_timing_text,
               p.fee_restore_condition_text, p.effective_from, p.effective_to, f.is_active
        FROM category_policies p INNER JOIN fee_policies f ON f.id = p.fee_policy_id
        EXCEPT
        SELECT legacy_category_policy_id, category_id, policy_version, source_fee_policy_id, policy_kind_code,
               transaction_type_code, calculation_method_text, fee_amount, min_base_amount, max_base_amount,
               rate, monthly_amount, per_visit_amount, currency_code, charge_timing_text, restore_rule_text,
               effective_from, effective_to, is_active
        FROM category_fee_policies)
        THROW 51000, 'Fee policy value validation failed.', 1;

    IF EXISTS (
        SELECT p.id, p.category_id, p.policy_version, p.request_method_text, p.onsite_requirement_text,
               p.is_emergency_allowed, p.subscription_option_text, p.max_quote_count, p.quote_validity_minutes,
               p.matching_area_rule_text, p.notification_target_rule_text, p.provider_response_deadline_minutes,
               p.request_field_summary_text, p.required_completion_photo_count, p.required_qualification_summary_text,
               p.insurance_requirement_text, p.safety_grade_code, p.completion_evidence_rule_text,
               p.default_warranty_days, p.trust_score_display_text, p.default_sort_code,
               p.service_area_level_code, p.reference_url, p.admin_note, p.effective_from, p.effective_to
        FROM category_policies p
        EXCEPT
        SELECT legacy_category_policy_id, category_id, policy_version, request_method_text,
               onsite_requirement_text, is_emergency_allowed, subscription_option_text, max_quote_count,
               quote_validity_minutes, matching_area_rule_text, notification_target_rule_text,
               provider_response_deadline_minutes, request_field_summary_text, required_completion_photo_count,
               required_qualification_summary_text, insurance_requirement_text, safety_grade_code,
               completion_evidence_rule_text, default_warranty_days, trust_score_display_text, default_sort_code,
               service_area_level_code, reference_url, admin_note, effective_from, effective_to
        FROM category_operation_policies)
        THROW 51000, 'Operation policy value validation failed.', 1;
    IF EXISTS (SELECT 1 FROM category_price_policy_options) OR EXISTS (SELECT 1 FROM category_price_policy_surcharges)
        THROW 51000, 'Price option or surcharge data must not be inferred.', 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE INDEX [IX_category_fee_policies_category_id_is_active_effective_from_effective_to] ON [category_fee_policies] ([category_id], [is_active], [effective_from], [effective_to]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE UNIQUE INDEX [IX_category_fee_policies_category_id_policy_version] ON [category_fee_policies] ([category_id], [policy_version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE INDEX [IX_category_fee_policies_created_by_user_id] ON [category_fee_policies] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_category_fee_policies_legacy_category_policy_id] ON [category_fee_policies] ([legacy_category_policy_id]) WHERE [legacy_category_policy_id] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE UNIQUE INDEX [IX_category_fee_policies_public_id] ON [category_fee_policies] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE INDEX [IX_category_fee_policies_source_fee_policy_id] ON [category_fee_policies] ([source_fee_policy_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE INDEX [IX_category_fee_policies_updated_by_user_id] ON [category_fee_policies] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE INDEX [IX_category_operation_policies_category_id_is_active_effective_from_effective_to] ON [category_operation_policies] ([category_id], [is_active], [effective_from], [effective_to]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE UNIQUE INDEX [IX_category_operation_policies_category_id_policy_version] ON [category_operation_policies] ([category_id], [policy_version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE INDEX [IX_category_operation_policies_created_by_user_id] ON [category_operation_policies] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_category_operation_policies_legacy_category_policy_id] ON [category_operation_policies] ([legacy_category_policy_id]) WHERE [legacy_category_policy_id] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE UNIQUE INDEX [IX_category_operation_policies_public_id] ON [category_operation_policies] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE INDEX [IX_category_operation_policies_updated_by_user_id] ON [category_operation_policies] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE INDEX [IX_category_price_policies_category_id_is_active_effective_from_effective_to] ON [category_price_policies] ([category_id], [is_active], [effective_from], [effective_to]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE UNIQUE INDEX [IX_category_price_policies_category_id_policy_version] ON [category_price_policies] ([category_id], [policy_version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE INDEX [IX_category_price_policies_created_by_user_id] ON [category_price_policies] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_category_price_policies_legacy_category_policy_id] ON [category_price_policies] ([legacy_category_policy_id]) WHERE [legacy_category_policy_id] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE UNIQUE INDEX [IX_category_price_policies_public_id] ON [category_price_policies] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE INDEX [IX_category_price_policies_updated_by_user_id] ON [category_price_policies] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE INDEX [IX_category_price_policy_options_created_by_user_id] ON [category_price_policy_options] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE INDEX [IX_category_price_policy_options_price_policy_id_is_active_display_order] ON [category_price_policy_options] ([price_policy_id], [is_active], [display_order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE UNIQUE INDEX [IX_category_price_policy_options_public_id] ON [category_price_policy_options] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE INDEX [IX_category_price_policy_options_updated_by_user_id] ON [category_price_policy_options] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE INDEX [IX_category_price_policy_surcharges_created_by_user_id] ON [category_price_policy_surcharges] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE INDEX [IX_category_price_policy_surcharges_price_policy_id_is_active_display_order] ON [category_price_policy_surcharges] ([price_policy_id], [is_active], [display_order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE UNIQUE INDEX [IX_category_price_policy_surcharges_public_id] ON [category_price_policy_surcharges] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    CREATE INDEX [IX_category_price_policy_surcharges_updated_by_user_id] ON [category_price_policy_surcharges] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810052214_NormalizeCategoryPolicyStructure'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810052214_NormalizeCategoryPolicyStructure', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    ALTER TABLE [provider_documents] ADD [document_type_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE TABLE [provider_document_types] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [code] varchar(50) NOT NULL,
        [name] nvarchar(200) NOT NULL,
        [supports_expiry] bit NOT NULL DEFAULT CAST(0 AS bit),
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_provider_document_types] PRIMARY KEY ([id]),
        CONSTRAINT [FK_provider_document_types_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_provider_document_types_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE TABLE [provider_requirement_types] (
        [code] varchar(30) NOT NULL,
        [name] nvarchar(100) NOT NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_provider_requirement_types] PRIMARY KEY ([code])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE TABLE [provider_requirement_definitions] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [requirement_type_code] varchar(30) NOT NULL,
        [requirement_code] varchar(50) NOT NULL,
        [name] nvarchar(200) NOT NULL,
        [description] nvarchar(1000) NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_provider_requirement_definitions] PRIMARY KEY ([id]),
        CONSTRAINT [FK_provider_requirement_definitions_provider_requirement_types_requirement_type_code] FOREIGN KEY ([requirement_type_code]) REFERENCES [provider_requirement_types] ([code]),
        CONSTRAINT [FK_provider_requirement_definitions_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_provider_requirement_definitions_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE TABLE [category_provider_requirement_assignments] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [category_operation_policy_id] bigint NOT NULL,
        [requirement_definition_id] bigint NOT NULL,
        [is_required] bit NOT NULL DEFAULT CAST(1 AS bit),
        [verification_required] bit NOT NULL DEFAULT CAST(1 AS bit),
        [expiry_check_required] bit NOT NULL DEFAULT CAST(0 AS bit),
        [minimum_valid_days] smallint NULL,
        [display_order] int NOT NULL DEFAULT 0,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_category_provider_requirement_assignments] PRIMARY KEY ([id]),
        CONSTRAINT [CK_category_provider_requirement_assignments_display_order] CHECK ([display_order] >= 0),
        CONSTRAINT [CK_category_provider_requirement_assignments_minimum_valid_days] CHECK ([minimum_valid_days] IS NULL OR [minimum_valid_days] >= 0),
        CONSTRAINT [FK_category_provider_requirement_assignments_category_operation_policies_category_operation_policy_id] FOREIGN KEY ([category_operation_policy_id]) REFERENCES [category_operation_policies] ([id]),
        CONSTRAINT [FK_category_provider_requirement_assignments_provider_requirement_definitions_requirement_definition_id] FOREIGN KEY ([requirement_definition_id]) REFERENCES [provider_requirement_definitions] ([id]),
        CONSTRAINT [FK_category_provider_requirement_assignments_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_category_provider_requirement_assignments_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE TABLE [category_provider_requirement_evidence_types] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [requirement_assignment_id] bigint NOT NULL,
        [document_type_id] bigint NOT NULL,
        [is_required] bit NOT NULL DEFAULT CAST(1 AS bit),
        [display_order] int NOT NULL DEFAULT 0,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_category_provider_requirement_evidence_types] PRIMARY KEY ([id]),
        CONSTRAINT [CK_category_provider_requirement_evidence_types_display_order] CHECK ([display_order] >= 0),
        CONSTRAINT [FK_category_provider_requirement_evidence_types_category_provider_requirement_assignments_requirement_assignment_id] FOREIGN KEY ([requirement_assignment_id]) REFERENCES [category_provider_requirement_assignments] ([id]),
        CONSTRAINT [FK_category_provider_requirement_evidence_types_provider_document_types_document_type_id] FOREIGN KEY ([document_type_id]) REFERENCES [provider_document_types] ([id]),
        CONSTRAINT [FK_category_provider_requirement_evidence_types_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_category_provider_requirement_evidence_types_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE TABLE [provider_service_requirement_verifications] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [provider_service_category_id] bigint NOT NULL,
        [requirement_assignment_id] bigint NOT NULL,
        [provider_document_id] bigint NULL,
        [verification_status_code] varchar(20) NOT NULL DEFAULT 'PENDING',
        [verified_by_user_id] bigint NULL,
        [verified_at] datetime2(7) NULL,
        [expires_at] date NULL,
        [rejection_reason] nvarchar(1000) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_provider_service_requirement_verifications] PRIMARY KEY ([id]),
        CONSTRAINT [FK_provider_service_requirement_verifications_category_provider_requirement_assignments_requirement_assignment_id] FOREIGN KEY ([requirement_assignment_id]) REFERENCES [category_provider_requirement_assignments] ([id]),
        CONSTRAINT [FK_provider_service_requirement_verifications_provider_documents_provider_document_id] FOREIGN KEY ([provider_document_id]) REFERENCES [provider_documents] ([id]),
        CONSTRAINT [FK_provider_service_requirement_verifications_provider_service_categories_provider_service_category_id] FOREIGN KEY ([provider_service_category_id]) REFERENCES [provider_service_categories] ([id]),
        CONSTRAINT [FK_provider_service_requirement_verifications_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_provider_service_requirement_verifications_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_provider_service_requirement_verifications_users_verified_by_user_id] FOREIGN KEY ([verified_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'code', N'is_active', N'name') AND [object_id] = OBJECT_ID(N'[provider_requirement_types]'))
        SET IDENTITY_INSERT [provider_requirement_types] ON;
    EXEC(N'INSERT INTO [provider_requirement_types] ([code], [is_active], [name])
    VALUES (''EVIDENCE_VALIDITY'', CAST(1 AS bit), N''증빙 유효성''),
    (''INSURANCE'', CAST(1 AS bit), N''보험''),
    (''LICENSE'', CAST(1 AS bit), N''면허''),
    (''QUALIFICATION'', CAST(1 AS bit), N''자격''),
    (''SAFETY'', CAST(1 AS bit), N''안전'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'code', N'is_active', N'name') AND [object_id] = OBJECT_ID(N'[provider_requirement_types]'))
        SET IDENTITY_INSERT [provider_requirement_types] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE INDEX [IX_provider_documents_document_type_id] ON [provider_documents] ([document_type_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE INDEX [IX_category_provider_requirement_assignments_category_operation_policy_id_is_active_display_order] ON [category_provider_requirement_assignments] ([category_operation_policy_id], [is_active], [display_order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_category_provider_requirement_assignments_category_operation_policy_id_requirement_definition_id] ON [category_provider_requirement_assignments] ([category_operation_policy_id], [requirement_definition_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE INDEX [IX_category_provider_requirement_assignments_created_by_user_id] ON [category_provider_requirement_assignments] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_category_provider_requirement_assignments_public_id] ON [category_provider_requirement_assignments] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE INDEX [IX_category_provider_requirement_assignments_requirement_definition_id] ON [category_provider_requirement_assignments] ([requirement_definition_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE INDEX [IX_category_provider_requirement_assignments_updated_by_user_id] ON [category_provider_requirement_assignments] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE INDEX [IX_category_provider_requirement_evidence_types_created_by_user_id] ON [category_provider_requirement_evidence_types] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE INDEX [IX_category_provider_requirement_evidence_types_document_type_id] ON [category_provider_requirement_evidence_types] ([document_type_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_category_provider_requirement_evidence_types_public_id] ON [category_provider_requirement_evidence_types] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE INDEX [IX_category_provider_requirement_evidence_types_requirement_assignment_id_display_order] ON [category_provider_requirement_evidence_types] ([requirement_assignment_id], [display_order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_category_provider_requirement_evidence_types_requirement_assignment_id_document_type_id] ON [category_provider_requirement_evidence_types] ([requirement_assignment_id], [document_type_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE INDEX [IX_category_provider_requirement_evidence_types_updated_by_user_id] ON [category_provider_requirement_evidence_types] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_document_types_code] ON [provider_document_types] ([code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE INDEX [IX_provider_document_types_created_by_user_id] ON [provider_document_types] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE INDEX [IX_provider_document_types_is_active_name] ON [provider_document_types] ([is_active], [name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_document_types_public_id] ON [provider_document_types] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE INDEX [IX_provider_document_types_updated_by_user_id] ON [provider_document_types] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE INDEX [IX_provider_requirement_definitions_created_by_user_id] ON [provider_requirement_definitions] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_requirement_definitions_public_id] ON [provider_requirement_definitions] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_requirement_definitions_requirement_code] ON [provider_requirement_definitions] ([requirement_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE INDEX [IX_provider_requirement_definitions_requirement_type_code_is_active_name] ON [provider_requirement_definitions] ([requirement_type_code], [is_active], [name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE INDEX [IX_provider_requirement_definitions_updated_by_user_id] ON [provider_requirement_definitions] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE INDEX [IX_provider_service_requirement_verifications_created_by_user_id] ON [provider_service_requirement_verifications] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE INDEX [IX_provider_service_requirement_verifications_provider_document_id] ON [provider_service_requirement_verifications] ([provider_document_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_service_requirement_verifications_provider_service_category_id_requirement_assignment_id] ON [provider_service_requirement_verifications] ([provider_service_category_id], [requirement_assignment_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_service_requirement_verifications_public_id] ON [provider_service_requirement_verifications] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE INDEX [IX_provider_service_requirement_verifications_requirement_assignment_id] ON [provider_service_requirement_verifications] ([requirement_assignment_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE INDEX [IX_provider_service_requirement_verifications_updated_by_user_id] ON [provider_service_requirement_verifications] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE INDEX [IX_provider_service_requirement_verifications_verification_status_code_expires_at] ON [provider_service_requirement_verifications] ([verification_status_code], [expires_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    CREATE INDEX [IX_provider_service_requirement_verifications_verified_by_user_id] ON [provider_service_requirement_verifications] ([verified_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    ALTER TABLE [provider_documents] ADD CONSTRAINT [FK_provider_documents_provider_document_types_document_type_id] FOREIGN KEY ([document_type_id]) REFERENCES [provider_document_types] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810061026_AddStructuredProviderRequirements'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810061026_AddStructuredProviderRequirements', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810071853_AddProviderServiceApprovalWorkflow'
)
BEGIN
    CREATE TABLE [provider_service_approval_events] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [provider_service_category_id] bigint NOT NULL,
        [from_status_code] varchar(20) NULL,
        [to_status_code] varchar(20) NOT NULL,
        [action_code] varchar(20) NOT NULL,
        [decision_reason] nvarchar(1000) NULL,
        [decided_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [decided_by_user_id] bigint NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_provider_service_approval_events] PRIMARY KEY ([id]),
        CONSTRAINT [CK_provider_service_approval_events_action] CHECK ([action_code] IN ('APPROVE','REJECT','SUSPEND','REOPEN')),
        CONSTRAINT [CK_provider_service_approval_events_from_status] CHECK ([from_status_code] IS NULL OR [from_status_code] IN ('PENDING','APPROVED','REJECTED','SUSPENDED')),
        CONSTRAINT [CK_provider_service_approval_events_to_status] CHECK ([to_status_code] IN ('PENDING','APPROVED','REJECTED','SUSPENDED')),
        CONSTRAINT [FK_provider_service_approval_events_provider_service_categories_provider_service_category_id] FOREIGN KEY ([provider_service_category_id]) REFERENCES [provider_service_categories] ([id]),
        CONSTRAINT [FK_provider_service_approval_events_users_decided_by_user_id] FOREIGN KEY ([decided_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810071853_AddProviderServiceApprovalWorkflow'
)
BEGIN
    CREATE TABLE [provider_service_approvals] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [provider_service_category_id] bigint NOT NULL,
        [approval_status_code] varchar(20) NOT NULL DEFAULT 'PENDING',
        [approval_requested_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [approval_decided_at] datetime2(7) NULL,
        [approval_decided_by_user_id] bigint NULL,
        [decision_reason] nvarchar(1000) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_provider_service_approvals] PRIMARY KEY ([id]),
        CONSTRAINT [CK_provider_service_approvals_status] CHECK ([approval_status_code] IN ('PENDING','APPROVED','REJECTED','SUSPENDED')),
        CONSTRAINT [FK_provider_service_approvals_provider_service_categories_provider_service_category_id] FOREIGN KEY ([provider_service_category_id]) REFERENCES [provider_service_categories] ([id]),
        CONSTRAINT [FK_provider_service_approvals_users_approval_decided_by_user_id] FOREIGN KEY ([approval_decided_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_provider_service_approvals_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_provider_service_approvals_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810071853_AddProviderServiceApprovalWorkflow'
)
BEGIN
    INSERT INTO provider_service_approvals
        (public_id, provider_service_category_id, approval_status_code, approval_requested_at,
         approval_decided_at, approval_decided_by_user_id, decision_reason,
         created_at, created_by_user_id, updated_at, updated_by_user_id)
    SELECT NEWID(), id, 'PENDING', activated_at,
           NULL, NULL, NULL,
           SYSUTCDATETIME(), NULL, SYSUTCDATETIME(), NULL
    FROM provider_service_categories;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810071853_AddProviderServiceApprovalWorkflow'
)
BEGIN
    CREATE INDEX [IX_provider_service_approval_events_decided_by_user_id] ON [provider_service_approval_events] ([decided_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810071853_AddProviderServiceApprovalWorkflow'
)
BEGIN
    CREATE INDEX [IX_provider_service_approval_events_provider_service_category_id] ON [provider_service_approval_events] ([provider_service_category_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810071853_AddProviderServiceApprovalWorkflow'
)
BEGIN
    CREATE INDEX [IX_provider_service_approval_events_provider_service_category_id_decided_at] ON [provider_service_approval_events] ([provider_service_category_id], [decided_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810071853_AddProviderServiceApprovalWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_service_approval_events_public_id] ON [provider_service_approval_events] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810071853_AddProviderServiceApprovalWorkflow'
)
BEGIN
    CREATE INDEX [IX_provider_service_approvals_approval_decided_by_user_id] ON [provider_service_approvals] ([approval_decided_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810071853_AddProviderServiceApprovalWorkflow'
)
BEGIN
    CREATE INDEX [IX_provider_service_approvals_approval_requested_at] ON [provider_service_approvals] ([approval_requested_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810071853_AddProviderServiceApprovalWorkflow'
)
BEGIN
    CREATE INDEX [IX_provider_service_approvals_approval_status_code] ON [provider_service_approvals] ([approval_status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810071853_AddProviderServiceApprovalWorkflow'
)
BEGIN
    CREATE INDEX [IX_provider_service_approvals_created_by_user_id] ON [provider_service_approvals] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810071853_AddProviderServiceApprovalWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_service_approvals_provider_service_category_id] ON [provider_service_approvals] ([provider_service_category_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810071853_AddProviderServiceApprovalWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_service_approvals_public_id] ON [provider_service_approvals] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810071853_AddProviderServiceApprovalWorkflow'
)
BEGIN
    CREATE INDEX [IX_provider_service_approvals_updated_by_user_id] ON [provider_service_approvals] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810071853_AddProviderServiceApprovalWorkflow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810071853_AddProviderServiceApprovalWorkflow', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE TABLE [wallets] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [currency_code] char(3) NOT NULL DEFAULT 'KRW',
        [available_balance] decimal(19,4) NOT NULL DEFAULT 0.0,
        [reserved_balance] decimal(19,4) NOT NULL DEFAULT 0.0,
        [status_code] varchar(20) NOT NULL DEFAULT 'ACTIVE',
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_wallets] PRIMARY KEY ([id]),
        CONSTRAINT [CK_wallets_balances] CHECK ([available_balance] >= 0 AND [reserved_balance] >= 0),
        CONSTRAINT [CK_wallets_status] CHECK ([status_code] IN ('ACTIVE','FROZEN','CLOSED')),
        CONSTRAINT [FK_wallets_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_wallets_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_wallets_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE TABLE [wallet_ledger] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [wallet_id] bigint NOT NULL,
        [transaction_id] bigint NULL,
        [entry_type_code] varchar(20) NOT NULL,
        [amount] decimal(19,4) NOT NULL,
        [balance_after] decimal(19,4) NOT NULL,
        [idempotency_key] varchar(150) NOT NULL,
        [reason] nvarchar(1000) NOT NULL,
        [reference_type] varchar(50) NULL,
        [reference_public_id] uniqueidentifier NULL,
        [payment_method_code] varchar(40) NULL,
        [occurred_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_wallet_ledger] PRIMARY KEY ([id]),
        CONSTRAINT [CK_wallet_ledger_amount] CHECK ([amount] <> 0),
        CONSTRAINT [CK_wallet_ledger_balance] CHECK ([balance_after] >= 0),
        CONSTRAINT [CK_wallet_ledger_entry_type] CHECK ([entry_type_code] IN ('CHARGE','USE','RESTORE','REFUND','ADJUST')),
        CONSTRAINT [FK_wallet_ledger_transactions_transaction_id] FOREIGN KEY ([transaction_id]) REFERENCES [transactions] ([id]),
        CONSTRAINT [FK_wallet_ledger_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_wallet_ledger_wallets_wallet_id] FOREIGN KEY ([wallet_id]) REFERENCES [wallets] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE TABLE [fee_charges] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [transaction_id] bigint NOT NULL,
        [category_fee_policy_id] bigint NOT NULL,
        [wallet_id] bigint NOT NULL,
        [ledger_entry_id] bigint NOT NULL,
        [fee_amount] decimal(19,4) NOT NULL,
        [charged_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [restore_status_code] varchar(20) NOT NULL DEFAULT 'NOT_RESTORED',
        [restore_ledger_entry_id] bigint NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_fee_charges] PRIMARY KEY ([id]),
        CONSTRAINT [CK_fee_charges_amount] CHECK ([fee_amount] > 0),
        CONSTRAINT [CK_fee_charges_restore_status] CHECK ([restore_status_code] IN ('NOT_RESTORED','RESTORED')),
        CONSTRAINT [FK_fee_charges_category_fee_policies_category_fee_policy_id] FOREIGN KEY ([category_fee_policy_id]) REFERENCES [category_fee_policies] ([id]),
        CONSTRAINT [FK_fee_charges_transactions_transaction_id] FOREIGN KEY ([transaction_id]) REFERENCES [transactions] ([id]),
        CONSTRAINT [FK_fee_charges_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_fee_charges_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_fee_charges_wallet_ledger_ledger_entry_id] FOREIGN KEY ([ledger_entry_id]) REFERENCES [wallet_ledger] ([id]),
        CONSTRAINT [FK_fee_charges_wallet_ledger_restore_ledger_entry_id] FOREIGN KEY ([restore_ledger_entry_id]) REFERENCES [wallet_ledger] ([id]),
        CONSTRAINT [FK_fee_charges_wallets_wallet_id] FOREIGN KEY ([wallet_id]) REFERENCES [wallets] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE TABLE [wallet_charge_requests] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [wallet_id] bigint NOT NULL,
        [requested_amount] decimal(19,4) NOT NULL,
        [payment_method_code] varchar(40) NOT NULL,
        [status_code] varchar(20) NOT NULL DEFAULT 'REQUESTED',
        [requested_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [completed_at] datetime2(7) NULL,
        [failed_at] datetime2(7) NULL,
        [failure_reason] nvarchar(1000) NULL,
        [external_payment_reference] varchar(200) NULL,
        [idempotency_key] varchar(150) NOT NULL,
        [ledger_entry_id] bigint NULL,
        [request_reason] nvarchar(1000) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_wallet_charge_requests] PRIMARY KEY ([id]),
        CONSTRAINT [CK_wallet_charge_requests_amount] CHECK ([requested_amount] > 0),
        CONSTRAINT [CK_wallet_charge_requests_status] CHECK ([status_code] IN ('REQUESTED','PROCESSING','SUCCEEDED','FAILED','CANCELLED')),
        CONSTRAINT [FK_wallet_charge_requests_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_wallet_charge_requests_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_wallet_charge_requests_wallet_ledger_ledger_entry_id] FOREIGN KEY ([ledger_entry_id]) REFERENCES [wallet_ledger] ([id]),
        CONSTRAINT [FK_wallet_charge_requests_wallets_wallet_id] FOREIGN KEY ([wallet_id]) REFERENCES [wallets] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE TABLE [wallet_refund_requests] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [wallet_id] bigint NOT NULL,
        [requested_amount] decimal(19,4) NOT NULL,
        [status_code] varchar(20) NOT NULL DEFAULT 'REQUESTED',
        [request_reason] nvarchar(1000) NOT NULL,
        [requested_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [reviewed_at] datetime2(7) NULL,
        [reviewed_by_user_id] bigint NULL,
        [review_reason] nvarchar(1000) NULL,
        [completed_at] datetime2(7) NULL,
        [failed_at] datetime2(7) NULL,
        [failure_reason] nvarchar(1000) NULL,
        [external_refund_reference] varchar(200) NULL,
        [ledger_entry_id] bigint NULL,
        [idempotency_key] varchar(150) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_wallet_refund_requests] PRIMARY KEY ([id]),
        CONSTRAINT [CK_wallet_refund_requests_amount] CHECK ([requested_amount] > 0),
        CONSTRAINT [CK_wallet_refund_requests_status] CHECK ([status_code] IN ('REQUESTED','APPROVED','PROCESSING','COMPLETED','REJECTED','CANCELLED','FAILED')),
        CONSTRAINT [FK_wallet_refund_requests_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_wallet_refund_requests_users_reviewed_by_user_id] FOREIGN KEY ([reviewed_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_wallet_refund_requests_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_wallet_refund_requests_wallet_ledger_ledger_entry_id] FOREIGN KEY ([ledger_entry_id]) REFERENCES [wallet_ledger] ([id]),
        CONSTRAINT [FK_wallet_refund_requests_wallets_wallet_id] FOREIGN KEY ([wallet_id]) REFERENCES [wallets] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE TABLE [fee_restores] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [fee_charge_id] bigint NOT NULL,
        [ledger_entry_id] bigint NOT NULL,
        [reason_code] varchar(40) NOT NULL,
        [reason] nvarchar(1000) NOT NULL,
        [idempotency_key] varchar(150) NOT NULL,
        [restored_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [restored_by_user_id] bigint NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_fee_restores] PRIMARY KEY ([id]),
        CONSTRAINT [CK_fee_restores_reason_code] CHECK ([reason_code] IN ('SYSTEM_ERROR','DUPLICATE_ACCEPTANCE','FALSE_REQUEST','HEAD_OFFICE_APPROVAL')),
        CONSTRAINT [FK_fee_restores_fee_charges_fee_charge_id] FOREIGN KEY ([fee_charge_id]) REFERENCES [fee_charges] ([id]),
        CONSTRAINT [FK_fee_restores_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_fee_restores_users_restored_by_user_id] FOREIGN KEY ([restored_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_fee_restores_wallet_ledger_ledger_entry_id] FOREIGN KEY ([ledger_entry_id]) REFERENCES [wallet_ledger] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE INDEX [IX_fee_charges_category_fee_policy_id] ON [fee_charges] ([category_fee_policy_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE INDEX [IX_fee_charges_created_by_user_id] ON [fee_charges] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE UNIQUE INDEX [IX_fee_charges_ledger_entry_id] ON [fee_charges] ([ledger_entry_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE UNIQUE INDEX [IX_fee_charges_public_id] ON [fee_charges] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_fee_charges_restore_ledger_entry_id] ON [fee_charges] ([restore_ledger_entry_id]) WHERE [restore_ledger_entry_id] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE UNIQUE INDEX [IX_fee_charges_transaction_id_category_fee_policy_id] ON [fee_charges] ([transaction_id], [category_fee_policy_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE INDEX [IX_fee_charges_updated_by_user_id] ON [fee_charges] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE INDEX [IX_fee_charges_wallet_id_charged_at] ON [fee_charges] ([wallet_id], [charged_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE INDEX [IX_fee_restores_created_by_user_id] ON [fee_restores] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE UNIQUE INDEX [IX_fee_restores_fee_charge_id] ON [fee_restores] ([fee_charge_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE UNIQUE INDEX [IX_fee_restores_idempotency_key] ON [fee_restores] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE UNIQUE INDEX [IX_fee_restores_ledger_entry_id] ON [fee_restores] ([ledger_entry_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE UNIQUE INDEX [IX_fee_restores_public_id] ON [fee_restores] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE INDEX [IX_fee_restores_restored_by_user_id] ON [fee_restores] ([restored_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE INDEX [IX_wallet_charge_requests_created_by_user_id] ON [wallet_charge_requests] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE UNIQUE INDEX [IX_wallet_charge_requests_idempotency_key] ON [wallet_charge_requests] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE INDEX [IX_wallet_charge_requests_ledger_entry_id] ON [wallet_charge_requests] ([ledger_entry_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE UNIQUE INDEX [IX_wallet_charge_requests_public_id] ON [wallet_charge_requests] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE INDEX [IX_wallet_charge_requests_status_code] ON [wallet_charge_requests] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE INDEX [IX_wallet_charge_requests_updated_by_user_id] ON [wallet_charge_requests] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE INDEX [IX_wallet_charge_requests_wallet_id_requested_at] ON [wallet_charge_requests] ([wallet_id], [requested_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE INDEX [IX_wallet_ledger_created_by_user_id] ON [wallet_ledger] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE UNIQUE INDEX [IX_wallet_ledger_idempotency_key] ON [wallet_ledger] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE UNIQUE INDEX [IX_wallet_ledger_public_id] ON [wallet_ledger] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE INDEX [IX_wallet_ledger_reference_type_reference_public_id] ON [wallet_ledger] ([reference_type], [reference_public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE INDEX [IX_wallet_ledger_transaction_id] ON [wallet_ledger] ([transaction_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE INDEX [IX_wallet_ledger_wallet_id_occurred_at] ON [wallet_ledger] ([wallet_id], [occurred_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE INDEX [IX_wallet_refund_requests_created_by_user_id] ON [wallet_refund_requests] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE UNIQUE INDEX [IX_wallet_refund_requests_idempotency_key] ON [wallet_refund_requests] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_wallet_refund_requests_ledger_entry_id] ON [wallet_refund_requests] ([ledger_entry_id]) WHERE [ledger_entry_id] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE UNIQUE INDEX [IX_wallet_refund_requests_public_id] ON [wallet_refund_requests] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE INDEX [IX_wallet_refund_requests_reviewed_by_user_id] ON [wallet_refund_requests] ([reviewed_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE INDEX [IX_wallet_refund_requests_status_code] ON [wallet_refund_requests] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE INDEX [IX_wallet_refund_requests_updated_by_user_id] ON [wallet_refund_requests] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE INDEX [IX_wallet_refund_requests_wallet_id_requested_at] ON [wallet_refund_requests] ([wallet_id], [requested_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE INDEX [IX_wallets_created_by_user_id] ON [wallets] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE UNIQUE INDEX [IX_wallets_provider_profile_id_currency_code] ON [wallets] ([provider_profile_id], [currency_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE UNIQUE INDEX [IX_wallets_public_id] ON [wallets] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE INDEX [IX_wallets_status_code] ON [wallets] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE INDEX [IX_wallets_updated_by_user_id] ON [wallets] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    INSERT INTO wallets
        (public_id, provider_profile_id, currency_code, available_balance, reserved_balance, status_code,
         created_at, created_by_user_id, updated_at, updated_by_user_id)
    SELECT NEWID(), provider.id, 'KRW', 0, 0, 'ACTIVE',
           SYSUTCDATETIME(), NULL, SYSUTCDATETIME(), NULL
    FROM provider_profiles provider
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM wallets wallet
        WHERE wallet.provider_profile_id = provider.id
          AND wallet.currency_code = 'KRW'
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    CREATE TRIGGER TR_wallet_ledger_append_only
    ON wallet_ledger
    INSTEAD OF UPDATE, DELETE
    AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51000, N'Wallet 원장은 수정하거나 삭제할 수 없습니다. 반대 방향의 새 원장 항목을 생성해 주세요.', 1;
    END;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810075427_AddProviderWalletAndLedger'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810075427_AddProviderWalletAndLedger', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810082959_IntegrateQuoteAcceptanceAndWalletFee'
)
BEGIN
    ALTER TABLE [transactions] ADD [actual_charged_fee_amount] decimal(19,4) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810082959_IntegrateQuoteAcceptanceAndWalletFee'
)
BEGIN
    ALTER TABLE [transactions] ADD [calculated_fee_amount] decimal(19,4) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810082959_IntegrateQuoteAcceptanceAndWalletFee'
)
BEGIN
    ALTER TABLE [transactions] ADD [category_fee_policy_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810082959_IntegrateQuoteAcceptanceAndWalletFee'
)
BEGIN
    ALTER TABLE [transactions] ADD [fee_calculation_method_snapshot] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810082959_IntegrateQuoteAcceptanceAndWalletFee'
)
BEGIN
    ALTER TABLE [transactions] ADD [fee_charge_timing_snapshot] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810082959_IntegrateQuoteAcceptanceAndWalletFee'
)
BEGIN
    ALTER TABLE [transactions] ADD [fee_currency_code] char(3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810082959_IntegrateQuoteAcceptanceAndWalletFee'
)
BEGIN
    ALTER TABLE [transactions] ADD [fee_policy_kind_snapshot] varchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810082959_IntegrateQuoteAcceptanceAndWalletFee'
)
BEGIN
    ALTER TABLE [transactions] ADD [fee_policy_snapshot_json] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810082959_IntegrateQuoteAcceptanceAndWalletFee'
)
BEGIN
    ALTER TABLE [transactions] ADD [fee_policy_version_snapshot] varchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810082959_IntegrateQuoteAcceptanceAndWalletFee'
)
BEGIN
    ALTER TABLE [transactions] ADD [fee_restore_rule_snapshot] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810082959_IntegrateQuoteAcceptanceAndWalletFee'
)
BEGIN
    ALTER TABLE [transactions] ADD [fee_transaction_type_snapshot] varchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810082959_IntegrateQuoteAcceptanceAndWalletFee'
)
BEGIN
    ALTER TABLE [transactions] ADD [wallet_ledger_entry_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810082959_IntegrateQuoteAcceptanceAndWalletFee'
)
BEGIN
    CREATE INDEX [IX_transactions_category_fee_policy_id] ON [transactions] ([category_fee_policy_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810082959_IntegrateQuoteAcceptanceAndWalletFee'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_transactions_wallet_ledger_entry_id] ON [transactions] ([wallet_ledger_entry_id]) WHERE [wallet_ledger_entry_id] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810082959_IntegrateQuoteAcceptanceAndWalletFee'
)
BEGIN
    EXEC(N'ALTER TABLE [transactions] ADD CONSTRAINT [CK_transactions_fee_amounts] CHECK (([calculated_fee_amount] IS NULL OR [calculated_fee_amount] >= 0) AND ([actual_charged_fee_amount] IS NULL OR [actual_charged_fee_amount] >= 0))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810082959_IntegrateQuoteAcceptanceAndWalletFee'
)
BEGIN
    EXEC(N'ALTER TABLE [transactions] ADD CONSTRAINT [CK_transactions_fee_policy_json] CHECK ([fee_policy_snapshot_json] IS NULL OR ISJSON([fee_policy_snapshot_json]) = 1)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810082959_IntegrateQuoteAcceptanceAndWalletFee'
)
BEGIN
    ALTER TABLE [transactions] ADD CONSTRAINT [FK_transactions_category_fee_policies_category_fee_policy_id] FOREIGN KEY ([category_fee_policy_id]) REFERENCES [category_fee_policies] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810082959_IntegrateQuoteAcceptanceAndWalletFee'
)
BEGIN
    ALTER TABLE [transactions] ADD CONSTRAINT [FK_transactions_wallet_ledger_wallet_ledger_entry_id] FOREIGN KEY ([wallet_ledger_entry_id]) REFERENCES [wallet_ledger] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810082959_IntegrateQuoteAcceptanceAndWalletFee'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810082959_IntegrateQuoteAcceptanceAndWalletFee', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE TABLE [advertising_campaigns] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [campaign_name] nvarchar(200) NOT NULL,
        [campaign_type_code] varchar(30) NOT NULL,
        [audience_type_code] varchar(20) NOT NULL,
        [owner_type_code] varchar(30) NOT NULL,
        [owner_display_name] nvarchar(200) NULL,
        [start_at] datetime2(7) NOT NULL,
        [end_at] datetime2(7) NULL,
        [status_code] varchar(20) NOT NULL,
        [priority] int NOT NULL DEFAULT 0,
        [destination_type_code] varchar(30) NOT NULL,
        [destination_value] varchar(2000) NULL,
        [review_status_code] varchar(20) NOT NULL,
        [approved_by_user_id] bigint NULL,
        [approved_at] datetime2(7) NULL,
        [rejection_reason] nvarchar(1000) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_advertising_campaigns] PRIMARY KEY ([id]),
        CONSTRAINT [CK_advertising_campaigns_audience] CHECK ([audience_type_code] IN ('ALL','CUSTOMER','PROVIDER')),
        CONSTRAINT [CK_advertising_campaigns_destination] CHECK ([destination_type_code] IN ('NONE','INTERNAL_PATH','EXTERNAL_URL')),
        CONSTRAINT [CK_advertising_campaigns_owner] CHECK ([owner_type_code] IN ('HEAD_OFFICE','PLATFORM','EXTERNAL','PROVIDER')),
        CONSTRAINT [CK_advertising_campaigns_period] CHECK ([end_at] IS NULL OR [end_at] > [start_at]),
        CONSTRAINT [CK_advertising_campaigns_priority] CHECK ([priority] >= 0),
        CONSTRAINT [CK_advertising_campaigns_review] CHECK ([review_status_code] IN ('DRAFT','PENDING','APPROVED','REJECTED')),
        CONSTRAINT [CK_advertising_campaigns_status] CHECK ([status_code] IN ('DRAFT','ACTIVE','PAUSED','ARCHIVED')),
        CONSTRAINT [CK_advertising_campaigns_type] CHECK ([campaign_type_code] IN ('ADVERTISEMENT','PROMOTION','BANNER','POPUP')),
        CONSTRAINT [FK_advertising_campaigns_users_approved_by_user_id] FOREIGN KEY ([approved_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_advertising_campaigns_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_advertising_campaigns_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE TABLE [advertising_placements] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [code] varchar(50) NOT NULL,
        [name] nvarchar(100) NOT NULL,
        [description] nvarchar(500) NULL,
        [route_hint] varchar(200) NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_advertising_placements] PRIMARY KEY ([id]),
        CONSTRAINT [FK_advertising_placements_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_advertising_placements_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE TABLE [managed_contents] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [content_type_code] varchar(30) NOT NULL,
        [audience_type_code] varchar(20) NOT NULL,
        [status_code] varchar(20) NOT NULL,
        [review_status_code] varchar(20) NOT NULL,
        [current_version_no] int NOT NULL DEFAULT 1,
        [display_order] int NOT NULL DEFAULT 0,
        [start_at] datetime2(7) NOT NULL,
        [end_at] datetime2(7) NULL,
        [approved_by_user_id] bigint NULL,
        [approved_at] datetime2(7) NULL,
        [rejection_reason] nvarchar(1000) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_managed_contents] PRIMARY KEY ([id]),
        CONSTRAINT [CK_managed_contents_audience] CHECK ([audience_type_code] IN ('ALL','CUSTOMER','PROVIDER')),
        CONSTRAINT [CK_managed_contents_order] CHECK ([display_order] >= 0),
        CONSTRAINT [CK_managed_contents_period] CHECK ([end_at] IS NULL OR [end_at] > [start_at]),
        CONSTRAINT [CK_managed_contents_review] CHECK ([review_status_code] IN ('DRAFT','PENDING','APPROVED','REJECTED')),
        CONSTRAINT [CK_managed_contents_status] CHECK ([status_code] IN ('DRAFT','ACTIVE','PAUSED','ARCHIVED')),
        CONSTRAINT [CK_managed_contents_type] CHECK ([content_type_code] IN ('NOTICE','FAQ','SAFETY_GUIDE','CATEGORY_GUIDE','PRICE_REFERENCE')),
        CONSTRAINT [CK_managed_contents_version] CHECK ([current_version_no] > 0),
        CONSTRAINT [FK_managed_contents_users_approved_by_user_id] FOREIGN KEY ([approved_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_managed_contents_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_managed_contents_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE TABLE [advertising_campaign_areas] (
        [id] bigint NOT NULL IDENTITY,
        [campaign_id] bigint NOT NULL,
        [administrative_area_id] bigint NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_advertising_campaign_areas] PRIMARY KEY ([id]),
        CONSTRAINT [FK_advertising_campaign_areas_administrative_areas_administrative_area_id] FOREIGN KEY ([administrative_area_id]) REFERENCES [administrative_areas] ([id]),
        CONSTRAINT [FK_advertising_campaign_areas_advertising_campaigns_campaign_id] FOREIGN KEY ([campaign_id]) REFERENCES [advertising_campaigns] ([id]),
        CONSTRAINT [FK_advertising_campaign_areas_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE TABLE [advertising_campaign_categories] (
        [id] bigint NOT NULL IDENTITY,
        [campaign_id] bigint NOT NULL,
        [category_id] bigint NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_advertising_campaign_categories] PRIMARY KEY ([id]),
        CONSTRAINT [FK_advertising_campaign_categories_advertising_campaigns_campaign_id] FOREIGN KEY ([campaign_id]) REFERENCES [advertising_campaigns] ([id]),
        CONSTRAINT [FK_advertising_campaign_categories_service_categories_category_id] FOREIGN KEY ([category_id]) REFERENCES [service_categories] ([id]),
        CONSTRAINT [FK_advertising_campaign_categories_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE TABLE [advertising_creatives] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [campaign_id] bigint NOT NULL,
        [title] nvarchar(200) NOT NULL,
        [subtitle] nvarchar(300) NULL,
        [body_text] nvarchar(4000) NULL,
        [file_id] bigint NULL,
        [alt_text] nvarchar(300) NULL,
        [button_text] nvarchar(50) NULL,
        [destination_type_code] varchar(30) NULL,
        [destination_value] varchar(2000) NULL,
        [display_order] int NOT NULL DEFAULT 0,
        [status_code] varchar(20) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_advertising_creatives] PRIMARY KEY ([id]),
        CONSTRAINT [CK_advertising_creatives_destination] CHECK ([destination_type_code] IS NULL OR [destination_type_code] IN ('NONE','INTERNAL_PATH','EXTERNAL_URL')),
        CONSTRAINT [CK_advertising_creatives_order] CHECK ([display_order] >= 0),
        CONSTRAINT [CK_advertising_creatives_status] CHECK ([status_code] IN ('ACTIVE','INACTIVE')),
        CONSTRAINT [FK_advertising_creatives_advertising_campaigns_campaign_id] FOREIGN KEY ([campaign_id]) REFERENCES [advertising_campaigns] ([id]),
        CONSTRAINT [FK_advertising_creatives_files_file_id] FOREIGN KEY ([file_id]) REFERENCES [files] ([id]),
        CONSTRAINT [FK_advertising_creatives_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_advertising_creatives_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE TABLE [advertising_campaign_placements] (
        [id] bigint NOT NULL IDENTITY,
        [campaign_id] bigint NOT NULL,
        [placement_id] bigint NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_advertising_campaign_placements] PRIMARY KEY ([id]),
        CONSTRAINT [FK_advertising_campaign_placements_advertising_campaigns_campaign_id] FOREIGN KEY ([campaign_id]) REFERENCES [advertising_campaigns] ([id]),
        CONSTRAINT [FK_advertising_campaign_placements_advertising_placements_placement_id] FOREIGN KEY ([placement_id]) REFERENCES [advertising_placements] ([id]),
        CONSTRAINT [FK_advertising_campaign_placements_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE TABLE [managed_content_areas] (
        [id] bigint NOT NULL IDENTITY,
        [content_id] bigint NOT NULL,
        [administrative_area_id] bigint NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_managed_content_areas] PRIMARY KEY ([id]),
        CONSTRAINT [FK_managed_content_areas_administrative_areas_administrative_area_id] FOREIGN KEY ([administrative_area_id]) REFERENCES [administrative_areas] ([id]),
        CONSTRAINT [FK_managed_content_areas_managed_contents_content_id] FOREIGN KEY ([content_id]) REFERENCES [managed_contents] ([id]),
        CONSTRAINT [FK_managed_content_areas_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE TABLE [managed_content_categories] (
        [id] bigint NOT NULL IDENTITY,
        [content_id] bigint NOT NULL,
        [category_id] bigint NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_managed_content_categories] PRIMARY KEY ([id]),
        CONSTRAINT [FK_managed_content_categories_managed_contents_content_id] FOREIGN KEY ([content_id]) REFERENCES [managed_contents] ([id]),
        CONSTRAINT [FK_managed_content_categories_service_categories_category_id] FOREIGN KEY ([category_id]) REFERENCES [service_categories] ([id]),
        CONSTRAINT [FK_managed_content_categories_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE TABLE [managed_content_versions] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [content_id] bigint NOT NULL,
        [version_no] int NOT NULL,
        [title] nvarchar(300) NOT NULL,
        [body_text] nvarchar(max) NULL,
        [question_text] nvarchar(1000) NULL,
        [answer_text] nvarchar(max) NULL,
        [file_id] bigint NULL,
        [link_text] nvarchar(100) NULL,
        [destination_type_code] varchar(30) NOT NULL,
        [destination_value] varchar(2000) NULL,
        [change_reason] nvarchar(1000) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_managed_content_versions] PRIMARY KEY ([id]),
        CONSTRAINT [CK_managed_content_versions_destination] CHECK ([destination_type_code] IN ('NONE','INTERNAL_PATH','EXTERNAL_URL')),
        CONSTRAINT [CK_managed_content_versions_version] CHECK ([version_no] > 0),
        CONSTRAINT [FK_managed_content_versions_files_file_id] FOREIGN KEY ([file_id]) REFERENCES [files] ([id]),
        CONSTRAINT [FK_managed_content_versions_managed_contents_content_id] FOREIGN KEY ([content_id]) REFERENCES [managed_contents] ([id]),
        CONSTRAINT [FK_managed_content_versions_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE TABLE [advertising_events] (
        [id] bigint NOT NULL IDENTITY,
        [creative_id] bigint NOT NULL,
        [placement_id] bigint NOT NULL,
        [event_type_code] varchar(20) NOT NULL,
        [occurred_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_advertising_events] PRIMARY KEY ([id]),
        CONSTRAINT [CK_advertising_events_type] CHECK ([event_type_code] IN ('IMPRESSION','CLICK')),
        CONSTRAINT [FK_advertising_events_advertising_creatives_creative_id] FOREIGN KEY ([creative_id]) REFERENCES [advertising_creatives] ([id]),
        CONSTRAINT [FK_advertising_events_advertising_placements_placement_id] FOREIGN KEY ([placement_id]) REFERENCES [advertising_placements] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'code', N'created_at', N'created_by_user_id', N'description', N'is_active', N'name', N'public_id', N'route_hint', N'updated_at', N'updated_by_user_id') AND [object_id] = OBJECT_ID(N'[advertising_placements]'))
        SET IDENTITY_INSERT [advertising_placements] ON;
    EXEC(N'INSERT INTO [advertising_placements] ([id], [code], [created_at], [created_by_user_id], [description], [is_active], [name], [public_id], [route_hint], [updated_at], [updated_by_user_id])
    VALUES (CAST(1 AS bigint), ''CUSTOMER_HOME'', ''2026-08-10T00:00:00.0000000Z'', NULL, N''고객 역할 홈 화면'', CAST(1 AS bit), N''고객 홈'', ''11a10000-0000-0000-0000-000000000001'', ''/customer'', ''2026-08-10T00:00:00.0000000Z'', NULL),
    (CAST(2 AS bigint), ''PROVIDER_HOME'', ''2026-08-10T00:00:00.0000000Z'', NULL, N''공급자 역할 홈 화면'', CAST(1 AS bit), N''공급자 홈'', ''11a10000-0000-0000-0000-000000000002'', ''/provider'', ''2026-08-10T00:00:00.0000000Z'', NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'code', N'created_at', N'created_by_user_id', N'description', N'is_active', N'name', N'public_id', N'route_hint', N'updated_at', N'updated_by_user_id') AND [object_id] = OBJECT_ID(N'[advertising_placements]'))
        SET IDENTITY_INSERT [advertising_placements] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_advertising_campaign_areas_administrative_area_id] ON [advertising_campaign_areas] ([administrative_area_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_advertising_campaign_areas_campaign_id_administrative_area_id] ON [advertising_campaign_areas] ([campaign_id], [administrative_area_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_advertising_campaign_areas_created_by_user_id] ON [advertising_campaign_areas] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_advertising_campaign_categories_campaign_id_category_id] ON [advertising_campaign_categories] ([campaign_id], [category_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_advertising_campaign_categories_category_id] ON [advertising_campaign_categories] ([category_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_advertising_campaign_categories_created_by_user_id] ON [advertising_campaign_categories] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_advertising_campaign_placements_campaign_id_placement_id] ON [advertising_campaign_placements] ([campaign_id], [placement_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_advertising_campaign_placements_created_by_user_id] ON [advertising_campaign_placements] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_advertising_campaign_placements_placement_id] ON [advertising_campaign_placements] ([placement_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_advertising_campaigns_approved_by_user_id] ON [advertising_campaigns] ([approved_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_advertising_campaigns_audience_type_code_start_at] ON [advertising_campaigns] ([audience_type_code], [start_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_advertising_campaigns_campaign_name] ON [advertising_campaigns] ([campaign_name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_advertising_campaigns_created_by_user_id] ON [advertising_campaigns] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_advertising_campaigns_public_id] ON [advertising_campaigns] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_advertising_campaigns_status_code_review_status_code_start_at_end_at] ON [advertising_campaigns] ([status_code], [review_status_code], [start_at], [end_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_advertising_campaigns_updated_by_user_id] ON [advertising_campaigns] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_advertising_creatives_campaign_id_display_order] ON [advertising_creatives] ([campaign_id], [display_order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_advertising_creatives_created_by_user_id] ON [advertising_creatives] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_advertising_creatives_file_id] ON [advertising_creatives] ([file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_advertising_creatives_public_id] ON [advertising_creatives] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_advertising_creatives_updated_by_user_id] ON [advertising_creatives] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_advertising_events_creative_id_placement_id_event_type_code_occurred_at] ON [advertising_events] ([creative_id], [placement_id], [event_type_code], [occurred_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_advertising_events_placement_id] ON [advertising_events] ([placement_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_advertising_placements_code] ON [advertising_placements] ([code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_advertising_placements_created_by_user_id] ON [advertising_placements] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_advertising_placements_is_active_name] ON [advertising_placements] ([is_active], [name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_advertising_placements_public_id] ON [advertising_placements] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_advertising_placements_updated_by_user_id] ON [advertising_placements] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_managed_content_areas_administrative_area_id] ON [managed_content_areas] ([administrative_area_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_managed_content_areas_content_id_administrative_area_id] ON [managed_content_areas] ([content_id], [administrative_area_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_managed_content_areas_created_by_user_id] ON [managed_content_areas] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_managed_content_categories_category_id] ON [managed_content_categories] ([category_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_managed_content_categories_content_id_category_id] ON [managed_content_categories] ([content_id], [category_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_managed_content_categories_created_by_user_id] ON [managed_content_categories] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_managed_content_versions_content_id_version_no] ON [managed_content_versions] ([content_id], [version_no]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_managed_content_versions_created_by_user_id] ON [managed_content_versions] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_managed_content_versions_file_id] ON [managed_content_versions] ([file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_managed_content_versions_public_id] ON [managed_content_versions] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_managed_contents_approved_by_user_id] ON [managed_contents] ([approved_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_managed_contents_audience_type_code_start_at] ON [managed_contents] ([audience_type_code], [start_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_managed_contents_content_type_code_status_code_review_status_code_start_at_end_at] ON [managed_contents] ([content_type_code], [status_code], [review_status_code], [start_at], [end_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_managed_contents_created_by_user_id] ON [managed_contents] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_managed_contents_public_id] ON [managed_contents] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    CREATE INDEX [IX_managed_contents_updated_by_user_id] ON [managed_contents] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810090134_AddAdvertisingPromotionAndContentManagement'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810090134_AddAdvertisingPromotionAndContentManagement', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_cases] DROP CONSTRAINT [CK_after_service_cases_status];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_actions] DROP CONSTRAINT [CK_after_service_actions_from_status];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_actions] DROP CONSTRAINT [CK_after_service_actions_to_status];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_cases] ADD [assigned_admin_user_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_cases] ADD [converted_to_dispute_at] datetime2(7) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_cases] ADD [due_at] datetime2(7) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_cases] ADD [is_within_warranty] bit NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_cases] ADD [last_action_at] datetime2(7) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_cases] ADD [provider_confirmed_at] datetime2(7) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_cases] ADD [provider_response_text] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_cases] ADD [recurrence_occurred] bit NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_cases] ADD [reported_by_user_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_cases] ADD [request_details] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_cases] ADD [resolution_summary] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_cases] ADD [unresolved_reason] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_cases] ADD [visit_required] bit NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_cases] ADD [warranty_end_date] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_cases] ADD [warranty_start_date] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_actions] ADD [action_type_code] varchar(30) NOT NULL DEFAULT 'STATE_CHANGE';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_actions] ADD [materials_text] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_actions] ADD [performed_at] datetime2(7) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_actions] ADD [provider_profile_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_actions] ADD [reason] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_actions] ADD [recurrence_occurred] bit NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_actions] ADD [result_text] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_actions] ADD [scheduled_at] datetime2(7) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_actions] ADD [visit_occurred] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE TABLE [dispute_cases] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [transaction_id] bigint NOT NULL,
        [after_service_case_id] bigint NULL,
        [applicant_user_id] bigint NOT NULL,
        [counterparty_user_id] bigint NOT NULL,
        [assigned_admin_user_id] bigint NULL,
        [subject] nvarchar(200) NOT NULL,
        [description] nvarchar(max) NOT NULL,
        [status_code] varchar(30) NOT NULL DEFAULT 'OPEN',
        [received_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [due_at] datetime2(7) NULL,
        [resolved_at] datetime2(7) NULL,
        [closed_at] datetime2(7) NULL,
        [last_action_at] datetime2(7) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_dispute_cases] PRIMARY KEY ([id]),
        CONSTRAINT [CK_dispute_cases_status] CHECK ([status_code] IN ('OPEN','UNDER_REVIEW','WAITING_CUSTOMER','WAITING_PROVIDER','RESOLVED','CLOSED')),
        CONSTRAINT [FK_dispute_cases_after_service_cases_after_service_case_id] FOREIGN KEY ([after_service_case_id]) REFERENCES [after_service_cases] ([id]),
        CONSTRAINT [FK_dispute_cases_transactions_transaction_id] FOREIGN KEY ([transaction_id]) REFERENCES [transactions] ([id]),
        CONSTRAINT [FK_dispute_cases_users_applicant_user_id] FOREIGN KEY ([applicant_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_dispute_cases_users_assigned_admin_user_id] FOREIGN KEY ([assigned_admin_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_dispute_cases_users_counterparty_user_id] FOREIGN KEY ([counterparty_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_dispute_cases_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_dispute_cases_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE TABLE [dispute_actions] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [dispute_case_id] bigint NOT NULL,
        [action_type_code] varchar(30) NOT NULL,
        [from_status_code] varchar(30) NULL,
        [to_status_code] varchar(30) NULL,
        [action_note] nvarchar(2000) NULL,
        [reason] nvarchar(1000) NULL,
        [related_reference_type] varchar(50) NULL,
        [related_reference_public_id] uniqueidentifier NULL,
        [occurred_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [actor_user_id] bigint NOT NULL,
        [idempotency_key] varchar(100) NOT NULL,
        CONSTRAINT [PK_dispute_actions] PRIMARY KEY ([id]),
        CONSTRAINT [CK_dispute_actions_type] CHECK ([action_type_code] IN ('CREATED','STATUS_CHANGE','ASSIGNMENT','EVIDENCE_ADDED','EVIDENCE_REQUEST','NOTE','RESOLUTION','FEE_RESTORE_LINK')),
        CONSTRAINT [FK_dispute_actions_dispute_cases_dispute_case_id] FOREIGN KEY ([dispute_case_id]) REFERENCES [dispute_cases] ([id]),
        CONSTRAINT [FK_dispute_actions_users_actor_user_id] FOREIGN KEY ([actor_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE TABLE [dispute_evidence] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [dispute_case_id] bigint NOT NULL,
        [file_id] bigint NULL,
        [submitted_by_user_id] bigint NULL,
        [source_type_code] varchar(40) NOT NULL,
        [source_public_id] uniqueidentifier NULL,
        [description] nvarchar(1000) NULL,
        [status_code] varchar(20) NOT NULL DEFAULT 'ACTIVE',
        [submitted_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [withdrawn_at] datetime2(7) NULL,
        [withdrawn_by_user_id] bigint NULL,
        [withdrawal_reason] nvarchar(1000) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_dispute_evidence] PRIMARY KEY ([id]),
        CONSTRAINT [CK_dispute_evidence_source] CHECK ([file_id] IS NOT NULL OR [source_public_id] IS NOT NULL),
        CONSTRAINT [CK_dispute_evidence_status] CHECK ([status_code] IN ('ACTIVE','WITHDRAWN')),
        CONSTRAINT [FK_dispute_evidence_dispute_cases_dispute_case_id] FOREIGN KEY ([dispute_case_id]) REFERENCES [dispute_cases] ([id]),
        CONSTRAINT [FK_dispute_evidence_files_file_id] FOREIGN KEY ([file_id]) REFERENCES [files] ([id]),
        CONSTRAINT [FK_dispute_evidence_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_dispute_evidence_users_submitted_by_user_id] FOREIGN KEY ([submitted_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_dispute_evidence_users_withdrawn_by_user_id] FOREIGN KEY ([withdrawn_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE TABLE [dispute_resolutions] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [dispute_case_id] bigint NOT NULL,
        [version_no] int NOT NULL,
        [result_summary] nvarchar(1000) NOT NULL,
        [decision_details] nvarchar(max) NOT NULL,
        [basis_text] nvarchar(2000) NOT NULL,
        [follow_up_action] nvarchar(2000) NULL,
        [decided_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [decided_by_user_id] bigint NOT NULL,
        [is_current] bit NOT NULL DEFAULT CAST(1 AS bit),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_dispute_resolutions] PRIMARY KEY ([id]),
        CONSTRAINT [FK_dispute_resolutions_dispute_cases_dispute_case_id] FOREIGN KEY ([dispute_case_id]) REFERENCES [dispute_cases] ([id]),
        CONSTRAINT [FK_dispute_resolutions_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_dispute_resolutions_users_decided_by_user_id] FOREIGN KEY ([decided_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_after_service_cases_assigned_admin_user_id] ON [after_service_cases] ([assigned_admin_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_after_service_cases_due_at] ON [after_service_cases] ([due_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_after_service_cases_last_action_at] ON [after_service_cases] ([last_action_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_after_service_cases_reported_by_user_id] ON [after_service_cases] ([reported_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    EXEC(N'ALTER TABLE [after_service_cases] ADD CONSTRAINT [CK_after_service_cases_status] CHECK ([status_code] IN (''RECEIVED'',''PROVIDER_CONFIRMED'',''VISIT_SCHEDULED'',''IN_PROGRESS'',''RESOLVED'',''UNRESOLVED_CLOSED'',''CONVERTED_TO_DISPUTE''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_after_service_actions_action_type_code] ON [after_service_actions] ([action_type_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_after_service_actions_provider_profile_id] ON [after_service_actions] ([provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    EXEC(N'ALTER TABLE [after_service_actions] ADD CONSTRAINT [CK_after_service_actions_from_status] CHECK ([from_status_code] IS NULL OR [from_status_code] IN (''RECEIVED'',''PROVIDER_CONFIRMED'',''VISIT_SCHEDULED'',''IN_PROGRESS'',''RESOLVED'',''UNRESOLVED_CLOSED'',''CONVERTED_TO_DISPUTE''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    EXEC(N'ALTER TABLE [after_service_actions] ADD CONSTRAINT [CK_after_service_actions_to_status] CHECK ([to_status_code] IN (''RECEIVED'',''PROVIDER_CONFIRMED'',''VISIT_SCHEDULED'',''IN_PROGRESS'',''RESOLVED'',''UNRESOLVED_CLOSED'',''CONVERTED_TO_DISPUTE''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    EXEC(N'ALTER TABLE [after_service_actions] ADD CONSTRAINT [CK_after_service_actions_type] CHECK ([action_type_code] IN (''RECEIVED'',''PROVIDER_CONFIRMATION'',''VISIT_SCHEDULED'',''VISIT'',''REVISIT'',''TREATMENT'',''STATE_CHANGE'',''RESOLUTION'',''UNRESOLVED_CLOSURE'',''DISPUTE_CONVERSION'',''ADMIN_OVERRIDE''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_actions_action_type_code] ON [dispute_actions] ([action_type_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_actions_actor_user_id] ON [dispute_actions] ([actor_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_actions_dispute_case_id] ON [dispute_actions] ([dispute_case_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_actions_dispute_case_id_occurred_at] ON [dispute_actions] ([dispute_case_id], [occurred_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_dispute_actions_idempotency_key] ON [dispute_actions] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_actions_occurred_at] ON [dispute_actions] ([occurred_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_dispute_actions_public_id] ON [dispute_actions] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_dispute_cases_after_service_case_id] ON [dispute_cases] ([after_service_case_id]) WHERE [after_service_case_id] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_cases_applicant_user_id] ON [dispute_cases] ([applicant_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_cases_assigned_admin_user_id] ON [dispute_cases] ([assigned_admin_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_cases_counterparty_user_id] ON [dispute_cases] ([counterparty_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_cases_created_by_user_id] ON [dispute_cases] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_cases_due_at] ON [dispute_cases] ([due_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_dispute_cases_public_id] ON [dispute_cases] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_cases_received_at] ON [dispute_cases] ([received_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_cases_status_code] ON [dispute_cases] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_cases_status_code_received_at] ON [dispute_cases] ([status_code], [received_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_cases_transaction_id] ON [dispute_cases] ([transaction_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_cases_updated_by_user_id] ON [dispute_cases] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_evidence_created_by_user_id] ON [dispute_evidence] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_evidence_dispute_case_id] ON [dispute_evidence] ([dispute_case_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_dispute_evidence_dispute_case_id_source_type_code_source_public_id] ON [dispute_evidence] ([dispute_case_id], [source_type_code], [source_public_id]) WHERE [source_public_id] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_evidence_file_id] ON [dispute_evidence] ([file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_dispute_evidence_public_id] ON [dispute_evidence] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_evidence_source_public_id] ON [dispute_evidence] ([source_public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_evidence_source_type_code] ON [dispute_evidence] ([source_type_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_evidence_status_code] ON [dispute_evidence] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_evidence_submitted_by_user_id] ON [dispute_evidence] ([submitted_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_evidence_withdrawn_by_user_id] ON [dispute_evidence] ([withdrawn_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_resolutions_created_by_user_id] ON [dispute_resolutions] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE INDEX [IX_dispute_resolutions_decided_by_user_id] ON [dispute_resolutions] ([decided_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_dispute_resolutions_dispute_case_id] ON [dispute_resolutions] ([dispute_case_id]) WHERE [is_current] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_dispute_resolutions_dispute_case_id_version_no] ON [dispute_resolutions] ([dispute_case_id], [version_no]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_dispute_resolutions_public_id] ON [dispute_resolutions] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_actions] ADD CONSTRAINT [FK_after_service_actions_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_cases] ADD CONSTRAINT [FK_after_service_cases_users_assigned_admin_user_id] FOREIGN KEY ([assigned_admin_user_id]) REFERENCES [users] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    ALTER TABLE [after_service_cases] ADD CONSTRAINT [FK_after_service_cases_users_reported_by_user_id] FOREIGN KEY ([reported_by_user_id]) REFERENCES [users] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810093206_ImplementAfterServiceAndDisputeManagement'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810093206_ImplementAfterServiceAndDisputeManagement', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810121158_AddProviderTrustManagementFoundation'
)
BEGIN
    CREATE TABLE [trust_policies] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [policy_version] varchar(50) NOT NULL,
        [policy_name] nvarchar(200) NOT NULL,
        [target_type_code] varchar(30) NOT NULL,
        [scope_type_code] varchar(30) NOT NULL,
        [status_code] varchar(20) NOT NULL,
        [rules_json] nvarchar(max) NOT NULL,
        [effective_from] datetime2(7) NOT NULL,
        [effective_to] datetime2(7) NULL,
        [approved_at] datetime2(7) NULL,
        [approved_by_user_id] bigint NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_trust_policies] PRIMARY KEY ([id]),
        CONSTRAINT [CK_trust_policies_period] CHECK ([effective_to] IS NULL OR [effective_to] > [effective_from]),
        CONSTRAINT [CK_trust_policies_rules_json] CHECK (ISJSON([rules_json]) = 1),
        CONSTRAINT [FK_trust_policies_users_approved_by_user_id] FOREIGN KEY ([approved_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_trust_policies_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_trust_policies_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810121158_AddProviderTrustManagementFoundation'
)
BEGIN
    CREATE TABLE [trust_score_events] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [trust_policy_id] bigint NULL,
        [event_type_code] varchar(50) NOT NULL,
        [source_type_code] varchar(50) NOT NULL,
        [source_public_id] uniqueidentifier NULL,
        [idempotency_key] varchar(150) NOT NULL,
        [score_before] decimal(9,4) NULL,
        [score_delta] decimal(9,4) NULL,
        [score_after] decimal(9,4) NULL,
        [grade_before] varchar(30) NULL,
        [grade_after] varchar(30) NULL,
        [decision_code] varchar(30) NULL,
        [reason_text] nvarchar(2000) NULL,
        [policy_snapshot_json] nvarchar(max) NULL,
        [source_snapshot_json] nvarchar(max) NULL,
        [occurred_at] datetime2(7) NOT NULL,
        [processed_at] datetime2(7) NULL,
        [processed_by_user_id] bigint NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_trust_score_events] PRIMARY KEY ([id]),
        CONSTRAINT [CK_trust_score_events_policy_json] CHECK ([policy_snapshot_json] IS NULL OR ISJSON([policy_snapshot_json]) = 1),
        CONSTRAINT [CK_trust_score_events_score_range] CHECK (([score_before] IS NULL OR ([score_before] >= 0 AND [score_before] <= 100)) AND ([score_after] IS NULL OR ([score_after] >= 0 AND [score_after] <= 100))),
        CONSTRAINT [CK_trust_score_events_source_json] CHECK ([source_snapshot_json] IS NULL OR ISJSON([source_snapshot_json]) = 1),
        CONSTRAINT [FK_trust_score_events_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_trust_score_events_trust_policies_trust_policy_id] FOREIGN KEY ([trust_policy_id]) REFERENCES [trust_policies] ([id]),
        CONSTRAINT [FK_trust_score_events_users_processed_by_user_id] FOREIGN KEY ([processed_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810121158_AddProviderTrustManagementFoundation'
)
BEGIN
    CREATE TABLE [provider_trust_score_current] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [trust_policy_id] bigint NULL,
        [score] decimal(9,4) NULL,
        [grade_code] varchar(30) NULL,
        [evaluation_status_code] varchar(30) NOT NULL DEFAULT 'NEW_OR_EVALUATING',
        [calculated_at] datetime2(7) NULL,
        [last_event_id] bigint NULL,
        [source_type_code] varchar(30) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_provider_trust_score_current] PRIMARY KEY ([id]),
        CONSTRAINT [CK_provider_trust_score_current_score] CHECK ([score] IS NULL OR ([score] >= 0 AND [score] <= 100)),
        CONSTRAINT [CK_provider_trust_score_current_state] CHECK (([evaluation_status_code] = 'NEW_OR_EVALUATING' AND [score] IS NULL AND [grade_code] IS NULL) OR [evaluation_status_code] <> 'NEW_OR_EVALUATING'),
        CONSTRAINT [CK_provider_trust_score_current_status] CHECK ([evaluation_status_code] IN ('NEW_OR_EVALUATING','CALCULATED','LEGACY_UNKNOWN_POLICY')),
        CONSTRAINT [FK_provider_trust_score_current_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_provider_trust_score_current_trust_policies_trust_policy_id] FOREIGN KEY ([trust_policy_id]) REFERENCES [trust_policies] ([id]),
        CONSTRAINT [FK_provider_trust_score_current_trust_score_events_last_event_id] FOREIGN KEY ([last_event_id]) REFERENCES [trust_score_events] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810121158_AddProviderTrustManagementFoundation'
)
BEGIN
    CREATE INDEX [IX_provider_trust_score_current_evaluation_status_code_score] ON [provider_trust_score_current] ([evaluation_status_code], [score] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810121158_AddProviderTrustManagementFoundation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_provider_trust_score_current_last_event_id] ON [provider_trust_score_current] ([last_event_id]) WHERE [last_event_id] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810121158_AddProviderTrustManagementFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_trust_score_current_provider_profile_id] ON [provider_trust_score_current] ([provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810121158_AddProviderTrustManagementFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_trust_score_current_public_id] ON [provider_trust_score_current] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810121158_AddProviderTrustManagementFoundation'
)
BEGIN
    CREATE INDEX [IX_provider_trust_score_current_trust_policy_id] ON [provider_trust_score_current] ([trust_policy_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810121158_AddProviderTrustManagementFoundation'
)
BEGIN
    CREATE INDEX [IX_trust_policies_approved_by_user_id] ON [trust_policies] ([approved_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810121158_AddProviderTrustManagementFoundation'
)
BEGIN
    CREATE INDEX [IX_trust_policies_created_by_user_id] ON [trust_policies] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810121158_AddProviderTrustManagementFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_trust_policies_policy_version] ON [trust_policies] ([policy_version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810121158_AddProviderTrustManagementFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_trust_policies_public_id] ON [trust_policies] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810121158_AddProviderTrustManagementFoundation'
)
BEGIN
    CREATE INDEX [IX_trust_policies_target_type_code_scope_type_code_status_code_effective_from] ON [trust_policies] ([target_type_code], [scope_type_code], [status_code], [effective_from]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810121158_AddProviderTrustManagementFoundation'
)
BEGIN
    CREATE INDEX [IX_trust_policies_updated_by_user_id] ON [trust_policies] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810121158_AddProviderTrustManagementFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_trust_score_events_idempotency_key] ON [trust_score_events] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810121158_AddProviderTrustManagementFoundation'
)
BEGIN
    CREATE INDEX [IX_trust_score_events_processed_by_user_id] ON [trust_score_events] ([processed_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810121158_AddProviderTrustManagementFoundation'
)
BEGIN
    CREATE INDEX [IX_trust_score_events_provider_profile_id_occurred_at] ON [trust_score_events] ([provider_profile_id], [occurred_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810121158_AddProviderTrustManagementFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_trust_score_events_public_id] ON [trust_score_events] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810121158_AddProviderTrustManagementFoundation'
)
BEGIN
    CREATE INDEX [IX_trust_score_events_source_type_code_source_public_id] ON [trust_score_events] ([source_type_code], [source_public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810121158_AddProviderTrustManagementFoundation'
)
BEGIN
    CREATE INDEX [IX_trust_score_events_trust_policy_id] ON [trust_score_events] ([trust_policy_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810121158_AddProviderTrustManagementFoundation'
)
BEGIN
    INSERT INTO provider_trust_score_current
        (public_id, provider_profile_id, trust_policy_id, score, grade_code, evaluation_status_code,
         calculated_at, last_event_id, source_type_code, created_at, updated_at)
    SELECT NEWID(), p.id, NULL, p.trust_score, NULL,
           CASE WHEN p.trust_score IS NULL THEN 'NEW_OR_EVALUATING' ELSE 'LEGACY_UNKNOWN_POLICY' END,
           NULL, NULL, 'LEGACY_PROFILE', SYSUTCDATETIME(), SYSUTCDATETIME()
    FROM provider_profiles p;

    IF (SELECT COUNT_BIG(*) FROM provider_trust_score_current) <> (SELECT COUNT_BIG(*) FROM provider_profiles)
        THROW 51000, '공급자 신뢰도 Current 초기화 건수가 일치하지 않습니다.', 1;

    IF EXISTS (
        SELECT 1
        FROM provider_profiles p
        JOIN provider_trust_score_current c ON c.provider_profile_id = p.id
        WHERE (p.trust_score <> c.score)
           OR (p.trust_score IS NULL AND c.score IS NOT NULL)
           OR (p.trust_score IS NOT NULL AND c.score IS NULL))
        THROW 51000, '기존 공급자 신뢰점수 보존 검증에 실패했습니다.', 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810121158_AddProviderTrustManagementFoundation'
)
BEGIN
    CREATE TRIGGER TR_trust_score_events_append_only
    ON trust_score_events
    AFTER UPDATE, DELETE
    AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51000, '신뢰도 변경이력은 수정하거나 삭제할 수 없습니다.', 1;
    END;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810121158_AddProviderTrustManagementFoundation'
)
BEGIN
    CREATE TRIGGER TR_trust_policies_no_overlapping_period
    ON trust_policies
    AFTER INSERT, UPDATE
    AS
    BEGIN
        SET NOCOUNT ON;
        IF EXISTS (
            SELECT 1
            FROM inserted i
            JOIN trust_policies p
              ON p.id <> i.id
             AND p.target_type_code = i.target_type_code
             AND p.scope_type_code = i.scope_type_code
             AND i.effective_from < ISNULL(p.effective_to, CONVERT(datetime2, '9999-12-31'))
             AND p.effective_from < ISNULL(i.effective_to, CONVERT(datetime2, '9999-12-31')))
            THROW 51000, '동일 대상과 범위의 신뢰도 정책 적용기간은 중복될 수 없습니다.', 1;
    END;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810121158_AddProviderTrustManagementFoundation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810121158_AddProviderTrustManagementFoundation', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    ALTER TABLE [files] DROP CONSTRAINT [CK_files_purpose];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    CREATE TABLE [review_rating_items] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [code] varchar(50) NOT NULL,
        [name] nvarchar(100) NOT NULL,
        [description] nvarchar(1000) NULL,
        [min_value] decimal(9,4) NOT NULL,
        [max_value] decimal(9,4) NOT NULL,
        [display_order] int NOT NULL,
        [is_required] bit NOT NULL DEFAULT CAST(1 AS bit),
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [effective_from] datetime2(7) NULL,
        [effective_to] datetime2(7) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_review_rating_items] PRIMARY KEY ([id]),
        CONSTRAINT [CK_review_rating_items_period] CHECK ([effective_to] IS NULL OR [effective_from] IS NULL OR [effective_to] > [effective_from]),
        CONSTRAINT [CK_review_rating_items_range] CHECK ([max_value] > [min_value]),
        CONSTRAINT [FK_review_rating_items_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_review_rating_items_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    CREATE TABLE [reviews] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [transaction_id] bigint NOT NULL,
        [customer_profile_id] bigint NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [body_text] nvarchar(4000) NOT NULL,
        [overall_rating] decimal(9,4) NULL,
        [verification_status_code] varchar(30) NOT NULL,
        [visibility_status_code] varchar(20) NOT NULL,
        [idempotency_key] varchar(150) NOT NULL,
        [submitted_at] datetime2(7) NOT NULL,
        [published_at] datetime2(7) NULL,
        [hidden_at] datetime2(7) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_reviews] PRIMARY KEY ([id]),
        CONSTRAINT [CK_reviews_verification] CHECK ([verification_status_code] = 'VERIFIED_TRANSACTION'),
        CONSTRAINT [CK_reviews_visibility] CHECK ([visibility_status_code] IN ('PUBLIC','HIDDEN')),
        CONSTRAINT [FK_reviews_customer_profiles_customer_profile_id] FOREIGN KEY ([customer_profile_id]) REFERENCES [customer_profiles] ([id]),
        CONSTRAINT [FK_reviews_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_reviews_transactions_transaction_id] FOREIGN KEY ([transaction_id]) REFERENCES [transactions] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    CREATE TABLE [review_files] (
        [id] bigint NOT NULL IDENTITY,
        [review_id] bigint NOT NULL,
        [file_id] bigint NOT NULL,
        [display_order] int NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_review_files] PRIMARY KEY ([id]),
        CONSTRAINT [FK_review_files_files_file_id] FOREIGN KEY ([file_id]) REFERENCES [files] ([id]),
        CONSTRAINT [FK_review_files_reviews_review_id] FOREIGN KEY ([review_id]) REFERENCES [reviews] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    CREATE TABLE [review_provider_replies] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [review_id] bigint NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [body_text] nvarchar(2000) NOT NULL,
        [idempotency_key] varchar(150) NOT NULL,
        [submitted_at] datetime2(7) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_review_provider_replies] PRIMARY KEY ([id]),
        CONSTRAINT [FK_review_provider_replies_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_review_provider_replies_reviews_review_id] FOREIGN KEY ([review_id]) REFERENCES [reviews] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    CREATE TABLE [review_ratings] (
        [id] bigint NOT NULL IDENTITY,
        [review_id] bigint NOT NULL,
        [rating_item_id] bigint NOT NULL,
        [rating_value] decimal(9,4) NOT NULL,
        [display_order] int NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_review_ratings] PRIMARY KEY ([id]),
        CONSTRAINT [FK_review_ratings_review_rating_items_rating_item_id] FOREIGN KEY ([rating_item_id]) REFERENCES [review_rating_items] ([id]),
        CONSTRAINT [FK_review_ratings_reviews_review_id] FOREIGN KEY ([review_id]) REFERENCES [reviews] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    EXEC(N'ALTER TABLE [files] ADD CONSTRAINT [CK_files_purpose] CHECK ([purpose_code] IN (''PROVIDER_DOCUMENT'',''REQUEST_ANSWER'',''COMPLETION_EVIDENCE'',''AFTER_SERVICE'',''REVIEW''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    CREATE UNIQUE INDEX [IX_review_files_file_id] ON [review_files] ([file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    CREATE UNIQUE INDEX [IX_review_files_review_id_file_id] ON [review_files] ([review_id], [file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    CREATE UNIQUE INDEX [IX_review_provider_replies_idempotency_key] ON [review_provider_replies] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    CREATE INDEX [IX_review_provider_replies_provider_profile_id] ON [review_provider_replies] ([provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    CREATE UNIQUE INDEX [IX_review_provider_replies_public_id] ON [review_provider_replies] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    CREATE UNIQUE INDEX [IX_review_provider_replies_review_id] ON [review_provider_replies] ([review_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    CREATE UNIQUE INDEX [IX_review_rating_items_code] ON [review_rating_items] ([code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    CREATE INDEX [IX_review_rating_items_created_by_user_id] ON [review_rating_items] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    CREATE INDEX [IX_review_rating_items_is_active_display_order] ON [review_rating_items] ([is_active], [display_order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    CREATE UNIQUE INDEX [IX_review_rating_items_public_id] ON [review_rating_items] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    CREATE INDEX [IX_review_rating_items_updated_by_user_id] ON [review_rating_items] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    CREATE INDEX [IX_review_ratings_rating_item_id] ON [review_ratings] ([rating_item_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    CREATE UNIQUE INDEX [IX_review_ratings_review_id_rating_item_id] ON [review_ratings] ([review_id], [rating_item_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    CREATE INDEX [IX_reviews_customer_profile_id] ON [reviews] ([customer_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    CREATE UNIQUE INDEX [IX_reviews_idempotency_key] ON [reviews] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    CREATE INDEX [IX_reviews_provider_profile_id_visibility_status_code_submitted_at] ON [reviews] ([provider_profile_id], [visibility_status_code], [submitted_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    CREATE UNIQUE INDEX [IX_reviews_public_id] ON [reviews] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    CREATE UNIQUE INDEX [IX_reviews_transaction_id] ON [reviews] ([transaction_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810123807_AddCustomerReviewsAndRatings'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810123807_AddCustomerReviewsAndRatings', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    ALTER TABLE [files] DROP CONSTRAINT [CK_files_purpose];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    ALTER TABLE [dispute_resolutions] ADD [liability_type_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE TABLE [dispute_liability_types] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [code] varchar(50) NOT NULL,
        [name] nvarchar(100) NOT NULL,
        [description] nvarchar(1000) NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [display_order] int NOT NULL DEFAULT 0,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_dispute_liability_types] PRIMARY KEY ([id]),
        CONSTRAINT [FK_dispute_liability_types_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_dispute_liability_types_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE TABLE [report_types] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [code] varchar(50) NOT NULL,
        [name] nvarchar(100) NOT NULL,
        [description] nvarchar(1000) NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [display_order] int NOT NULL DEFAULT 0,
        [effective_from] datetime2(7) NULL,
        [effective_to] datetime2(7) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_report_types] PRIMARY KEY ([id]),
        CONSTRAINT [CK_report_types_period] CHECK ([effective_to] IS NULL OR [effective_from] IS NULL OR [effective_to] > [effective_from]),
        CONSTRAINT [FK_report_types_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_report_types_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE TABLE [sanction_types] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [code] varchar(50) NOT NULL,
        [name] nvarchar(100) NOT NULL,
        [description] nvarchar(1000) NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [display_order] int NOT NULL DEFAULT 0,
        [effective_from] datetime2(7) NULL,
        [effective_to] datetime2(7) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_sanction_types] PRIMARY KEY ([id]),
        CONSTRAINT [CK_sanction_types_period] CHECK ([effective_to] IS NULL OR [effective_from] IS NULL OR [effective_to] > [effective_from]),
        CONSTRAINT [FK_sanction_types_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_sanction_types_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE TABLE [reports] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [reporter_user_id] bigint NOT NULL,
        [reported_user_id] bigint NULL,
        [report_type_id] bigint NOT NULL,
        [service_request_id] bigint NULL,
        [transaction_id] bigint NULL,
        [review_id] bigint NULL,
        [after_service_case_id] bigint NULL,
        [dispute_case_id] bigint NULL,
        [description] nvarchar(4000) NOT NULL,
        [status_code] varchar(30) NOT NULL DEFAULT 'RECEIVED',
        [assigned_admin_user_id] bigint NULL,
        [result_summary] nvarchar(2000) NULL,
        [received_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [resolved_at] datetime2(7) NULL,
        [cancelled_at] datetime2(7) NULL,
        [idempotency_key] varchar(150) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_reports] PRIMARY KEY ([id]),
        CONSTRAINT [CK_reports_status] CHECK ([status_code] IN ('RECEIVED','UNDER_REVIEW','EVIDENCE_REQUESTED','RESOLVED','CANCELLED')),
        CONSTRAINT [CK_reports_target] CHECK ([reported_user_id] IS NOT NULL OR [service_request_id] IS NOT NULL OR [transaction_id] IS NOT NULL OR [review_id] IS NOT NULL OR [after_service_case_id] IS NOT NULL OR [dispute_case_id] IS NOT NULL),
        CONSTRAINT [FK_reports_after_service_cases_after_service_case_id] FOREIGN KEY ([after_service_case_id]) REFERENCES [after_service_cases] ([id]),
        CONSTRAINT [FK_reports_dispute_cases_dispute_case_id] FOREIGN KEY ([dispute_case_id]) REFERENCES [dispute_cases] ([id]),
        CONSTRAINT [FK_reports_report_types_report_type_id] FOREIGN KEY ([report_type_id]) REFERENCES [report_types] ([id]),
        CONSTRAINT [FK_reports_reviews_review_id] FOREIGN KEY ([review_id]) REFERENCES [reviews] ([id]),
        CONSTRAINT [FK_reports_service_requests_service_request_id] FOREIGN KEY ([service_request_id]) REFERENCES [service_requests] ([id]),
        CONSTRAINT [FK_reports_transactions_transaction_id] FOREIGN KEY ([transaction_id]) REFERENCES [transactions] ([id]),
        CONSTRAINT [FK_reports_users_assigned_admin_user_id] FOREIGN KEY ([assigned_admin_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_reports_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_reports_users_reported_user_id] FOREIGN KEY ([reported_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_reports_users_reporter_user_id] FOREIGN KEY ([reporter_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_reports_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE TABLE [sanctions] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [target_user_id] bigint NOT NULL,
        [target_role_id] bigint NULL,
        [provider_profile_id] bigint NULL,
        [provider_service_category_id] bigint NULL,
        [sanction_type_id] bigint NOT NULL,
        [status_code] varchar(20) NOT NULL DEFAULT 'DECIDED',
        [reason] nvarchar(2000) NOT NULL,
        [start_at] datetime2(7) NOT NULL,
        [end_at] datetime2(7) NULL,
        [decided_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [decided_by_user_id] bigint NOT NULL,
        [released_at] datetime2(7) NULL,
        [released_by_user_id] bigint NULL,
        [release_reason] nvarchar(1000) NULL,
        [idempotency_key] varchar(150) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_sanctions] PRIMARY KEY ([id]),
        CONSTRAINT [CK_sanctions_period] CHECK ([end_at] IS NULL OR [end_at] > [start_at]),
        CONSTRAINT [CK_sanctions_scope] CHECK ([provider_service_category_id] IS NULL OR [provider_profile_id] IS NOT NULL),
        CONSTRAINT [CK_sanctions_status] CHECK ([status_code] IN ('DECIDED','ACTIVE','RELEASED','EXPIRED','CANCELLED')),
        CONSTRAINT [FK_sanctions_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_sanctions_provider_service_categories_provider_service_category_id] FOREIGN KEY ([provider_service_category_id]) REFERENCES [provider_service_categories] ([id]),
        CONSTRAINT [FK_sanctions_roles_target_role_id] FOREIGN KEY ([target_role_id]) REFERENCES [roles] ([id]),
        CONSTRAINT [FK_sanctions_sanction_types_sanction_type_id] FOREIGN KEY ([sanction_type_id]) REFERENCES [sanction_types] ([id]),
        CONSTRAINT [FK_sanctions_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_sanctions_users_decided_by_user_id] FOREIGN KEY ([decided_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_sanctions_users_released_by_user_id] FOREIGN KEY ([released_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_sanctions_users_target_user_id] FOREIGN KEY ([target_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_sanctions_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE TABLE [report_actions] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [report_id] bigint NOT NULL,
        [action_type_code] varchar(30) NOT NULL,
        [from_status_code] varchar(30) NULL,
        [to_status_code] varchar(30) NULL,
        [actor_user_id] bigint NOT NULL,
        [reason] nvarchar(1000) NULL,
        [note] nvarchar(2000) NULL,
        [occurred_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [idempotency_key] varchar(150) NOT NULL,
        CONSTRAINT [PK_report_actions] PRIMARY KEY ([id]),
        CONSTRAINT [CK_report_actions_type] CHECK ([action_type_code] IN ('CREATED','ASSIGNED','STATUS_CHANGED','EVIDENCE_REQUESTED','EVIDENCE_ADDED','RESOLVED','CANCELLED')),
        CONSTRAINT [FK_report_actions_reports_report_id] FOREIGN KEY ([report_id]) REFERENCES [reports] ([id]),
        CONSTRAINT [FK_report_actions_users_actor_user_id] FOREIGN KEY ([actor_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE TABLE [report_evidence] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [report_id] bigint NOT NULL,
        [file_id] bigint NOT NULL,
        [submitted_by_user_id] bigint NOT NULL,
        [description] nvarchar(1000) NULL,
        [status_code] varchar(20) NOT NULL DEFAULT 'ACTIVE',
        [submitted_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [withdrawn_at] datetime2(7) NULL,
        [withdrawn_by_user_id] bigint NULL,
        [withdrawal_reason] nvarchar(1000) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_report_evidence] PRIMARY KEY ([id]),
        CONSTRAINT [CK_report_evidence_status] CHECK ([status_code] IN ('ACTIVE','WITHDRAWN')),
        CONSTRAINT [FK_report_evidence_files_file_id] FOREIGN KEY ([file_id]) REFERENCES [files] ([id]),
        CONSTRAINT [FK_report_evidence_reports_report_id] FOREIGN KEY ([report_id]) REFERENCES [reports] ([id]),
        CONSTRAINT [FK_report_evidence_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_report_evidence_users_submitted_by_user_id] FOREIGN KEY ([submitted_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_report_evidence_users_withdrawn_by_user_id] FOREIGN KEY ([withdrawn_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE TABLE [sanction_appeals] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [sanction_id] bigint NOT NULL,
        [applicant_user_id] bigint NOT NULL,
        [previous_appeal_id] bigint NULL,
        [statement] nvarchar(4000) NOT NULL,
        [status_code] varchar(20) NOT NULL DEFAULT 'RECEIVED',
        [assigned_admin_user_id] bigint NULL,
        [decision_code] varchar(20) NULL,
        [decision_reason] nvarchar(2000) NULL,
        [submitted_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [decided_at] datetime2(7) NULL,
        [idempotency_key] varchar(150) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_sanction_appeals] PRIMARY KEY ([id]),
        CONSTRAINT [CK_sanction_appeals_decision] CHECK ([decision_code] IS NULL OR [decision_code] IN ('APPROVED','REJECTED')),
        CONSTRAINT [CK_sanction_appeals_status] CHECK ([status_code] IN ('RECEIVED','UNDER_REVIEW','APPROVED','REJECTED','CLOSED')),
        CONSTRAINT [FK_sanction_appeals_sanction_appeals_previous_appeal_id] FOREIGN KEY ([previous_appeal_id]) REFERENCES [sanction_appeals] ([id]),
        CONSTRAINT [FK_sanction_appeals_sanctions_sanction_id] FOREIGN KEY ([sanction_id]) REFERENCES [sanctions] ([id]),
        CONSTRAINT [FK_sanction_appeals_users_applicant_user_id] FOREIGN KEY ([applicant_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_sanction_appeals_users_assigned_admin_user_id] FOREIGN KEY ([assigned_admin_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_sanction_appeals_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_sanction_appeals_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE TABLE [sanction_events] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [sanction_id] bigint NOT NULL,
        [action_type_code] varchar(30) NOT NULL,
        [from_status_code] varchar(20) NULL,
        [to_status_code] varchar(20) NOT NULL,
        [actor_user_id] bigint NOT NULL,
        [reason] nvarchar(1000) NOT NULL,
        [snapshot_json] nvarchar(max) NOT NULL,
        [occurred_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [idempotency_key] varchar(150) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_sanction_events] PRIMARY KEY ([id]),
        CONSTRAINT [CK_sanction_events_action] CHECK ([action_type_code] IN ('DECIDED','ACTIVATED','STATUS_CHANGED','RELEASED','EXPIRED','CANCELLED')),
        CONSTRAINT [CK_sanction_events_snapshot] CHECK (ISJSON([snapshot_json]) = 1),
        CONSTRAINT [FK_sanction_events_sanctions_sanction_id] FOREIGN KEY ([sanction_id]) REFERENCES [sanctions] ([id]),
        CONSTRAINT [FK_sanction_events_users_actor_user_id] FOREIGN KEY ([actor_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE TABLE [sanction_sources] (
        [id] bigint NOT NULL IDENTITY,
        [sanction_id] bigint NOT NULL,
        [report_id] bigint NULL,
        [dispute_case_id] bigint NULL,
        [review_id] bigint NULL,
        [after_service_case_id] bigint NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_sanction_sources] PRIMARY KEY ([id]),
        CONSTRAINT [CK_sanction_sources_one_source] CHECK ((CASE WHEN [report_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [dispute_case_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [review_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [after_service_case_id] IS NULL THEN 0 ELSE 1 END) = 1),
        CONSTRAINT [FK_sanction_sources_after_service_cases_after_service_case_id] FOREIGN KEY ([after_service_case_id]) REFERENCES [after_service_cases] ([id]),
        CONSTRAINT [FK_sanction_sources_dispute_cases_dispute_case_id] FOREIGN KEY ([dispute_case_id]) REFERENCES [dispute_cases] ([id]),
        CONSTRAINT [FK_sanction_sources_reports_report_id] FOREIGN KEY ([report_id]) REFERENCES [reports] ([id]),
        CONSTRAINT [FK_sanction_sources_reviews_review_id] FOREIGN KEY ([review_id]) REFERENCES [reviews] ([id]),
        CONSTRAINT [FK_sanction_sources_sanctions_sanction_id] FOREIGN KEY ([sanction_id]) REFERENCES [sanctions] ([id]),
        CONSTRAINT [FK_sanction_sources_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE TABLE [sanction_appeal_actions] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [sanction_appeal_id] bigint NOT NULL,
        [action_type_code] varchar(30) NOT NULL,
        [from_status_code] varchar(20) NULL,
        [to_status_code] varchar(20) NOT NULL,
        [actor_user_id] bigint NOT NULL,
        [reason] nvarchar(1000) NOT NULL,
        [occurred_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [idempotency_key] varchar(150) NOT NULL,
        CONSTRAINT [PK_sanction_appeal_actions] PRIMARY KEY ([id]),
        CONSTRAINT [CK_sanction_appeal_actions_type] CHECK ([action_type_code] IN ('CREATED','ASSIGNED','REVIEW_STARTED','DECIDED','CLOSED','EVIDENCE_ADDED')),
        CONSTRAINT [FK_sanction_appeal_actions_sanction_appeals_sanction_appeal_id] FOREIGN KEY ([sanction_appeal_id]) REFERENCES [sanction_appeals] ([id]),
        CONSTRAINT [FK_sanction_appeal_actions_users_actor_user_id] FOREIGN KEY ([actor_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE TABLE [sanction_appeal_evidence] (
        [id] bigint NOT NULL IDENTITY,
        [sanction_appeal_id] bigint NOT NULL,
        [file_id] bigint NOT NULL,
        [submitted_by_user_id] bigint NOT NULL,
        [description] nvarchar(1000) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_sanction_appeal_evidence] PRIMARY KEY ([id]),
        CONSTRAINT [FK_sanction_appeal_evidence_files_file_id] FOREIGN KEY ([file_id]) REFERENCES [files] ([id]),
        CONSTRAINT [FK_sanction_appeal_evidence_sanction_appeals_sanction_appeal_id] FOREIGN KEY ([sanction_appeal_id]) REFERENCES [sanction_appeals] ([id]),
        CONSTRAINT [FK_sanction_appeal_evidence_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_sanction_appeal_evidence_users_submitted_by_user_id] FOREIGN KEY ([submitted_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    EXEC(N'ALTER TABLE [files] ADD CONSTRAINT [CK_files_purpose] CHECK ([purpose_code] IN (''PROVIDER_DOCUMENT'',''REQUEST_ANSWER'',''COMPLETION_EVIDENCE'',''AFTER_SERVICE'',''REVIEW'',''REPORT_EVIDENCE'',''SANCTION_APPEAL_EVIDENCE''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_dispute_resolutions_liability_type_id] ON [dispute_resolutions] ([liability_type_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_dispute_liability_types_code] ON [dispute_liability_types] ([code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_dispute_liability_types_created_by_user_id] ON [dispute_liability_types] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_dispute_liability_types_is_active_display_order] ON [dispute_liability_types] ([is_active], [display_order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_dispute_liability_types_public_id] ON [dispute_liability_types] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_dispute_liability_types_updated_by_user_id] ON [dispute_liability_types] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_report_actions_actor_user_id] ON [report_actions] ([actor_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_report_actions_idempotency_key] ON [report_actions] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_report_actions_public_id] ON [report_actions] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_report_actions_report_id_occurred_at] ON [report_actions] ([report_id], [occurred_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_report_evidence_created_by_user_id] ON [report_evidence] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_report_evidence_file_id] ON [report_evidence] ([file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_report_evidence_public_id] ON [report_evidence] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_report_evidence_report_id_file_id] ON [report_evidence] ([report_id], [file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_report_evidence_status_code] ON [report_evidence] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_report_evidence_submitted_by_user_id] ON [report_evidence] ([submitted_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_report_evidence_withdrawn_by_user_id] ON [report_evidence] ([withdrawn_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_report_types_code] ON [report_types] ([code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_report_types_created_by_user_id] ON [report_types] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_report_types_is_active_display_order] ON [report_types] ([is_active], [display_order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_report_types_public_id] ON [report_types] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_report_types_updated_by_user_id] ON [report_types] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_reports_after_service_case_id] ON [reports] ([after_service_case_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_reports_assigned_admin_user_id_status_code_received_at] ON [reports] ([assigned_admin_user_id], [status_code], [received_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_reports_created_by_user_id] ON [reports] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_reports_dispute_case_id] ON [reports] ([dispute_case_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_reports_idempotency_key] ON [reports] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_reports_public_id] ON [reports] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_reports_report_type_id] ON [reports] ([report_type_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_reports_reported_user_id] ON [reports] ([reported_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_reports_reporter_user_id] ON [reports] ([reporter_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_reports_review_id] ON [reports] ([review_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_reports_service_request_id] ON [reports] ([service_request_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_reports_status_code_received_at] ON [reports] ([status_code], [received_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_reports_transaction_id] ON [reports] ([transaction_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_reports_updated_by_user_id] ON [reports] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanction_appeal_actions_actor_user_id] ON [sanction_appeal_actions] ([actor_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_sanction_appeal_actions_idempotency_key] ON [sanction_appeal_actions] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_sanction_appeal_actions_public_id] ON [sanction_appeal_actions] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanction_appeal_actions_sanction_appeal_id_occurred_at] ON [sanction_appeal_actions] ([sanction_appeal_id], [occurred_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanction_appeal_evidence_created_by_user_id] ON [sanction_appeal_evidence] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanction_appeal_evidence_file_id] ON [sanction_appeal_evidence] ([file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_sanction_appeal_evidence_sanction_appeal_id_file_id] ON [sanction_appeal_evidence] ([sanction_appeal_id], [file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanction_appeal_evidence_submitted_by_user_id] ON [sanction_appeal_evidence] ([submitted_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanction_appeals_applicant_user_id] ON [sanction_appeals] ([applicant_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanction_appeals_assigned_admin_user_id] ON [sanction_appeals] ([assigned_admin_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanction_appeals_created_by_user_id] ON [sanction_appeals] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_sanction_appeals_idempotency_key] ON [sanction_appeals] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanction_appeals_previous_appeal_id] ON [sanction_appeals] ([previous_appeal_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_sanction_appeals_public_id] ON [sanction_appeals] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanction_appeals_sanction_id_submitted_at] ON [sanction_appeals] ([sanction_id], [submitted_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanction_appeals_status_code_submitted_at] ON [sanction_appeals] ([status_code], [submitted_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanction_appeals_updated_by_user_id] ON [sanction_appeals] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanction_events_actor_user_id] ON [sanction_events] ([actor_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_sanction_events_idempotency_key] ON [sanction_events] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_sanction_events_public_id] ON [sanction_events] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanction_events_sanction_id_occurred_at] ON [sanction_events] ([sanction_id], [occurred_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanction_sources_after_service_case_id] ON [sanction_sources] ([after_service_case_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanction_sources_created_by_user_id] ON [sanction_sources] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanction_sources_dispute_case_id] ON [sanction_sources] ([dispute_case_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanction_sources_report_id] ON [sanction_sources] ([report_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanction_sources_review_id] ON [sanction_sources] ([review_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_sanction_sources_sanction_id_after_service_case_id] ON [sanction_sources] ([sanction_id], [after_service_case_id]) WHERE [after_service_case_id] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_sanction_sources_sanction_id_dispute_case_id] ON [sanction_sources] ([sanction_id], [dispute_case_id]) WHERE [dispute_case_id] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_sanction_sources_sanction_id_report_id] ON [sanction_sources] ([sanction_id], [report_id]) WHERE [report_id] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_sanction_sources_sanction_id_review_id] ON [sanction_sources] ([sanction_id], [review_id]) WHERE [review_id] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_sanction_types_code] ON [sanction_types] ([code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanction_types_created_by_user_id] ON [sanction_types] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanction_types_is_active_display_order] ON [sanction_types] ([is_active], [display_order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_sanction_types_public_id] ON [sanction_types] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanction_types_updated_by_user_id] ON [sanction_types] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanctions_created_by_user_id] ON [sanctions] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanctions_decided_by_user_id] ON [sanctions] ([decided_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_sanctions_idempotency_key] ON [sanctions] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanctions_provider_profile_id] ON [sanctions] ([provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanctions_provider_service_category_id] ON [sanctions] ([provider_service_category_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_sanctions_public_id] ON [sanctions] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanctions_released_by_user_id] ON [sanctions] ([released_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanctions_sanction_type_id] ON [sanctions] ([sanction_type_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanctions_status_code_start_at_end_at] ON [sanctions] ([status_code], [start_at], [end_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanctions_target_role_id] ON [sanctions] ([target_role_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanctions_target_user_id] ON [sanctions] ([target_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanctions_target_user_id_status_code_start_at] ON [sanctions] ([target_user_id], [status_code], [start_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    CREATE INDEX [IX_sanctions_updated_by_user_id] ON [sanctions] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    ALTER TABLE [dispute_resolutions] ADD CONSTRAINT [FK_dispute_resolutions_dispute_liability_types_liability_type_id] FOREIGN KEY ([liability_type_id]) REFERENCES [dispute_liability_types] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    EXEC(N'CREATE TRIGGER [TR_sanction_events_append_only]
    ON [sanction_events]
    INSTEAD OF UPDATE, DELETE
    AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51000, ''sanction_events is append-only.'', 1;
    END')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810131717_ImplementReportingSanctionsAppealsAndLiability'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810131717_ImplementReportingSanctionsAppealsAndLiability', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810135033_ImplementTrustScoreCalculationEngine'
)
BEGIN
    CREATE TABLE [provider_trust_calculation_results] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [trust_policy_id] bigint NOT NULL,
        [calculation_mode_code] varchar(20) NOT NULL,
        [result_status_code] varchar(30) NOT NULL,
        [score] decimal(9,4) NULL,
        [grade_code] varchar(30) NULL,
        [evaluation_status_code] varchar(30) NOT NULL,
        [insufficiency_reason] nvarchar(2000) NULL,
        [completed_transaction_count] int NOT NULL DEFAULT 0,
        [verified_review_count] int NOT NULL DEFAULT 0,
        [policy_snapshot_json] nvarchar(max) NOT NULL,
        [source_snapshot_json] nvarchar(max) NOT NULL,
        [calculated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [requested_by_user_id] bigint NULL,
        [idempotency_key] varchar(150) NOT NULL,
        [applied_trust_score_event_id] bigint NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_provider_trust_calculation_results] PRIMARY KEY ([id]),
        CONSTRAINT [CK_trust_results_mode] CHECK ([calculation_mode_code] IN ('SIMULATION','ACTUAL')),
        CONSTRAINT [CK_trust_results_policy_json] CHECK (ISJSON([policy_snapshot_json]) = 1),
        CONSTRAINT [CK_trust_results_score] CHECK ([score] IS NULL OR ([score] >= 0 AND [score] <= 100)),
        CONSTRAINT [CK_trust_results_source_json] CHECK (ISJSON([source_snapshot_json]) = 1),
        CONSTRAINT [CK_trust_results_status] CHECK ([result_status_code] IN ('CALCULATED','INSUFFICIENT_DATA','FAILED')),
        CONSTRAINT [FK_provider_trust_calculation_results_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_provider_trust_calculation_results_trust_policies_trust_policy_id] FOREIGN KEY ([trust_policy_id]) REFERENCES [trust_policies] ([id]),
        CONSTRAINT [FK_provider_trust_calculation_results_trust_score_events_applied_trust_score_event_id] FOREIGN KEY ([applied_trust_score_event_id]) REFERENCES [trust_score_events] ([id]),
        CONSTRAINT [FK_provider_trust_calculation_results_users_requested_by_user_id] FOREIGN KEY ([requested_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810135033_ImplementTrustScoreCalculationEngine'
)
BEGIN
    CREATE TABLE [provider_trust_score_components] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [calculation_result_id] bigint NOT NULL,
        [component_code] varchar(30) NOT NULL,
        [weight] decimal(9,4) NOT NULL,
        [raw_value_json] nvarchar(max) NOT NULL,
        [normalized_score] decimal(9,4) NULL,
        [weighted_score] decimal(9,4) NULL,
        [sample_count] int NOT NULL DEFAULT 0,
        [is_calculable] bit NOT NULL DEFAULT CAST(0 AS bit),
        [unavailable_reason] nvarchar(1000) NULL,
        [source_snapshot_json] nvarchar(max) NOT NULL,
        [calculated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_provider_trust_score_components] PRIMARY KEY ([id]),
        CONSTRAINT [CK_trust_components_raw_json] CHECK (ISJSON([raw_value_json]) = 1),
        CONSTRAINT [CK_trust_components_score] CHECK (([normalized_score] IS NULL OR ([normalized_score] >= 0 AND [normalized_score] <= 100)) AND ([weighted_score] IS NULL OR ([weighted_score] >= 0 AND [weighted_score] <= [weight]))),
        CONSTRAINT [CK_trust_components_source_json] CHECK (ISJSON([source_snapshot_json]) = 1),
        CONSTRAINT [FK_provider_trust_score_components_provider_trust_calculation_results_calculation_result_id] FOREIGN KEY ([calculation_result_id]) REFERENCES [provider_trust_calculation_results] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810135033_ImplementTrustScoreCalculationEngine'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'approved_at', N'approved_by_user_id', N'created_at', N'created_by_user_id', N'effective_from', N'effective_to', N'policy_name', N'policy_version', N'public_id', N'rules_json', N'scope_type_code', N'status_code', N'target_type_code', N'updated_at', N'updated_by_user_id') AND [object_id] = OBJECT_ID(N'[trust_policies]'))
        SET IDENTITY_INSERT [trust_policies] ON;
    EXEC(N'INSERT INTO [trust_policies] ([id], [approved_at], [approved_by_user_id], [created_at], [created_by_user_id], [effective_from], [effective_to], [policy_name], [policy_version], [public_id], [rules_json], [scope_type_code], [status_code], [target_type_code], [updated_at], [updated_by_user_id])
    VALUES (CAST(1 AS bigint), NULL, NULL, ''2026-08-10T00:00:00.0000000Z'', NULL, ''2026-08-10T00:00:00.0000000Z'', NULL, N''TrustScore v1.0 정책 초안'', ''v1.0-draft'', ''f84f8728-8e1e-4ef8-a7ee-4bea94548ff0'', N''{"minimumCompletedTransactions":3,"minimumVerifiedReviews":3,"components":[{"code":"EVIDENCE","weight":15,"ruleType":"EVIDENCE_COMPLETENESS","settings":{"approvalRatio":0.25,"serviceApprovalRatio":0.25,"requiredVerificationRatio":0.4,"notExpiredRatio":0.1}},{"code":"TRANSACTION","weight":30,"ruleType":"TRANSACTION_COMPLETION_RATE","settings":{"completionRateRatio":0.8,"completionEvidenceRatio":0.2}},{"code":"REVIEW","weight":30,"ruleType":"VERIFIED_PUBLIC_RATING_AVERAGE","settings":{}},{"code":"AFTER_SERVICE","weight":10,"ruleType":"FINALIZED_AFTER_SERVICE_OUTCOME","settings":{"resolvedValue":1.0,"unresolvedValue":0.0,"recurrencePenalty":0.25,"disputeConversionPenalty":0.25}},{"code":"DISPUTE","weight":10,"ruleType":"STRUCTURED_LIABILITY_MAPPING","settings":{"liabilityScores":{}}},{"code":"SANCTION","weight":5,"ruleType":"DECIDED_SANCTION_MAPPING","settings":{"sanctionScores":{}}}]}'', ''GLOBAL'', ''DRAFT'', ''PROVIDER'', ''2026-08-10T00:00:00.0000000Z'', NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'approved_at', N'approved_by_user_id', N'created_at', N'created_by_user_id', N'effective_from', N'effective_to', N'policy_name', N'policy_version', N'public_id', N'rules_json', N'scope_type_code', N'status_code', N'target_type_code', N'updated_at', N'updated_by_user_id') AND [object_id] = OBJECT_ID(N'[trust_policies]'))
        SET IDENTITY_INSERT [trust_policies] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810135033_ImplementTrustScoreCalculationEngine'
)
BEGIN
    EXEC(N'ALTER TABLE [trust_policies] ADD CONSTRAINT [CK_trust_policies_status] CHECK ([status_code] IN (''DRAFT'',''APPROVED'',''ACTIVE'',''RETIRED''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810135033_ImplementTrustScoreCalculationEngine'
)
BEGIN
    CREATE INDEX [IX_provider_trust_calculation_results_applied_trust_score_event_id] ON [provider_trust_calculation_results] ([applied_trust_score_event_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810135033_ImplementTrustScoreCalculationEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_trust_calculation_results_idempotency_key] ON [provider_trust_calculation_results] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810135033_ImplementTrustScoreCalculationEngine'
)
BEGIN
    CREATE INDEX [IX_provider_trust_calculation_results_provider_profile_id_calculated_at] ON [provider_trust_calculation_results] ([provider_profile_id], [calculated_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810135033_ImplementTrustScoreCalculationEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_trust_calculation_results_public_id] ON [provider_trust_calculation_results] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810135033_ImplementTrustScoreCalculationEngine'
)
BEGIN
    CREATE INDEX [IX_provider_trust_calculation_results_requested_by_user_id] ON [provider_trust_calculation_results] ([requested_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810135033_ImplementTrustScoreCalculationEngine'
)
BEGIN
    CREATE INDEX [IX_provider_trust_calculation_results_trust_policy_id_calculation_mode_code_calculated_at] ON [provider_trust_calculation_results] ([trust_policy_id], [calculation_mode_code], [calculated_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810135033_ImplementTrustScoreCalculationEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_trust_score_components_calculation_result_id_component_code] ON [provider_trust_score_components] ([calculation_result_id], [component_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810135033_ImplementTrustScoreCalculationEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_trust_score_components_public_id] ON [provider_trust_score_components] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810135033_ImplementTrustScoreCalculationEngine'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810135033_ImplementTrustScoreCalculationEngine', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    DROP INDEX [IX_reviews_transaction_id] ON [reviews];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    ALTER TABLE [reviews] DROP CONSTRAINT [CK_reviews_verification];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    ALTER TABLE [service_history_entries] ADD [subscription_visit_schedule_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[reviews]') AND [c].[name] = N'transaction_id');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [reviews] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [reviews] ALTER COLUMN [transaction_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    ALTER TABLE [reviews] ADD [subscription_visit_schedule_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dispute_cases]') AND [c].[name] = N'transaction_id');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [dispute_cases] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [dispute_cases] ALTER COLUMN [transaction_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    ALTER TABLE [dispute_cases] ADD [subscription_visit_schedule_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[after_service_cases]') AND [c].[name] = N'transaction_id');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [after_service_cases] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [after_service_cases] ALTER COLUMN [transaction_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    ALTER TABLE [after_service_cases] ADD [subscription_visit_schedule_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE TABLE [care_products] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [service_category_id] bigint NOT NULL,
        [product_name] nvarchar(200) NOT NULL,
        [description] nvarchar(2000) NULL,
        [service_scope_text] nvarchar(4000) NOT NULL,
        [visits_per_period] int NOT NULL,
        [expected_duration_minutes] int NOT NULL,
        [billing_period_code] varchar(30) NOT NULL,
        [standard_monthly_amount] decimal(19,4) NULL,
        [standard_visit_amount] decimal(19,4) NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [effective_from] date NOT NULL,
        [effective_to] date NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_care_products] PRIMARY KEY ([id]),
        CONSTRAINT [CK_care_products_amount] CHECK ([standard_monthly_amount] IS NULL OR [standard_monthly_amount] >= 0),
        CONSTRAINT [CK_care_products_period] CHECK ([effective_to] IS NULL OR [effective_to] > [effective_from]),
        CONSTRAINT [CK_care_products_visits] CHECK ([visits_per_period] > 0 AND [expected_duration_minutes] > 0),
        CONSTRAINT [FK_care_products_service_categories_service_category_id] FOREIGN KEY ([service_category_id]) REFERENCES [service_categories] ([id]),
        CONSTRAINT [FK_care_products_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_care_products_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE TABLE [subscription_applications] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [subscription_request_id] bigint NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [proposed_scope_text] nvarchar(4000) NOT NULL,
        [proposed_monthly_amount] decimal(19,4) NULL,
        [proposed_visit_amount] decimal(19,4) NULL,
        [available_schedule_text] nvarchar(2000) NULL,
        [status_code] varchar(30) NOT NULL,
        [submitted_at] datetime2(7) NOT NULL,
        [idempotency_key] varchar(150) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_subscription_applications] PRIMARY KEY ([id]),
        CONSTRAINT [CK_subscription_applications_amount] CHECK ([proposed_monthly_amount] IS NULL OR [proposed_monthly_amount] >= 0),
        CONSTRAINT [CK_subscription_applications_status] CHECK ([status_code] IN ('SUBMITTED','SELECTED','NOT_SELECTED','WITHDRAWN')),
        CONSTRAINT [FK_subscription_applications_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_subscription_applications_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_subscription_applications_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE TABLE [subscription_requests] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [customer_profile_id] bigint NOT NULL,
        [service_category_id] bigint NOT NULL,
        [care_product_id] bigint NULL,
        [administrative_area_id] bigint NOT NULL,
        [request_type_code] varchar(20) NOT NULL,
        [requested_scope_text] nvarchar(4000) NOT NULL,
        [preferred_start_date] date NOT NULL,
        [detail_address] nvarchar(500) NULL,
        [status_code] varchar(30) NOT NULL,
        [selected_application_id] bigint NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_subscription_requests] PRIMARY KEY ([id]),
        CONSTRAINT [CK_subscription_requests_product] CHECK (([request_type_code] = 'STANDARD' AND [care_product_id] IS NOT NULL) OR [request_type_code] = 'CUSTOM'),
        CONSTRAINT [CK_subscription_requests_status] CHECK ([status_code] IN ('OPEN','SELECTED','CONTRACTED','CANCELLED','CLOSED')),
        CONSTRAINT [CK_subscription_requests_type] CHECK ([request_type_code] IN ('STANDARD','CUSTOM')),
        CONSTRAINT [FK_subscription_requests_administrative_areas_administrative_area_id] FOREIGN KEY ([administrative_area_id]) REFERENCES [administrative_areas] ([id]),
        CONSTRAINT [FK_subscription_requests_care_products_care_product_id] FOREIGN KEY ([care_product_id]) REFERENCES [care_products] ([id]),
        CONSTRAINT [FK_subscription_requests_customer_profiles_customer_profile_id] FOREIGN KEY ([customer_profile_id]) REFERENCES [customer_profiles] ([id]),
        CONSTRAINT [FK_subscription_requests_service_categories_service_category_id] FOREIGN KEY ([service_category_id]) REFERENCES [service_categories] ([id]),
        CONSTRAINT [FK_subscription_requests_subscription_applications_selected_application_id] FOREIGN KEY ([selected_application_id]) REFERENCES [subscription_applications] ([id]),
        CONSTRAINT [FK_subscription_requests_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_subscription_requests_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE TABLE [subscription_contracts] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [subscription_request_id] bigint NOT NULL,
        [customer_profile_id] bigint NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [service_category_id] bigint NOT NULL,
        [care_product_id] bigint NULL,
        [subscription_application_id] bigint NOT NULL,
        [status_code] varchar(30) NOT NULL,
        [started_at] datetime2(7) NOT NULL,
        [ended_at] datetime2(7) NULL,
        [pause_started_at] datetime2(7) NULL,
        [resume_planned_at] datetime2(7) NULL,
        [termination_requested_at] datetime2(7) NULL,
        [terminated_at] datetime2(7) NULL,
        [termination_reason] nvarchar(2000) NULL,
        [price_snapshot_json] nvarchar(max) NOT NULL,
        [fee_policy_snapshot_json] nvarchar(max) NULL,
        [service_scope_snapshot_json] nvarchar(max) NOT NULL,
        [recurrence_snapshot_json] nvarchar(max) NOT NULL,
        [provider_trust_score_snapshot] decimal(9,4) NULL,
        [currency_code] char(3) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_subscription_contracts] PRIMARY KEY ([id]),
        CONSTRAINT [CK_subscription_contracts_status] CHECK ([status_code] IN ('ACTIVE','PAUSED','TERMINATION_REQUESTED','TERMINATED')),
        CONSTRAINT [FK_subscription_contracts_care_products_care_product_id] FOREIGN KEY ([care_product_id]) REFERENCES [care_products] ([id]),
        CONSTRAINT [FK_subscription_contracts_customer_profiles_customer_profile_id] FOREIGN KEY ([customer_profile_id]) REFERENCES [customer_profiles] ([id]),
        CONSTRAINT [FK_subscription_contracts_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_subscription_contracts_service_categories_service_category_id] FOREIGN KEY ([service_category_id]) REFERENCES [service_categories] ([id]),
        CONSTRAINT [FK_subscription_contracts_subscription_applications_subscription_application_id] FOREIGN KEY ([subscription_application_id]) REFERENCES [subscription_applications] ([id]),
        CONSTRAINT [FK_subscription_contracts_subscription_requests_subscription_request_id] FOREIGN KEY ([subscription_request_id]) REFERENCES [subscription_requests] ([id]),
        CONSTRAINT [FK_subscription_contracts_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_subscription_contracts_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE TABLE [subscription_recurrence_rules] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [subscription_request_id] bigint NOT NULL,
        [frequency_type_code] varchar(30) NOT NULL,
        [interval_value] int NOT NULL,
        [visits_per_period] int NULL,
        [weekdays_json] nvarchar(1000) NULL,
        [preferred_time_from] time(0) NOT NULL,
        [preferred_time_to] time(0) NULL,
        [expected_duration_minutes] int NOT NULL,
        [start_date] date NOT NULL,
        [end_date] date NULL,
        [additional_rule_json] nvarchar(max) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_subscription_recurrence_rules] PRIMARY KEY ([id]),
        CONSTRAINT [CK_subscription_recurrence_frequency] CHECK ([frequency_type_code] IN ('WEEKLY','BIWEEKLY','MONTHLY','QUARTERLY','HALF_YEARLY')),
        CONSTRAINT [CK_subscription_recurrence_period] CHECK ([end_date] IS NULL OR [end_date] >= [start_date]),
        CONSTRAINT [CK_subscription_recurrence_values] CHECK ([interval_value] > 0 AND [expected_duration_minutes] > 0),
        CONSTRAINT [FK_subscription_recurrence_rules_subscription_requests_subscription_request_id] FOREIGN KEY ([subscription_request_id]) REFERENCES [subscription_requests] ([id]),
        CONSTRAINT [FK_subscription_recurrence_rules_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_subscription_recurrence_rules_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE TABLE [subscription_visit_schedules] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [subscription_contract_id] bigint NOT NULL,
        [visit_no] int NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [scheduled_start_at] datetime2(7) NOT NULL,
        [scheduled_end_at] datetime2(7) NULL,
        [status_code] varchar(30) NOT NULL,
        [visit_verified_at] datetime2(7) NULL,
        [visit_verification_method_code] varchar(30) NULL,
        [visit_verification_result_code] varchar(30) NULL,
        [work_started_at] datetime2(7) NULL,
        [work_completed_at] datetime2(7) NULL,
        [provider_completion_submitted_at] datetime2(7) NULL,
        [customer_confirmed_at] datetime2(7) NULL,
        [completion_checklist_json] nvarchar(max) NULL,
        [completion_note] nvarchar(2000) NULL,
        [settlement_status_code] varchar(30) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_subscription_visit_schedules] PRIMARY KEY ([id]),
        CONSTRAINT [CK_subscription_visits_number] CHECK ([visit_no] > 0),
        CONSTRAINT [CK_subscription_visits_settlement] CHECK ([settlement_status_code] IN ('NOT_READY','READY','HOLD','SETTLED')),
        CONSTRAINT [CK_subscription_visits_status] CHECK ([status_code] IN ('SCHEDULED','RESCHEDULED','SKIPPED','PAUSED','IN_PROGRESS','PROVIDER_COMPLETED','COMPLETED','CANCELLED','DISPUTED')),
        CONSTRAINT [FK_subscription_visit_schedules_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_subscription_visit_schedules_subscription_contracts_subscription_contract_id] FOREIGN KEY ([subscription_contract_id]) REFERENCES [subscription_contracts] ([id]),
        CONSTRAINT [FK_subscription_visit_schedules_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_subscription_visit_schedules_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE TABLE [subscription_events] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [subscription_request_id] bigint NULL,
        [subscription_contract_id] bigint NULL,
        [subscription_visit_schedule_id] bigint NULL,
        [event_type_code] varchar(50) NOT NULL,
        [event_data_json] nvarchar(max) NULL,
        [occurred_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [actor_user_id] bigint NULL,
        [idempotency_key] varchar(150) NOT NULL,
        CONSTRAINT [PK_subscription_events] PRIMARY KEY ([id]),
        CONSTRAINT [CK_subscription_events_parent] CHECK ([subscription_request_id] IS NOT NULL OR [subscription_contract_id] IS NOT NULL OR [subscription_visit_schedule_id] IS NOT NULL),
        CONSTRAINT [FK_subscription_events_subscription_contracts_subscription_contract_id] FOREIGN KEY ([subscription_contract_id]) REFERENCES [subscription_contracts] ([id]),
        CONSTRAINT [FK_subscription_events_subscription_requests_subscription_request_id] FOREIGN KEY ([subscription_request_id]) REFERENCES [subscription_requests] ([id]),
        CONSTRAINT [FK_subscription_events_subscription_visit_schedules_subscription_visit_schedule_id] FOREIGN KEY ([subscription_visit_schedule_id]) REFERENCES [subscription_visit_schedules] ([id]),
        CONSTRAINT [FK_subscription_events_users_actor_user_id] FOREIGN KEY ([actor_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE TABLE [subscription_schedule_changes] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [subscription_visit_schedule_id] bigint NOT NULL,
        [requested_by_user_id] bigint NOT NULL,
        [old_schedule_json] nvarchar(max) NOT NULL,
        [new_schedule_json] nvarchar(max) NOT NULL,
        [reason] nvarchar(2000) NOT NULL,
        [status_code] varchar(30) NOT NULL,
        [requested_at] datetime2(7) NOT NULL,
        [decided_at] datetime2(7) NULL,
        [decided_by_user_id] bigint NULL,
        [idempotency_key] varchar(150) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_subscription_schedule_changes] PRIMARY KEY ([id]),
        CONSTRAINT [CK_subscription_schedule_changes_status] CHECK ([status_code] IN ('REQUESTED','APPROVED','REJECTED')),
        CONSTRAINT [FK_subscription_schedule_changes_subscription_visit_schedules_subscription_visit_schedule_id] FOREIGN KEY ([subscription_visit_schedule_id]) REFERENCES [subscription_visit_schedules] ([id]),
        CONSTRAINT [FK_subscription_schedule_changes_users_decided_by_user_id] FOREIGN KEY ([decided_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_subscription_schedule_changes_users_requested_by_user_id] FOREIGN KEY ([requested_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE TABLE [subscription_visit_files] (
        [id] bigint NOT NULL IDENTITY,
        [subscription_visit_schedule_id] bigint NOT NULL,
        [file_id] bigint NOT NULL,
        [purpose_code] varchar(50) NOT NULL,
        [display_order] int NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_subscription_visit_files] PRIMARY KEY ([id]),
        CONSTRAINT [FK_subscription_visit_files_files_file_id] FOREIGN KEY ([file_id]) REFERENCES [files] ([id]),
        CONSTRAINT [FK_subscription_visit_files_subscription_visit_schedules_subscription_visit_schedule_id] FOREIGN KEY ([subscription_visit_schedule_id]) REFERENCES [subscription_visit_schedules] ([id]),
        CONSTRAINT [FK_subscription_visit_files_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_service_history_entries_subscription_visit_schedule_id] ON [dbo].[service_history_entries] ([subscription_visit_schedule_id]) WHERE [subscription_visit_schedule_id] IS NOT NULL')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_reviews_subscription_visit_schedule_id] ON [dbo].[reviews] ([subscription_visit_schedule_id]) WHERE [subscription_visit_schedule_id] IS NOT NULL')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_reviews_transaction_id] ON [reviews] ([transaction_id]) WHERE [transaction_id] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[reviews] ADD CONSTRAINT [CK_reviews_source] CHECK (([transaction_id] IS NOT NULL AND [subscription_visit_schedule_id] IS NULL) OR ([transaction_id] IS NULL AND [subscription_visit_schedule_id] IS NOT NULL))')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    EXEC(N'ALTER TABLE [reviews] ADD CONSTRAINT [CK_reviews_verification] CHECK ([verification_status_code] IN (''VERIFIED_TRANSACTION'',''VERIFIED_SUBSCRIPTION_VISIT''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_dispute_cases_subscription_visit_schedule_id] ON [dbo].[dispute_cases] ([subscription_visit_schedule_id])')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[dispute_cases] ADD CONSTRAINT [CK_dispute_cases_source] CHECK (([transaction_id] IS NOT NULL AND [subscription_visit_schedule_id] IS NULL) OR ([transaction_id] IS NULL AND [subscription_visit_schedule_id] IS NOT NULL))')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_after_service_cases_subscription_visit_schedule_id] ON [dbo].[after_service_cases] ([subscription_visit_schedule_id])')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[after_service_cases] ADD CONSTRAINT [CK_after_service_cases_source] CHECK (([transaction_id] IS NOT NULL AND [subscription_visit_schedule_id] IS NULL) OR ([transaction_id] IS NULL AND [subscription_visit_schedule_id] IS NOT NULL))')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_care_products_created_by_user_id] ON [care_products] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_care_products_product_name_effective_from] ON [care_products] ([product_name], [effective_from]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_care_products_public_id] ON [care_products] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_care_products_service_category_id_is_active] ON [care_products] ([service_category_id], [is_active]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_care_products_updated_by_user_id] ON [care_products] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_applications_created_by_user_id] ON [subscription_applications] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_applications_idempotency_key] ON [subscription_applications] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_applications_provider_profile_id] ON [subscription_applications] ([provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_applications_public_id] ON [subscription_applications] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_applications_subscription_request_id_provider_profile_id] ON [subscription_applications] ([subscription_request_id], [provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_applications_subscription_request_id_status_code] ON [subscription_applications] ([subscription_request_id], [status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_applications_updated_by_user_id] ON [subscription_applications] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_contracts_care_product_id] ON [subscription_contracts] ([care_product_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_contracts_created_by_user_id] ON [subscription_contracts] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_contracts_customer_profile_id_status_code] ON [subscription_contracts] ([customer_profile_id], [status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_contracts_provider_profile_id_status_code] ON [subscription_contracts] ([provider_profile_id], [status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_contracts_public_id] ON [subscription_contracts] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_contracts_service_category_id] ON [subscription_contracts] ([service_category_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_contracts_subscription_application_id] ON [subscription_contracts] ([subscription_application_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_contracts_subscription_request_id] ON [subscription_contracts] ([subscription_request_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_contracts_updated_by_user_id] ON [subscription_contracts] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_events_actor_user_id] ON [subscription_events] ([actor_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_events_idempotency_key] ON [subscription_events] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_events_public_id] ON [subscription_events] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_events_subscription_contract_id_occurred_at] ON [subscription_events] ([subscription_contract_id], [occurred_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_events_subscription_request_id] ON [subscription_events] ([subscription_request_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_events_subscription_visit_schedule_id_occurred_at] ON [subscription_events] ([subscription_visit_schedule_id], [occurred_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_recurrence_rules_created_by_user_id] ON [subscription_recurrence_rules] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_recurrence_rules_public_id] ON [subscription_recurrence_rules] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_recurrence_rules_subscription_request_id] ON [subscription_recurrence_rules] ([subscription_request_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_recurrence_rules_updated_by_user_id] ON [subscription_recurrence_rules] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_requests_administrative_area_id] ON [subscription_requests] ([administrative_area_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_requests_care_product_id] ON [subscription_requests] ([care_product_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_requests_created_by_user_id] ON [subscription_requests] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_requests_customer_profile_id_status_code] ON [subscription_requests] ([customer_profile_id], [status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_requests_public_id] ON [subscription_requests] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_subscription_requests_selected_application_id] ON [subscription_requests] ([selected_application_id]) WHERE [selected_application_id] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_requests_service_category_id_administrative_area_id_status_code] ON [subscription_requests] ([service_category_id], [administrative_area_id], [status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_requests_updated_by_user_id] ON [subscription_requests] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_schedule_changes_decided_by_user_id] ON [subscription_schedule_changes] ([decided_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_schedule_changes_idempotency_key] ON [subscription_schedule_changes] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_schedule_changes_public_id] ON [subscription_schedule_changes] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_schedule_changes_requested_by_user_id] ON [subscription_schedule_changes] ([requested_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_schedule_changes_subscription_visit_schedule_id_status_code] ON [subscription_schedule_changes] ([subscription_visit_schedule_id], [status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_visit_files_created_by_user_id] ON [subscription_visit_files] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_visit_files_file_id] ON [subscription_visit_files] ([file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_visit_files_subscription_visit_schedule_id_file_id] ON [subscription_visit_files] ([subscription_visit_schedule_id], [file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_visit_schedules_created_by_user_id] ON [subscription_visit_schedules] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_visit_schedules_provider_profile_id_scheduled_start_at] ON [subscription_visit_schedules] ([provider_profile_id], [scheduled_start_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_visit_schedules_public_id] ON [subscription_visit_schedules] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_visit_schedules_status_code_scheduled_start_at] ON [subscription_visit_schedules] ([status_code], [scheduled_start_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_visit_schedules_subscription_contract_id_visit_no] ON [subscription_visit_schedules] ([subscription_contract_id], [visit_no]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    CREATE INDEX [IX_subscription_visit_schedules_updated_by_user_id] ON [subscription_visit_schedules] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[after_service_cases] ADD CONSTRAINT [FK_after_service_cases_subscription_visit_schedules_subscription_visit_schedule_id] FOREIGN KEY ([subscription_visit_schedule_id]) REFERENCES [dbo].[subscription_visit_schedules] ([id])')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[dispute_cases] ADD CONSTRAINT [FK_dispute_cases_subscription_visit_schedules_subscription_visit_schedule_id] FOREIGN KEY ([subscription_visit_schedule_id]) REFERENCES [dbo].[subscription_visit_schedules] ([id])')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[reviews] ADD CONSTRAINT [FK_reviews_subscription_visit_schedules_subscription_visit_schedule_id] FOREIGN KEY ([subscription_visit_schedule_id]) REFERENCES [dbo].[subscription_visit_schedules] ([id])')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    EXEC(N'ALTER TABLE [dbo].[service_history_entries] ADD CONSTRAINT [FK_service_history_entries_subscription_visit_schedules_subscription_visit_schedule_id] FOREIGN KEY ([subscription_visit_schedule_id]) REFERENCES [dbo].[subscription_visit_schedules] ([id])')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    ALTER TABLE [subscription_applications] ADD CONSTRAINT [FK_subscription_applications_subscription_requests_subscription_request_id] FOREIGN KEY ([subscription_request_id]) REFERENCES [subscription_requests] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    EXEC(N'CREATE OR ALTER TRIGGER [dbo].[TR_subscription_events_append_only]
    ON [dbo].[subscription_events]
    INSTEAD OF UPDATE, DELETE
    AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51003, N''구독 변경이력은 수정하거나 삭제할 수 없습니다.'', 1;
    END;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810144322_ImplementCareSubscriptionCore'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810144322_ImplementCareSubscriptionCore', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    ALTER TABLE [subscription_contracts] ADD [billing_status_code] varchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    ALTER TABLE [subscription_contracts] ADD [next_billing_at] datetime2(7) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE TABLE [monthly_settlements] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [settlement_year] int NOT NULL,
        [settlement_month] int NOT NULL,
        [status_code] varchar(30) NOT NULL,
        [gross_total] decimal(19,4) NOT NULL DEFAULT 0.0,
        [fee_total] decimal(19,4) NOT NULL DEFAULT 0.0,
        [adjustment_total] decimal(19,4) NOT NULL DEFAULT 0.0,
        [net_total] decimal(19,4) NOT NULL DEFAULT 0.0,
        [item_count] int NOT NULL DEFAULT 0,
        [idempotency_key] varchar(150) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [prepared_at] datetime2(7) NULL,
        [approved_at] datetime2(7) NULL,
        [approved_by_user_id] bigint NULL,
        [paid_at] datetime2(7) NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_monthly_settlements] PRIMARY KEY ([id]),
        CONSTRAINT [CK_monthly_settlements_month] CHECK ([settlement_month] BETWEEN 1 AND 12),
        CONSTRAINT [CK_monthly_settlements_status] CHECK ([status_code] IN ('DRAFT','REVIEW','APPROVED','PAYMENT_PENDING','PAID','CANCELLED')),
        CONSTRAINT [FK_monthly_settlements_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_monthly_settlements_users_approved_by_user_id] FOREIGN KEY ([approved_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_monthly_settlements_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE TABLE [subscription_payment_methods] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [customer_profile_id] bigint NOT NULL,
        [provider_code] varchar(50) NULL,
        [payment_method_type_code] varchar(30) NOT NULL,
        [external_token_reference] varchar(300) NULL,
        [masked_display_text] nvarchar(100) NULL,
        [status_code] varchar(20) NOT NULL,
        [is_default] bit NOT NULL DEFAULT CAST(0 AS bit),
        [registered_at] datetime2(7) NOT NULL,
        [disabled_at] datetime2(7) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_subscription_payment_methods] PRIMARY KEY ([id]),
        CONSTRAINT [CK_subscription_payment_methods_status] CHECK ([status_code] IN ('ACTIVE','DISABLED')),
        CONSTRAINT [FK_subscription_payment_methods_customer_profiles_customer_profile_id] FOREIGN KEY ([customer_profile_id]) REFERENCES [customer_profiles] ([id]),
        CONSTRAINT [FK_subscription_payment_methods_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_subscription_payment_methods_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE TABLE [subscription_payouts] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [monthly_settlement_id] bigint NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [requested_amount] decimal(19,4) NOT NULL,
        [approved_amount] decimal(19,4) NULL,
        [status_code] varchar(30) NOT NULL,
        [requested_at] datetime2(7) NOT NULL,
        [approved_at] datetime2(7) NULL,
        [completed_at] datetime2(7) NULL,
        [failed_at] datetime2(7) NULL,
        [external_payout_reference] varchar(200) NULL,
        [idempotency_key] varchar(150) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_subscription_payouts] PRIMARY KEY ([id]),
        CONSTRAINT [CK_subscription_payouts_amount] CHECK ([requested_amount] >= 0 AND ([approved_amount] IS NULL OR [approved_amount] >= 0)),
        CONSTRAINT [CK_subscription_payouts_status] CHECK ([status_code] IN ('REQUESTED','APPROVED','PROCESSING','COMPLETED','FAILED','CANCELLED')),
        CONSTRAINT [FK_subscription_payouts_monthly_settlements_monthly_settlement_id] FOREIGN KEY ([monthly_settlement_id]) REFERENCES [monthly_settlements] ([id]),
        CONSTRAINT [FK_subscription_payouts_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_subscription_payouts_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_subscription_payouts_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE TABLE [subscription_settlement_items] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [subscription_visit_schedule_id] bigint NOT NULL,
        [subscription_contract_id] bigint NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [settlement_month] date NOT NULL,
        [gross_amount] decimal(19,4) NULL,
        [fee_policy_code] varchar(30) NULL,
        [fee_policy_snapshot_json] nvarchar(max) NULL,
        [calculated_fee_amount] decimal(19,4) NULL,
        [adjustment_amount] decimal(19,4) NOT NULL DEFAULT 0.0,
        [net_amount] decimal(19,4) NULL,
        [status_code] varchar(30) NOT NULL,
        [hold_reason] nvarchar(1000) NULL,
        [monthly_settlement_id] bigint NULL,
        [idempotency_key] varchar(180) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_subscription_settlement_items] PRIMARY KEY ([id]),
        CONSTRAINT [CK_subscription_settlement_items_amounts] CHECK ([gross_amount] IS NULL OR [gross_amount] >= 0),
        CONSTRAINT [CK_subscription_settlement_items_status] CHECK ([status_code] IN ('CALCULATION_PENDING','POLICY_PENDING','READY','HOLD','SETTLED','CANCELLED')),
        CONSTRAINT [FK_subscription_settlement_items_monthly_settlements_monthly_settlement_id] FOREIGN KEY ([monthly_settlement_id]) REFERENCES [monthly_settlements] ([id]),
        CONSTRAINT [FK_subscription_settlement_items_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_subscription_settlement_items_subscription_contracts_subscription_contract_id] FOREIGN KEY ([subscription_contract_id]) REFERENCES [subscription_contracts] ([id]),
        CONSTRAINT [FK_subscription_settlement_items_subscription_visit_schedules_subscription_visit_schedule_id] FOREIGN KEY ([subscription_visit_schedule_id]) REFERENCES [subscription_visit_schedules] ([id]),
        CONSTRAINT [FK_subscription_settlement_items_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_subscription_settlement_items_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE TABLE [subscription_payment_requests] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [subscription_contract_id] bigint NOT NULL,
        [customer_profile_id] bigint NOT NULL,
        [payment_method_id] bigint NULL,
        [billing_period_start] date NOT NULL,
        [billing_period_end] date NOT NULL,
        [requested_amount] decimal(19,4) NOT NULL,
        [currency_code] char(3) NOT NULL,
        [status_code] varchar(30) NOT NULL,
        [requested_at] datetime2(7) NOT NULL,
        [authorized_at] datetime2(7) NULL,
        [completed_at] datetime2(7) NULL,
        [failed_at] datetime2(7) NULL,
        [cancelled_at] datetime2(7) NULL,
        [failure_code] varchar(100) NULL,
        [failure_reason] nvarchar(1000) NULL,
        [external_payment_reference] varchar(200) NULL,
        [idempotency_key] varchar(150) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_subscription_payment_requests] PRIMARY KEY ([id]),
        CONSTRAINT [CK_subscription_payment_requests_amount] CHECK ([requested_amount] > 0),
        CONSTRAINT [CK_subscription_payment_requests_period] CHECK ([billing_period_end] >= [billing_period_start]),
        CONSTRAINT [CK_subscription_payment_requests_status] CHECK ([status_code] IN ('REQUESTED','PROCESSING','COMPLETED','FAILED','CANCELLED','PARTIALLY_REFUNDED','REFUNDED')),
        CONSTRAINT [FK_subscription_payment_requests_customer_profiles_customer_profile_id] FOREIGN KEY ([customer_profile_id]) REFERENCES [customer_profiles] ([id]),
        CONSTRAINT [FK_subscription_payment_requests_subscription_contracts_subscription_contract_id] FOREIGN KEY ([subscription_contract_id]) REFERENCES [subscription_contracts] ([id]),
        CONSTRAINT [FK_subscription_payment_requests_subscription_payment_methods_payment_method_id] FOREIGN KEY ([payment_method_id]) REFERENCES [subscription_payment_methods] ([id]),
        CONSTRAINT [FK_subscription_payment_requests_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_subscription_payment_requests_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE TABLE [subscription_payout_events] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [subscription_payout_id] bigint NOT NULL,
        [event_type_code] varchar(40) NOT NULL,
        [event_data_json] nvarchar(max) NULL,
        [occurred_at] datetime2(7) NOT NULL,
        [actor_user_id] bigint NULL,
        [idempotency_key] varchar(150) NOT NULL,
        CONSTRAINT [PK_subscription_payout_events] PRIMARY KEY ([id]),
        CONSTRAINT [FK_subscription_payout_events_subscription_payouts_subscription_payout_id] FOREIGN KEY ([subscription_payout_id]) REFERENCES [subscription_payouts] ([id]),
        CONSTRAINT [FK_subscription_payout_events_users_actor_user_id] FOREIGN KEY ([actor_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE TABLE [subscription_payment_ledger] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [subscription_contract_id] bigint NOT NULL,
        [payment_request_id] bigint NULL,
        [entry_type_code] varchar(30) NOT NULL,
        [amount] decimal(19,4) NOT NULL,
        [currency_code] char(3) NOT NULL,
        [balance_after] decimal(19,4) NULL,
        [reference_type] varchar(50) NOT NULL,
        [reference_public_id] uniqueidentifier NULL,
        [reason_text] nvarchar(1000) NULL,
        [occurred_at] datetime2(7) NOT NULL,
        [processed_by_user_id] bigint NULL,
        [idempotency_key] varchar(150) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_subscription_payment_ledger] PRIMARY KEY ([id]),
        CONSTRAINT [CK_subscription_payment_ledger_amount] CHECK ([amount] <> 0),
        CONSTRAINT [CK_subscription_payment_ledger_type] CHECK ([entry_type_code] IN ('PAYMENT','REFUND','ADJUSTMENT','REVERSAL')),
        CONSTRAINT [FK_subscription_payment_ledger_subscription_contracts_subscription_contract_id] FOREIGN KEY ([subscription_contract_id]) REFERENCES [subscription_contracts] ([id]),
        CONSTRAINT [FK_subscription_payment_ledger_subscription_payment_requests_payment_request_id] FOREIGN KEY ([payment_request_id]) REFERENCES [subscription_payment_requests] ([id]),
        CONSTRAINT [FK_subscription_payment_ledger_users_processed_by_user_id] FOREIGN KEY ([processed_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE TABLE [subscription_refund_adjustments] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [subscription_contract_id] bigint NOT NULL,
        [payment_request_id] bigint NULL,
        [visit_schedule_id] bigint NULL,
        [type_code] varchar(20) NOT NULL,
        [requested_amount] decimal(19,4) NOT NULL,
        [approved_amount] decimal(19,4) NULL,
        [reason] nvarchar(1000) NOT NULL,
        [status_code] varchar(30) NOT NULL,
        [requested_at] datetime2(7) NOT NULL,
        [approved_at] datetime2(7) NULL,
        [completed_at] datetime2(7) NULL,
        [processed_by_user_id] bigint NULL,
        [idempotency_key] varchar(150) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_subscription_refund_adjustments] PRIMARY KEY ([id]),
        CONSTRAINT [CK_subscription_refund_adjustments_amount] CHECK ([requested_amount] > 0 AND ([approved_amount] IS NULL OR [approved_amount] >= 0)),
        CONSTRAINT [CK_subscription_refund_adjustments_status] CHECK ([status_code] IN ('REQUESTED','APPROVED','PROCESSING','COMPLETED','REJECTED','CANCELLED','FAILED')),
        CONSTRAINT [CK_subscription_refund_adjustments_type] CHECK ([type_code] IN ('REFUND','ADJUSTMENT')),
        CONSTRAINT [FK_subscription_refund_adjustments_subscription_contracts_subscription_contract_id] FOREIGN KEY ([subscription_contract_id]) REFERENCES [subscription_contracts] ([id]),
        CONSTRAINT [FK_subscription_refund_adjustments_subscription_payment_requests_payment_request_id] FOREIGN KEY ([payment_request_id]) REFERENCES [subscription_payment_requests] ([id]),
        CONSTRAINT [FK_subscription_refund_adjustments_subscription_visit_schedules_visit_schedule_id] FOREIGN KEY ([visit_schedule_id]) REFERENCES [subscription_visit_schedules] ([id]),
        CONSTRAINT [FK_subscription_refund_adjustments_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_subscription_refund_adjustments_users_processed_by_user_id] FOREIGN KEY ([processed_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_subscription_refund_adjustments_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_monthly_settlements_approved_by_user_id] ON [monthly_settlements] ([approved_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_monthly_settlements_idempotency_key] ON [monthly_settlements] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_monthly_settlements_provider_profile_id_settlement_year_settlement_month] ON [monthly_settlements] ([provider_profile_id], [settlement_year], [settlement_month]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_monthly_settlements_public_id] ON [monthly_settlements] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_monthly_settlements_status_code_settlement_year_settlement_month] ON [monthly_settlements] ([status_code], [settlement_year], [settlement_month]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_monthly_settlements_updated_by_user_id] ON [monthly_settlements] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_payment_ledger_idempotency_key] ON [subscription_payment_ledger] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_payment_ledger_payment_request_id] ON [subscription_payment_ledger] ([payment_request_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_payment_ledger_processed_by_user_id] ON [subscription_payment_ledger] ([processed_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_payment_ledger_public_id] ON [subscription_payment_ledger] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_payment_ledger_subscription_contract_id_occurred_at] ON [subscription_payment_ledger] ([subscription_contract_id], [occurred_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_payment_methods_created_by_user_id] ON [subscription_payment_methods] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_payment_methods_customer_profile_id_is_default] ON [subscription_payment_methods] ([customer_profile_id], [is_default]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_payment_methods_customer_profile_id_status_code] ON [subscription_payment_methods] ([customer_profile_id], [status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_payment_methods_public_id] ON [subscription_payment_methods] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_payment_methods_updated_by_user_id] ON [subscription_payment_methods] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_payment_requests_created_by_user_id] ON [subscription_payment_requests] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_payment_requests_customer_profile_id] ON [subscription_payment_requests] ([customer_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_payment_requests_idempotency_key] ON [subscription_payment_requests] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_payment_requests_payment_method_id] ON [subscription_payment_requests] ([payment_method_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_payment_requests_public_id] ON [subscription_payment_requests] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_payment_requests_status_code_requested_at] ON [subscription_payment_requests] ([status_code], [requested_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_payment_requests_subscription_contract_id_billing_period_start_billing_period_end] ON [subscription_payment_requests] ([subscription_contract_id], [billing_period_start], [billing_period_end]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_payment_requests_updated_by_user_id] ON [subscription_payment_requests] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_payout_events_actor_user_id] ON [subscription_payout_events] ([actor_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_payout_events_idempotency_key] ON [subscription_payout_events] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_payout_events_public_id] ON [subscription_payout_events] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_payout_events_subscription_payout_id_occurred_at] ON [subscription_payout_events] ([subscription_payout_id], [occurred_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_payouts_created_by_user_id] ON [subscription_payouts] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_payouts_idempotency_key] ON [subscription_payouts] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_payouts_monthly_settlement_id] ON [subscription_payouts] ([monthly_settlement_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_payouts_provider_profile_id] ON [subscription_payouts] ([provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_payouts_public_id] ON [subscription_payouts] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_payouts_updated_by_user_id] ON [subscription_payouts] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_refund_adjustments_created_by_user_id] ON [subscription_refund_adjustments] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_refund_adjustments_idempotency_key] ON [subscription_refund_adjustments] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_refund_adjustments_payment_request_id] ON [subscription_refund_adjustments] ([payment_request_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_refund_adjustments_processed_by_user_id] ON [subscription_refund_adjustments] ([processed_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_refund_adjustments_public_id] ON [subscription_refund_adjustments] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_refund_adjustments_subscription_contract_id_status_code] ON [subscription_refund_adjustments] ([subscription_contract_id], [status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_refund_adjustments_updated_by_user_id] ON [subscription_refund_adjustments] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_refund_adjustments_visit_schedule_id] ON [subscription_refund_adjustments] ([visit_schedule_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_settlement_items_created_by_user_id] ON [subscription_settlement_items] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_settlement_items_idempotency_key] ON [subscription_settlement_items] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_settlement_items_monthly_settlement_id] ON [subscription_settlement_items] ([monthly_settlement_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_settlement_items_provider_profile_id_settlement_month_status_code] ON [subscription_settlement_items] ([provider_profile_id], [settlement_month], [status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_settlement_items_public_id] ON [subscription_settlement_items] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_settlement_items_subscription_contract_id] ON [subscription_settlement_items] ([subscription_contract_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_subscription_settlement_items_subscription_visit_schedule_id] ON [subscription_settlement_items] ([subscription_visit_schedule_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    CREATE INDEX [IX_subscription_settlement_items_updated_by_user_id] ON [subscription_settlement_items] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    EXEC(N'CREATE TRIGGER [TR_subscription_payment_ledger_append_only]
    ON [subscription_payment_ledger]
    INSTEAD OF UPDATE, DELETE
    AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51000, N''구독 결제 원장은 수정하거나 삭제할 수 없습니다.'', 1;
    END')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    EXEC(N'CREATE TRIGGER [TR_subscription_payout_events_append_only]
    ON [subscription_payout_events]
    INSTEAD OF UPDATE, DELETE
    AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51001, N''구독 지급 이력은 수정하거나 삭제할 수 없습니다.'', 1;
    END')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810151708_ImplementSubscriptionBillingAndSettlement'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810151708_ImplementSubscriptionBillingAndSettlement', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    ALTER TABLE [work_completions] ADD [interior_project_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    ALTER TABLE [service_history_entries] ADD [interior_project_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    ALTER TABLE [quote_revisions] ADD [revision_purpose_code] varchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    ALTER TABLE [quote_items] ADD [item_category_code] varchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    ALTER TABLE [quote_items] ADD [labor_note_text] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    ALTER TABLE [quote_items] ADD [material_spec_text] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    ALTER TABLE [quote_items] ADD [space_text] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    ALTER TABLE [quote_items] ADD [work_trade_text] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    ALTER TABLE [dispute_cases] ADD [interior_contract_change_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    ALTER TABLE [dispute_cases] ADD [interior_project_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    ALTER TABLE [dispute_cases] ADD [interior_work_stage_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE TABLE [interior_contract_changes] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [interior_contract_id] bigint NOT NULL,
        [change_no] int NOT NULL,
        [requested_by_user_id] bigint NOT NULL,
        [change_type_code] varchar(30) NULL,
        [reason_text] nvarchar(2000) NOT NULL,
        [scope_change_text] nvarchar(4000) NOT NULL,
        [amount_delta] decimal(19,4) NOT NULL,
        [schedule_impact_days] int NULL,
        [status_code] varchar(30) NOT NULL,
        [requested_at] datetime2(7) NOT NULL,
        [customer_decided_at] datetime2(7) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_interior_contract_changes] PRIMARY KEY ([id]),
        CONSTRAINT [CK_interior_contract_changes_status] CHECK ([status_code] IN ('REQUESTED','APPROVED','REJECTED','CANCELLED')),
        CONSTRAINT [FK_interior_contract_changes_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_interior_contract_changes_users_requested_by_user_id] FOREIGN KEY ([requested_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_interior_contract_changes_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE TABLE [interior_contract_versions] (
        [id] bigint NOT NULL IDENTITY,
        [interior_contract_id] bigint NOT NULL,
        [version_no] int NOT NULL,
        [source_contract_change_id] bigint NULL,
        [contract_amount] decimal(19,4) NOT NULL,
        [currency_code] char(3) NOT NULL,
        [scope_snapshot_json] nvarchar(max) NOT NULL,
        [schedule_snapshot_json] nvarchar(max) NOT NULL,
        [payment_plan_snapshot_json] nvarchar(max) NOT NULL,
        [warranty_snapshot_json] nvarchar(max) NULL,
        [customer_agreed_at] datetime2(7) NULL,
        [provider_agreed_at] datetime2(7) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_interior_contract_versions] PRIMARY KEY ([id]),
        CONSTRAINT [FK_interior_contract_versions_interior_contract_changes_source_contract_change_id] FOREIGN KEY ([source_contract_change_id]) REFERENCES [interior_contract_changes] ([id]),
        CONSTRAINT [FK_interior_contract_versions_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE TABLE [interior_contracts] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [interior_project_id] bigint NOT NULL,
        [customer_profile_id] bigint NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [contract_version] int NOT NULL DEFAULT 1,
        [status_code] varchar(30) NOT NULL,
        [selected_quote_revision_id] bigint NOT NULL,
        [contract_amount] decimal(19,4) NOT NULL,
        [currency_code] char(3) NOT NULL,
        [scope_snapshot_json] nvarchar(max) NOT NULL,
        [quote_snapshot_json] nvarchar(max) NOT NULL,
        [schedule_snapshot_json] nvarchar(max) NOT NULL,
        [warranty_snapshot_json] nvarchar(max) NULL,
        [provider_trust_score_snapshot] decimal(9,4) NULL,
        [planned_start_date] date NOT NULL,
        [planned_completion_date] date NOT NULL,
        [customer_agreed_at] datetime2(7) NULL,
        [provider_agreed_at] datetime2(7) NULL,
        [effective_at] datetime2(7) NULL,
        [terminated_at] datetime2(7) NULL,
        [termination_reason] nvarchar(2000) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_interior_contracts] PRIMARY KEY ([id]),
        CONSTRAINT [CK_interior_contracts_amount] CHECK ([contract_amount] >= 0),
        CONSTRAINT [CK_interior_contracts_period] CHECK ([planned_completion_date] >= [planned_start_date]),
        CONSTRAINT [FK_interior_contracts_customer_profiles_customer_profile_id] FOREIGN KEY ([customer_profile_id]) REFERENCES [customer_profiles] ([id]),
        CONSTRAINT [FK_interior_contracts_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_interior_contracts_quote_revisions_selected_quote_revision_id] FOREIGN KEY ([selected_quote_revision_id]) REFERENCES [quote_revisions] ([id]),
        CONSTRAINT [FK_interior_contracts_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_interior_contracts_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE TABLE [interior_payment_plans] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [interior_contract_id] bigint NOT NULL,
        [sequence_no] int NOT NULL,
        [payment_name] nvarchar(200) NOT NULL,
        [planned_amount] decimal(19,4) NOT NULL,
        [planned_due_date] date NULL,
        [condition_text] nvarchar(2000) NULL,
        [status_code] varchar(30) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_interior_payment_plans] PRIMARY KEY ([id]),
        CONSTRAINT [CK_interior_payment_plans_amount] CHECK ([planned_amount] >= 0),
        CONSTRAINT [FK_interior_payment_plans_interior_contracts_interior_contract_id] FOREIGN KEY ([interior_contract_id]) REFERENCES [interior_contracts] ([id]),
        CONSTRAINT [FK_interior_payment_plans_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_interior_payment_plans_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE TABLE [interior_projects] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [service_request_id] bigint NOT NULL,
        [customer_profile_id] bigint NOT NULL,
        [service_category_id] bigint NOT NULL,
        [status_code] varchar(40) NOT NULL,
        [selected_site_visit_provider_id] bigint NULL,
        [selected_contractor_provider_id] bigint NULL,
        [site_visit_selected_at] datetime2(7) NULL,
        [contractor_selected_at] datetime2(7) NULL,
        [current_quote_revision_id] bigint NULL,
        [current_contract_id] bigint NULL,
        [site_visit_provider_trust_score_snapshot] decimal(9,4) NULL,
        [contractor_trust_score_snapshot] decimal(9,4) NULL,
        [fee_assessment_status_code] varchar(30) NOT NULL DEFAULT 'POLICY_PENDING',
        [project_start_date] date NULL,
        [expected_completion_date] date NULL,
        [actual_completion_date] date NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_interior_projects] PRIMARY KEY ([id]),
        CONSTRAINT [CK_interior_projects_fee] CHECK ([fee_assessment_status_code] IN ('POLICY_PENDING','NOT_APPLICABLE','ASSESSED')),
        CONSTRAINT [CK_interior_projects_status] CHECK ([status_code] IN ('CONSULTATION','SITE_VISIT_SELECTION','SITE_VISIT_SCHEDULED','SITE_VISIT_COMPLETED','ESTIMATE_IN_PROGRESS','ESTIMATE_READY','CONTRACT_PENDING','CONTRACTED','CONSTRUCTION','INSPECTION','COMPLETED','DEFECT_MANAGEMENT','CANCELLED')),
        CONSTRAINT [FK_interior_projects_customer_profiles_customer_profile_id] FOREIGN KEY ([customer_profile_id]) REFERENCES [customer_profiles] ([id]),
        CONSTRAINT [FK_interior_projects_interior_contracts_current_contract_id] FOREIGN KEY ([current_contract_id]) REFERENCES [interior_contracts] ([id]),
        CONSTRAINT [FK_interior_projects_provider_profiles_selected_contractor_provider_id] FOREIGN KEY ([selected_contractor_provider_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_interior_projects_provider_profiles_selected_site_visit_provider_id] FOREIGN KEY ([selected_site_visit_provider_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_interior_projects_quote_revisions_current_quote_revision_id] FOREIGN KEY ([current_quote_revision_id]) REFERENCES [quote_revisions] ([id]),
        CONSTRAINT [FK_interior_projects_service_categories_service_category_id] FOREIGN KEY ([service_category_id]) REFERENCES [service_categories] ([id]),
        CONSTRAINT [FK_interior_projects_service_requests_service_request_id] FOREIGN KEY ([service_request_id]) REFERENCES [service_requests] ([id]),
        CONSTRAINT [FK_interior_projects_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_interior_projects_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE TABLE [interior_payment_confirmations] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [payment_plan_id] bigint NOT NULL,
        [confirmed_by_user_id] bigint NOT NULL,
        [confirmation_type_code] varchar(30) NOT NULL,
        [confirmed_amount] decimal(19,4) NOT NULL,
        [confirmed_at] datetime2(7) NOT NULL,
        [evidence_file_id] bigint NULL,
        [note_text] nvarchar(2000) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_interior_payment_confirmations] PRIMARY KEY ([id]),
        CONSTRAINT [CK_interior_payment_confirmations_amount] CHECK ([confirmed_amount] >= 0),
        CONSTRAINT [FK_interior_payment_confirmations_files_evidence_file_id] FOREIGN KEY ([evidence_file_id]) REFERENCES [files] ([id]),
        CONSTRAINT [FK_interior_payment_confirmations_interior_payment_plans_payment_plan_id] FOREIGN KEY ([payment_plan_id]) REFERENCES [interior_payment_plans] ([id]),
        CONSTRAINT [FK_interior_payment_confirmations_users_confirmed_by_user_id] FOREIGN KEY ([confirmed_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE TABLE [interior_design_versions] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [interior_project_id] bigint NOT NULL,
        [version_no] int NOT NULL,
        [title] nvarchar(300) NOT NULL,
        [description] nvarchar(4000) NULL,
        [status_code] varchar(30) NOT NULL,
        [customer_approved_at] datetime2(7) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_interior_design_versions] PRIMARY KEY ([id]),
        CONSTRAINT [FK_interior_design_versions_interior_projects_interior_project_id] FOREIGN KEY ([interior_project_id]) REFERENCES [interior_projects] ([id]),
        CONSTRAINT [FK_interior_design_versions_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_interior_design_versions_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE TABLE [interior_project_events] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [interior_project_id] bigint NOT NULL,
        [event_type_code] varchar(50) NOT NULL,
        [actor_user_id] bigint NULL,
        [idempotency_key] varchar(150) NOT NULL,
        [event_data_json] nvarchar(max) NULL,
        [occurred_at] datetime2(7) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_interior_project_events] PRIMARY KEY ([id]),
        CONSTRAINT [FK_interior_project_events_interior_projects_interior_project_id] FOREIGN KEY ([interior_project_id]) REFERENCES [interior_projects] ([id]),
        CONSTRAINT [FK_interior_project_events_users_actor_user_id] FOREIGN KEY ([actor_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE TABLE [interior_site_visits] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [interior_project_id] bigint NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [status_code] varchar(30) NOT NULL,
        [proposed_at] datetime2(7) NULL,
        [confirmed_at] datetime2(7) NULL,
        [scheduled_start_at] datetime2(7) NOT NULL,
        [scheduled_end_at] datetime2(7) NULL,
        [access_condition_text] nvarchar(2000) NULL,
        [visited_at] datetime2(7) NULL,
        [completed_at] datetime2(7) NULL,
        [measurement_summary_text] nvarchar(4000) NULL,
        [constraint_text] nvarchar(4000) NULL,
        [risk_note_text] nvarchar(4000) NULL,
        [customer_confirmed_at] datetime2(7) NULL,
        [cancelled_at] datetime2(7) NULL,
        [cancellation_reason] nvarchar(2000) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_interior_site_visits] PRIMARY KEY ([id]),
        CONSTRAINT [CK_interior_site_visits_status] CHECK ([status_code] IN ('PROPOSED','CONFIRMED','COMPLETED','CANCELLED','NO_SHOW')),
        CONSTRAINT [FK_interior_site_visits_interior_projects_interior_project_id] FOREIGN KEY ([interior_project_id]) REFERENCES [interior_projects] ([id]),
        CONSTRAINT [FK_interior_site_visits_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_interior_site_visits_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_interior_site_visits_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE TABLE [interior_work_stages] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [interior_project_id] bigint NOT NULL,
        [sequence_no] int NOT NULL,
        [stage_name] nvarchar(200) NOT NULL,
        [planned_start_date] date NOT NULL,
        [planned_end_date] date NOT NULL,
        [actual_start_at] datetime2(7) NULL,
        [actual_end_at] datetime2(7) NULL,
        [progress_percent] int NOT NULL,
        [status_code] varchar(30) NOT NULL,
        [provider_note] nvarchar(2000) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_interior_work_stages] PRIMARY KEY ([id]),
        CONSTRAINT [CK_interior_work_stages_period] CHECK ([planned_end_date] >= [planned_start_date]),
        CONSTRAINT [CK_interior_work_stages_progress] CHECK ([progress_percent] BETWEEN 0 AND 100),
        CONSTRAINT [FK_interior_work_stages_interior_projects_interior_project_id] FOREIGN KEY ([interior_project_id]) REFERENCES [interior_projects] ([id]),
        CONSTRAINT [FK_interior_work_stages_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_interior_work_stages_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE TABLE [interior_design_files] (
        [id] bigint NOT NULL IDENTITY,
        [design_version_id] bigint NOT NULL,
        [file_id] bigint NOT NULL,
        [purpose_code] varchar(50) NOT NULL,
        [display_order] int NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_interior_design_files] PRIMARY KEY ([id]),
        CONSTRAINT [FK_interior_design_files_files_file_id] FOREIGN KEY ([file_id]) REFERENCES [files] ([id]),
        CONSTRAINT [FK_interior_design_files_interior_design_versions_design_version_id] FOREIGN KEY ([design_version_id]) REFERENCES [interior_design_versions] ([id]),
        CONSTRAINT [FK_interior_design_files_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE TABLE [interior_site_visit_files] (
        [id] bigint NOT NULL IDENTITY,
        [site_visit_id] bigint NOT NULL,
        [file_id] bigint NOT NULL,
        [purpose_code] varchar(50) NOT NULL,
        [display_order] int NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_interior_site_visit_files] PRIMARY KEY ([id]),
        CONSTRAINT [FK_interior_site_visit_files_files_file_id] FOREIGN KEY ([file_id]) REFERENCES [files] ([id]),
        CONSTRAINT [FK_interior_site_visit_files_interior_site_visits_site_visit_id] FOREIGN KEY ([site_visit_id]) REFERENCES [interior_site_visits] ([id]),
        CONSTRAINT [FK_interior_site_visit_files_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE TABLE [interior_site_visit_measurements] (
        [id] bigint NOT NULL IDENTITY,
        [site_visit_id] bigint NOT NULL,
        [measurement_key] varchar(100) NOT NULL,
        [measurement_value] decimal(19,4) NULL,
        [measurement_text] nvarchar(1000) NULL,
        [unit_text] nvarchar(50) NULL,
        [location_text] nvarchar(500) NULL,
        [note_text] nvarchar(2000) NULL,
        [additional_data_json] nvarchar(max) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_interior_site_visit_measurements] PRIMARY KEY ([id]),
        CONSTRAINT [FK_interior_site_visit_measurements_interior_site_visits_site_visit_id] FOREIGN KEY ([site_visit_id]) REFERENCES [interior_site_visits] ([id]),
        CONSTRAINT [FK_interior_site_visit_measurements_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE TABLE [interior_defects] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [interior_project_id] bigint NOT NULL,
        [after_service_case_id] bigint NOT NULL,
        [work_stage_id] bigint NULL,
        [contract_version] int NULL,
        [defect_location_text] nvarchar(1000) NOT NULL,
        [defect_description] nvarchar(4000) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_interior_defects] PRIMARY KEY ([id]),
        CONSTRAINT [FK_interior_defects_after_service_cases_after_service_case_id] FOREIGN KEY ([after_service_case_id]) REFERENCES [after_service_cases] ([id]),
        CONSTRAINT [FK_interior_defects_interior_projects_interior_project_id] FOREIGN KEY ([interior_project_id]) REFERENCES [interior_projects] ([id]),
        CONSTRAINT [FK_interior_defects_interior_work_stages_work_stage_id] FOREIGN KEY ([work_stage_id]) REFERENCES [interior_work_stages] ([id]),
        CONSTRAINT [FK_interior_defects_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE TABLE [interior_stage_inspections] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [work_stage_id] bigint NOT NULL,
        [inspection_status_code] varchar(30) NOT NULL,
        [inspected_by_user_id] bigint NOT NULL,
        [checklist_json] nvarchar(max) NULL,
        [result_text] nvarchar(4000) NOT NULL,
        [requested_correction_text] nvarchar(4000) NULL,
        [inspected_at] datetime2(7) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_interior_stage_inspections] PRIMARY KEY ([id]),
        CONSTRAINT [FK_interior_stage_inspections_interior_work_stages_work_stage_id] FOREIGN KEY ([work_stage_id]) REFERENCES [interior_work_stages] ([id]),
        CONSTRAINT [FK_interior_stage_inspections_users_inspected_by_user_id] FOREIGN KEY ([inspected_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE TABLE [interior_work_updates] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [work_stage_id] bigint NOT NULL,
        [progress_percent] int NOT NULL,
        [update_text] nvarchar(4000) NOT NULL,
        [issue_text] nvarchar(4000) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_interior_work_updates] PRIMARY KEY ([id]),
        CONSTRAINT [CK_interior_work_updates_progress] CHECK ([progress_percent] BETWEEN 0 AND 100),
        CONSTRAINT [FK_interior_work_updates_interior_work_stages_work_stage_id] FOREIGN KEY ([work_stage_id]) REFERENCES [interior_work_stages] ([id]),
        CONSTRAINT [FK_interior_work_updates_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE TABLE [interior_work_update_files] (
        [id] bigint NOT NULL IDENTITY,
        [work_update_id] bigint NOT NULL,
        [file_id] bigint NOT NULL,
        [purpose_code] varchar(50) NOT NULL,
        [display_order] int NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_interior_work_update_files] PRIMARY KEY ([id]),
        CONSTRAINT [FK_interior_work_update_files_files_file_id] FOREIGN KEY ([file_id]) REFERENCES [files] ([id]),
        CONSTRAINT [FK_interior_work_update_files_interior_work_updates_work_update_id] FOREIGN KEY ([work_update_id]) REFERENCES [interior_work_updates] ([id]),
        CONSTRAINT [FK_interior_work_update_files_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_work_completions_interior_project_id] ON [work_completions] ([interior_project_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_service_history_entries_interior_project_id] ON [service_history_entries] ([interior_project_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_dispute_cases_interior_contract_change_id] ON [dispute_cases] ([interior_contract_change_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_dispute_cases_interior_project_id] ON [dispute_cases] ([interior_project_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_dispute_cases_interior_work_stage_id] ON [dispute_cases] ([interior_work_stage_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_contract_changes_created_by_user_id] ON [interior_contract_changes] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_contract_changes_interior_contract_id_change_no] ON [interior_contract_changes] ([interior_contract_id], [change_no]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_contract_changes_public_id] ON [interior_contract_changes] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_contract_changes_requested_by_user_id] ON [interior_contract_changes] ([requested_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_contract_changes_updated_by_user_id] ON [interior_contract_changes] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_contract_versions_created_by_user_id] ON [interior_contract_versions] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_contract_versions_interior_contract_id_version_no] ON [interior_contract_versions] ([interior_contract_id], [version_no]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_contract_versions_source_contract_change_id] ON [interior_contract_versions] ([source_contract_change_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_contracts_created_by_user_id] ON [interior_contracts] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_contracts_customer_profile_id] ON [interior_contracts] ([customer_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_contracts_interior_project_id_contract_version] ON [interior_contracts] ([interior_project_id], [contract_version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_contracts_provider_profile_id] ON [interior_contracts] ([provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_contracts_public_id] ON [interior_contracts] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_contracts_selected_quote_revision_id] ON [interior_contracts] ([selected_quote_revision_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_contracts_updated_by_user_id] ON [interior_contracts] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_defects_after_service_case_id] ON [interior_defects] ([after_service_case_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_defects_created_by_user_id] ON [interior_defects] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_defects_interior_project_id] ON [interior_defects] ([interior_project_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_defects_public_id] ON [interior_defects] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_defects_work_stage_id] ON [interior_defects] ([work_stage_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_design_files_created_by_user_id] ON [interior_design_files] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_design_files_design_version_id_file_id] ON [interior_design_files] ([design_version_id], [file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_design_files_file_id] ON [interior_design_files] ([file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_design_versions_created_by_user_id] ON [interior_design_versions] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_design_versions_interior_project_id_version_no] ON [interior_design_versions] ([interior_project_id], [version_no]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_design_versions_public_id] ON [interior_design_versions] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_design_versions_updated_by_user_id] ON [interior_design_versions] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_payment_confirmations_confirmed_by_user_id] ON [interior_payment_confirmations] ([confirmed_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_payment_confirmations_evidence_file_id] ON [interior_payment_confirmations] ([evidence_file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_payment_confirmations_payment_plan_id_confirmation_type_code_confirmed_by_user_id] ON [interior_payment_confirmations] ([payment_plan_id], [confirmation_type_code], [confirmed_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_payment_confirmations_public_id] ON [interior_payment_confirmations] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_payment_plans_created_by_user_id] ON [interior_payment_plans] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_payment_plans_interior_contract_id_sequence_no] ON [interior_payment_plans] ([interior_contract_id], [sequence_no]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_payment_plans_public_id] ON [interior_payment_plans] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_payment_plans_updated_by_user_id] ON [interior_payment_plans] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_project_events_actor_user_id] ON [interior_project_events] ([actor_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_project_events_idempotency_key] ON [interior_project_events] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_project_events_interior_project_id_occurred_at] ON [interior_project_events] ([interior_project_id], [occurred_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_project_events_public_id] ON [interior_project_events] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_projects_created_by_user_id] ON [interior_projects] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_projects_current_contract_id] ON [interior_projects] ([current_contract_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_projects_current_quote_revision_id] ON [interior_projects] ([current_quote_revision_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_projects_customer_profile_id] ON [interior_projects] ([customer_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_projects_public_id] ON [interior_projects] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_projects_selected_contractor_provider_id] ON [interior_projects] ([selected_contractor_provider_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_projects_selected_site_visit_provider_id] ON [interior_projects] ([selected_site_visit_provider_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_projects_service_category_id] ON [interior_projects] ([service_category_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_projects_service_request_id] ON [interior_projects] ([service_request_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_projects_status_code_updated_at] ON [interior_projects] ([status_code], [updated_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_projects_updated_by_user_id] ON [interior_projects] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_site_visit_files_created_by_user_id] ON [interior_site_visit_files] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_site_visit_files_file_id] ON [interior_site_visit_files] ([file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_site_visit_files_site_visit_id_file_id] ON [interior_site_visit_files] ([site_visit_id], [file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_site_visit_measurements_created_by_user_id] ON [interior_site_visit_measurements] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_site_visit_measurements_site_visit_id_measurement_key] ON [interior_site_visit_measurements] ([site_visit_id], [measurement_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_site_visits_created_by_user_id] ON [interior_site_visits] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_site_visits_interior_project_id_provider_profile_id_scheduled_start_at] ON [interior_site_visits] ([interior_project_id], [provider_profile_id], [scheduled_start_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_site_visits_provider_profile_id] ON [interior_site_visits] ([provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_site_visits_public_id] ON [interior_site_visits] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_site_visits_status_code_scheduled_start_at] ON [interior_site_visits] ([status_code], [scheduled_start_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_site_visits_updated_by_user_id] ON [interior_site_visits] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_stage_inspections_inspected_by_user_id] ON [interior_stage_inspections] ([inspected_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_stage_inspections_public_id] ON [interior_stage_inspections] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_stage_inspections_work_stage_id_inspected_at] ON [interior_stage_inspections] ([work_stage_id], [inspected_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_work_stages_created_by_user_id] ON [interior_work_stages] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_work_stages_interior_project_id_sequence_no] ON [interior_work_stages] ([interior_project_id], [sequence_no]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_work_stages_public_id] ON [interior_work_stages] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_work_stages_updated_by_user_id] ON [interior_work_stages] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_work_update_files_created_by_user_id] ON [interior_work_update_files] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_work_update_files_file_id] ON [interior_work_update_files] ([file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_work_update_files_work_update_id_file_id] ON [interior_work_update_files] ([work_update_id], [file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_work_updates_created_by_user_id] ON [interior_work_updates] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_work_updates_public_id] ON [interior_work_updates] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    CREATE INDEX [IX_interior_work_updates_work_stage_id_created_at] ON [interior_work_updates] ([work_stage_id], [created_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    ALTER TABLE [dispute_cases] ADD CONSTRAINT [FK_dispute_cases_interior_contract_changes_interior_contract_change_id] FOREIGN KEY ([interior_contract_change_id]) REFERENCES [interior_contract_changes] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    ALTER TABLE [dispute_cases] ADD CONSTRAINT [FK_dispute_cases_interior_projects_interior_project_id] FOREIGN KEY ([interior_project_id]) REFERENCES [interior_projects] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    ALTER TABLE [dispute_cases] ADD CONSTRAINT [FK_dispute_cases_interior_work_stages_interior_work_stage_id] FOREIGN KEY ([interior_work_stage_id]) REFERENCES [interior_work_stages] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    ALTER TABLE [service_history_entries] ADD CONSTRAINT [FK_service_history_entries_interior_projects_interior_project_id] FOREIGN KEY ([interior_project_id]) REFERENCES [interior_projects] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    ALTER TABLE [work_completions] ADD CONSTRAINT [FK_work_completions_interior_projects_interior_project_id] FOREIGN KEY ([interior_project_id]) REFERENCES [interior_projects] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    ALTER TABLE [interior_contract_changes] ADD CONSTRAINT [FK_interior_contract_changes_interior_contracts_interior_contract_id] FOREIGN KEY ([interior_contract_id]) REFERENCES [interior_contracts] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    ALTER TABLE [interior_contract_versions] ADD CONSTRAINT [FK_interior_contract_versions_interior_contracts_interior_contract_id] FOREIGN KEY ([interior_contract_id]) REFERENCES [interior_contracts] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    ALTER TABLE [interior_contracts] ADD CONSTRAINT [FK_interior_contracts_interior_projects_interior_project_id] FOREIGN KEY ([interior_project_id]) REFERENCES [interior_projects] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    EXEC(N'CREATE TRIGGER [dbo].[TR_interior_project_events_append_only]
    ON [dbo].[interior_project_events]
    AFTER UPDATE, DELETE
    AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51000, ''interior_project_events is append-only.'', 1;
    END')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810163228_ImplementInteriorProjectCore'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810163228_ImplementInteriorProjectCore', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notification_deliveries] DROP CONSTRAINT [CK_notification_deliveries_channel];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notification_deliveries] DROP CONSTRAINT [CK_notification_deliveries_status];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD [after_service_case_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD [dispute_case_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD [expires_at] datetime2(7) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD [interior_project_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD [priority_code] varchar(20) NOT NULL DEFAULT 'NORMAL';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD [quote_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD [review_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD [sanction_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD [service_request_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD [source_public_id] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD [source_type_code] varchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD [subscription_contract_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD [subscription_visit_schedule_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD [target_public_id] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD [target_type_code] varchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD [template_code_snapshot] varchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD [template_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD [transaction_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notification_deliveries] ADD [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME());
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notification_deliveries] ADD [delivered_at] datetime2(7) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notification_deliveries] ADD [external_provider_code] varchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notification_deliveries] ADD [failed_at] datetime2(7) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notification_deliveries] ADD [notification_recipient_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notification_deliveries] ADD [public_id] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notification_deliveries] ADD [retry_count] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notification_deliveries] ADD [row_version] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notification_deliveries] ADD [scheduled_at] datetime2(7) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notification_deliveries] ADD [sent_at] datetime2(7) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notification_deliveries] ADD [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME());
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE TABLE [notification_delivery_attempts] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [notification_delivery_id] bigint NOT NULL,
        [attempt_no] int NOT NULL,
        [started_at] datetime2(7) NOT NULL,
        [completed_at] datetime2(7) NULL,
        [result_code] varchar(30) NOT NULL,
        [provider_response_code] varchar(100) NULL,
        [failure_reason] nvarchar(1000) NULL,
        [correlation_id] uniqueidentifier NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_notification_delivery_attempts] PRIMARY KEY ([id]),
        CONSTRAINT [FK_notification_delivery_attempts_notification_deliveries_notification_delivery_id] FOREIGN KEY ([notification_delivery_id]) REFERENCES [notification_deliveries] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE TABLE [notification_preferences] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [user_id] bigint NOT NULL,
        [event_group_code] varchar(50) NOT NULL,
        [web_enabled] bit NOT NULL DEFAULT CAST(1 AS bit),
        [kakao_enabled] bit NOT NULL DEFAULT CAST(1 AS bit),
        [sms_enabled] bit NOT NULL DEFAULT CAST(0 AS bit),
        [email_enabled] bit NOT NULL DEFAULT CAST(0 AS bit),
        [push_enabled] bit NOT NULL DEFAULT CAST(0 AS bit),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_notification_preferences] PRIMARY KEY ([id]),
        CONSTRAINT [FK_notification_preferences_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE TABLE [notification_recipients] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [notification_id] bigint NOT NULL,
        [user_id] bigint NOT NULL,
        [recipient_role_code] varchar(30) NOT NULL,
        [read_at] datetime2(7) NULL,
        [archived_at] datetime2(7) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_notification_recipients] PRIMARY KEY ([id]),
        CONSTRAINT [FK_notification_recipients_notifications_notification_id] FOREIGN KEY ([notification_id]) REFERENCES [notifications] ([id]),
        CONSTRAINT [FK_notification_recipients_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE TABLE [notification_templates] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [template_code] varchar(100) NOT NULL,
        [name] nvarchar(200) NOT NULL,
        [description] nvarchar(2000) NULL,
        [audience_type_code] varchar(30) NOT NULL,
        [event_type_code] varchar(100) NOT NULL,
        [channel_code] varchar(20) NOT NULL,
        [title_template] nvarchar(300) NOT NULL,
        [body_template] nvarchar(3000) NOT NULL,
        [allowed_variables_json] nvarchar(max) NOT NULL,
        [is_required_business_notice] bit NOT NULL DEFAULT CAST(0 AS bit),
        [is_marketing] bit NOT NULL DEFAULT CAST(0 AS bit),
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [effective_from] datetime2(7) NULL,
        [effective_to] datetime2(7) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_notification_templates] PRIMARY KEY ([id]),
        CONSTRAINT [CK_notification_templates_audience] CHECK ([audience_type_code] IN ('CUSTOMER','PROVIDER','ADMIN','ALL')),
        CONSTRAINT [CK_notification_templates_channel] CHECK ([channel_code] IN ('WEB','KAKAO','SMS','EMAIL','PUSH')),
        CONSTRAINT [CK_notification_templates_period] CHECK ([effective_to] IS NULL OR [effective_from] IS NULL OR [effective_to] > [effective_from]),
        CONSTRAINT [CK_notification_templates_variables_json] CHECK (ISJSON([allowed_variables_json]) = 1),
        CONSTRAINT [FK_notification_templates_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_notification_templates_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE TABLE [notification_events] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [notification_id] bigint NOT NULL,
        [notification_recipient_id] bigint NULL,
        [notification_delivery_id] bigint NULL,
        [actor_user_id] bigint NULL,
        [event_type_code] varchar(50) NOT NULL,
        [idempotency_key] varchar(180) NOT NULL,
        [event_data_json] nvarchar(max) NULL,
        [occurred_at] datetime2(7) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_notification_events] PRIMARY KEY ([id]),
        CONSTRAINT [CK_notification_events_data_json] CHECK ([event_data_json] IS NULL OR ISJSON([event_data_json]) = 1),
        CONSTRAINT [FK_notification_events_notification_deliveries_notification_delivery_id] FOREIGN KEY ([notification_delivery_id]) REFERENCES [notification_deliveries] ([id]),
        CONSTRAINT [FK_notification_events_notification_recipients_notification_recipient_id] FOREIGN KEY ([notification_recipient_id]) REFERENCES [notification_recipients] ([id]),
        CONSTRAINT [FK_notification_events_notifications_notification_id] FOREIGN KEY ([notification_id]) REFERENCES [notifications] ([id]),
        CONSTRAINT [FK_notification_events_users_actor_user_id] FOREIGN KEY ([actor_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    UPDATE [notification_deliveries]
    SET [public_id] = NEWID()
    WHERE [public_id] IS NULL;

    INSERT INTO [notification_recipients]
        ([public_id], [notification_id], [user_id], [recipient_role_code], [read_at], [archived_at], [created_at])
    SELECT
        NEWID(),
        n.[id],
        n.[recipient_user_id],
        COALESCE((
            SELECT TOP (1) r.[code]
            FROM [user_roles] ur
            INNER JOIN [roles] r ON r.[id] = ur.[role_id]
            WHERE ur.[user_id] = n.[recipient_user_id]
              AND ur.[revoked_at] IS NULL
              AND r.[is_active] = 1
            ORDER BY CASE r.[code]
                WHEN 'ADMIN' THEN 1
                WHEN 'PROVIDER' THEN 2
                WHEN 'CUSTOMER' THEN 3
                ELSE 4
            END
        ), 'CUSTOMER'),
        n.[read_at],
        NULL,
        n.[recorded_at]
    FROM [notifications] n
    WHERE NOT EXISTS (
        SELECT 1
        FROM [notification_recipients] nr
        WHERE nr.[notification_id] = n.[id]
          AND nr.[user_id] = n.[recipient_user_id]
    );

    UPDATE d
    SET d.[notification_recipient_id] = nr.[id]
    FROM [notification_deliveries] d
    INNER JOIN [notifications] n ON n.[id] = d.[notification_id]
    INNER JOIN [notification_recipients] nr
        ON nr.[notification_id] = n.[id]
       AND nr.[user_id] = n.[recipient_user_id]
    WHERE d.[notification_recipient_id] IS NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[notification_deliveries]') AND [c].[name] = N'public_id');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [notification_deliveries] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [notification_deliveries] ALTER COLUMN [public_id] uniqueidentifier NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE INDEX [IX_notifications_after_service_case_id] ON [notifications] ([after_service_case_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE INDEX [IX_notifications_dispute_case_id] ON [notifications] ([dispute_case_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE INDEX [IX_notifications_interior_project_id] ON [notifications] ([interior_project_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE INDEX [IX_notifications_quote_id] ON [notifications] ([quote_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE INDEX [IX_notifications_review_id] ON [notifications] ([review_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE INDEX [IX_notifications_sanction_id] ON [notifications] ([sanction_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE INDEX [IX_notifications_service_request_id] ON [notifications] ([service_request_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE INDEX [IX_notifications_subscription_contract_id] ON [notifications] ([subscription_contract_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE INDEX [IX_notifications_subscription_visit_schedule_id] ON [notifications] ([subscription_visit_schedule_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE INDEX [IX_notifications_template_id] ON [notifications] ([template_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE INDEX [IX_notifications_transaction_id] ON [notifications] ([transaction_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE INDEX [IX_notification_deliveries_notification_recipient_id] ON [notification_deliveries] ([notification_recipient_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_notification_deliveries_public_id] ON [notification_deliveries] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    EXEC(N'ALTER TABLE [notification_deliveries] ADD CONSTRAINT [CK_notification_deliveries_channel] CHECK ([channel_code] IN (''WEB'',''IN_APP'',''KAKAO'',''ALIMTALK'',''SMS'',''EMAIL'',''PUSH''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    EXEC(N'ALTER TABLE [notification_deliveries] ADD CONSTRAINT [CK_notification_deliveries_status] CHECK ([status_code] IN (''PENDING'',''PROCESSING'',''SENT'',''DELIVERED'',''FAILED'',''CANCELLED'',''SKIPPED''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_notification_delivery_attempts_notification_delivery_id_attempt_no] ON [notification_delivery_attempts] ([notification_delivery_id], [attempt_no]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_notification_delivery_attempts_public_id] ON [notification_delivery_attempts] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE INDEX [IX_notification_events_actor_user_id] ON [notification_events] ([actor_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_notification_events_idempotency_key] ON [notification_events] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE INDEX [IX_notification_events_notification_delivery_id] ON [notification_events] ([notification_delivery_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE INDEX [IX_notification_events_notification_id_occurred_at] ON [notification_events] ([notification_id], [occurred_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE INDEX [IX_notification_events_notification_recipient_id] ON [notification_events] ([notification_recipient_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_notification_events_public_id] ON [notification_events] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_notification_preferences_public_id] ON [notification_preferences] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_notification_preferences_user_id_event_group_code] ON [notification_preferences] ([user_id], [event_group_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_notification_recipients_notification_id_user_id] ON [notification_recipients] ([notification_id], [user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_notification_recipients_public_id] ON [notification_recipients] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE INDEX [IX_notification_recipients_user_id_archived_at_read_at_created_at] ON [notification_recipients] ([user_id], [archived_at], [read_at], [created_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE INDEX [IX_notification_templates_created_by_user_id] ON [notification_templates] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE INDEX [IX_notification_templates_event_type_code_is_active] ON [notification_templates] ([event_type_code], [is_active]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_notification_templates_public_id] ON [notification_templates] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE UNIQUE INDEX [IX_notification_templates_template_code_channel_code] ON [notification_templates] ([template_code], [channel_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    CREATE INDEX [IX_notification_templates_updated_by_user_id] ON [notification_templates] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notification_deliveries] ADD CONSTRAINT [FK_notification_deliveries_notification_recipients_notification_recipient_id] FOREIGN KEY ([notification_recipient_id]) REFERENCES [notification_recipients] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD CONSTRAINT [FK_notifications_after_service_cases_after_service_case_id] FOREIGN KEY ([after_service_case_id]) REFERENCES [after_service_cases] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD CONSTRAINT [FK_notifications_dispute_cases_dispute_case_id] FOREIGN KEY ([dispute_case_id]) REFERENCES [dispute_cases] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD CONSTRAINT [FK_notifications_interior_projects_interior_project_id] FOREIGN KEY ([interior_project_id]) REFERENCES [interior_projects] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD CONSTRAINT [FK_notifications_notification_templates_template_id] FOREIGN KEY ([template_id]) REFERENCES [notification_templates] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD CONSTRAINT [FK_notifications_quotes_quote_id] FOREIGN KEY ([quote_id]) REFERENCES [quotes] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD CONSTRAINT [FK_notifications_reviews_review_id] FOREIGN KEY ([review_id]) REFERENCES [reviews] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD CONSTRAINT [FK_notifications_sanctions_sanction_id] FOREIGN KEY ([sanction_id]) REFERENCES [sanctions] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD CONSTRAINT [FK_notifications_service_requests_service_request_id] FOREIGN KEY ([service_request_id]) REFERENCES [service_requests] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD CONSTRAINT [FK_notifications_subscription_contracts_subscription_contract_id] FOREIGN KEY ([subscription_contract_id]) REFERENCES [subscription_contracts] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD CONSTRAINT [FK_notifications_subscription_visit_schedules_subscription_visit_schedule_id] FOREIGN KEY ([subscription_visit_schedule_id]) REFERENCES [subscription_visit_schedules] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    ALTER TABLE [notifications] ADD CONSTRAINT [FK_notifications_transactions_transaction_id] FOREIGN KEY ([transaction_id]) REFERENCES [transactions] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    EXEC(N'CREATE TRIGGER [TR_notification_events_append_only]
    ON [notification_events]
    INSTEAD OF UPDATE, DELETE
    AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51000, ''notification_events is append-only.'', 1;
    END;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    EXEC(N'CREATE TRIGGER [TR_notification_delivery_attempts_append_only]
    ON [notification_delivery_attempts]
    INSTEAD OF UPDATE, DELETE
    AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51000, ''notification_delivery_attempts is append-only.'', 1;
    END;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810165407_ImplementNotificationManagement'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810165407_ImplementNotificationManagement', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811090000_ProtectAuditLogsAppendOnly'
)
BEGIN
    EXEC(N'CREATE TRIGGER [TR_audit_logs_append_only]
    ON [audit_logs]
    INSTEAD OF UPDATE, DELETE
    AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51000, N''감사로그는 수정하거나 삭제할 수 없습니다.'', 1;
    END;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811090000_ProtectAuditLogsAppendOnly'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260811090000_ProtectAuditLogsAppendOnly', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    ALTER TABLE [users] ADD [email_verification_status_code] varchar(30) NOT NULL DEFAULT 'NOT_INTEGRATED';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    ALTER TABLE [users] ADD [normalized_email] nvarchar(320) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    ALTER TABLE [users] ADD [phone_verification_status_code] varchar(30) NOT NULL DEFAULT 'NOT_INTEGRATED';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE TABLE [customer_addresses] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [customer_profile_id] bigint NOT NULL,
        [address_name] nvarchar(100) NOT NULL,
        [recipient_name] nvarchar(100) NULL,
        [postal_code] nvarchar(10) NOT NULL,
        [road_address] nvarchar(500) NOT NULL,
        [detail_address] nvarchar(500) NOT NULL,
        [administrative_area_id] bigint NULL,
        [latitude] decimal(10,7) NULL,
        [longitude] decimal(10,7) NULL,
        [is_default] bit NOT NULL DEFAULT CAST(0 AS bit),
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_customer_addresses] PRIMARY KEY ([id]),
        CONSTRAINT [FK_customer_addresses_administrative_areas_administrative_area_id] FOREIGN KEY ([administrative_area_id]) REFERENCES [administrative_areas] ([id]),
        CONSTRAINT [FK_customer_addresses_customer_profiles_customer_profile_id] FOREIGN KEY ([customer_profile_id]) REFERENCES [customer_profiles] ([id]),
        CONSTRAINT [FK_customer_addresses_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_customer_addresses_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE TABLE [customer_withdrawal_requests] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [user_id] bigint NOT NULL,
        [scope_code] varchar(30) NOT NULL,
        [status_code] varchar(20) NOT NULL DEFAULT 'REQUESTED',
        [reason] nvarchar(1000) NULL,
        [requested_at] datetime2(7) NOT NULL,
        [processed_at] datetime2(7) NULL,
        [processed_by_user_id] bigint NULL,
        [decision_reason] nvarchar(1000) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_customer_withdrawal_requests] PRIMARY KEY ([id]),
        CONSTRAINT [CK_customer_withdrawal_scope] CHECK ([scope_code] IN ('CUSTOMER_ROLE','ACCOUNT')),
        CONSTRAINT [CK_customer_withdrawal_status] CHECK ([status_code] IN ('REQUESTED','APPROVED','REJECTED','CANCELLED')),
        CONSTRAINT [FK_customer_withdrawal_requests_users_processed_by_user_id] FOREIGN KEY ([processed_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_customer_withdrawal_requests_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE TABLE [legal_documents] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [code] varchar(50) NOT NULL,
        [audience_code] varchar(20) NOT NULL DEFAULT 'CUSTOMER',
        [requirement_code] varchar(20) NOT NULL,
        [display_order] int NOT NULL DEFAULT 0,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [is_placeholder] bit NOT NULL DEFAULT CAST(0 AS bit),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_legal_documents] PRIMARY KEY ([id]),
        CONSTRAINT [CK_legal_documents_audience] CHECK ([audience_code] IN ('CUSTOMER','PROVIDER','ALL')),
        CONSTRAINT [CK_legal_documents_requirement] CHECK ([requirement_code] IN ('REQUIRED','OPTIONAL','NOTICE')),
        CONSTRAINT [FK_legal_documents_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_legal_documents_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE TABLE [password_reset_requests] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [user_id] bigint NULL,
        [requested_identifier_hash] binary(32) NOT NULL,
        [requested_identifier_masked] nvarchar(320) NOT NULL,
        [token_hash] binary(32) NOT NULL,
        [expires_at] datetime2(7) NOT NULL,
        [used_at] datetime2(7) NULL,
        [delivery_status_code] varchar(30) NOT NULL DEFAULT 'NOT_INTEGRATED',
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_password_reset_requests] PRIMARY KEY ([id]),
        CONSTRAINT [CK_password_reset_delivery] CHECK ([delivery_status_code] IN ('NOT_INTEGRATED','PENDING','SENT','FAILED')),
        CONSTRAINT [FK_password_reset_requests_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE TABLE [legal_document_versions] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [legal_document_id] bigint NOT NULL,
        [version_no] int NOT NULL,
        [title] nvarchar(300) NOT NULL,
        [content] nvarchar(max) NOT NULL,
        [effective_from] datetime2(7) NOT NULL,
        [effective_to] datetime2(7) NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [is_placeholder] bit NOT NULL DEFAULT CAST(0 AS bit),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_legal_document_versions] PRIMARY KEY ([id]),
        CONSTRAINT [FK_legal_document_versions_legal_documents_legal_document_id] FOREIGN KEY ([legal_document_id]) REFERENCES [legal_documents] ([id]),
        CONSTRAINT [FK_legal_document_versions_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE TABLE [user_consents] (
        [id] bigint NOT NULL IDENTITY,
        [user_id] bigint NOT NULL,
        [legal_document_version_id] bigint NOT NULL,
        [consent_status_code] varchar(20) NOT NULL,
        [consented_at] datetime2(7) NOT NULL,
        [withdrawn_at] datetime2(7) NULL,
        [source_code] varchar(30) NOT NULL,
        [ip_address] varchar(45) NULL,
        [user_agent] nvarchar(1000) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_user_consents] PRIMARY KEY ([id]),
        CONSTRAINT [CK_user_consents_status] CHECK ([consent_status_code] IN ('CONSENTED','WITHDRAWN')),
        CONSTRAINT [FK_user_consents_legal_document_versions_legal_document_version_id] FOREIGN KEY ([legal_document_version_id]) REFERENCES [legal_document_versions] ([id]),
        CONSTRAINT [FK_user_consents_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_users_normalized_email] ON [users] ([normalized_email]) WHERE [normalized_email] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    EXEC(N'ALTER TABLE [users] ADD CONSTRAINT [CK_users_email_verification] CHECK ([email_verification_status_code] IN (''NOT_INTEGRATED'',''PENDING'',''VERIFIED''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    EXEC(N'ALTER TABLE [users] ADD CONSTRAINT [CK_users_phone_verification] CHECK ([phone_verification_status_code] IN (''NOT_INTEGRATED'',''PENDING'',''VERIFIED''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE INDEX [IX_customer_addresses_administrative_area_id] ON [customer_addresses] ([administrative_area_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE INDEX [IX_customer_addresses_created_by_user_id] ON [customer_addresses] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE INDEX [IX_customer_addresses_customer_profile_id] ON [customer_addresses] ([customer_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE INDEX [IX_customer_addresses_customer_profile_id_is_active_created_at] ON [customer_addresses] ([customer_profile_id], [is_active], [created_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_customer_addresses_customer_profile_id_is_default] ON [customer_addresses] ([customer_profile_id], [is_default]) WHERE [is_active] = 1 AND [is_default] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE UNIQUE INDEX [IX_customer_addresses_public_id] ON [customer_addresses] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE INDEX [IX_customer_addresses_updated_by_user_id] ON [customer_addresses] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE INDEX [IX_customer_withdrawal_requests_processed_by_user_id] ON [customer_withdrawal_requests] ([processed_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE UNIQUE INDEX [IX_customer_withdrawal_requests_public_id] ON [customer_withdrawal_requests] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_customer_withdrawal_requests_user_id_scope_code] ON [customer_withdrawal_requests] ([user_id], [scope_code]) WHERE [status_code] = ''REQUESTED''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE INDEX [IX_customer_withdrawal_requests_user_id_status_code] ON [customer_withdrawal_requests] ([user_id], [status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE INDEX [IX_legal_document_versions_created_by_user_id] ON [legal_document_versions] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE INDEX [IX_legal_document_versions_is_active_effective_from_effective_to] ON [legal_document_versions] ([is_active], [effective_from], [effective_to]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE UNIQUE INDEX [IX_legal_document_versions_legal_document_id_version_no] ON [legal_document_versions] ([legal_document_id], [version_no]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE UNIQUE INDEX [IX_legal_document_versions_public_id] ON [legal_document_versions] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE INDEX [IX_legal_documents_audience_code_is_active_display_order] ON [legal_documents] ([audience_code], [is_active], [display_order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE UNIQUE INDEX [IX_legal_documents_code] ON [legal_documents] ([code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE INDEX [IX_legal_documents_created_by_user_id] ON [legal_documents] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE UNIQUE INDEX [IX_legal_documents_public_id] ON [legal_documents] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE INDEX [IX_legal_documents_updated_by_user_id] ON [legal_documents] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE INDEX [IX_password_reset_requests_expires_at_used_at] ON [password_reset_requests] ([expires_at], [used_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE UNIQUE INDEX [IX_password_reset_requests_public_id] ON [password_reset_requests] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE INDEX [IX_password_reset_requests_requested_identifier_hash_created_at] ON [password_reset_requests] ([requested_identifier_hash], [created_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE UNIQUE INDEX [IX_password_reset_requests_token_hash] ON [password_reset_requests] ([token_hash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE INDEX [IX_password_reset_requests_user_id] ON [password_reset_requests] ([user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE INDEX [IX_user_consents_legal_document_version_id] ON [user_consents] ([legal_document_version_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE INDEX [IX_user_consents_user_id_consent_status_code] ON [user_consents] ([user_id], [consent_status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    CREATE UNIQUE INDEX [IX_user_consents_user_id_legal_document_version_id] ON [user_consents] ([user_id], [legal_document_version_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'public_id', N'code', N'audience_code', N'requirement_code', N'display_order', N'is_active', N'is_placeholder') AND [object_id] = OBJECT_ID(N'[legal_documents]'))
        SET IDENTITY_INSERT [legal_documents] ON;
    EXEC(N'INSERT INTO [legal_documents] ([id], [public_id], [code], [audience_code], [requirement_code], [display_order], [is_active], [is_placeholder])
    VALUES (CAST(1 AS bigint), ''10000000-0000-4000-8000-000000000001'', ''TERMS_OF_SERVICE'', ''CUSTOMER'', ''REQUIRED'', 10, CAST(1 AS bit), CAST(1 AS bit)),
    (CAST(2 AS bigint), ''10000000-0000-4000-8000-000000000002'', ''PRIVACY_POLICY'', ''CUSTOMER'', ''REQUIRED'', 20, CAST(1 AS bit), CAST(1 AS bit)),
    (CAST(3 AS bigint), ''10000000-0000-4000-8000-000000000003'', ''MARKETING_CONSENT'', ''CUSTOMER'', ''OPTIONAL'', 30, CAST(1 AS bit), CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'public_id', N'code', N'audience_code', N'requirement_code', N'display_order', N'is_active', N'is_placeholder') AND [object_id] = OBJECT_ID(N'[legal_documents]'))
        SET IDENTITY_INSERT [legal_documents] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'public_id', N'legal_document_id', N'version_no', N'title', N'content', N'effective_from', N'is_active', N'is_placeholder') AND [object_id] = OBJECT_ID(N'[legal_document_versions]'))
        SET IDENTITY_INSERT [legal_document_versions] ON;
    EXEC(N'INSERT INTO [legal_document_versions] ([id], [public_id], [legal_document_id], [version_no], [title], [content], [effective_from], [is_active], [is_placeholder])
    VALUES (CAST(1 AS bigint), ''20000000-0000-4000-8000-000000000001'', CAST(1 AS bigint), 1, N''[개발용] 수달 라이프 이용약관'', N''개발 및 화면 검증용 Placeholder입니다. 운영 전 검토·승인된 이용약관으로 교체해야 합니다.'', ''2026-08-11T00:00:00.0000000Z'', CAST(1 AS bit), CAST(1 AS bit)),
    (CAST(2 AS bigint), ''20000000-0000-4000-8000-000000000002'', CAST(2 AS bigint), 1, N''[개발용] 개인정보 필수동의'', N''개발 및 화면 검증용 Placeholder입니다. 운영 전 검토·승인된 개인정보 문서로 교체해야 합니다.'', ''2026-08-11T00:00:00.0000000Z'', CAST(1 AS bit), CAST(1 AS bit)),
    (CAST(3 AS bigint), ''20000000-0000-4000-8000-000000000003'', CAST(3 AS bigint), 1, N''[선택·개발용] 마케팅 수신 동의'', N''개발 및 화면 검증용 Placeholder입니다. 실제 메시지를 발송하지 않으며 운영 전 승인 문서로 교체해야 합니다.'', ''2026-08-11T00:00:00.0000000Z'', CAST(1 AS bit), CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'public_id', N'legal_document_id', N'version_no', N'title', N'content', N'effective_from', N'is_active', N'is_placeholder') AND [object_id] = OBJECT_ID(N'[legal_document_versions]'))
        SET IDENTITY_INSERT [legal_document_versions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811100000_ImplementCustomerAccountProfileAndConsent'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260811100000_ImplementCustomerAccountProfileAndConsent', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811110000_ImplementCustomerRequestFilesAndQuoteComparison'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[service_requests]') AND [c].[name] = N'administrative_area_id');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [service_requests] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [service_requests] ALTER COLUMN [administrative_area_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811110000_ImplementCustomerRequestFilesAndQuoteComparison'
)
BEGIN
    CREATE TABLE [service_request_files] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [service_request_id] bigint NOT NULL,
        [file_id] bigint NOT NULL,
        [request_field_id] bigint NULL,
        [purpose_code] varchar(30) NOT NULL DEFAULT 'REQUEST_REFERENCE',
        [display_order] int NOT NULL DEFAULT 0,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_service_request_files] PRIMARY KEY ([id]),
        CONSTRAINT [CK_service_request_files_purpose] CHECK ([purpose_code] IN ('REQUEST_REFERENCE','DYNAMIC_FIELD')),
        CONSTRAINT [FK_service_request_files_category_field_definitions_request_field_id] FOREIGN KEY ([request_field_id]) REFERENCES [category_field_definitions] ([id]),
        CONSTRAINT [FK_service_request_files_files_file_id] FOREIGN KEY ([file_id]) REFERENCES [files] ([id]),
        CONSTRAINT [FK_service_request_files_service_requests_service_request_id] FOREIGN KEY ([service_request_id]) REFERENCES [service_requests] ([id]),
        CONSTRAINT [FK_service_request_files_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811110000_ImplementCustomerRequestFilesAndQuoteComparison'
)
BEGIN
    CREATE INDEX [IX_service_request_files_created_by_user_id] ON [service_request_files] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811110000_ImplementCustomerRequestFilesAndQuoteComparison'
)
BEGIN
    CREATE UNIQUE INDEX [IX_service_request_files_file_id] ON [service_request_files] ([file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811110000_ImplementCustomerRequestFilesAndQuoteComparison'
)
BEGIN
    CREATE UNIQUE INDEX [IX_service_request_files_public_id] ON [service_request_files] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811110000_ImplementCustomerRequestFilesAndQuoteComparison'
)
BEGIN
    CREATE INDEX [IX_service_request_files_request_field_id] ON [service_request_files] ([request_field_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811110000_ImplementCustomerRequestFilesAndQuoteComparison'
)
BEGIN
    CREATE INDEX [IX_service_request_files_service_request_id] ON [service_request_files] ([service_request_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811110000_ImplementCustomerRequestFilesAndQuoteComparison'
)
BEGIN
    CREATE INDEX [IX_service_request_files_service_request_id_display_order] ON [service_request_files] ([service_request_id], [display_order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811110000_ImplementCustomerRequestFilesAndQuoteComparison'
)
BEGIN
    CREATE UNIQUE INDEX [IX_service_request_files_service_request_id_file_id] ON [service_request_files] ([service_request_id], [file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811110000_ImplementCustomerRequestFilesAndQuoteComparison'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260811110000_ImplementCustomerRequestFilesAndQuoteComparison', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811120000_ImplementCustomerTransactionAppointmentsAndCompletionFlow'
)
BEGIN
    CREATE TABLE [transaction_appointments] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [transaction_id] bigint NOT NULL,
        [scheduled_start_at] datetime2(7) NOT NULL,
        [scheduled_end_at] datetime2(7) NULL,
        [estimated_duration_minutes] int NULL,
        [customer_memo] nvarchar(1000) NULL,
        [provider_memo] nvarchar(1000) NULL,
        [status_code] varchar(20) NOT NULL DEFAULT 'PROPOSED',
        [confirmed_at] datetime2(7) NULL,
        [cancelled_at] datetime2(7) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_transaction_appointments] PRIMARY KEY ([id]),
        CONSTRAINT [CK_transaction_appointments_duration] CHECK ([estimated_duration_minutes] IS NULL OR [estimated_duration_minutes] > 0),
        CONSTRAINT [CK_transaction_appointments_period] CHECK ([scheduled_end_at] IS NULL OR [scheduled_end_at] > [scheduled_start_at]),
        CONSTRAINT [CK_transaction_appointments_status] CHECK ([status_code] IN ('PROPOSED','CONFIRMED','CANCELLED')),
        CONSTRAINT [FK_transaction_appointments_transactions_transaction_id] FOREIGN KEY ([transaction_id]) REFERENCES [transactions] ([id]),
        CONSTRAINT [FK_transaction_appointments_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_transaction_appointments_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811120000_ImplementCustomerTransactionAppointmentsAndCompletionFlow'
)
BEGIN
    CREATE TABLE [transaction_appointment_change_requests] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [transaction_appointment_id] bigint NOT NULL,
        [requested_by_user_id] bigint NOT NULL,
        [requested_start_at] datetime2(7) NOT NULL,
        [requested_end_at] datetime2(7) NULL,
        [reason] nvarchar(1000) NOT NULL,
        [status_code] varchar(20) NOT NULL DEFAULT 'REQUESTED',
        [requested_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [processed_at] datetime2(7) NULL,
        [processed_by_user_id] bigint NULL,
        [processing_note] nvarchar(1000) NULL,
        [idempotency_key] varchar(100) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_transaction_appointment_change_requests] PRIMARY KEY ([id]),
        CONSTRAINT [CK_transaction_appointment_changes_period] CHECK ([requested_end_at] IS NULL OR [requested_end_at] > [requested_start_at]),
        CONSTRAINT [CK_transaction_appointment_changes_status] CHECK ([status_code] IN ('REQUESTED','APPROVED','REJECTED','CANCELLED')),
        CONSTRAINT [FK_transaction_appointment_change_requests_transaction_appointments_transaction_appointment_id] FOREIGN KEY ([transaction_appointment_id]) REFERENCES [transaction_appointments] ([id]),
        CONSTRAINT [FK_transaction_appointment_change_requests_users_processed_by_user_id] FOREIGN KEY ([processed_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_transaction_appointment_change_requests_users_requested_by_user_id] FOREIGN KEY ([requested_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811120000_ImplementCustomerTransactionAppointmentsAndCompletionFlow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_transaction_appointment_change_requests_idempotency_key] ON [transaction_appointment_change_requests] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811120000_ImplementCustomerTransactionAppointmentsAndCompletionFlow'
)
BEGIN
    CREATE INDEX [IX_transaction_appointment_change_requests_processed_by_user_id] ON [transaction_appointment_change_requests] ([processed_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811120000_ImplementCustomerTransactionAppointmentsAndCompletionFlow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_transaction_appointment_change_requests_public_id] ON [transaction_appointment_change_requests] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811120000_ImplementCustomerTransactionAppointmentsAndCompletionFlow'
)
BEGIN
    CREATE INDEX [IX_transaction_appointment_change_requests_requested_at] ON [transaction_appointment_change_requests] ([requested_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811120000_ImplementCustomerTransactionAppointmentsAndCompletionFlow'
)
BEGIN
    CREATE INDEX [IX_transaction_appointment_change_requests_requested_by_user_id] ON [transaction_appointment_change_requests] ([requested_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811120000_ImplementCustomerTransactionAppointmentsAndCompletionFlow'
)
BEGIN
    CREATE INDEX [IX_transaction_appointment_change_requests_status_code] ON [transaction_appointment_change_requests] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811120000_ImplementCustomerTransactionAppointmentsAndCompletionFlow'
)
BEGIN
    CREATE INDEX [IX_transaction_appointment_change_requests_transaction_appointment_id] ON [transaction_appointment_change_requests] ([transaction_appointment_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811120000_ImplementCustomerTransactionAppointmentsAndCompletionFlow'
)
BEGIN
    CREATE INDEX [IX_transaction_appointments_created_by_user_id] ON [transaction_appointments] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811120000_ImplementCustomerTransactionAppointmentsAndCompletionFlow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_transaction_appointments_public_id] ON [transaction_appointments] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811120000_ImplementCustomerTransactionAppointmentsAndCompletionFlow'
)
BEGIN
    CREATE INDEX [IX_transaction_appointments_scheduled_start_at] ON [transaction_appointments] ([scheduled_start_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811120000_ImplementCustomerTransactionAppointmentsAndCompletionFlow'
)
BEGIN
    CREATE INDEX [IX_transaction_appointments_status_code] ON [transaction_appointments] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811120000_ImplementCustomerTransactionAppointmentsAndCompletionFlow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_transaction_appointments_transaction_id] ON [transaction_appointments] ([transaction_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811120000_ImplementCustomerTransactionAppointmentsAndCompletionFlow'
)
BEGIN
    CREATE INDEX [IX_transaction_appointments_updated_by_user_id] ON [transaction_appointments] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811120000_ImplementCustomerTransactionAppointmentsAndCompletionFlow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260811120000_ImplementCustomerTransactionAppointmentsAndCompletionFlow', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811233431_20260812100000_LinkInteriorProjectsToAfterServiceCases'
)
BEGIN
    ALTER TABLE [dispute_cases] DROP CONSTRAINT [CK_dispute_cases_source];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811233431_20260812100000_LinkInteriorProjectsToAfterServiceCases'
)
BEGIN
    ALTER TABLE [after_service_cases] DROP CONSTRAINT [CK_after_service_cases_source];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811233431_20260812100000_LinkInteriorProjectsToAfterServiceCases'
)
BEGIN
    ALTER TABLE [after_service_cases] ADD [interior_project_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811233431_20260812100000_LinkInteriorProjectsToAfterServiceCases'
)
BEGIN
    CREATE INDEX [IX_after_service_cases_interior_project_id] ON [after_service_cases] ([interior_project_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811233431_20260812100000_LinkInteriorProjectsToAfterServiceCases'
)
BEGIN
    EXEC(N'ALTER TABLE [after_service_cases] ADD CONSTRAINT [CK_after_service_cases_source] CHECK (([transaction_id] IS NOT NULL AND [subscription_visit_schedule_id] IS NULL AND [interior_project_id] IS NULL) OR ([transaction_id] IS NULL AND [subscription_visit_schedule_id] IS NOT NULL AND [interior_project_id] IS NULL) OR ([transaction_id] IS NULL AND [subscription_visit_schedule_id] IS NULL AND [interior_project_id] IS NOT NULL))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811233431_20260812100000_LinkInteriorProjectsToAfterServiceCases'
)
BEGIN
    ALTER TABLE [after_service_cases] ADD CONSTRAINT [FK_after_service_cases_interior_projects_interior_project_id] FOREIGN KEY ([interior_project_id]) REFERENCES [interior_projects] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811233431_20260812100000_LinkInteriorProjectsToAfterServiceCases'
)
BEGIN
    EXEC(N'ALTER TABLE [dispute_cases] ADD CONSTRAINT [CK_dispute_cases_source] CHECK (([transaction_id] IS NOT NULL AND [subscription_visit_schedule_id] IS NULL AND [interior_project_id] IS NULL) OR ([transaction_id] IS NULL AND [subscription_visit_schedule_id] IS NOT NULL AND [interior_project_id] IS NULL) OR ([transaction_id] IS NULL AND [subscription_visit_schedule_id] IS NULL AND [interior_project_id] IS NOT NULL))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260811233431_20260812100000_LinkInteriorProjectsToAfterServiceCases'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260811233431_20260812100000_LinkInteriorProjectsToAfterServiceCases', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812003939_ImplementPrivacyProtectionFoundation'
)
BEGIN
    ALTER TABLE [users] ADD [email_encrypted] varbinary(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812003939_ImplementPrivacyProtectionFoundation'
)
BEGIN
    ALTER TABLE [users] ADD [email_search_hash] binary(32) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812003939_ImplementPrivacyProtectionFoundation'
)
BEGIN
    ALTER TABLE [users] ADD [phone_encrypted] varbinary(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812003939_ImplementPrivacyProtectionFoundation'
)
BEGIN
    ALTER TABLE [users] ADD [phone_search_hash] binary(32) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812003939_ImplementPrivacyProtectionFoundation'
)
BEGIN
    ALTER TABLE [users] ADD [privacy_protection_version] smallint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812003939_ImplementPrivacyProtectionFoundation'
)
BEGIN
    ALTER TABLE [subscription_requests] ADD [detail_address_encrypted] varbinary(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812003939_ImplementPrivacyProtectionFoundation'
)
BEGIN
    ALTER TABLE [subscription_requests] ADD [privacy_protection_version] smallint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812003939_ImplementPrivacyProtectionFoundation'
)
BEGIN
    ALTER TABLE [service_requests] ADD [detail_address_encrypted] varbinary(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812003939_ImplementPrivacyProtectionFoundation'
)
BEGIN
    ALTER TABLE [service_requests] ADD [privacy_protection_version] smallint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812003939_ImplementPrivacyProtectionFoundation'
)
BEGIN
    ALTER TABLE [customer_addresses] ADD [detail_address_encrypted] varbinary(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812003939_ImplementPrivacyProtectionFoundation'
)
BEGIN
    ALTER TABLE [customer_addresses] ADD [privacy_protection_version] smallint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812003939_ImplementPrivacyProtectionFoundation'
)
BEGIN
    ALTER TABLE [customer_addresses] ADD [recipient_name_encrypted] varbinary(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812003939_ImplementPrivacyProtectionFoundation'
)
BEGIN
    ALTER TABLE [customer_addresses] ADD [road_address_encrypted] varbinary(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812003939_ImplementPrivacyProtectionFoundation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_users_email_search_hash] ON [users] ([email_search_hash]) WHERE [email_search_hash] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812003939_ImplementPrivacyProtectionFoundation'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_users_phone_search_hash] ON [users] ([phone_search_hash]) WHERE [phone_search_hash] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812003939_ImplementPrivacyProtectionFoundation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812003939_ImplementPrivacyProtectionFoundation', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812010650_ImplementFilePrivacyFoundation'
)
BEGIN
    ALTER TABLE [files] ADD [malware_scan_status_code] varchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812010650_ImplementFilePrivacyFoundation'
)
BEGIN
    ALTER TABLE [files] ADD [privacy_adapter_version] varchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812010650_ImplementFilePrivacyFoundation'
)
BEGIN
    ALTER TABLE [files] ADD [privacy_detection_types_json] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812010650_ImplementFilePrivacyFoundation'
)
BEGIN
    ALTER TABLE [files] ADD [privacy_inspected_at] datetime2(7) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812010650_ImplementFilePrivacyFoundation'
)
BEGIN
    ALTER TABLE [files] ADD [privacy_inspection_error_code] varchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812010650_ImplementFilePrivacyFoundation'
)
BEGIN
    ALTER TABLE [files] ADD [privacy_inspection_status_code] varchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812010650_ImplementFilePrivacyFoundation'
)
BEGIN
    ALTER TABLE [files] ADD [sanitization_completed_at] datetime2(7) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812010650_ImplementFilePrivacyFoundation'
)
BEGIN
    ALTER TABLE [files] ADD [sanitization_status_code] varchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812010650_ImplementFilePrivacyFoundation'
)
BEGIN
    CREATE TABLE [file_derivatives] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [original_file_id] bigint NOT NULL,
        [derived_file_id] bigint NOT NULL,
        [derivative_type_code] varchar(40) NOT NULL,
        [adapter_version] varchar(100) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_file_derivatives] PRIMARY KEY ([id]),
        CONSTRAINT [CK_file_derivatives_distinct] CHECK ([original_file_id] <> [derived_file_id]),
        CONSTRAINT [CK_file_derivatives_type] CHECK ([derivative_type_code] IN ('PRIVACY_SANITIZED')),
        CONSTRAINT [FK_file_derivatives_files_derived_file_id] FOREIGN KEY ([derived_file_id]) REFERENCES [files] ([id]),
        CONSTRAINT [FK_file_derivatives_files_original_file_id] FOREIGN KEY ([original_file_id]) REFERENCES [files] ([id]),
        CONSTRAINT [FK_file_derivatives_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812010650_ImplementFilePrivacyFoundation'
)
BEGIN
    CREATE INDEX [IX_files_malware_scan_status_code] ON [files] ([malware_scan_status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812010650_ImplementFilePrivacyFoundation'
)
BEGIN
    CREATE INDEX [IX_files_privacy_inspection_status_code] ON [files] ([privacy_inspection_status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812010650_ImplementFilePrivacyFoundation'
)
BEGIN
    CREATE INDEX [IX_files_sanitization_status_code] ON [files] ([sanitization_status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812010650_ImplementFilePrivacyFoundation'
)
BEGIN
    EXEC(N'ALTER TABLE [files] ADD CONSTRAINT [CK_files_malware_scan_status] CHECK ([malware_scan_status_code] IS NULL OR [malware_scan_status_code] IN (''NOT_INTEGRATED'',''PENDING'',''PROCESSING'',''CLEAN'',''INFECTED'',''FAILED''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812010650_ImplementFilePrivacyFoundation'
)
BEGIN
    EXEC(N'ALTER TABLE [files] ADD CONSTRAINT [CK_files_privacy_inspection_status] CHECK ([privacy_inspection_status_code] IS NULL OR [privacy_inspection_status_code] IN (''NOT_INTEGRATED'',''PENDING'',''PROCESSING'',''SAFE'',''SENSITIVE_DETECTED'',''FAILED''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812010650_ImplementFilePrivacyFoundation'
)
BEGIN
    EXEC(N'ALTER TABLE [files] ADD CONSTRAINT [CK_files_sanitization_status] CHECK ([sanitization_status_code] IS NULL OR [sanitization_status_code] IN (''NOT_INTEGRATED'',''NOT_REQUIRED'',''PENDING'',''PROCESSING'',''COMPLETED'',''FAILED''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812010650_ImplementFilePrivacyFoundation'
)
BEGIN
    CREATE INDEX [IX_file_derivatives_created_by_user_id] ON [file_derivatives] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812010650_ImplementFilePrivacyFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_file_derivatives_derived_file_id] ON [file_derivatives] ([derived_file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812010650_ImplementFilePrivacyFoundation'
)
BEGIN
    CREATE INDEX [IX_file_derivatives_original_file_id] ON [file_derivatives] ([original_file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812010650_ImplementFilePrivacyFoundation'
)
BEGIN
    CREATE INDEX [IX_file_derivatives_original_file_id_derivative_type_code] ON [file_derivatives] ([original_file_id], [derivative_type_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812010650_ImplementFilePrivacyFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_file_derivatives_public_id] ON [file_derivatives] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812010650_ImplementFilePrivacyFoundation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812010650_ImplementFilePrivacyFoundation', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow'
)
BEGIN
    ALTER TABLE [transaction_appointments] DROP CONSTRAINT [CK_transaction_appointments_status];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow'
)
BEGIN
    ALTER TABLE [transaction_appointment_change_requests] DROP CONSTRAINT [CK_transaction_appointment_changes_period];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow'
)
BEGIN
    ALTER TABLE [transaction_appointments] ADD [proposal_idempotency_key] varchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[transaction_appointment_change_requests]') AND [c].[name] = N'requested_start_at');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [transaction_appointment_change_requests] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [transaction_appointment_change_requests] ALTER COLUMN [requested_start_at] datetime2(7) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow'
)
BEGIN
    ALTER TABLE [transaction_appointment_change_requests] ADD [change_type_code] varchar(20) NOT NULL DEFAULT 'RESCHEDULE';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow'
)
BEGIN
    ALTER TABLE [transaction_appointment_change_requests] ADD [row_version] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow'
)
BEGIN
    CREATE TABLE [transaction_appointment_events] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [transaction_appointment_id] bigint NOT NULL,
        [actor_user_id] bigint NOT NULL,
        [event_type_code] varchar(40) NOT NULL,
        [before_status_code] varchar(20) NULL,
        [after_status_code] varchar(20) NOT NULL,
        [before_start_at] datetime2(7) NULL,
        [before_end_at] datetime2(7) NULL,
        [after_start_at] datetime2(7) NULL,
        [after_end_at] datetime2(7) NULL,
        [reason] nvarchar(1000) NULL,
        [idempotency_key] varchar(100) NOT NULL,
        [occurred_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_transaction_appointment_events] PRIMARY KEY ([id]),
        CONSTRAINT [FK_transaction_appointment_events_transaction_appointments_transaction_appointment_id] FOREIGN KEY ([transaction_appointment_id]) REFERENCES [transaction_appointments] ([id]),
        CONSTRAINT [FK_transaction_appointment_events_users_actor_user_id] FOREIGN KEY ([actor_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow'
)
BEGIN
    CREATE TABLE [transaction_cancellation_requests] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [transaction_id] bigint NOT NULL,
        [requested_by_user_id] bigint NOT NULL,
        [reason] nvarchar(1000) NOT NULL,
        [status_code] varchar(30) NOT NULL DEFAULT 'REQUESTED',
        [requested_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [processed_at] datetime2(7) NULL,
        [processed_by_user_id] bigint NULL,
        [processing_note] nvarchar(1000) NULL,
        [idempotency_key] varchar(100) NOT NULL,
        [decision_idempotency_key] varchar(100) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_transaction_cancellation_requests] PRIMARY KEY ([id]),
        CONSTRAINT [CK_transaction_cancellation_requests_status] CHECK ([status_code] IN ('REQUESTED','ADMIN_REVIEW_REQUIRED','APPROVED','REJECTED','CANCELLED')),
        CONSTRAINT [FK_transaction_cancellation_requests_transactions_transaction_id] FOREIGN KEY ([transaction_id]) REFERENCES [transactions] ([id]),
        CONSTRAINT [FK_transaction_cancellation_requests_users_processed_by_user_id] FOREIGN KEY ([processed_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_transaction_cancellation_requests_users_requested_by_user_id] FOREIGN KEY ([requested_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_transaction_appointments_proposal_idempotency_key] ON [transaction_appointments] ([proposal_idempotency_key]) WHERE [proposal_idempotency_key] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow'
)
BEGIN
    EXEC(N'ALTER TABLE [transaction_appointments] ADD CONSTRAINT [CK_transaction_appointments_status] CHECK ([status_code] IN (''PROPOSED'',''CONFIRMED'',''REJECTED'',''CANCELLED'',''COMPLETED''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow'
)
BEGIN
    EXEC(N'ALTER TABLE [transaction_appointment_change_requests] ADD CONSTRAINT [CK_transaction_appointment_changes_period] CHECK (([change_type_code] = ''CANCEL'' AND [requested_start_at] IS NULL AND [requested_end_at] IS NULL) OR ([change_type_code] = ''RESCHEDULE'' AND [requested_start_at] IS NOT NULL AND ([requested_end_at] IS NULL OR [requested_end_at] > [requested_start_at])))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow'
)
BEGIN
    EXEC(N'ALTER TABLE [transaction_appointment_change_requests] ADD CONSTRAINT [CK_transaction_appointment_changes_type] CHECK ([change_type_code] IN (''RESCHEDULE'',''CANCEL''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow'
)
BEGIN
    CREATE INDEX [IX_transaction_appointment_events_actor_user_id] ON [transaction_appointment_events] ([actor_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_transaction_appointment_events_idempotency_key] ON [transaction_appointment_events] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_transaction_appointment_events_public_id] ON [transaction_appointment_events] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow'
)
BEGIN
    CREATE INDEX [IX_transaction_appointment_events_transaction_appointment_id_occurred_at] ON [transaction_appointment_events] ([transaction_appointment_id], [occurred_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_transaction_cancellation_requests_decision_idempotency_key] ON [transaction_cancellation_requests] ([decision_idempotency_key]) WHERE [decision_idempotency_key] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_transaction_cancellation_requests_idempotency_key] ON [transaction_cancellation_requests] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow'
)
BEGIN
    CREATE INDEX [IX_transaction_cancellation_requests_processed_by_user_id] ON [transaction_cancellation_requests] ([processed_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_transaction_cancellation_requests_public_id] ON [transaction_cancellation_requests] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow'
)
BEGIN
    CREATE INDEX [IX_transaction_cancellation_requests_requested_by_user_id] ON [transaction_cancellation_requests] ([requested_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow'
)
BEGIN
    CREATE INDEX [IX_transaction_cancellation_requests_status_code] ON [transaction_cancellation_requests] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow'
)
BEGIN
    CREATE INDEX [IX_transaction_cancellation_requests_transaction_id_requested_at] ON [transaction_cancellation_requests] ([transaction_id], [requested_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812014022_ImplementTransactionAppointmentAndCancellationWorkflow', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    ALTER TABLE [subscription_schedule_changes] DROP CONSTRAINT [CK_subscription_schedule_changes_status];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    ALTER TABLE [subscription_visit_schedules] ADD [gps_verification_status_code] varchar(30) NOT NULL DEFAULT 'NOT_INTEGRATED';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    ALTER TABLE [subscription_visit_schedules] ADD [possession_verification_status_code] varchar(30) NOT NULL DEFAULT 'NOT_INTEGRATED';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    ALTER TABLE [subscription_visit_schedules] ADD [verification_override_at] datetime2(7) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    ALTER TABLE [subscription_visit_schedules] ADD [verification_override_by_user_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    ALTER TABLE [subscription_visit_schedules] ADD [verification_override_reason] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    ALTER TABLE [subscription_visit_schedules] ADD [visit_verification_status_code] varchar(30) NOT NULL DEFAULT 'NOT_VERIFIED';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    ALTER TABLE [subscription_contracts] ADD [completion_policy_snapshot_json] nvarchar(max) NOT NULL DEFAULT N'{}';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    ALTER TABLE [interior_projects] ADD [admin_completed_at] datetime2(7) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    ALTER TABLE [interior_projects] ADD [admin_completed_by_user_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    ALTER TABLE [interior_projects] ADD [customer_completion_acknowledged_at] datetime2(7) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    ALTER TABLE [interior_projects] ADD [customer_completion_acknowledged_by_user_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    ALTER TABLE [interior_projects] ADD [customer_completion_comment] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    ALTER TABLE [interior_projects] ADD [provider_completion_submitted_at] datetime2(7) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    ALTER TABLE [interior_projects] ADD [provider_completion_submitted_by_user_id] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    ALTER TABLE [interior_projects] ADD [provider_completion_summary] nvarchar(4000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    ALTER TABLE [interior_projects] ADD [provider_final_checklist_json] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    CREATE TABLE [interior_project_participants] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [interior_project_id] bigint NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [role_code] varchar(40) NOT NULL,
        [effective_from] datetime2(7) NOT NULL,
        [effective_to] datetime2(7) NULL,
        [status_code] varchar(20) NOT NULL,
        [is_primary] bit NOT NULL DEFAULT CAST(0 AS bit),
        [scope_text] nvarchar(2000) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_interior_project_participants] PRIMARY KEY ([id]),
        CONSTRAINT [CK_interior_project_participants_period] CHECK ([effective_to] IS NULL OR [effective_to] >= [effective_from]),
        CONSTRAINT [CK_interior_project_participants_role] CHECK ([role_code] IN ('SITE_SURVEY','DESIGN','PRIMARY_CONTRACTOR','TRADE_CONTRACTOR','INSPECTION','AFTER_SERVICE')),
        CONSTRAINT [CK_interior_project_participants_status] CHECK ([status_code] IN ('ACTIVE','ENDED','CANCELLED')),
        CONSTRAINT [FK_interior_project_participants_interior_projects_interior_project_id] FOREIGN KEY ([interior_project_id]) REFERENCES [interior_projects] ([id]),
        CONSTRAINT [FK_interior_project_participants_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_interior_project_participants_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_interior_project_participants_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    CREATE TABLE [interior_stage_inspection_acknowledgements] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [stage_inspection_id] bigint NOT NULL,
        [customer_profile_id] bigint NOT NULL,
        [acknowledged_at] datetime2(7) NOT NULL,
        [comment] nvarchar(2000) NULL,
        [idempotency_key] varchar(150) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_interior_stage_inspection_acknowledgements] PRIMARY KEY ([id]),
        CONSTRAINT [FK_interior_stage_inspection_acknowledgements_customer_profiles_customer_profile_id] FOREIGN KEY ([customer_profile_id]) REFERENCES [customer_profiles] ([id]),
        CONSTRAINT [FK_interior_stage_inspection_acknowledgements_interior_stage_inspections_stage_inspection_id] FOREIGN KEY ([stage_inspection_id]) REFERENCES [interior_stage_inspections] ([id]),
        CONSTRAINT [FK_interior_stage_inspection_acknowledgements_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    CREATE INDEX [IX_subscription_visit_schedules_verification_override_by_user_id] ON [subscription_visit_schedules] ([verification_override_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    EXEC(N'ALTER TABLE [subscription_visit_schedules] ADD CONSTRAINT [CK_subscription_visits_verification] CHECK ([visit_verification_status_code] IN (''NOT_VERIFIED'',''PENDING'',''VERIFIED'',''OVERRIDE_APPROVED'',''FAILED'',''NOT_INTEGRATED''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    EXEC(N'ALTER TABLE [subscription_schedule_changes] ADD CONSTRAINT [CK_subscription_schedule_changes_status] CHECK ([status_code] IN (''REQUESTED'',''APPROVED'',''REJECTED'',''CANCELLED''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    CREATE INDEX [IX_interior_projects_admin_completed_by_user_id] ON [interior_projects] ([admin_completed_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    CREATE INDEX [IX_interior_projects_customer_completion_acknowledged_by_user_id] ON [interior_projects] ([customer_completion_acknowledged_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    CREATE INDEX [IX_interior_projects_provider_completion_submitted_by_user_id] ON [interior_projects] ([provider_completion_submitted_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    CREATE INDEX [IX_interior_project_participants_created_by_user_id] ON [interior_project_participants] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_project_participants_interior_project_id_provider_profile_id_role_code] ON [interior_project_participants] ([interior_project_id], [provider_profile_id], [role_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    CREATE INDEX [IX_interior_project_participants_provider_profile_id_status_code_effective_to] ON [interior_project_participants] ([provider_profile_id], [status_code], [effective_to]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_project_participants_public_id] ON [interior_project_participants] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    CREATE INDEX [IX_interior_project_participants_updated_by_user_id] ON [interior_project_participants] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    CREATE INDEX [IX_interior_stage_inspection_acknowledgements_created_by_user_id] ON [interior_stage_inspection_acknowledgements] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    CREATE INDEX [IX_interior_stage_inspection_acknowledgements_customer_profile_id] ON [interior_stage_inspection_acknowledgements] ([customer_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_stage_inspection_acknowledgements_idempotency_key] ON [interior_stage_inspection_acknowledgements] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_stage_inspection_acknowledgements_public_id] ON [interior_stage_inspection_acknowledgements] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_stage_inspection_acknowledgements_stage_inspection_id_customer_profile_id] ON [interior_stage_inspection_acknowledgements] ([stage_inspection_id], [customer_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    ALTER TABLE [interior_projects] ADD CONSTRAINT [FK_interior_projects_users_admin_completed_by_user_id] FOREIGN KEY ([admin_completed_by_user_id]) REFERENCES [users] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    ALTER TABLE [interior_projects] ADD CONSTRAINT [FK_interior_projects_users_customer_completion_acknowledged_by_user_id] FOREIGN KEY ([customer_completion_acknowledged_by_user_id]) REFERENCES [users] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    ALTER TABLE [interior_projects] ADD CONSTRAINT [FK_interior_projects_users_provider_completion_submitted_by_user_id] FOREIGN KEY ([provider_completion_submitted_by_user_id]) REFERENCES [users] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    ALTER TABLE [subscription_visit_schedules] ADD CONSTRAINT [FK_subscription_visit_schedules_users_verification_override_by_user_id] FOREIGN KEY ([verification_override_by_user_id]) REFERENCES [users] ([id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812022221_ImplementProviderPredevelopmentP0DomainFoundation', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812031317_ImplementProviderAppOnboardingFoundation'
)
BEGIN
    ALTER TABLE [provider_profiles] ADD [business_address] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812031317_ImplementProviderAppOnboardingFoundation'
)
BEGIN
    ALTER TABLE [provider_profiles] ADD [business_address_encrypted] varbinary(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812031317_ImplementProviderAppOnboardingFoundation'
)
BEGIN
    ALTER TABLE [provider_profiles] ADD [business_item_text] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812031317_ImplementProviderAppOnboardingFoundation'
)
BEGIN
    ALTER TABLE [provider_profiles] ADD [business_type_text] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812031317_ImplementProviderAppOnboardingFoundation'
)
BEGIN
    ALTER TABLE [provider_profiles] ADD [contact_name] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812031317_ImplementProviderAppOnboardingFoundation'
)
BEGIN
    ALTER TABLE [provider_profiles] ADD [introduction] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812031317_ImplementProviderAppOnboardingFoundation'
)
BEGIN
    ALTER TABLE [provider_profiles] ADD [privacy_protection_version] smallint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812031317_ImplementProviderAppOnboardingFoundation'
)
BEGIN
    ALTER TABLE [provider_profiles] ADD [representative_name] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812031317_ImplementProviderAppOnboardingFoundation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812031317_ImplementProviderAppOnboardingFoundation', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812043717_ImplementProviderAftercareEvidencePurpose'
)
BEGIN
    ALTER TABLE [files] DROP CONSTRAINT [CK_files_purpose];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812043717_ImplementProviderAftercareEvidencePurpose'
)
BEGIN
    EXEC(N'ALTER TABLE [files] ADD CONSTRAINT [CK_files_purpose] CHECK ([purpose_code] IN (''PROVIDER_DOCUMENT'',''REQUEST_ANSWER'',''COMPLETION_EVIDENCE'',''AFTER_SERVICE'',''DISPUTE_EVIDENCE'',''REVIEW'',''REPORT_EVIDENCE'',''SANCTION_APPEAL_EVIDENCE''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812043717_ImplementProviderAftercareEvidencePurpose'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812043717_ImplementProviderAftercareEvidencePurpose', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063935_ImplementTransactionChat'
)
BEGIN
    CREATE TABLE [chat_rooms] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [resource_type] varchar(30) NOT NULL,
        [resource_public_id] uniqueidentifier NOT NULL,
        [room_type] varchar(30) NOT NULL DEFAULT 'DIRECT',
        [status_code] varchar(20) NOT NULL DEFAULT 'ACTIVE',
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_chat_rooms] PRIMARY KEY ([id]),
        CONSTRAINT [CK_chat_rooms_resource_type] CHECK ([resource_type] IN ('TRANSACTION')),
        CONSTRAINT [CK_chat_rooms_room_type] CHECK ([room_type] IN ('DIRECT')),
        CONSTRAINT [CK_chat_rooms_status] CHECK ([status_code] IN ('ACTIVE','READ_ONLY','CLOSED'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063935_ImplementTransactionChat'
)
BEGIN
    CREATE TABLE [chat_participants] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [chat_room_id] bigint NOT NULL,
        [user_id] bigint NOT NULL,
        [participant_role] varchar(20) NOT NULL,
        [status_code] varchar(20) NOT NULL DEFAULT 'ACTIVE',
        [joined_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [access_started_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [access_ended_at] datetime2(7) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_chat_participants] PRIMARY KEY ([id]),
        CONSTRAINT [CK_chat_participants_access] CHECK ([access_ended_at] IS NULL OR [access_ended_at] >= [access_started_at]),
        CONSTRAINT [CK_chat_participants_role] CHECK ([participant_role] IN ('CUSTOMER','PROVIDER')),
        CONSTRAINT [CK_chat_participants_status] CHECK ([status_code] IN ('ACTIVE','ENDED')),
        CONSTRAINT [FK_chat_participants_chat_rooms_chat_room_id] FOREIGN KEY ([chat_room_id]) REFERENCES [chat_rooms] ([id]),
        CONSTRAINT [FK_chat_participants_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063935_ImplementTransactionChat'
)
BEGIN
    CREATE TABLE [chat_messages] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [chat_room_id] bigint NOT NULL,
        [sender_participant_id] bigint NOT NULL,
        [message_type] varchar(20) NOT NULL,
        [body] nvarchar(4000) NULL,
        [idempotency_key] varchar(120) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_chat_messages] PRIMARY KEY ([id]),
        CONSTRAINT [CK_chat_messages_body] CHECK (([message_type] = 'TEXT' AND [body] IS NOT NULL AND LEN(LTRIM(RTRIM([body]))) > 0) OR [message_type] = 'FILE'),
        CONSTRAINT [CK_chat_messages_type] CHECK ([message_type] IN ('TEXT','FILE')),
        CONSTRAINT [FK_chat_messages_chat_participants_sender_participant_id] FOREIGN KEY ([sender_participant_id]) REFERENCES [chat_participants] ([id]),
        CONSTRAINT [FK_chat_messages_chat_rooms_chat_room_id] FOREIGN KEY ([chat_room_id]) REFERENCES [chat_rooms] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063935_ImplementTransactionChat'
)
BEGIN
    CREATE TABLE [chat_attachments] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [chat_message_id] bigint NOT NULL,
        [file_id] bigint NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NOT NULL,
        CONSTRAINT [PK_chat_attachments] PRIMARY KEY ([id]),
        CONSTRAINT [FK_chat_attachments_chat_messages_chat_message_id] FOREIGN KEY ([chat_message_id]) REFERENCES [chat_messages] ([id]),
        CONSTRAINT [FK_chat_attachments_files_file_id] FOREIGN KEY ([file_id]) REFERENCES [files] ([id]),
        CONSTRAINT [FK_chat_attachments_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063935_ImplementTransactionChat'
)
BEGIN
    CREATE TABLE [chat_message_reads] (
        [id] bigint NOT NULL IDENTITY,
        [chat_room_id] bigint NOT NULL,
        [chat_message_id] bigint NOT NULL,
        [user_id] bigint NOT NULL,
        [read_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_chat_message_reads] PRIMARY KEY ([id]),
        CONSTRAINT [FK_chat_message_reads_chat_messages_chat_message_id] FOREIGN KEY ([chat_message_id]) REFERENCES [chat_messages] ([id]),
        CONSTRAINT [FK_chat_message_reads_chat_rooms_chat_room_id] FOREIGN KEY ([chat_room_id]) REFERENCES [chat_rooms] ([id]),
        CONSTRAINT [FK_chat_message_reads_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063935_ImplementTransactionChat'
)
BEGIN
    CREATE UNIQUE INDEX [IX_chat_attachments_chat_message_id_file_id] ON [chat_attachments] ([chat_message_id], [file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063935_ImplementTransactionChat'
)
BEGIN
    CREATE INDEX [IX_chat_attachments_created_by_user_id] ON [chat_attachments] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063935_ImplementTransactionChat'
)
BEGIN
    CREATE UNIQUE INDEX [IX_chat_attachments_file_id] ON [chat_attachments] ([file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063935_ImplementTransactionChat'
)
BEGIN
    CREATE UNIQUE INDEX [IX_chat_attachments_public_id] ON [chat_attachments] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063935_ImplementTransactionChat'
)
BEGIN
    CREATE UNIQUE INDEX [IX_chat_message_reads_chat_message_id_user_id] ON [chat_message_reads] ([chat_message_id], [user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063935_ImplementTransactionChat'
)
BEGIN
    CREATE INDEX [IX_chat_message_reads_chat_room_id_user_id_read_at] ON [chat_message_reads] ([chat_room_id], [user_id], [read_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063935_ImplementTransactionChat'
)
BEGIN
    CREATE INDEX [IX_chat_message_reads_user_id] ON [chat_message_reads] ([user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063935_ImplementTransactionChat'
)
BEGIN
    CREATE INDEX [IX_chat_messages_chat_room_id_id] ON [chat_messages] ([chat_room_id], [id] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063935_ImplementTransactionChat'
)
BEGIN
    CREATE UNIQUE INDEX [IX_chat_messages_idempotency_key] ON [chat_messages] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063935_ImplementTransactionChat'
)
BEGIN
    CREATE UNIQUE INDEX [IX_chat_messages_public_id] ON [chat_messages] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063935_ImplementTransactionChat'
)
BEGIN
    CREATE INDEX [IX_chat_messages_sender_participant_id] ON [chat_messages] ([sender_participant_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063935_ImplementTransactionChat'
)
BEGIN
    CREATE UNIQUE INDEX [IX_chat_participants_chat_room_id_user_id] ON [chat_participants] ([chat_room_id], [user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063935_ImplementTransactionChat'
)
BEGIN
    CREATE UNIQUE INDEX [IX_chat_participants_public_id] ON [chat_participants] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063935_ImplementTransactionChat'
)
BEGIN
    CREATE INDEX [IX_chat_participants_user_id_status_code] ON [chat_participants] ([user_id], [status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063935_ImplementTransactionChat'
)
BEGIN
    CREATE UNIQUE INDEX [IX_chat_rooms_public_id] ON [chat_rooms] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063935_ImplementTransactionChat'
)
BEGIN
    CREATE UNIQUE INDEX [IX_chat_rooms_resource_type_resource_public_id_room_type] ON [chat_rooms] ([resource_type], [resource_public_id], [room_type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063935_ImplementTransactionChat'
)
BEGIN
    CREATE INDEX [IX_chat_rooms_status_code] ON [chat_rooms] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063935_ImplementTransactionChat'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812063935_ImplementTransactionChat', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812105822_ImplementProviderInteriorWorkflow'
)
BEGIN
    CREATE TABLE [interior_contract_change_files] (
        [id] bigint NOT NULL IDENTITY,
        [contract_change_id] bigint NOT NULL,
        [file_id] bigint NOT NULL,
        [purpose_code] varchar(50) NOT NULL,
        [display_order] int NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_interior_contract_change_files] PRIMARY KEY ([id]),
        CONSTRAINT [FK_interior_contract_change_files_files_file_id] FOREIGN KEY ([file_id]) REFERENCES [files] ([id]),
        CONSTRAINT [FK_interior_contract_change_files_interior_contract_changes_contract_change_id] FOREIGN KEY ([contract_change_id]) REFERENCES [interior_contract_changes] ([id]),
        CONSTRAINT [FK_interior_contract_change_files_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812105822_ImplementProviderInteriorWorkflow'
)
BEGIN
    CREATE TABLE [interior_stage_inspection_files] (
        [id] bigint NOT NULL IDENTITY,
        [stage_inspection_id] bigint NOT NULL,
        [file_id] bigint NOT NULL,
        [purpose_code] varchar(50) NOT NULL,
        [display_order] int NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_interior_stage_inspection_files] PRIMARY KEY ([id]),
        CONSTRAINT [FK_interior_stage_inspection_files_files_file_id] FOREIGN KEY ([file_id]) REFERENCES [files] ([id]),
        CONSTRAINT [FK_interior_stage_inspection_files_interior_stage_inspections_stage_inspection_id] FOREIGN KEY ([stage_inspection_id]) REFERENCES [interior_stage_inspections] ([id]),
        CONSTRAINT [FK_interior_stage_inspection_files_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812105822_ImplementProviderInteriorWorkflow'
)
BEGIN
    CREATE TABLE [interior_work_stage_assignments] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [work_stage_id] bigint NOT NULL,
        [project_participant_id] bigint NOT NULL,
        [status_code] varchar(20) NOT NULL,
        [effective_from] datetime2(7) NOT NULL,
        [effective_to] datetime2(7) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_interior_work_stage_assignments] PRIMARY KEY ([id]),
        CONSTRAINT [CK_interior_work_stage_assignments_period] CHECK ([effective_to] IS NULL OR [effective_to] >= [effective_from]),
        CONSTRAINT [CK_interior_work_stage_assignments_status] CHECK ([status_code] IN ('ACTIVE','ENDED','CANCELLED')),
        CONSTRAINT [FK_interior_work_stage_assignments_interior_project_participants_project_participant_id] FOREIGN KEY ([project_participant_id]) REFERENCES [interior_project_participants] ([id]),
        CONSTRAINT [FK_interior_work_stage_assignments_interior_work_stages_work_stage_id] FOREIGN KEY ([work_stage_id]) REFERENCES [interior_work_stages] ([id]),
        CONSTRAINT [FK_interior_work_stage_assignments_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_interior_work_stage_assignments_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812105822_ImplementProviderInteriorWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_contract_change_files_contract_change_id_file_id] ON [interior_contract_change_files] ([contract_change_id], [file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812105822_ImplementProviderInteriorWorkflow'
)
BEGIN
    CREATE INDEX [IX_interior_contract_change_files_created_by_user_id] ON [interior_contract_change_files] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812105822_ImplementProviderInteriorWorkflow'
)
BEGIN
    CREATE INDEX [IX_interior_contract_change_files_file_id] ON [interior_contract_change_files] ([file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812105822_ImplementProviderInteriorWorkflow'
)
BEGIN
    CREATE INDEX [IX_interior_stage_inspection_files_created_by_user_id] ON [interior_stage_inspection_files] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812105822_ImplementProviderInteriorWorkflow'
)
BEGIN
    CREATE INDEX [IX_interior_stage_inspection_files_file_id] ON [interior_stage_inspection_files] ([file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812105822_ImplementProviderInteriorWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_stage_inspection_files_stage_inspection_id_file_id] ON [interior_stage_inspection_files] ([stage_inspection_id], [file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812105822_ImplementProviderInteriorWorkflow'
)
BEGIN
    CREATE INDEX [IX_interior_work_stage_assignments_created_by_user_id] ON [interior_work_stage_assignments] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812105822_ImplementProviderInteriorWorkflow'
)
BEGIN
    CREATE INDEX [IX_interior_work_stage_assignments_project_participant_id_status_code_effective_to] ON [interior_work_stage_assignments] ([project_participant_id], [status_code], [effective_to]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812105822_ImplementProviderInteriorWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_work_stage_assignments_public_id] ON [interior_work_stage_assignments] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812105822_ImplementProviderInteriorWorkflow'
)
BEGIN
    CREATE INDEX [IX_interior_work_stage_assignments_updated_by_user_id] ON [interior_work_stage_assignments] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812105822_ImplementProviderInteriorWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_interior_work_stage_assignments_work_stage_id_project_participant_id] ON [interior_work_stage_assignments] ([work_stage_id], [project_participant_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812105822_ImplementProviderInteriorWorkflow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812105822_ImplementProviderInteriorWorkflow', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE TABLE [emergency_progress_events] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [transaction_id] bigint NOT NULL,
        [actor_user_id] bigint NOT NULL,
        [event_type_code] varchar(30) NOT NULL,
        [note] nvarchar(1000) NULL,
        [occurred_at] datetime2(7) NOT NULL,
        [idempotency_key] varchar(100) NOT NULL,
        CONSTRAINT [PK_emergency_progress_events] PRIMARY KEY ([id]),
        CONSTRAINT [CK_emergency_progress_events_type] CHECK ([event_type_code] IN ('DISPATCH_CONFIRMED','DEPARTED','EN_ROUTE','ARRIVED')),
        CONSTRAINT [FK_emergency_progress_events_transactions_transaction_id] FOREIGN KEY ([transaction_id]) REFERENCES [transactions] ([id]),
        CONSTRAINT [FK_emergency_progress_events_users_actor_user_id] FOREIGN KEY ([actor_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE TABLE [emergency_responses] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [service_request_id] bigint NOT NULL,
        [request_dispatch_id] bigint NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [status_code] varchar(20) NOT NULL DEFAULT 'PENDING',
        [eta_minutes] int NULL,
        [estimated_arrival_at] datetime2(7) NULL,
        [conditions_text] nvarchar(1000) NULL,
        [responded_at] datetime2(7) NOT NULL,
        [expires_at] datetime2(7) NOT NULL,
        [idempotency_key] varchar(100) NOT NULL,
        [selected_at] datetime2(7) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_emergency_responses] PRIMARY KEY ([id]),
        CONSTRAINT [CK_emergency_responses_eta] CHECK (([status_code] <> 'AVAILABLE' AND [status_code] <> 'SELECTED') OR ([eta_minutes] IS NOT NULL AND [eta_minutes] > 0) OR [estimated_arrival_at] IS NOT NULL),
        CONSTRAINT [CK_emergency_responses_status] CHECK ([status_code] IN ('PENDING','AVAILABLE','UNAVAILABLE','EXPIRED','SELECTED','NOT_SELECTED')),
        CONSTRAINT [FK_emergency_responses_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_emergency_responses_request_dispatches_request_dispatch_id] FOREIGN KEY ([request_dispatch_id]) REFERENCES [request_dispatches] ([id]),
        CONSTRAINT [FK_emergency_responses_service_requests_service_request_id] FOREIGN KEY ([service_request_id]) REFERENCES [service_requests] ([id]),
        CONSTRAINT [FK_emergency_responses_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_emergency_responses_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE TABLE [provider_emergency_settings] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [is_enabled] bit NOT NULL DEFAULT CAST(0 AS bit),
        [temporarily_unavailable_until] datetime2(7) NULL,
        [temporary_unavailable_reason] nvarchar(500) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_provider_emergency_settings] PRIMARY KEY ([id]),
        CONSTRAINT [FK_provider_emergency_settings_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_provider_emergency_settings_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_provider_emergency_settings_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE TABLE [provider_emergency_exceptions] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [provider_emergency_setting_id] bigint NOT NULL,
        [starts_at] datetime2(7) NOT NULL,
        [ends_at] datetime2(7) NOT NULL,
        [reason] nvarchar(500) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_provider_emergency_exceptions] PRIMARY KEY ([id]),
        CONSTRAINT [CK_provider_emergency_exceptions_period] CHECK ([ends_at] > [starts_at]),
        CONSTRAINT [FK_provider_emergency_exceptions_provider_emergency_settings_provider_emergency_setting_id] FOREIGN KEY ([provider_emergency_setting_id]) REFERENCES [provider_emergency_settings] ([id]),
        CONSTRAINT [FK_provider_emergency_exceptions_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE TABLE [provider_emergency_service_settings] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [provider_emergency_setting_id] bigint NOT NULL,
        [provider_service_category_id] bigint NOT NULL,
        [is_enabled] bit NOT NULL DEFAULT CAST(0 AS bit),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_provider_emergency_service_settings] PRIMARY KEY ([id]),
        CONSTRAINT [FK_provider_emergency_service_settings_provider_emergency_settings_provider_emergency_setting_id] FOREIGN KEY ([provider_emergency_setting_id]) REFERENCES [provider_emergency_settings] ([id]),
        CONSTRAINT [FK_provider_emergency_service_settings_provider_service_categories_provider_service_category_id] FOREIGN KEY ([provider_service_category_id]) REFERENCES [provider_service_categories] ([id]),
        CONSTRAINT [FK_provider_emergency_service_settings_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_provider_emergency_service_settings_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE TABLE [provider_emergency_availability_slots] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [provider_emergency_service_setting_id] bigint NOT NULL,
        [day_of_week] tinyint NOT NULL,
        [start_time] time(0) NULL,
        [end_time] time(0) NULL,
        [is_24_hours] bit NOT NULL DEFAULT CAST(0 AS bit),
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        CONSTRAINT [PK_provider_emergency_availability_slots] PRIMARY KEY ([id]),
        CONSTRAINT [CK_provider_emergency_availability_day] CHECK ([day_of_week] BETWEEN 0 AND 6),
        CONSTRAINT [CK_provider_emergency_availability_period] CHECK (([is_24_hours] = 1 AND [start_time] IS NULL AND [end_time] IS NULL) OR ([is_24_hours] = 0 AND [start_time] IS NOT NULL AND [end_time] IS NOT NULL AND [start_time] < [end_time])),
        CONSTRAINT [FK_provider_emergency_availability_slots_provider_emergency_service_settings_provider_emergency_service_setting_id] FOREIGN KEY ([provider_emergency_service_setting_id]) REFERENCES [provider_emergency_service_settings] ([id]),
        CONSTRAINT [FK_provider_emergency_availability_slots_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE INDEX [IX_emergency_progress_events_actor_user_id] ON [emergency_progress_events] ([actor_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_emergency_progress_events_idempotency_key] ON [emergency_progress_events] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_emergency_progress_events_public_id] ON [emergency_progress_events] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE INDEX [IX_emergency_progress_events_transaction_id_occurred_at] ON [emergency_progress_events] ([transaction_id], [occurred_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE INDEX [IX_emergency_responses_created_by_user_id] ON [emergency_responses] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_emergency_responses_idempotency_key] ON [emergency_responses] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE INDEX [IX_emergency_responses_provider_profile_id] ON [emergency_responses] ([provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_emergency_responses_public_id] ON [emergency_responses] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_emergency_responses_request_dispatch_id] ON [emergency_responses] ([request_dispatch_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE INDEX [IX_emergency_responses_service_request_id_status_code] ON [emergency_responses] ([service_request_id], [status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE INDEX [IX_emergency_responses_updated_by_user_id] ON [emergency_responses] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE INDEX [IX_provider_emergency_availability_slots_created_by_user_id] ON [provider_emergency_availability_slots] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE INDEX [IX_provider_emergency_availability_slots_provider_emergency_service_setting_id_day_of_week] ON [provider_emergency_availability_slots] ([provider_emergency_service_setting_id], [day_of_week]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_emergency_availability_slots_public_id] ON [provider_emergency_availability_slots] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE INDEX [IX_provider_emergency_exceptions_created_by_user_id] ON [provider_emergency_exceptions] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE INDEX [IX_provider_emergency_exceptions_provider_emergency_setting_id_starts_at_ends_at] ON [provider_emergency_exceptions] ([provider_emergency_setting_id], [starts_at], [ends_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_emergency_exceptions_public_id] ON [provider_emergency_exceptions] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE INDEX [IX_provider_emergency_service_settings_created_by_user_id] ON [provider_emergency_service_settings] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE INDEX [IX_provider_emergency_service_settings_provider_emergency_setting_id_is_enabled] ON [provider_emergency_service_settings] ([provider_emergency_setting_id], [is_enabled]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_emergency_service_settings_provider_service_category_id] ON [provider_emergency_service_settings] ([provider_service_category_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_emergency_service_settings_public_id] ON [provider_emergency_service_settings] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE INDEX [IX_provider_emergency_service_settings_updated_by_user_id] ON [provider_emergency_service_settings] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE INDEX [IX_provider_emergency_settings_created_by_user_id] ON [provider_emergency_settings] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_emergency_settings_provider_profile_id] ON [provider_emergency_settings] ([provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_emergency_settings_public_id] ON [provider_emergency_settings] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    CREATE INDEX [IX_provider_emergency_settings_updated_by_user_id] ON [provider_emergency_settings] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812115255_ImplementEmergencyWorkflow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812115255_ImplementEmergencyWorkflow', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812220043_ImplementOperationsAutomationSecurity'
)
BEGIN
    ALTER TABLE [outbox_events] DROP CONSTRAINT [CK_outbox_events_status];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812220043_ImplementOperationsAutomationSecurity'
)
BEGIN
    CREATE TABLE [scheduled_job_leases] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [job_name] varchar(100) NOT NULL,
        [configuration_status_code] varchar(30) NOT NULL,
        [lease_owner] varchar(150) NULL,
        [lease_expires_at] datetime2(7) NULL,
        [last_started_at] datetime2(7) NULL,
        [last_succeeded_at] datetime2(7) NULL,
        [last_failed_at] datetime2(7) NULL,
        [last_error_code] varchar(100) NULL,
        [next_scheduled_at] datetime2(7) NULL,
        [processing_count] int NOT NULL DEFAULT 0,
        [failed_count] int NOT NULL DEFAULT 0,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_scheduled_job_leases] PRIMARY KEY ([id]),
        CONSTRAINT [CK_scheduled_job_leases_configuration] CHECK ([configuration_status_code] IN ('ENABLED','DISABLED','NOT_CONFIGURED'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812220043_ImplementOperationsAutomationSecurity'
)
BEGIN
    CREATE TABLE [scheduled_job_runs] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [job_name] varchar(100) NOT NULL,
        [instance_id] varchar(150) NOT NULL,
        [status_code] varchar(20) NOT NULL,
        [started_at] datetime2(7) NOT NULL,
        [completed_at] datetime2(7) NULL,
        [processed_count] int NOT NULL DEFAULT 0,
        [failed_count] int NOT NULL DEFAULT 0,
        [error_code] varchar(100) NULL,
        CONSTRAINT [PK_scheduled_job_runs] PRIMARY KEY ([id]),
        CONSTRAINT [CK_scheduled_job_runs_status] CHECK ([status_code] IN ('RUNNING','SUCCEEDED','FAILED','SKIPPED'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812220043_ImplementOperationsAutomationSecurity'
)
BEGIN
    EXEC(N'ALTER TABLE [outbox_events] ADD CONSTRAINT [CK_outbox_events_status] CHECK ([status_code] IN (''PENDING'',''PROCESSING'',''PUBLISHED'',''FAILED'',''DEAD''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812220043_ImplementOperationsAutomationSecurity'
)
BEGIN
    CREATE INDEX [IX_scheduled_job_leases_configuration_status_code_next_scheduled_at] ON [scheduled_job_leases] ([configuration_status_code], [next_scheduled_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812220043_ImplementOperationsAutomationSecurity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_scheduled_job_leases_job_name] ON [scheduled_job_leases] ([job_name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812220043_ImplementOperationsAutomationSecurity'
)
BEGIN
    CREATE INDEX [IX_scheduled_job_leases_lease_expires_at] ON [scheduled_job_leases] ([lease_expires_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812220043_ImplementOperationsAutomationSecurity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_scheduled_job_leases_public_id] ON [scheduled_job_leases] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812220043_ImplementOperationsAutomationSecurity'
)
BEGIN
    CREATE INDEX [IX_scheduled_job_runs_job_name_started_at] ON [scheduled_job_runs] ([job_name], [started_at] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812220043_ImplementOperationsAutomationSecurity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_scheduled_job_runs_public_id] ON [scheduled_job_runs] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812220043_ImplementOperationsAutomationSecurity'
)
BEGIN
    CREATE INDEX [IX_scheduled_job_runs_status_code] ON [scheduled_job_runs] ([status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812220043_ImplementOperationsAutomationSecurity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812220043_ImplementOperationsAutomationSecurity', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812232518_ExpandCommonChatResources'
)
BEGIN
    ALTER TABLE [chat_rooms] DROP CONSTRAINT [CK_chat_rooms_resource_type];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812232518_ExpandCommonChatResources'
)
BEGIN
    ALTER TABLE [chat_rooms] DROP CONSTRAINT [CK_chat_rooms_room_type];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812232518_ExpandCommonChatResources'
)
BEGIN
    EXEC(N'ALTER TABLE [chat_rooms] ADD CONSTRAINT [CK_chat_rooms_resource_type] CHECK ([resource_type] IN (''TRANSACTION'',''SUBSCRIPTION'',''INTERIOR'',''AFTER_SERVICE''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812232518_ExpandCommonChatResources'
)
BEGIN
    EXEC(N'ALTER TABLE [chat_rooms] ADD CONSTRAINT [CK_chat_rooms_room_type] CHECK ([room_type] IN (''DIRECT'',''PRIMARY_CONTRACTOR'',''SITE_SURVEY''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812232518_ExpandCommonChatResources'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812232518_ExpandCommonChatResources', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813002116_ImplementProviderExitWalletRefundClosure'
)
BEGIN
    CREATE TABLE [provider_exit_requests] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [user_id] bigint NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [wallet_refund_request_id] bigint NULL,
        [request_type_code] varchar(30) NOT NULL,
        [reason] nvarchar(1000) NOT NULL,
        [status_code] varchar(40) NOT NULL DEFAULT 'REQUESTED',
        [review_status_code] varchar(30) NOT NULL DEFAULT 'PENDING',
        [requested_at] datetime2(7) NOT NULL,
        [reviewed_at] datetime2(7) NULL,
        [reviewed_by_user_id] bigint NULL,
        [decision_reason] nvarchar(1000) NULL,
        [completed_at] datetime2(7) NULL,
        [idempotency_key] varchar(150) NOT NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_provider_exit_requests] PRIMARY KEY ([id]),
        CONSTRAINT [CK_provider_exit_requests_review] CHECK ([review_status_code] IN ('PENDING','UNDER_REVIEW','APPROVED','REJECTED')),
        CONSTRAINT [CK_provider_exit_requests_status] CHECK ([status_code] IN ('REQUESTED','UNDER_REVIEW','REFUND_REQUIRED','BLOCKED_BY_ACTIVE_WORK','READY_TO_COMPLETE','COMPLETED','REJECTED','CANCELLED')),
        CONSTRAINT [CK_provider_exit_requests_type] CHECK ([request_type_code] IN ('PROVIDER_ROLE_EXIT','ACCOUNT_WITHDRAWAL')),
        CONSTRAINT [FK_provider_exit_requests_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_provider_exit_requests_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_provider_exit_requests_users_reviewed_by_user_id] FOREIGN KEY ([reviewed_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_provider_exit_requests_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_provider_exit_requests_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_provider_exit_requests_wallet_refund_requests_wallet_refund_request_id] FOREIGN KEY ([wallet_refund_request_id]) REFERENCES [wallet_refund_requests] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813002116_ImplementProviderExitWalletRefundClosure'
)
BEGIN
    CREATE INDEX [IX_provider_exit_requests_created_by_user_id] ON [provider_exit_requests] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813002116_ImplementProviderExitWalletRefundClosure'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_exit_requests_idempotency_key] ON [provider_exit_requests] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813002116_ImplementProviderExitWalletRefundClosure'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_provider_exit_requests_provider_profile_id] ON [provider_exit_requests] ([provider_profile_id]) WHERE [status_code] <> ''COMPLETED'' AND [status_code] <> ''REJECTED'' AND [status_code] <> ''CANCELLED''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813002116_ImplementProviderExitWalletRefundClosure'
)
BEGIN
    CREATE INDEX [IX_provider_exit_requests_provider_profile_id_status_code] ON [provider_exit_requests] ([provider_profile_id], [status_code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813002116_ImplementProviderExitWalletRefundClosure'
)
BEGIN
    CREATE UNIQUE INDEX [IX_provider_exit_requests_public_id] ON [provider_exit_requests] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813002116_ImplementProviderExitWalletRefundClosure'
)
BEGIN
    CREATE INDEX [IX_provider_exit_requests_reviewed_by_user_id] ON [provider_exit_requests] ([reviewed_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813002116_ImplementProviderExitWalletRefundClosure'
)
BEGIN
    CREATE INDEX [IX_provider_exit_requests_updated_by_user_id] ON [provider_exit_requests] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813002116_ImplementProviderExitWalletRefundClosure'
)
BEGIN
    CREATE INDEX [IX_provider_exit_requests_user_id] ON [provider_exit_requests] ([user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813002116_ImplementProviderExitWalletRefundClosure'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_provider_exit_requests_wallet_refund_request_id] ON [provider_exit_requests] ([wallet_refund_request_id]) WHERE [wallet_refund_request_id] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813002116_ImplementProviderExitWalletRefundClosure'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260813002116_ImplementProviderExitWalletRefundClosure', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813050633_ImplementTransactionDirectPaymentClosure'
)
BEGIN
    CREATE TABLE [transaction_direct_payments] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [transaction_id] bigint NOT NULL,
        [registered_by_user_id] bigint NOT NULL,
        [registered_by_role_code] varchar(20) NOT NULL,
        [amount] decimal(19,4) NOT NULL,
        [currency_code] char(3) NOT NULL,
        [payment_method_code] varchar(30) NOT NULL,
        [paid_at] datetime2(7) NOT NULL,
        [note_text] nvarchar(1000) NULL,
        [evidence_file_id] bigint NULL,
        [status_code] varchar(30) NOT NULL DEFAULT 'REGISTERED',
        [registered_at] datetime2(7) NOT NULL,
        [decided_by_user_id] bigint NULL,
        [decided_at] datetime2(7) NULL,
        [rejection_reason] nvarchar(1000) NULL,
        [registration_idempotency_key] varchar(100) NOT NULL,
        [decision_idempotency_key] varchar(100) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_transaction_direct_payments] PRIMARY KEY ([id]),
        CONSTRAINT [CK_transaction_direct_payments_amount] CHECK ([amount] > 0),
        CONSTRAINT [CK_transaction_direct_payments_method] CHECK ([payment_method_code] IN ('BANK_TRANSFER','ON_SITE_CARD','CASH','OTHER')),
        CONSTRAINT [CK_transaction_direct_payments_role] CHECK ([registered_by_role_code] IN ('CUSTOMER','PROVIDER')),
        CONSTRAINT [CK_transaction_direct_payments_status] CHECK ([status_code] IN ('REGISTERED','COUNTERPART_CONFIRMED','REJECTED')),
        CONSTRAINT [FK_transaction_direct_payments_files_evidence_file_id] FOREIGN KEY ([evidence_file_id]) REFERENCES [files] ([id]),
        CONSTRAINT [FK_transaction_direct_payments_transactions_transaction_id] FOREIGN KEY ([transaction_id]) REFERENCES [transactions] ([id]),
        CONSTRAINT [FK_transaction_direct_payments_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_transaction_direct_payments_users_decided_by_user_id] FOREIGN KEY ([decided_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_transaction_direct_payments_users_registered_by_user_id] FOREIGN KEY ([registered_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_transaction_direct_payments_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813050633_ImplementTransactionDirectPaymentClosure'
)
BEGIN
    CREATE INDEX [IX_transaction_direct_payments_created_by_user_id] ON [transaction_direct_payments] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813050633_ImplementTransactionDirectPaymentClosure'
)
BEGIN
    CREATE INDEX [IX_transaction_direct_payments_decided_by_user_id] ON [transaction_direct_payments] ([decided_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813050633_ImplementTransactionDirectPaymentClosure'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_transaction_direct_payments_decision_idempotency_key] ON [transaction_direct_payments] ([decision_idempotency_key]) WHERE [decision_idempotency_key] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813050633_ImplementTransactionDirectPaymentClosure'
)
BEGIN
    CREATE INDEX [IX_transaction_direct_payments_evidence_file_id] ON [transaction_direct_payments] ([evidence_file_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813050633_ImplementTransactionDirectPaymentClosure'
)
BEGIN
    CREATE UNIQUE INDEX [IX_transaction_direct_payments_public_id] ON [transaction_direct_payments] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813050633_ImplementTransactionDirectPaymentClosure'
)
BEGIN
    CREATE INDEX [IX_transaction_direct_payments_registered_by_user_id] ON [transaction_direct_payments] ([registered_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813050633_ImplementTransactionDirectPaymentClosure'
)
BEGIN
    CREATE UNIQUE INDEX [IX_transaction_direct_payments_registration_idempotency_key] ON [transaction_direct_payments] ([registration_idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813050633_ImplementTransactionDirectPaymentClosure'
)
BEGIN
    CREATE UNIQUE INDEX [IX_transaction_direct_payments_transaction_id] ON [transaction_direct_payments] ([transaction_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813050633_ImplementTransactionDirectPaymentClosure'
)
BEGIN
    CREATE INDEX [IX_transaction_direct_payments_updated_by_user_id] ON [transaction_direct_payments] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813050633_ImplementTransactionDirectPaymentClosure'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260813050633_ImplementTransactionDirectPaymentClosure', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813070709_ImplementCustomerAccountWithdrawalClosure'
)
BEGIN
    DROP INDEX [IX_customer_withdrawal_requests_user_id_scope_code] ON [customer_withdrawal_requests];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813070709_ImplementCustomerAccountWithdrawalClosure'
)
BEGIN
    ALTER TABLE [customer_withdrawal_requests] DROP CONSTRAINT [CK_customer_withdrawal_status];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813070709_ImplementCustomerAccountWithdrawalClosure'
)
BEGIN
    ALTER TABLE [customer_withdrawal_requests] ADD [decision_idempotency_key] varchar(150) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813070709_ImplementCustomerAccountWithdrawalClosure'
)
BEGIN
    ALTER TABLE [customer_withdrawal_requests] ADD [idempotency_key] varchar(150) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813070709_ImplementCustomerAccountWithdrawalClosure'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_customer_withdrawal_requests_decision_idempotency_key] ON [customer_withdrawal_requests] ([decision_idempotency_key]) WHERE [decision_idempotency_key] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813070709_ImplementCustomerAccountWithdrawalClosure'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_customer_withdrawal_requests_idempotency_key] ON [customer_withdrawal_requests] ([idempotency_key]) WHERE [idempotency_key] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813070709_ImplementCustomerAccountWithdrawalClosure'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_customer_withdrawal_requests_user_id_scope_code] ON [customer_withdrawal_requests] ([user_id], [scope_code]) WHERE [status_code] IN (''REQUESTED'',''UNDER_REVIEW'',''BLOCKED_BY_ACTIVE_WORK'',''READY_TO_COMPLETE'')');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813070709_ImplementCustomerAccountWithdrawalClosure'
)
BEGIN
    EXEC(N'ALTER TABLE [customer_withdrawal_requests] ADD CONSTRAINT [CK_customer_withdrawal_status] CHECK ([status_code] IN (''REQUESTED'',''UNDER_REVIEW'',''BLOCKED_BY_ACTIVE_WORK'',''READY_TO_COMPLETE'',''APPROVED'',''COMPLETED'',''REJECTED'',''CANCELLED''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813070709_ImplementCustomerAccountWithdrawalClosure'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260813070709_ImplementCustomerAccountWithdrawalClosure', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813083413_ImplementCustomerProviderUserBlock'
)
BEGIN
    CREATE TABLE [user_relationship_blocks] (
        [id] bigint NOT NULL IDENTITY,
        [public_id] uniqueidentifier NOT NULL,
        [customer_profile_id] bigint NOT NULL,
        [provider_profile_id] bigint NOT NULL,
        [direction_code] varchar(30) NOT NULL,
        [status_code] varchar(20) NOT NULL DEFAULT 'ACTIVE',
        [reason_code] varchar(50) NULL,
        [private_memo] nvarchar(1000) NULL,
        [created_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [created_by_user_id] bigint NULL,
        [updated_at] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [updated_by_user_id] bigint NULL,
        [released_at] datetime2(7) NULL,
        [released_by_user_id] bigint NULL,
        [idempotency_key] varchar(150) NOT NULL,
        [release_idempotency_key] varchar(150) NULL,
        [row_version] rowversion NOT NULL,
        CONSTRAINT [PK_user_relationship_blocks] PRIMARY KEY ([id]),
        CONSTRAINT [CK_user_relationship_blocks_direction] CHECK ([direction_code] IN ('CUSTOMER_TO_PROVIDER','PROVIDER_TO_CUSTOMER')),
        CONSTRAINT [CK_user_relationship_blocks_status] CHECK ([status_code] IN ('ACTIVE','RELEASED')),
        CONSTRAINT [FK_user_relationship_blocks_customer_profiles_customer_profile_id] FOREIGN KEY ([customer_profile_id]) REFERENCES [customer_profiles] ([id]),
        CONSTRAINT [FK_user_relationship_blocks_provider_profiles_provider_profile_id] FOREIGN KEY ([provider_profile_id]) REFERENCES [provider_profiles] ([id]),
        CONSTRAINT [FK_user_relationship_blocks_users_created_by_user_id] FOREIGN KEY ([created_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_user_relationship_blocks_users_released_by_user_id] FOREIGN KEY ([released_by_user_id]) REFERENCES [users] ([id]),
        CONSTRAINT [FK_user_relationship_blocks_users_updated_by_user_id] FOREIGN KEY ([updated_by_user_id]) REFERENCES [users] ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813083413_ImplementCustomerProviderUserBlock'
)
BEGIN
    CREATE INDEX [IX_user_relationship_blocks_created_by_user_id] ON [user_relationship_blocks] ([created_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813083413_ImplementCustomerProviderUserBlock'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_user_relationship_blocks_customer_profile_id_provider_profile_id_direction_code] ON [user_relationship_blocks] ([customer_profile_id], [provider_profile_id], [direction_code]) WHERE [status_code] = ''ACTIVE''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813083413_ImplementCustomerProviderUserBlock'
)
BEGIN
    CREATE INDEX [IX_user_relationship_blocks_customer_profile_id_status_code_created_at] ON [user_relationship_blocks] ([customer_profile_id], [status_code], [created_at]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813083413_ImplementCustomerProviderUserBlock'
)
BEGIN
    CREATE UNIQUE INDEX [IX_user_relationship_blocks_idempotency_key] ON [user_relationship_blocks] ([idempotency_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813083413_ImplementCustomerProviderUserBlock'
)
BEGIN
    CREATE INDEX [IX_user_relationship_blocks_provider_profile_id] ON [user_relationship_blocks] ([provider_profile_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813083413_ImplementCustomerProviderUserBlock'
)
BEGIN
    CREATE UNIQUE INDEX [IX_user_relationship_blocks_public_id] ON [user_relationship_blocks] ([public_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813083413_ImplementCustomerProviderUserBlock'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_user_relationship_blocks_release_idempotency_key] ON [user_relationship_blocks] ([release_idempotency_key]) WHERE [release_idempotency_key] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813083413_ImplementCustomerProviderUserBlock'
)
BEGIN
    CREATE INDEX [IX_user_relationship_blocks_released_by_user_id] ON [user_relationship_blocks] ([released_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813083413_ImplementCustomerProviderUserBlock'
)
BEGIN
    CREATE INDEX [IX_user_relationship_blocks_updated_by_user_id] ON [user_relationship_blocks] ([updated_by_user_id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813083413_ImplementCustomerProviderUserBlock'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260813083413_ImplementCustomerProviderUserBlock', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814063006_ImplementInteriorFinalSelectionFee'
)
BEGIN
    ALTER TABLE [interior_projects] DROP CONSTRAINT [CK_interior_projects_fee];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814063006_ImplementInteriorFinalSelectionFee'
)
BEGIN
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[interior_projects]') AND [c].[name] = N'fee_assessment_status_code');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [interior_projects] DROP CONSTRAINT ' + @var8 + ';');
    ALTER TABLE [interior_projects] ADD DEFAULT 'PENDING_SELECTION' FOR [fee_assessment_status_code];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814063006_ImplementInteriorFinalSelectionFee'
)
BEGIN
    UPDATE [interior_projects]
    SET [fee_assessment_status_code] = CASE
        WHEN [selected_contractor_provider_id] IS NULL AND [current_quote_revision_id] IS NULL THEN 'PENDING_SELECTION'
        ELSE 'NOT_APPLICABLE'
    END
    WHERE [fee_assessment_status_code] = 'POLICY_PENDING';

    UPDATE [fee_policies]
    SET [policy_kind_code] = 'PROJECT',
        [transaction_type_code] = 'PROJECT',
        [applies_to_text] = N'인테리어 최종 시공업체 선택',
        [calculation_method_text] = N'SELECTED_QUOTE_TIER:FEE-Q1-FEE-Q7',
        [min_base_amount] = 0,
        [max_base_amount] = 999999999,
        [display_fee_amount] = 0,
        [currency_code] = 'KRW',
        [charge_timing_text] = N'고객이 최종 시공업체의 유효한 최신 견적을 선택할 때',
        [restore_rule_text] = N'법정 청약철회·업무 시작 전 고객 취소·허위/중복 요청·시스템 오류·관리자 승인 사유 시 FeeRestore',
        [note] = N'선택 견적금액에 따라 FEE-Q1~FEE-Q7 구간 적용; 현장실측·예비견적·미선택 공급자에는 차감하지 않음',
        [is_active] = 1,
        [updated_at] = SYSUTCDATETIME()
    WHERE [code] = 'FEE-I1';

    UPDATE category_policy
    SET category_policy.[policy_kind_code] = 'PROJECT',
        category_policy.[transaction_type_code] = 'PROJECT',
        category_policy.[calculation_method_text] = N'SELECTED_QUOTE_TIER:FEE-Q1-FEE-Q7',
        category_policy.[fee_amount] = NULL,
        category_policy.[min_base_amount] = 0,
        category_policy.[max_base_amount] = 999999999,
        category_policy.[currency_code] = 'KRW',
        category_policy.[charge_timing_text] = N'고객이 최종 시공업체의 유효한 최신 견적을 선택할 때',
        category_policy.[restore_rule_text] = N'법정 청약철회·업무 시작 전 고객 취소·허위/중복 요청·시스템 오류·관리자 승인 사유 시 FeeRestore',
        category_policy.[is_active] = 1,
        category_policy.[updated_at] = SYSUTCDATETIME()
    FROM [category_fee_policies] AS category_policy
    INNER JOIN [fee_policies] AS source_policy ON source_policy.[id] = category_policy.[source_fee_policy_id]
    WHERE source_policy.[code] = 'FEE-I1';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814063006_ImplementInteriorFinalSelectionFee'
)
BEGIN
    EXEC(N'ALTER TABLE [interior_projects] ADD CONSTRAINT [CK_interior_projects_fee] CHECK ([fee_assessment_status_code] IN (''POLICY_PENDING'',''PENDING_SELECTION'',''NOT_APPLICABLE'',''ASSESSED''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814063006_ImplementInteriorFinalSelectionFee'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260814063006_ImplementInteriorFinalSelectionFee', N'10.0.10');
END;

COMMIT;
GO

