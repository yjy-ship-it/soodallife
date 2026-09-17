SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @now datetime2(7)=SYSUTCDATETIME();
DECLARE @defaults TABLE
(
    code varchar(50) NOT NULL PRIMARY KEY,
    name nvarchar(100) NOT NULL,
    description nvarchar(1000) NOT NULL,
    display_order int NOT NULL
);

INSERT INTO @defaults(code,name,description,display_order) VALUES
('TRANSACTION_BREACH',N'계약·거래 불이행',N'약속한 작업, 계약조건 또는 거래 의무를 이행하지 않은 경우',10),
('QUALITY_DEFECT',N'작업 품질·하자',N'작업 결과의 품질 문제, 하자 또는 재작업이 필요한 경우',20),
('PAYMENT_CHARGE_ISSUE',N'부당요금·결제 문제',N'견적과 다른 비용 청구, 부당한 추가요금 또는 결제 관련 문제가 있는 경우',30),
('NO_SHOW_COMMUNICATION',N'예약 불이행·연락두절',N'예약시간 미준수, 무단 불참 또는 정당한 사유 없는 연락두절이 발생한 경우',40),
('SAFETY_PROPERTY_DAMAGE',N'안전위험·재산피해',N'작업 중 안전위험, 시설 훼손 또는 재산피해가 발생한 경우',50),
('HARASSMENT_ABUSE',N'폭언·괴롭힘',N'폭언, 위협, 성희롱, 차별 또는 반복적인 괴롭힘이 발생한 경우',60),
('FRAUD_MISREPRESENTATION',N'사기·허위정보',N'신원, 자격, 서비스 내용, 거래 조건을 속이거나 허위로 표시한 경우',70),
('PRIVACY_VIOLATION',N'개인정보 침해',N'연락처, 주소, 사진 등 개인정보를 동의 없이 이용하거나 공개한 경우',80),
('INAPPROPRIATE_CONTENT',N'부적절한 리뷰·콘텐츠',N'욕설, 광고, 허위 리뷰, 불법 또는 서비스 목적과 무관한 콘텐츠인 경우',90),
('OTHER',N'기타 신고',N'위 유형에 해당하지 않지만 운영 검토가 필요한 경우',100);

INSERT INTO dbo.report_types
(
    public_id,code,name,description,is_active,display_order,effective_from,
    created_at,created_by_user_id,updated_at,updated_by_user_id
)
SELECT NEWID(),d.code,d.name,d.description,1,d.display_order,@now,@now,NULL,@now,NULL
FROM @defaults d
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.report_types t WITH (UPDLOCK,HOLDLOCK) WHERE t.code=d.code
);

DECLARE @inserted int=@@ROWCOUNT;
COMMIT TRANSACTION;

SELECT @inserted AS inserted_count,
       (SELECT COUNT(*) FROM dbo.report_types WHERE code IN (SELECT code FROM @defaults)) AS default_type_count;
