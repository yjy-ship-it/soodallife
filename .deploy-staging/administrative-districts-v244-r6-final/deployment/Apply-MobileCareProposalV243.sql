SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.provider_profiles', N'U') IS NULL OR OBJECT_ID(N'dbo.users', N'U') IS NULL
    THROW 52400, 'V240 requires provider_profiles and users tables.', 1;

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.provider_quote_templates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.provider_quote_templates (
        id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_provider_quote_templates PRIMARY KEY,
        public_id uniqueidentifier NOT NULL,
        provider_profile_id bigint NOT NULL,
        name nvarchar(100) NOT NULL,
        summary nvarchar(1000) NOT NULL,
        terms nvarchar(max) NULL,
        estimated_duration_text nvarchar(200) NULL,
        vat_mode varchar(10) NOT NULL CONSTRAINT DF_provider_quote_templates_vat_mode DEFAULT 'EXCLUDED',
        items_json nvarchar(max) NOT NULL,
        created_at datetime2(7) NOT NULL CONSTRAINT DF_provider_quote_templates_created_at DEFAULT SYSUTCDATETIME(),
        created_by_user_id bigint NULL,
        updated_at datetime2(7) NOT NULL CONSTRAINT DF_provider_quote_templates_updated_at DEFAULT SYSUTCDATETIME(),
        updated_by_user_id bigint NULL,
        row_version rowversion NOT NULL,
        CONSTRAINT CK_provider_quote_templates_vat_mode CHECK (vat_mode IN ('INCLUDED','EXCLUDED')),
        CONSTRAINT FK_provider_quote_templates_provider_profiles_provider_profile_id FOREIGN KEY (provider_profile_id) REFERENCES dbo.provider_profiles(id),
        CONSTRAINT FK_provider_quote_templates_users_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES dbo.users(id),
        CONSTRAINT FK_provider_quote_templates_users_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES dbo.users(id)
    );
    CREATE UNIQUE INDEX IX_provider_quote_templates_public_id ON dbo.provider_quote_templates(public_id);
    CREATE INDEX IX_provider_quote_templates_provider_profile_id ON dbo.provider_quote_templates(provider_profile_id);
    CREATE UNIQUE INDEX IX_provider_quote_templates_provider_profile_id_name ON dbo.provider_quote_templates(provider_profile_id, name);
    CREATE INDEX IX_provider_quote_templates_created_by_user_id ON dbo.provider_quote_templates(created_by_user_id);
    CREATE INDEX IX_provider_quote_templates_updated_by_user_id ON dbo.provider_quote_templates(updated_by_user_id);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260909134137_AddProviderQuoteTemplatesV240')
    INSERT dbo.__EFMigrationsHistory(MigrationId, ProductVersion) VALUES(N'20260909134137_AddProviderQuoteTemplatesV240', N'10.0.10');

IF OBJECT_ID(N'dbo.dispatch_candidates', N'U') IS NULL OR OBJECT_ID(N'dbo.request_dispatches', N'U') IS NULL
    THROW 52401, 'V241 requires dispatch_candidates and request_dispatches tables.', 1;

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.dispatch_candidates') AND name=N'CK_dispatch_candidates_status')
    ALTER TABLE dbo.dispatch_candidates DROP CONSTRAINT CK_dispatch_candidates_status;
ALTER TABLE dbo.dispatch_candidates WITH CHECK ADD CONSTRAINT CK_dispatch_candidates_status CHECK ([status_code] IN ('ELIGIBLE','INELIGIBLE','DISPATCHED','DECLINED','EXPIRED'));

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.request_dispatches') AND name=N'CK_request_dispatches_status')
    ALTER TABLE dbo.request_dispatches DROP CONSTRAINT CK_request_dispatches_status;
ALTER TABLE dbo.request_dispatches WITH CHECK ADD CONSTRAINT CK_request_dispatches_status CHECK ([status_code] IN ('AVAILABLE','VIEWED','RESPONDED','DECLINED','EXPIRED'));

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260910103000_AllowDeclinedDispatchV241')
    INSERT dbo.__EFMigrationsHistory(MigrationId, ProductVersion) VALUES(N'20260910103000_AllowDeclinedDispatchV241', N'10.0.10');

COMMIT TRANSACTION;
SELECT 'V241_APPLY_OK' AS result;
