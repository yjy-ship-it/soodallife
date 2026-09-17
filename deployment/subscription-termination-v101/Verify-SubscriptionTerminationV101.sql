SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260819054200_CompleteSubscriptionTerminationV101')
    THROW 51000, 'V101 migration history is missing.', 1;

IF COL_LENGTH('dbo.subscription_contracts','gateway_termination_status_code') IS NULL
 OR COL_LENGTH('dbo.subscription_contracts','gateway_terminated_at') IS NULL
 OR COL_LENGTH('dbo.subscription_contracts','gateway_termination_failure_reason') IS NULL
    THROW 51001, 'Subscription gateway termination columns are missing.', 1;

IF COL_LENGTH('dbo.subscription_refund_adjustments','calculation_json') IS NULL
 OR COL_LENGTH('dbo.subscription_refund_adjustments','provider_adjustment_amount') IS NULL
 OR COL_LENGTH('dbo.subscription_refund_adjustments','external_refund_reference') IS NULL
 OR COL_LENGTH('dbo.subscription_refund_adjustments','failure_code') IS NULL
 OR COL_LENGTH('dbo.subscription_refund_adjustments','failure_reason') IS NULL
    THROW 51002, 'Automatic refund columns are missing.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name]=N'CK_subscription_refund_adjustments_status' AND [definition] LIKE N'%WAITING_CASES%')
    THROW 51003, 'V101 refund status constraint is not active.', 1;

SELECT N'V101_OK' AS verification_result,
       (SELECT COUNT(*) FROM dbo.subscription_contracts WHERE status_code=N'TERMINATION_REQUESTED') AS pending_terminations,
       (SELECT COUNT(*) FROM dbo.subscription_refund_adjustments WHERE status_code IN (N'WAITING_CASES',N'MANUAL_REQUIRED',N'FAILED')) AS pending_refunds;
