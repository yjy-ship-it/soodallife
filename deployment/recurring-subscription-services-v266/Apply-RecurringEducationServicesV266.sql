SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @targets TABLE (
    external_code varchar(30) NOT NULL PRIMARY KEY,
    service_name nvarchar(200) NOT NULL
);

INSERT INTO @targets (external_code, service_name) VALUES
('LIF-0088', N'국어'), ('LIF-0089', N'영어'), ('LIF-0090', N'수학'),
('LIF-0091', N'과학'), ('LIF-0092', N'사회'), ('LIF-0093', N'내신·수능지도'),
('LIF-0094', N'영어회화'), ('LIF-0095', N'중국어'), ('LIF-0096', N'日本語'),
('LIF-0097', N'기타 외국어'), ('LIF-0098', N'피아노'), ('LIF-0099', N'기타'),
('LIF-0100', N'바이올린'), ('LIF-0101', N'보컬'), ('LIF-0102', N'드럼'),
('LIF-0103', N'국악'), ('LIF-0104', N'드로잉'), ('LIF-0105', N'회화'),
('LIF-0106', N'공예'), ('LIF-0107', N'디자인'), ('LIF-0108', N'입시미술'),
('LIF-0109', N'컴퓨터 기초'), ('LIF-0110', N'문서작성'), ('LIF-0111', N'코딩'),
('LIF-0112', N'홈페이지 제작'), ('LIF-0113', N'앱 개발 교육'), ('LIF-0114', N'생성형 AI'),
('LIF-0115', N'ChatGPT 활용'), ('LIF-0116', N'업무자동화'), ('LIF-0117', N'AI 콘텐츠 제작'),
('LIF-0118', N'요리'), ('LIF-0119', N'제과제빵'), ('LIF-0120', N'사진'),
('LIF-0121', N'꽃꽂이'), ('LIF-0122', N'캘리그래피'), ('LIF-0123', N'헬스'),
('LIF-0124', N'요가'), ('LIF-0125', N'필라테스'), ('LIF-0126', N'골프'),
('LIF-0127', N'테니스'), ('LIF-0128', N'수영'), ('LIF-0129', N'자격증 교육'),
('LIF-0132', N'직무교육'), ('LIF-0133', N'스마트폰 사용'), ('LIF-0135', N'금융·보안 교육');

IF (SELECT COUNT(*) FROM @targets) <> 45
    THROW 52660, 'V266 target definition must contain exactly 45 services.', 1;

IF (
    SELECT COUNT(*)
    FROM @targets target
    JOIN service_categories service ON service.external_code = target.external_code
    JOIN service_categories middle ON middle.id = service.parent_id
    JOIN service_categories major ON major.id = middle.parent_id
    WHERE service.level_code = 'SERVICE'
      AND service.status_code = 'ACTIVE'
      AND service.name = target.service_name
      AND middle.name = N'교육·레슨'
      AND major.name = N'생활서비스'
) <> 45
    THROW 52661, 'V266 category validation failed. Expected 45 active 생활서비스 > 교육·레슨 services.', 1;

UPDATE operationPolicy
SET operationPolicy.subscription_option_text = N'허용',
    operationPolicy.updated_at = SYSUTCDATETIME(),
    operationPolicy.updated_by_user_id = NULL
FROM category_operation_policies operationPolicy
JOIN service_categories service ON service.id = operationPolicy.category_id
JOIN @targets target ON target.external_code = service.external_code
WHERE operationPolicy.is_active = 1
  AND operationPolicy.effective_from <= CAST(SYSUTCDATETIME() AS date)
  AND (operationPolicy.effective_to IS NULL OR operationPolicy.effective_to > CAST(SYSUTCDATETIME() AS date));

IF @@ROWCOUNT <> 45
    THROW 52662, 'V266 operation policy update failed. Expected 45 current rows.', 1;

UPDATE legacyPolicy
SET legacyPolicy.subscription_option_text = N'허용',
    legacyPolicy.updated_at = SYSUTCDATETIME(),
    legacyPolicy.updated_by_user_id = NULL
FROM category_policies legacyPolicy
JOIN service_categories service ON service.id = legacyPolicy.category_id
JOIN @targets target ON target.external_code = service.external_code
WHERE legacyPolicy.effective_from <= CAST(SYSUTCDATETIME() AS date)
  AND (legacyPolicy.effective_to IS NULL OR legacyPolicy.effective_to > CAST(SYSUTCDATETIME() AS date));

IF @@ROWCOUNT <> 45
    THROW 52663, 'V266 legacy policy update failed. Expected 45 current rows.', 1;

IF EXISTS (
    SELECT 1
    FROM @targets target
    JOIN service_categories service ON service.external_code = target.external_code
    WHERE NOT EXISTS (
        SELECT 1
        FROM category_operation_policies policy
        WHERE policy.category_id = service.id
          AND policy.is_active = 1
          AND policy.subscription_option_text = N'허용'
          AND policy.effective_from <= CAST(SYSUTCDATETIME() AS date)
          AND (policy.effective_to IS NULL OR policy.effective_to > CAST(SYSUTCDATETIME() AS date))
    )
)
    THROW 52664, 'V266 subscription policy verification failed.', 1;

COMMIT TRANSACTION;

SELECT service.external_code, service.name, operationPolicy.subscription_option_text
FROM @targets target
JOIN service_categories service ON service.external_code = target.external_code
JOIN category_operation_policies operationPolicy ON operationPolicy.category_id = service.id
WHERE operationPolicy.is_active = 1
  AND operationPolicy.effective_from <= CAST(SYSUTCDATETIME() AS date)
  AND (operationPolicy.effective_to IS NULL OR operationPolicy.effective_to > CAST(SYSUTCDATETIME() AS date))
ORDER BY service.external_code;
