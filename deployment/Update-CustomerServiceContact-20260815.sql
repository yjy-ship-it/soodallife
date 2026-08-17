SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = N'20260815153000_UpdateCustomerServiceContact'
)
BEGIN
    UPDATE dbo.managed_content_versions
    SET answer_text = N'개인정보 보호책임자 또는 개인정보 문의 이메일 info@dh9.kr로 문의할 수 있습니다. 일반 서비스 이용 문의는 전국 대표번호 1670-5073 또는 soodallife@dh9.kr로 접수해 주세요.'
    WHERE public_id = 'fa9b0026-0000-4000-8000-000000000026';

    UPDATE dbo.managed_content_versions
    SET answer_text = N'전국 대표번호 1670-5073 또는 이메일 soodallife@dh9.kr로 문의해 주세요. 문의할 때 요청번호나 거래번호와 함께 문제 상황을 알려주면 확인에 도움이 됩니다. 비밀번호, 주민등록번호와 금융 비밀번호는 보내지 마세요.'
    WHERE public_id = 'fa9b0028-0000-4000-8000-000000000028';

    IF @@ROWCOUNT <> 1
        THROW 51000, 'Customer service FAQ row was not updated.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.managed_content_versions
        WHERE public_id = 'fa9b0026-0000-4000-8000-000000000026'
          AND answer_text LIKE N'%soodallife@dh9.kr%'
    )
        THROW 51001, 'Privacy FAQ contact was not updated.', 1;

    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20260815153000_UpdateCustomerServiceContact', N'10.0.10');
END;

COMMIT;
GO
