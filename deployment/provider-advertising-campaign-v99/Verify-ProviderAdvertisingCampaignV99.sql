SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.provider_advertising_rate_policies',N'U') IS NULL THROW 51100,N'provider_advertising_rate_policies 테이블이 없습니다.',1;
IF OBJECT_ID(N'dbo.provider_advertising_applications',N'U') IS NULL THROW 51101,N'provider_advertising_applications 테이블이 없습니다.',1;
IF (SELECT COUNT(*) FROM dbo.provider_advertising_rate_policies r JOIN dbo.advertising_placements p ON p.id=r.placement_id WHERE r.is_active=1 AND r.effective_from<='2026-08-19' AND p.code IN('CUSTOMER_HOME','PROVIDER_HOME'))<6 THROW 51102,N'노출 위치·기간별 초기 정액 요금 6건이 없습니다.',1;
IF EXISTS(SELECT 1 FROM dbo.provider_advertising_rate_policies WHERE duration_days NOT IN(7,14,30) OR fixed_amount<=0) THROW 51103,N'광고 정액 요금 데이터가 유효하지 않습니다.',1;
SELECT p.code placement_code,r.duration_days,r.fixed_amount,r.currency_code,r.effective_from,r.effective_to,r.is_active
FROM dbo.provider_advertising_rate_policies r JOIN dbo.advertising_placements p ON p.id=r.placement_id
ORDER BY p.id,r.duration_days;
SELECT status_code,fee_status_code,COUNT(*) application_count FROM dbo.provider_advertising_applications GROUP BY status_code,fee_status_code ORDER BY status_code;
