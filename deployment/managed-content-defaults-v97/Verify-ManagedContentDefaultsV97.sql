SET NOCOUNT ON;
SELECT c.content_type_code,c.status_code,c.review_status_code,COUNT(*) AS registered_count
FROM dbo.managed_contents c INNER JOIN dbo.managed_content_versions v ON v.content_id=c.id AND v.version_no=c.current_version_no
WHERE (c.content_type_code='SAFETY_GUIDE' AND v.title IN (N'방문 작업 전 안전 확인',N'전기·가스·수도 작업 안전수칙',N'사다리·고소 작업 안전수칙',N'작업 중 사진·개인정보 보호',N'작업 완료 후 안전 점검'))
   OR (c.content_type_code='CATEGORY_GUIDE' AND v.title IN (N'서비스 요청을 정확하게 작성하는 방법',N'견적 비교와 공급자 선택 안내',N'방문 작업 전 확인사항',N'작업 완료 확인과 거래 종료',N'A/S·분쟁 신청 안내'))
   OR (c.content_type_code='PRICE_REFERENCE' AND v.title IN (N'가격 참고자료 이용 안내',N'견적 금액에 포함되는 항목',N'추가 비용이 발생할 수 있는 경우',N'출장비·자재비 확인 방법',N'결제·취소·환불 금액 확인'))
GROUP BY c.content_type_code,c.status_code,c.review_status_code ORDER BY c.content_type_code;
GO
