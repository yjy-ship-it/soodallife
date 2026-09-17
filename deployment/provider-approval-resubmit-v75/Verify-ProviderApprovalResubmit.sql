SET NOCOUNT ON;

IF NOT EXISTS
(
    SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = N'20260818090000_AllowProviderApprovalResubmitAction'
)
    THROW 51001, 'Provider approval resubmit migration history is missing.', 1;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.provider_approval_events')
      AND name = N'CK_provider_approval_events_action'
      AND definition LIKE N'%RESUBMIT%'
      AND is_disabled = 0
      AND is_not_trusted = 0
)
    THROW 51002, 'Provider approval RESUBMIT constraint was not applied or trusted.', 1;

SELECT
    migration.MigrationId,
    constraint_info.name AS ConstraintName,
    constraint_info.definition AS ConstraintDefinition,
    constraint_info.is_disabled AS IsDisabled,
    constraint_info.is_not_trusted AS IsNotTrusted
FROM dbo.__EFMigrationsHistory migration
CROSS JOIN sys.check_constraints constraint_info
WHERE migration.MigrationId = N'20260818090000_AllowProviderApprovalResubmitAction'
  AND constraint_info.parent_object_id = OBJECT_ID(N'dbo.provider_approval_events')
  AND constraint_info.name = N'CK_provider_approval_events_action';
