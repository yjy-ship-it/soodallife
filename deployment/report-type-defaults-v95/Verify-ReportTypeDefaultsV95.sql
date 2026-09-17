SET NOCOUNT ON;

DECLARE @defaults TABLE(code varchar(50) NOT NULL PRIMARY KEY,name nvarchar(100) NOT NULL);
INSERT INTO @defaults(code,name) VALUES
('TRANSACTION_BREACH',N'계약·거래 불이행'),
('QUALITY_DEFECT',N'작업 품질·하자'),
('PAYMENT_CHARGE_ISSUE',N'부당요금·결제 문제'),
('NO_SHOW_COMMUNICATION',N'예약 불이행·연락두절'),
('SAFETY_PROPERTY_DAMAGE',N'안전위험·재산피해'),
('HARASSMENT_ABUSE',N'폭언·괴롭힘'),
('FRAUD_MISREPRESENTATION',N'사기·허위정보'),
('PRIVACY_VIOLATION',N'개인정보 침해'),
('INAPPROPRIATE_CONTENT',N'부적절한 리뷰·콘텐츠'),
('OTHER',N'기타 신고');

IF EXISTS
(
    SELECT 1 FROM @defaults d
    LEFT JOIN dbo.report_types t ON t.code=d.code
    WHERE t.id IS NULL OR t.is_active<>1 OR t.name<>d.name
)
    THROW 51000,N'기본 신고유형 10개의 코드·명칭·사용상태를 확인해 주세요.',1;

SELECT t.code,t.name,t.is_active,t.display_order
FROM dbo.report_types t
JOIN @defaults d ON d.code=t.code
ORDER BY t.display_order,t.code;
