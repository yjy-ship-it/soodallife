SET NOCOUNT ON;

DECLARE @Cities table(area_code varchar(20) NOT NULL PRIMARY KEY, area_name nvarchar(100) NOT NULL);
INSERT INTO @Cities(area_code,area_name) VALUES
  ('4111000000',N'수원시'),('4113000000',N'성남시'),('4117000000',N'안양시'),
  ('4127000000',N'안산시'),('4128000000',N'고양시'),('4146000000',N'용인시'),
  ('4119000000',N'부천시'),('4159000000',N'화성시'),('4311000000',N'청주시'),
  ('4413000000',N'천안시'),('5211000000',N'전주시'),('4711000000',N'포항시'),
  ('4812000000',N'창원시');

IF EXISTS
(
    SELECT 1 FROM dbo.administrative_areas area
    INNER JOIN @Cities city ON city.area_code=area.area_code AND city.area_name=area.area_name
    WHERE area.area_level_code='SIGUNGU'
) THROW 52441, 'V244-R3 verification failed: one or more parent city rows still exist.', 1;

DECLARE @Districts table(area_name nvarchar(100) NOT NULL PRIMARY KEY);
INSERT INTO @Districts(area_name) VALUES
  (N'수원시 장안구'),(N'수원시 권선구'),(N'수원시 팔달구'),(N'수원시 영통구'),
  (N'성남시 수정구'),(N'성남시 중원구'),(N'성남시 분당구'),
  (N'안양시 만안구'),(N'안양시 동안구'),
  (N'안산시 상록구'),(N'안산시 단원구'),
  (N'고양시 덕양구'),(N'고양시 일산동구'),(N'고양시 일산서구'),
  (N'용인시 처인구'),(N'용인시 기흥구'),(N'용인시 수지구'),
  (N'부천시 원미구'),(N'부천시 소사구'),(N'부천시 오정구'),
  (N'화성시 만세구'),(N'화성시 효행구'),(N'화성시 병점구'),(N'화성시 동탄구'),
  (N'청주시 상당구'),(N'청주시 서원구'),(N'청주시 흥덕구'),(N'청주시 청원구'),
  (N'천안시 동남구'),(N'천안시 서북구'),
  (N'전주시 완산구'),(N'전주시 덕진구'),
  (N'포항시 남구'),(N'포항시 북구'),
  (N'창원시 의창구'),(N'창원시 성산구'),(N'창원시 마산합포구'),
  (N'창원시 마산회원구'),(N'창원시 진해구');

IF EXISTS
(
    SELECT 1 FROM @Districts expected
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.administrative_areas child
        INNER JOIN dbo.administrative_areas parent ON parent.id=child.parent_area_id
        WHERE child.area_name=expected.area_name AND child.area_level_code='SIGUNGU'
          AND child.is_active=1 AND parent.area_level_code='SIDO' AND parent.is_active=1
    )
) THROW 52442, 'V244-R3 verification failed: one or more district choices were removed or assigned incorrectly.', 1;

SELECT N'V244_OK' AS verification,
       (SELECT COUNT(*) FROM @Cities city WHERE NOT EXISTS
        (SELECT 1 FROM dbo.administrative_areas area WHERE area.area_code=city.area_code AND area.area_name=city.area_name)) AS deleted_city_count,
       (SELECT COUNT(*) FROM @Districts) AS preserved_district_count;
