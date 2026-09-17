SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    /* 긴급출동: 실제 현장 긴급 대응이 가능한 활성 수리·설치 서비스의 정책을 보정합니다. */
    UPDATE policy
       SET policy.is_emergency_allowed = 1,
           policy.updated_at = SYSUTCDATETIME(),
           policy.admin_note = CONCAT(COALESCE(policy.admin_note, N''), N' [V242 긴급출동 카테고리 보정]')
      FROM dbo.category_policies AS policy
      INNER JOIN dbo.service_categories AS service ON service.id = policy.category_id
      INNER JOIN dbo.service_categories AS middle_category ON middle_category.id = service.parent_id
     WHERE service.level_code = 'SERVICE'
       AND service.status_code = 'ACTIVE'
       AND policy.transaction_type_code IN ('ONE_TIME','PROJECT')
       AND policy.effective_from <= CAST(SYSUTCDATETIME() AS date)
       AND (policy.effective_to IS NULL OR policy.effective_to > CAST(SYSUTCDATETIME() AS date))
       AND policy.is_emergency_allowed = 0
       AND (
            service.name LIKE N'%에어컨%' OR service.name LIKE N'%냉난방%' OR service.name LIKE N'%보일러%'
         OR service.name LIKE N'%누수%' OR service.name LIKE N'%배관%' OR service.name LIKE N'%수도%'
         OR service.name LIKE N'%변기%' OR service.name LIKE N'%세면대%' OR service.name LIKE N'%수전%'
         OR service.name LIKE N'%전기%' OR service.name LIKE N'%누전%' OR service.name LIKE N'%차단기%'
         OR service.name LIKE N'%잠금%' OR service.name LIKE N'%도어락%' OR service.name LIKE N'%유리%'
         OR middle_category.name IN (N'냉난방기',N'수도·배관',N'전기·조명')
       );

    /* 경기도 수원시는 최종 서비스 지역을 구 단위로 직접 선택하도록 평탄화합니다. */
    DECLARE @GyeonggiId bigint = (
        SELECT TOP (1) id FROM dbo.administrative_areas
         WHERE area_level_code='SIDO' AND is_active=1 AND area_name IN (N'경기도',N'경기')
         ORDER BY CASE WHEN area_name=N'경기도' THEN 0 ELSE 1 END, id
    );
    IF @GyeonggiId IS NOT NULL
    BEGIN
        DECLARE @SuwonDistricts table(area_code varchar(20), area_name nvarchar(100));
        INSERT INTO @SuwonDistricts(area_code,area_name) VALUES
          ('V242-41111',N'수원시 장안구'),('V242-41113',N'수원시 권선구'),
          ('V242-41115',N'수원시 팔달구'),('V242-41117',N'수원시 영통구');

        UPDATE area
           SET area.area_name = district.area_name, area.updated_at = SYSUTCDATETIME()
          FROM dbo.administrative_areas area
          INNER JOIN @SuwonDistricts district
             ON area.area_name = REPLACE(district.area_name,N'수원시 ',N'')
         WHERE area.parent_area_id=@GyeonggiId AND area.area_level_code='SIGUNGU';

        INSERT INTO dbo.administrative_areas
          (public_id,source_system_code,area_code,area_name,area_level_code,parent_area_id,source_parent_area_code,effective_from,is_active,created_at,updated_at)
        SELECT NEWID(),'SOODAL_V242',district.area_code,district.area_name,'SIGUNGU',@GyeonggiId,
               province.area_code,CAST(SYSUTCDATETIME() AS date),1,SYSUTCDATETIME(),SYSUTCDATETIME()
          FROM @SuwonDistricts district
          CROSS APPLY (SELECT area_code FROM dbo.administrative_areas WHERE id=@GyeonggiId) province
         WHERE NOT EXISTS (
               SELECT 1 FROM dbo.administrative_areas existing
                WHERE existing.parent_area_id=@GyeonggiId AND existing.area_level_code='SIGUNGU'
                  AND existing.area_name=district.area_name AND existing.is_active=1
         );
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
