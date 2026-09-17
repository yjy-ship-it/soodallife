SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260819041000_ImproveSubscriptionBillingLifecycle')
    THROW 51200, 'V100 subscription billing migration is required before V102.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260819054200_CompleteSubscriptionTerminationV101')
    THROW 51201, 'V101 subscription termination migration is required before V102.', 1;
IF OBJECT_ID(N'dbo.subscription_payment_methods',N'U') IS NULL OR OBJECT_ID(N'dbo.subscription_payment_requests',N'U') IS NULL OR OBJECT_ID(N'dbo.subscription_payment_ledger',N'U') IS NULL
    THROW 51202, 'Subscription payment tables are missing.', 1;
IF OBJECT_ID(N'dbo.subscription_payouts',N'U') IS NULL OR OBJECT_ID(N'dbo.subscription_payout_events',N'U') IS NULL
    THROW 51203, 'Subscription payout tables are missing.', 1;

SELECT N'V102_PREFLIGHT_OK' AS apply_result;
GO
