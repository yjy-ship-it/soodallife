SET NOCOUNT ON;

SELECT COUNT(*) AS MigrationCount
FROM dbo.__EFMigrationsHistory
WHERE MigrationId = N'20260816160000_AddProviderPublicPromotionProfile';

SELECT
    CASE WHEN COL_LENGTH('dbo.provider_profiles', 'public_introduction_html') IS NOT NULL THEN 1 ELSE 0 END AS IntroductionColumn,
    CASE WHEN COL_LENGTH('dbo.provider_profiles', 'public_phone') IS NOT NULL THEN 1 ELSE 0 END AS PhoneColumn,
    CASE WHEN COL_LENGTH('dbo.provider_profiles', 'public_email') IS NOT NULL THEN 1 ELSE 0 END AS EmailColumn,
    CASE WHEN COL_LENGTH('dbo.provider_profiles', 'public_address') IS NOT NULL THEN 1 ELSE 0 END AS AddressColumn,
    CASE WHEN COL_LENGTH('dbo.provider_profiles', 'public_blog_url') IS NOT NULL THEN 1 ELSE 0 END AS BlogColumn,
    CASE WHEN COL_LENGTH('dbo.provider_profiles', 'public_website_url') IS NOT NULL THEN 1 ELSE 0 END AS WebsiteColumn,
    CASE WHEN COL_LENGTH('dbo.provider_profiles', 'public_logo_url') IS NOT NULL THEN 1 ELSE 0 END AS LogoColumn,
    CASE WHEN COL_LENGTH('dbo.provider_profiles', 'public_photo_urls_json') IS NOT NULL THEN 1 ELSE 0 END AS PhotosColumn;
