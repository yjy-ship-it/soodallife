SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.subscription_contracts', N'U') IS NULL
    THROW 51200, N'정기구독 계약 테이블이 없습니다.', 1;

DECLARE @definition nvarchar(max) = (
    SELECT definition FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.subscription_contracts') AND name = N'CK_subscription_contracts_status'
);

IF @definition IS NULL OR @definition NOT LIKE N'%PAYMENT_PENDING%'
BEGIN
    IF @definition IS NOT NULL
        ALTER TABLE dbo.subscription_contracts DROP CONSTRAINT CK_subscription_contracts_status;
    ALTER TABLE dbo.subscription_contracts WITH CHECK ADD CONSTRAINT CK_subscription_contracts_status
        CHECK (status_code IN ('PAYMENT_PENDING','ACTIVE','PAUSED','TERMINATION_REQUESTED','TERMINATED'));
END;

UPDATE dbo.subscription_contracts
SET billing_status_code = 'LEGACY_REVIEW_REQUIRED', updated_at = SYSUTCDATETIME()
WHERE billing_status_code IS NULL;

IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20260819041000_ImproveSubscriptionBillingLifecycle')
BEGIN
    INSERT dbo.__EFMigrationsHistory(MigrationId, ProductVersion)
    VALUES(N'20260819041000_ImproveSubscriptionBillingLifecycle', N'10.0.10');
END;

COMMIT TRANSACTION;

SELECT status_code, billing_status_code, COUNT(*) AS contract_count
FROM dbo.subscription_contracts
GROUP BY status_code, billing_status_code
ORDER BY status_code, billing_status_code;
