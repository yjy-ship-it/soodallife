SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @MigrationId nvarchar(150) = N'20260818090000_AllowProviderApprovalResubmitAction';

IF EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = @MigrationId)
BEGIN
    SELECT @MigrationId AS MigrationId, N'ALREADY_APPLIED' AS Result;
    RETURN;
END;

IF OBJECT_ID(N'dbo.provider_approval_events', N'U') IS NULL
    THROW 51000, 'provider_approval_events table was not found.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.provider_approval_events')
          AND name = N'CK_provider_approval_events_action'
    )
        ALTER TABLE dbo.provider_approval_events DROP CONSTRAINT CK_provider_approval_events_action;

    ALTER TABLE dbo.provider_approval_events WITH CHECK
        ADD CONSTRAINT CK_provider_approval_events_action
        CHECK ([action_code] IN ('APPROVE','REJECT','SUSPEND','RESUME','RESUBMIT'));

    ALTER TABLE dbo.provider_approval_events
        CHECK CONSTRAINT CK_provider_approval_events_action;

    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (@MigrationId, N'10.0.10');

    COMMIT TRANSACTION;
    SELECT @MigrationId AS MigrationId, N'APPLIED' AS Result;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
