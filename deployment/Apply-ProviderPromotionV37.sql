SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF COL_LENGTH('dbo.provider_profiles', 'public_introduction_html') IS NULL
    ALTER TABLE dbo.provider_profiles ADD public_introduction_html nvarchar(max) NULL;
IF COL_LENGTH('dbo.provider_profiles', 'public_phone') IS NULL
    ALTER TABLE dbo.provider_profiles ADD public_phone nvarchar(30) NULL;
IF COL_LENGTH('dbo.provider_profiles', 'public_email') IS NULL
    ALTER TABLE dbo.provider_profiles ADD public_email nvarchar(320) NULL;
IF COL_LENGTH('dbo.provider_profiles', 'public_address') IS NULL
    ALTER TABLE dbo.provider_profiles ADD public_address nvarchar(500) NULL;
IF COL_LENGTH('dbo.provider_profiles', 'public_blog_url') IS NULL
    ALTER TABLE dbo.provider_profiles ADD public_blog_url varchar(1000) NULL;
IF COL_LENGTH('dbo.provider_profiles', 'public_website_url') IS NULL
    ALTER TABLE dbo.provider_profiles ADD public_website_url varchar(1000) NULL;
IF COL_LENGTH('dbo.provider_profiles', 'public_logo_url') IS NULL
    ALTER TABLE dbo.provider_profiles ADD public_logo_url varchar(1000) NULL;
IF COL_LENGTH('dbo.provider_profiles', 'public_photo_urls_json') IS NULL
    ALTER TABLE dbo.provider_profiles ADD public_photo_urls_json nvarchar(max) NULL;

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20260816160000_AddProviderPublicPromotionProfile')
    INSERT dbo.__EFMigrationsHistory(MigrationId, ProductVersion)
    VALUES (N'20260816160000_AddProviderPublicPromotionProfile', N'10.0.10');

COMMIT TRANSACTION;

SELECT
    COL_LENGTH('dbo.provider_profiles', 'public_introduction_html') AS IntroductionColumn,
    COL_LENGTH('dbo.provider_profiles', 'public_logo_url') AS LogoColumn,
    COL_LENGTH('dbo.provider_profiles', 'public_photo_urls_json') AS PhotoColumn;
