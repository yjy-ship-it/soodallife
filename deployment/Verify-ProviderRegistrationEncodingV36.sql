SET NOCOUNT ON;

SELECT MigrationId, ProductVersion
FROM dbo.__EFMigrationsHistory
WHERE MigrationId = N'20260816150000_RepairProviderRegistrationLegalDocumentEncoding';

SELECT d.code, v.title, d.requirement_code, v.is_placeholder
FROM dbo.legal_documents d
INNER JOIN dbo.legal_document_versions v ON v.legal_document_id = d.id AND v.is_active = 1
WHERE d.code IN ('PROVIDER_TERMS','PROVIDER_PRIVACY_CONSENT','PROVIDER_MARKETING_CONSENT')
ORDER BY d.display_order;

SELECT COUNT(*) AS CorrectKoreanTitleCount
FROM dbo.legal_documents d
INNER JOIN dbo.legal_document_versions v ON v.legal_document_id = d.id AND v.is_active = 1
WHERE (d.code = 'PROVIDER_TERMS' AND v.title = N'수달 라이프 공급자 이용약관')
   OR (d.code = 'PROVIDER_PRIVACY_CONSENT' AND v.title = N'공급자 개인정보 수집·이용 동의')
   OR (d.code = 'PROVIDER_MARKETING_CONSENT' AND v.title = N'공급자 마케팅 정보 수신 동의');
