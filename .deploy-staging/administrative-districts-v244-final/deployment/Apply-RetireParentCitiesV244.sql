SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.administrative_areas', N'U') IS NULL
        THROW 52440, 'V244 apply failed: administrative_areas table is unavailable.', 1;

    DECLARE @Cities table(area_code varchar(20) NOT NULL PRIMARY KEY, area_name nvarchar(100) NOT NULL);
    INSERT INTO @Cities(area_code,area_name) VALUES
      ('4111000000',N'수원시'),('4113000000',N'성남시'),('4117000000',N'안양시'),
      ('4127000000',N'안산시'),('4128000000',N'고양시'),('4146000000',N'용인시'),
      ('4119000000',N'부천시'),('4159000000',N'화성시'),('4311000000',N'청주시'),
      ('4413000000',N'천안시'),('5211000000',N'전주시'),('4711000000',N'포항시'),
      ('4812000000',N'창원시');

    /* 기존 거래·주소 참조를 보존하기 위해 물리 삭제 대신 선택 목록에서만 비활성화합니다. */
    UPDATE area
       SET area.is_active=0,
           area.effective_to=CAST(SYSUTCDATETIME() AS date),
           area.updated_at=SYSUTCDATETIME()
      FROM dbo.administrative_areas area
      INNER JOIN @Cities city ON city.area_code=area.area_code AND city.area_name=area.area_name
     WHERE area.area_level_code='SIGUNGU' AND area.is_active=1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
