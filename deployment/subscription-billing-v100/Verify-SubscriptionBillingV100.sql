SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.subscription_contracts', N'U') IS NULL
    THROW 51210, N'정기구독 계약 테이블이 없습니다.', 1;

DECLARE @definition nvarchar(max) = (
    SELECT definition FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.subscription_contracts') AND name = N'CK_subscription_contracts_status'
);
IF @definition IS NULL OR @definition NOT LIKE N'%PAYMENT_PENDING%'
    THROW 51211, N'첫 결제 대기 상태 DB 제약이 적용되지 않았습니다.', 1;

IF EXISTS (SELECT 1 FROM dbo.subscription_contracts WHERE billing_status_code IS NULL)
    THROW 51212, N'기존 정기구독 계약의 결제상태 보정이 완료되지 않았습니다.', 1;

IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20260819041000_ImproveSubscriptionBillingLifecycle')
    THROW 51213, N'정기구독 개선 마이그레이션 이력이 없습니다.', 1;

SELECT status_code, billing_status_code, next_billing_at, COUNT(*) AS contract_count
FROM dbo.subscription_contracts
GROUP BY status_code, billing_status_code, next_billing_at
ORDER BY status_code, billing_status_code, next_billing_at;
