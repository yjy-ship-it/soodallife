SET NOCOUNT ON;

DECLARE @Expected table
(
    sido_name nvarchar(100) NOT NULL,
    alternate_sido_name nvarchar(100) NULL,
    area_code varchar(20) NOT NULL,
    area_name nvarchar(100) NOT NULL
);

INSERT INTO @Expected(sido_name, alternate_sido_name, area_code, area_name) VALUES
  (N'경기도',N'경기','4113100000',N'성남시 수정구'),(N'경기도',N'경기','4113300000',N'성남시 중원구'),(N'경기도',N'경기','4113500000',N'성남시 분당구'),
  (N'경기도',N'경기','4117100000',N'안양시 만안구'),(N'경기도',N'경기','4117300000',N'안양시 동안구'),
  (N'경기도',N'경기','4127100000',N'안산시 상록구'),(N'경기도',N'경기','4127300000',N'안산시 단원구'),
  (N'경기도',N'경기','4128100000',N'고양시 덕양구'),(N'경기도',N'경기','4128500000',N'고양시 일산동구'),(N'경기도',N'경기','4128700000',N'고양시 일산서구'),
  (N'경기도',N'경기','4146100000',N'용인시 처인구'),(N'경기도',N'경기','4146300000',N'용인시 기흥구'),(N'경기도',N'경기','4146500000',N'용인시 수지구'),
  (N'경기도',N'경기','4119200000',N'부천시 원미구'),(N'경기도',N'경기','4119400000',N'부천시 소사구'),(N'경기도',N'경기','4119600000',N'부천시 오정구'),
  (N'경기도',N'경기','4159100000',N'화성시 만세구'),(N'경기도',N'경기','4159300000',N'화성시 효행구'),(N'경기도',N'경기','4159500000',N'화성시 병점구'),(N'경기도',N'경기','4159700000',N'화성시 동탄구'),
  (N'충청북도',N'충북','4311100000',N'청주시 상당구'),(N'충청북도',N'충북','4311200000',N'청주시 서원구'),(N'충청북도',N'충북','4311300000',N'청주시 흥덕구'),(N'충청북도',N'충북','4311400000',N'청주시 청원구'),
  (N'충청남도',N'충남','4413100000',N'천안시 동남구'),(N'충청남도',N'충남','4413300000',N'천안시 서북구'),
  (N'전북특별자치도',N'전라북도','5211100000',N'전주시 완산구'),(N'전북특별자치도',N'전라북도','5211300000',N'전주시 덕진구'),
  (N'경상북도',N'경북','4711100000',N'포항시 남구'),(N'경상북도',N'경북','4711300000',N'포항시 북구'),
  (N'경상남도',N'경남','4812100000',N'창원시 의창구'),(N'경상남도',N'경남','4812300000',N'창원시 성산구'),(N'경상남도',N'경남','4812500000',N'창원시 마산합포구'),(N'경상남도',N'경남','4812700000',N'창원시 마산회원구'),(N'경상남도',N'경남','4812900000',N'창원시 진해구');

IF EXISTS
(
    SELECT 1 FROM @Expected expected
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.administrative_areas child
        INNER JOIN dbo.administrative_areas parent ON parent.id=child.parent_area_id
        WHERE child.area_code=expected.area_code AND child.area_name=expected.area_name
          AND child.area_level_code='SIGUNGU' AND child.is_active=1 AND parent.is_active=1
          AND parent.area_name IN (expected.sido_name, expected.alternate_sido_name)
    )
) THROW 52432, 'V243 verification failed: one or more attached districts are unavailable or assigned to the wrong province.', 1;

IF EXISTS
(
    SELECT child.area_code
    FROM dbo.administrative_areas child
    INNER JOIN @Expected expected ON expected.area_code=child.area_code
    WHERE child.is_active=1
    GROUP BY child.area_code
    HAVING COUNT(*)<>1
) THROW 52433, 'V243 verification failed: duplicated active district rows were found.', 1;

SELECT N'V243_OK' AS verification,
       (SELECT COUNT(*) FROM @Expected) AS attached_district_count,
       (SELECT COUNT(*) FROM dbo.administrative_areas WHERE is_active=1 AND area_name LIKE N'수원시 %구') AS suwon_district_count,
       (SELECT COUNT(*) FROM dbo.administrative_areas child INNER JOIN @Expected expected ON expected.area_code=child.area_code WHERE child.is_active=1) AS verified_district_count;
