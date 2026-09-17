BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819054200_CompleteSubscriptionTerminationV101'
)
BEGIN
    ALTER TABLE [subscription_refund_adjustments] DROP CONSTRAINT [CK_subscription_refund_adjustments_status];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819054200_CompleteSubscriptionTerminationV101'
)
BEGIN
    ALTER TABLE [subscription_refund_adjustments] ADD [calculation_json] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819054200_CompleteSubscriptionTerminationV101'
)
BEGIN
    ALTER TABLE [subscription_refund_adjustments] ADD [external_refund_reference] varchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819054200_CompleteSubscriptionTerminationV101'
)
BEGIN
    ALTER TABLE [subscription_refund_adjustments] ADD [failure_code] varchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819054200_CompleteSubscriptionTerminationV101'
)
BEGIN
    ALTER TABLE [subscription_refund_adjustments] ADD [failure_reason] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819054200_CompleteSubscriptionTerminationV101'
)
BEGIN
    ALTER TABLE [subscription_refund_adjustments] ADD [provider_adjustment_amount] decimal(19,4) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819054200_CompleteSubscriptionTerminationV101'
)
BEGIN
    ALTER TABLE [subscription_contracts] ADD [gateway_terminated_at] datetime2(7) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819054200_CompleteSubscriptionTerminationV101'
)
BEGIN
    ALTER TABLE [subscription_contracts] ADD [gateway_termination_failure_reason] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819054200_CompleteSubscriptionTerminationV101'
)
BEGIN
    ALTER TABLE [subscription_contracts] ADD [gateway_termination_status_code] varchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819054200_CompleteSubscriptionTerminationV101'
)
BEGIN
    EXEC(N'ALTER TABLE [subscription_refund_adjustments] ADD CONSTRAINT [CK_subscription_refund_adjustments_status] CHECK ([status_code] IN (''REQUESTED'',''WAITING_CASES'',''MANUAL_REQUIRED'',''APPROVED'',''PROCESSING'',''COMPLETED'',''REJECTED'',''CANCELLED'',''FAILED''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819054200_CompleteSubscriptionTerminationV101'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260819054200_CompleteSubscriptionTerminationV101', N'10.0.10');
END;

COMMIT;
GO

