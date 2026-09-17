SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260819122637_HardenSubscriptionPaymentV104') THROW 51400,'V104 migration missing.',1;
IF COL_LENGTH('dbo.subscription_contracts','payment_method_id') IS NULL THROW 51401,'Contract payment method column missing.',1;
IF COL_LENGTH('dbo.subscription_contracts','billing_anchor_day') IS NULL THROW 51402,'Billing anchor column missing.',1;
IF COL_LENGTH('dbo.subscription_payment_requests','gateway_attempt_no') IS NULL THROW 51403,'Gateway attempt column missing.',1;
IF EXISTS (SELECT 1 FROM dbo.subscription_contracts WHERE billing_anchor_day IS NOT NULL AND billing_anchor_day NOT BETWEEN 1 AND 31) THROW 51404,'Invalid billing anchor found.',1;
IF EXISTS (SELECT 1 FROM dbo.subscription_contracts c JOIN dbo.subscription_payment_methods m ON m.id=c.payment_method_id WHERE m.customer_profile_id<>c.customer_profile_id) THROW 51405,'Contract is linked to another customer payment method.',1;

SELECT N'V104_OK' AS verification_result,
       (SELECT COUNT(*) FROM dbo.subscription_contracts WHERE status_code IN(N'ACTIVE',N'PAUSED',N'PAYMENT_PENDING') AND payment_method_id IS NULL) AS active_contracts_without_pinned_method,
       (SELECT COUNT(*) FROM dbo.subscription_payment_requests WHERE status_code=N'PROCESSING' AND updated_at<DATEADD(MINUTE,-10,SYSUTCDATETIME())) AS payments_requiring_reconciliation,
       (SELECT COUNT(*) FROM dbo.subscription_payment_requests WHERE gateway_attempt_no<1) AS invalid_gateway_attempts,
       (SELECT COUNT(*) FROM dbo.subscription_payment_methods WHERE external_token_reference IS NOT NULL AND external_token_reference NOT LIKE 'dp:v1:%') AS legacy_unprotected_tokens;
GO
