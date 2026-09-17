SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260819122637_HardenSubscriptionPaymentV104')
    BEGIN
        IF COL_LENGTH('dbo.subscription_payment_requests','gateway_attempt_no') IS NULL
            ALTER TABLE dbo.subscription_payment_requests ADD gateway_attempt_no int NOT NULL CONSTRAINT DF_subscription_payment_requests_gateway_attempt_no DEFAULT(1);

        IF COL_LENGTH('dbo.subscription_payment_methods','external_token_reference') IS NOT NULL
            ALTER TABLE dbo.subscription_payment_methods ALTER COLUMN external_token_reference varchar(1000) NULL;

        IF COL_LENGTH('dbo.subscription_contracts','billing_anchor_day') IS NULL
            ALTER TABLE dbo.subscription_contracts ADD billing_anchor_day int NULL;

        IF COL_LENGTH('dbo.subscription_contracts','payment_method_id') IS NULL
            ALTER TABLE dbo.subscription_contracts ADD payment_method_id bigint NULL;

        EXEC sys.sp_executesql N'
            UPDATE contract
               SET payment_method_id = COALESCE(
                   (SELECT TOP (1) payment.payment_method_id
                      FROM dbo.subscription_payment_requests payment
                     WHERE payment.subscription_contract_id = contract.id
                       AND payment.payment_method_id IS NOT NULL
                     ORDER BY CASE WHEN payment.status_code = ''COMPLETED'' THEN 0 ELSE 1 END,
                              payment.requested_at DESC),
                   (SELECT TOP (1) method.id
                      FROM dbo.subscription_payment_methods method
                     WHERE method.customer_profile_id = contract.customer_profile_id
                       AND method.status_code = ''ACTIVE''
                     ORDER BY method.is_default DESC, method.registered_at DESC)),
                   billing_anchor_day = DAY(COALESCE(contract.started_at, contract.created_at))
              FROM dbo.subscription_contracts contract
             WHERE contract.payment_method_id IS NULL OR contract.billing_anchor_day IS NULL;';

        IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.subscription_contracts') AND name=N'CK_subscription_contracts_billing_anchor_day')
            EXEC(N'ALTER TABLE dbo.subscription_contracts ADD CONSTRAINT CK_subscription_contracts_billing_anchor_day CHECK (billing_anchor_day IS NULL OR billing_anchor_day BETWEEN 1 AND 31);');

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.subscription_contracts') AND name=N'IX_subscription_contracts_payment_method_id')
            EXEC(N'CREATE INDEX IX_subscription_contracts_payment_method_id ON dbo.subscription_contracts(payment_method_id);');

        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.subscription_contracts') AND name=N'FK_subscription_contracts_subscription_payment_methods_payment_method_id')
            EXEC(N'ALTER TABLE dbo.subscription_contracts ADD CONSTRAINT FK_subscription_contracts_subscription_payment_methods_payment_method_id FOREIGN KEY(payment_method_id) REFERENCES dbo.subscription_payment_methods(id);');

        INSERT dbo.__EFMigrationsHistory(MigrationId,ProductVersion)
        VALUES(N'20260819122637_HardenSubscriptionPaymentV104',N'10.0.10');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
