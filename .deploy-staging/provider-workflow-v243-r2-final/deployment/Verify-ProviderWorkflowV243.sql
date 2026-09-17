SET NOCOUNT ON;

IF NOT EXISTS (
    SELECT 1 FROM dbo.category_policies p
    INNER JOIN dbo.service_categories s ON s.id=p.category_id
    WHERE p.is_emergency_allowed=1 AND s.status_code='ACTIVE'
      AND (s.name LIKE N'%에어컨%' OR s.name LIKE N'%냉난방%')
) THROW 52421, 'V242 verification failed: emergency HVAC category is unavailable.', 1;

IF NOT EXISTS (
    SELECT 1 FROM dbo.administrative_areas child
    INNER JOIN dbo.administrative_areas parent ON parent.id=child.parent_area_id
    WHERE parent.area_name IN (N'경기도',N'경기') AND child.area_name=N'수원시 장안구'
      AND parent.is_active=1 AND child.is_active=1
) THROW 52422, 'V242 verification failed: Gyeonggi/Suwon Jangangu is unavailable.', 1;

SELECT N'V242_OK' AS verification,
       (SELECT COUNT(*) FROM dbo.category_policies WHERE is_emergency_allowed=1) AS emergency_policy_count,
       (SELECT COUNT(*) FROM dbo.administrative_areas WHERE area_name LIKE N'수원시 %구' AND is_active=1) AS suwon_district_count;
