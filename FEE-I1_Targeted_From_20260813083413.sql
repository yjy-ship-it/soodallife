BEGIN TRANSACTION;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260813083413_ImplementCustomerProviderUserBlock'
)
    THROW 51000, 'Expected source migration 20260813083413 is missing.', 1;

IF EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260814063006_ImplementInteriorFinalSelectionFee'
)
BEGIN
    ROLLBACK TRANSACTION;
    PRINT 'FEE-I1 migration is already applied.';
    RETURN;
END;

IF NOT EXISTS (SELECT 1 FROM [fee_policies] WHERE [code] = 'FEE-I1')
    THROW 51001, 'FEE-I1 source policy is missing.', 1;

IF NOT EXISTS (
    SELECT 1
    FROM [category_fee_policies] AS category_policy
    INNER JOIN [fee_policies] AS source_policy
        ON source_policy.[id] = category_policy.[source_fee_policy_id]
    WHERE source_policy.[code] = 'FEE-I1'
)
    THROW 51002, 'Normalized FEE-I1 category policies are missing.', 1;

ALTER TABLE [interior_projects] DROP CONSTRAINT [CK_interior_projects_fee];

DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[interior_projects]') AND [c].[name] = N'fee_assessment_status_code');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [interior_projects] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [interior_projects] ADD DEFAULT 'PENDING_SELECTION' FOR [fee_assessment_status_code];

UPDATE [interior_projects]
SET [fee_assessment_status_code] = CASE
    WHEN [selected_contractor_provider_id] IS NULL AND [current_quote_revision_id] IS NULL THEN 'PENDING_SELECTION'
    ELSE 'NOT_APPLICABLE'
END
WHERE [fee_assessment_status_code] = 'POLICY_PENDING';

UPDATE [fee_policies]
SET [policy_kind_code] = 'PROJECT',
    [transaction_type_code] = 'PROJECT',
    [applies_to_text] = N'인테리어 최종 시공업체 선택',
    [calculation_method_text] = N'SELECTED_QUOTE_TIER:FEE-Q1-FEE-Q7',
    [min_base_amount] = 0,
    [max_base_amount] = 999999999,
    [display_fee_amount] = 0,
    [currency_code] = 'KRW',
    [charge_timing_text] = N'고객이 최종 시공업체의 유효한 최신 견적을 선택할 때',
    [restore_rule_text] = N'법정 청약철회·업무 시작 전 고객 취소·허위/중복 요청·시스템 오류·관리자 승인 사유 시 FeeRestore',
    [note] = N'선택 견적금액에 따라 FEE-Q1~FEE-Q7 구간 적용; 현장실측·예비견적·미선택 공급자에는 차감하지 않음',
    [is_active] = 1,
    [updated_at] = SYSUTCDATETIME()
WHERE [code] = 'FEE-I1';

UPDATE category_policy
SET category_policy.[policy_kind_code] = 'PROJECT',
    category_policy.[transaction_type_code] = 'PROJECT',
    category_policy.[calculation_method_text] = N'SELECTED_QUOTE_TIER:FEE-Q1-FEE-Q7',
    category_policy.[fee_amount] = NULL,
    category_policy.[min_base_amount] = 0,
    category_policy.[max_base_amount] = 999999999,
    category_policy.[currency_code] = 'KRW',
    category_policy.[charge_timing_text] = N'고객이 최종 시공업체의 유효한 최신 견적을 선택할 때',
    category_policy.[restore_rule_text] = N'법정 청약철회·업무 시작 전 고객 취소·허위/중복 요청·시스템 오류·관리자 승인 사유 시 FeeRestore',
    category_policy.[is_active] = 1,
    category_policy.[updated_at] = SYSUTCDATETIME()
FROM [category_fee_policies] AS category_policy
INNER JOIN [fee_policies] AS source_policy ON source_policy.[id] = category_policy.[source_fee_policy_id]
WHERE source_policy.[code] = 'FEE-I1';

ALTER TABLE [interior_projects] ADD CONSTRAINT [CK_interior_projects_fee] CHECK ([fee_assessment_status_code] IN ('POLICY_PENDING','PENDING_SELECTION','NOT_APPLICABLE','ASSESSED'));

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260814063006_ImplementInteriorFinalSelectionFee', N'10.0.10');

COMMIT;
GO

