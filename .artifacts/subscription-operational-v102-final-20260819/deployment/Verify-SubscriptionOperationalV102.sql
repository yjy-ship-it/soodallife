SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260819041000_ImproveSubscriptionBillingLifecycle') THROW 51210,'V100 migration missing.',1;
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260819054200_CompleteSubscriptionTerminationV101') THROW 51211,'V101 migration missing.',1;
IF COL_LENGTH('dbo.subscription_payment_methods','external_token_reference') IS NULL THROW 51212,'Billing key storage column is missing.',1;
IF COL_LENGTH('dbo.subscription_payment_requests','external_payment_reference') IS NULL THROW 51213,'Payment key storage column is missing.',1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.subscription_payment_requests') AND is_unique=1 AND name IS NOT NULL) THROW 51214,'Subscription payment idempotency index is missing.',1;

SELECT N'V102_OK' AS verification_result,
       (SELECT COUNT(*) FROM dbo.subscription_payment_methods WHERE status_code=N'ACTIVE' AND external_token_reference IS NULL) AS active_methods_without_billing_key,
       (SELECT COUNT(*) FROM dbo.subscription_payment_requests WHERE status_code=N'PROCESSING' AND updated_at<DATEADD(HOUR,-2,SYSUTCDATETIME())) AS stale_processing_payments,
       (SELECT COUNT(*) FROM dbo.subscription_payment_requests WHERE status_code=N'FAILED') AS failed_payments,
       (SELECT COUNT(*) FROM dbo.subscription_payouts WHERE status_code=N'APPROVED') AS approved_bank_transfers_waiting_confirmation,
       (SELECT COUNT(*) FROM dbo.subscription_refund_adjustments WHERE status_code IN(N'WAITING_CASES',N'MANUAL_REQUIRED',N'FAILED')) AS refunds_needing_attention;
GO
