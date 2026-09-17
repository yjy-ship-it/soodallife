SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.administrative_areas', N'U') IS NULL
        THROW 52430, 'V243 apply failed: administrative_areas table is unavailable.', 1;

    DECLARE @Districts table
    (
        sido_name nvarchar(100) NOT NULL,
        alternate_sido_name nvarchar(100) NULL,
        area_code varchar(20) NOT NULL,
        area_name nvarchar(100) NOT NULL
    );

    INSERT INTO @Districts(sido_name, alternate_sido_name, area_code, area_name) VALUES
      (N'경기도',N'경기','4113100000',N'성남시 수정구'),
      (N'경기도',N'경기','4113300000',N'성남시 중원구'),
      (N'경기도',N'경기','4113500000',N'성남시 분당구'),
      (N'경기도',N'경기','4117100000',N'안양시 만안구'),
      (N'경기도',N'경기','4117300000',N'안양시 동안구'),
      (N'경기도',N'경기','4127100000',N'안산시 상록구'),
      (N'경기도',N'경기','4127300000',N'안산시 단원구'),
      (N'경기도',N'경기','4128100000',N'고양시 덕양구'),
      (N'경기도',N'경기','4128500000',N'고양시 일산동구'),
      (N'경기도',N'경기','4128700000',N'고양시 일산서구'),
      (N'경기도',N'경기','4146100000',N'용인시 처인구'),
      (N'경기도',N'경기','4146300000',N'용인시 기흥구'),
      (N'경기도',N'경기','4146500000',N'용인시 수지구'),
      (N'경기도',N'경기','4119200000',N'부천시 원미구'),
      (N'경기도',N'경기','4119400000',N'부천시 소사구'),
      (N'경기도',N'경기','4119600000',N'부천시 오정구'),
      (N'경기도',N'경기','4159100000',N'화성시 만세구'),
      (N'경기도',N'경기','4159300000',N'화성시 효행구'),
      (N'경기도',N'경기','4159500000',N'화성시 병점구'),
      (N'경기도',N'경기','4159700000',N'화성시 동탄구'),
      (N'충청북도',N'충북','4311100000',N'청주시 상당구'),
      (N'충청북도',N'충북','4311200000',N'청주시 서원구'),
      (N'충청북도',N'충북','4311300000',N'청주시 흥덕구'),
      (N'충청북도',N'충북','4311400000',N'청주시 청원구'),
      (N'충청남도',N'충남','4413100000',N'천안시 동남구'),
      (N'충청남도',N'충남','4413300000',N'천안시 서북구'),
      (N'전북특별자치도',N'전라북도','5211100000',N'전주시 완산구'),
      (N'전북특별자치도',N'전라북도','5211300000',N'전주시 덕진구'),
      (N'경상북도',N'경북','4711100000',N'포항시 남구'),
      (N'경상북도',N'경북','4711300000',N'포항시 북구'),
      (N'경상남도',N'경남','4812100000',N'창원시 의창구'),
      (N'경상남도',N'경남','4812300000',N'창원시 성산구'),
      (N'경상남도',N'경남','4812500000',N'창원시 마산합포구'),
      (N'경상남도',N'경남','4812700000',N'창원시 마산회원구'),
      (N'경상남도',N'경남','4812900000',N'창원시 진해구');

    IF EXISTS
    (
        SELECT 1
        FROM (SELECT DISTINCT sido_name, alternate_sido_name FROM @Districts) expected
        WHERE NOT EXISTS
        (
            SELECT 1 FROM dbo.administrative_areas sido
            WHERE sido.area_level_code='SIDO' AND sido.is_active=1
              AND sido.area_name IN (expected.sido_name, expected.alternate_sido_name)
        )
    ) THROW 52431, 'V243 apply failed: one or more parent provinces are unavailable.', 1;

    UPDATE target
       SET target.area_name = source.area_name,
           target.area_level_code = 'SIGUNGU',
           target.parent_area_id = source.parent_id,
           target.source_parent_area_code = source.parent_code,
           target.source_system_code = 'SOODAL_V243',
           target.effective_to = NULL,
           target.is_active = 1,
           target.updated_at = SYSUTCDATETIME()
      FROM dbo.administrative_areas target
      INNER JOIN
      (
          SELECT district.area_code, district.area_name, parent.id AS parent_id, parent.area_code AS parent_code
          FROM @Districts district
          CROSS APPLY
          (
              SELECT TOP (1) sido.id, sido.area_code
              FROM dbo.administrative_areas sido
              WHERE sido.area_level_code='SIDO' AND sido.is_active=1
                AND sido.area_name IN (district.sido_name, district.alternate_sido_name)
              ORDER BY CASE WHEN sido.area_name=district.sido_name THEN 0 ELSE 1 END, sido.id
          ) parent
      ) source ON source.area_code=target.area_code;

    INSERT INTO dbo.administrative_areas
      (public_id,source_system_code,area_code,area_name,area_level_code,parent_area_id,
       source_parent_area_code,effective_from,is_active,created_at,updated_at)
    SELECT NEWID(),'SOODAL_V243',district.area_code,district.area_name,'SIGUNGU',parent.id,
           parent.area_code,CAST(SYSUTCDATETIME() AS date),1,SYSUTCDATETIME(),SYSUTCDATETIME()
      FROM @Districts district
      CROSS APPLY
      (
          SELECT TOP (1) sido.id, sido.area_code
          FROM dbo.administrative_areas sido
          WHERE sido.area_level_code='SIDO' AND sido.is_active=1
            AND sido.area_name IN (district.sido_name, district.alternate_sido_name)
          ORDER BY CASE WHEN sido.area_name=district.sido_name THEN 0 ELSE 1 END, sido.id
      ) parent
     WHERE NOT EXISTS
     (
         SELECT 1 FROM dbo.administrative_areas existing
         WHERE existing.area_code=district.area_code
     );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
