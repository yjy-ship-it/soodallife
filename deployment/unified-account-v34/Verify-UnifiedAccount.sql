SET NOCOUNT ON;

SELECT MigrationId, ProductVersion
FROM dbo.__EFMigrationsHistory
WHERE MigrationId = N'20260816123000_UnifyCustomerProviderAccounts';

SELECT role_definition.code AS RoleCode, COUNT(DISTINCT user_role.user_id) AS ActiveUserCount
FROM dbo.roles role_definition
LEFT JOIN dbo.user_roles user_role
    ON user_role.role_id = role_definition.id
   AND user_role.revoked_at IS NULL
WHERE role_definition.code IN ('CUSTOMER', 'PROVIDER')
GROUP BY role_definition.code
ORDER BY role_definition.code;

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
