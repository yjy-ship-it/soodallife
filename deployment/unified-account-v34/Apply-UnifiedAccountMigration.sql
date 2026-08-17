SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;

DECLARE @MigrationId nvarchar(150) = N'20260816123000_UnifyCustomerProviderAccounts';

IF EXISTS
(
    SELECT 1
    FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = @MigrationId
)
BEGIN
    SELECT @MigrationId AS MigrationId, N'ALREADY_APPLIED' AS Result;
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @CustomerRoleId bigint =
    (
        SELECT TOP (1) id
        FROM dbo.roles
        WHERE code = 'CUSTOMER' AND is_active = 1
    );

    IF @CustomerRoleId IS NULL
        THROW 51000, 'Active CUSTOMER role is required before account unification.', 1;

    INSERT INTO dbo.user_roles (user_id, role_id, granted_at)
    SELECT DISTINCT provider_role.user_id, @CustomerRoleId, SYSUTCDATETIME()
    FROM dbo.user_roles provider_role
    INNER JOIN dbo.roles provider_role_definition
        ON provider_role_definition.id = provider_role.role_id
       AND provider_role_definition.code = 'PROVIDER'
    INNER JOIN dbo.users account
        ON account.id = provider_role.user_id
       AND account.status_code = 'ACTIVE'
    WHERE provider_role.revoked_at IS NULL
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.user_roles customer_role
          WHERE customer_role.user_id = provider_role.user_id
            AND customer_role.role_id = @CustomerRoleId
            AND customer_role.revoked_at IS NULL
      );

    DECLARE @AddedCustomerRoles int = @@ROWCOUNT;

    INSERT INTO dbo.customer_profiles
        (public_id, user_id, display_name, created_at, updated_at)
    SELECT
        NEWID(),
        provider.user_id,
        LEFT(COALESCE(NULLIF(provider.representative_name, N''),
                      NULLIF(provider.contact_name, N''),
                      NULLIF(provider.business_name, N''),
                      account.login_id), 100),
        SYSUTCDATETIME(),
        SYSUTCDATETIME()
    FROM dbo.provider_profiles provider
    INNER JOIN dbo.users account
        ON account.id = provider.user_id
       AND account.status_code = 'ACTIVE'
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.customer_profiles customer
        WHERE customer.user_id = provider.user_id
    );

    DECLARE @AddedCustomerProfiles int = @@ROWCOUNT;

    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (@MigrationId, N'10.0.10');

    COMMIT TRANSACTION;

    SELECT
        @MigrationId AS MigrationId,
        N'APPLIED' AS Result,
        @AddedCustomerRoles AS AddedCustomerRoles,
        @AddedCustomerProfiles AS AddedCustomerProfiles;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT
    SUM(CASE WHEN customer_role.id IS NULL THEN 1 ELSE 0 END) AS ActiveProviderWithoutCustomerRoleCount,
    SUM(CASE WHEN customer_profile.id IS NULL THEN 1 ELSE 0 END) AS ProviderWithoutCustomerProfileCount
FROM dbo.user_roles provider_role
INNER JOIN dbo.roles provider_definition
    ON provider_definition.id = provider_role.role_id
   AND provider_definition.code = 'PROVIDER'
INNER JOIN dbo.users account
    ON account.id = provider_role.user_id
   AND account.status_code = 'ACTIVE'
LEFT JOIN dbo.roles customer_definition
    ON customer_definition.code = 'CUSTOMER'
   AND customer_definition.is_active = 1
LEFT JOIN dbo.user_roles customer_role
    ON customer_role.user_id = provider_role.user_id
   AND customer_role.role_id = customer_definition.id
   AND customer_role.revoked_at IS NULL
LEFT JOIN dbo.customer_profiles customer_profile
    ON customer_profile.user_id = provider_role.user_id
WHERE provider_role.revoked_at IS NULL;
