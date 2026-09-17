SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.provider_proposal_campaigns',N'U') IS NULL THROW 51000,'provider proposal campaigns table is missing',1;
IF OBJECT_ID(N'dbo.provider_proposal_areas',N'U') IS NULL THROW 51000,'provider proposal areas table is missing',1;
IF OBJECT_ID(N'dbo.provider_proposal_applications',N'U') IS NULL THROW 51000,'provider proposal applications table is missing',1;
IF OBJECT_ID(N'dbo.customer_proposal_category_interests',N'U') IS NULL THROW 51000,'customer proposal category interests table is missing',1;
IF OBJECT_ID(N'dbo.customer_proposal_area_interests',N'U') IS NULL THROW 51000,'customer proposal area interests table is missing',1;
IF OBJECT_ID(N'dbo.customer_proposal_signals',N'U') IS NULL THROW 51000,'customer proposal signals table is missing',1;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_provider_proposal_scope') THROW 51000,'proposal scope check is missing',1;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_provider_proposal_fee_status') THROW 51000,'proposal fee status check is missing',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.provider_proposal_areas') AND is_unique=1 AND name=N'IX_provider_proposal_areas_campaign_id_administrative_area_id') THROW 51000,'proposal area unique index is missing',1;
IF OBJECT_ID(N'dbo.__EFMigrationsHistory',N'U') IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260830020412_AddProviderProposalMarketplaceV159') THROW 51000,'V159 migration history is missing',1;
SELECT 'V159_OK' verification_result,
       (SELECT COUNT_BIG(*) FROM dbo.provider_proposal_campaigns) campaign_count,
       (SELECT COUNT_BIG(*) FROM dbo.provider_proposal_applications) application_count,
       (SELECT COUNT_BIG(*) FROM dbo.customer_proposal_area_interests WHERE is_active=1) active_area_interest_count,
       (SELECT COUNT_BIG(*) FROM dbo.customer_proposal_category_interests WHERE is_active=1) active_category_interest_count,
       (SELECT COUNT_BIG(*) FROM dbo.customer_proposal_signals WHERE expires_at>SYSUTCDATETIME()) active_recent_signal_count;
