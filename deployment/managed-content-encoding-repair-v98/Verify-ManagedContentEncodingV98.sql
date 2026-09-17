SET NOCOUNT ON;

DECLARE @Expected TABLE (ContentPublicId uniqueidentifier NOT NULL, ContentTypeCode varchar(30) NOT NULL, Title nvarchar(300) NOT NULL);
INSERT INTO @Expected (ContentPublicId,ContentTypeCode,Title) VALUES
('d9700001-0000-4000-8000-000000000001','SAFETY_GUIDE',N'방문 작업 전 안전 확인'),
('d9700002-0000-4000-8000-000000000002','SAFETY_GUIDE',N'전기·가스·수도 작업 안전수칙'),
('d9700003-0000-4000-8000-000000000003','SAFETY_GUIDE',N'사다리·고소 작업 안전수칙'),
('d9700004-0000-4000-8000-000000000004','SAFETY_GUIDE',N'작업 중 사진·개인정보 보호'),
('d9700005-0000-4000-8000-000000000005','SAFETY_GUIDE',N'작업 완료 후 안전 점검'),
('d9700011-0000-4000-8000-000000000011','CATEGORY_GUIDE',N'서비스 요청을 정확하게 작성하는 방법'),
('d9700012-0000-4000-8000-000000000012','CATEGORY_GUIDE',N'견적 비교와 공급자 선택 안내'),
('d9700013-0000-4000-8000-000000000013','CATEGORY_GUIDE',N'방문 작업 전 확인사항'),
('d9700014-0000-4000-8000-000000000014','CATEGORY_GUIDE',N'작업 완료 확인과 거래 종료'),
('d9700015-0000-4000-8000-000000000015','CATEGORY_GUIDE',N'A/S·분쟁 신청 안내'),
('d9700021-0000-4000-8000-000000000021','PRICE_REFERENCE',N'가격 참고자료 이용 안내'),
('d9700022-0000-4000-8000-000000000022','PRICE_REFERENCE',N'견적 금액에 포함되는 항목'),
('d9700023-0000-4000-8000-000000000023','PRICE_REFERENCE',N'추가 비용이 발생할 수 있는 경우'),
('d9700024-0000-4000-8000-000000000024','PRICE_REFERENCE',N'출장비·자재비 확인 방법'),
('d9700025-0000-4000-8000-000000000025','PRICE_REFERENCE',N'결제·취소·환불 금액 확인');

IF (SELECT COUNT(*)
    FROM @Expected e
    INNER JOIN dbo.managed_contents c ON c.public_id=e.ContentPublicId AND c.content_type_code=e.ContentTypeCode
    INNER JOIN dbo.managed_content_versions v ON v.content_id=c.id AND v.version_no=c.current_version_no AND v.title=e.Title
    WHERE c.status_code='ACTIVE' AND c.review_status_code='APPROVED' AND LEN(v.body_text)>20) <> 15
    THROW 51010, N'기본 운영 콘텐츠 한글 복구 검증에 실패했습니다.', 1;

SELECT c.content_type_code,c.status_code,c.review_status_code,COUNT(*) AS repaired_count
FROM @Expected e
INNER JOIN dbo.managed_contents c ON c.public_id=e.ContentPublicId AND c.content_type_code=e.ContentTypeCode
INNER JOIN dbo.managed_content_versions v ON v.content_id=c.id AND v.version_no=c.current_version_no AND v.title=e.Title
GROUP BY c.content_type_code,c.status_code,c.review_status_code
ORDER BY c.content_type_code;
GO
