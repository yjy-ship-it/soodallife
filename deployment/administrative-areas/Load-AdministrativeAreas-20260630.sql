SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @ExpectedSido int = 16;
DECLARE @ExpectedSigungu int = 230;
DECLARE @Now datetime2(7) = SYSUTCDATETIME();
DECLARE @RetiredAt date = CONVERT(date, @Now);

IF OBJECT_ID(N'dbo.administrative_areas', N'U') IS NULL
    THROW 51000, 'administrative_areas table does not exist.', 1;

DECLARE @Source TABLE
(
    area_code varchar(20) NOT NULL PRIMARY KEY,
    area_name nvarchar(100) NOT NULL,
    area_level_code varchar(20) NOT NULL,
    parent_area_code varchar(20) NULL,
    source_created_date date NOT NULL
);

INSERT INTO @Source(area_code, area_name, area_level_code, parent_area_code, source_created_date)
VALUES
    ('1100000000', N'서울특별시', 'SIDO', NULL, CONVERT(date, '1988-04-23', 23)),
    ('1200000000', N'전남광주통합특별시', 'SIDO', NULL, CONVERT(date, '2026-06-30', 23)),
    ('2600000000', N'부산광역시', 'SIDO', NULL, CONVERT(date, '1995-01-01', 23)),
    ('2700000000', N'대구광역시', 'SIDO', NULL, CONVERT(date, '1995-01-01', 23)),
    ('2800000000', N'인천광역시', 'SIDO', NULL, CONVERT(date, '1995-01-01', 23)),
    ('3000000000', N'대전광역시', 'SIDO', NULL, CONVERT(date, '1995-01-01', 23)),
    ('3100000000', N'울산광역시', 'SIDO', NULL, CONVERT(date, '1997-07-15', 23)),
    ('3600000000', N'세종특별자치시', 'SIDO', NULL, CONVERT(date, '2012-07-01', 23)),
    ('4100000000', N'경기도', 'SIDO', NULL, CONVERT(date, '1988-04-23', 23)),
    ('4300000000', N'충청북도', 'SIDO', NULL, CONVERT(date, '1988-04-23', 23)),
    ('4400000000', N'충청남도', 'SIDO', NULL, CONVERT(date, '1988-04-23', 23)),
    ('4700000000', N'경상북도', 'SIDO', NULL, CONVERT(date, '1988-04-23', 23)),
    ('4800000000', N'경상남도', 'SIDO', NULL, CONVERT(date, '1988-04-23', 23)),
    ('5000000000', N'제주특별자치도', 'SIDO', NULL, CONVERT(date, '2006-07-01', 23)),
    ('5100000000', N'강원특별자치도', 'SIDO', NULL, CONVERT(date, '2023-06-09', 23)),
    ('5200000000', N'전북특별자치도', 'SIDO', NULL, CONVERT(date, '2024-01-18', 23)),
    ('1111000000', N'종로구', 'SIGUNGU', '1100000000', CONVERT(date, '1988-04-23', 23)),
    ('1114000000', N'중구', 'SIGUNGU', '1100000000', CONVERT(date, '1988-04-23', 23)),
    ('1117000000', N'용산구', 'SIGUNGU', '1100000000', CONVERT(date, '1988-04-23', 23)),
    ('1120000000', N'성동구', 'SIGUNGU', '1100000000', CONVERT(date, '1988-04-23', 23)),
    ('1121500000', N'광진구', 'SIGUNGU', '1100000000', CONVERT(date, '1995-03-01', 23)),
    ('1123000000', N'동대문구', 'SIGUNGU', '1100000000', CONVERT(date, '1988-04-23', 23)),
    ('1126000000', N'중랑구', 'SIGUNGU', '1100000000', CONVERT(date, '1988-04-23', 23)),
    ('1129000000', N'성북구', 'SIGUNGU', '1100000000', CONVERT(date, '1988-04-23', 23)),
    ('1130500000', N'강북구', 'SIGUNGU', '1100000000', CONVERT(date, '1995-03-01', 23)),
    ('1132000000', N'도봉구', 'SIGUNGU', '1100000000', CONVERT(date, '1988-04-23', 23)),
    ('1135000000', N'노원구', 'SIGUNGU', '1100000000', CONVERT(date, '1988-04-23', 23)),
    ('1138000000', N'은평구', 'SIGUNGU', '1100000000', CONVERT(date, '1988-04-23', 23)),
    ('1141000000', N'서대문구', 'SIGUNGU', '1100000000', CONVERT(date, '1988-04-23', 23)),
    ('1144000000', N'마포구', 'SIGUNGU', '1100000000', CONVERT(date, '1988-04-23', 23)),
    ('1147000000', N'양천구', 'SIGUNGU', '1100000000', CONVERT(date, '1988-04-23', 23)),
    ('1150000000', N'강서구', 'SIGUNGU', '1100000000', CONVERT(date, '1988-04-23', 23)),
    ('1153000000', N'구로구', 'SIGUNGU', '1100000000', CONVERT(date, '1988-04-23', 23)),
    ('1154500000', N'금천구', 'SIGUNGU', '1100000000', CONVERT(date, '1995-03-01', 23)),
    ('1156000000', N'영등포구', 'SIGUNGU', '1100000000', CONVERT(date, '1988-04-23', 23)),
    ('1159000000', N'동작구', 'SIGUNGU', '1100000000', CONVERT(date, '1988-04-23', 23)),
    ('1162000000', N'관악구', 'SIGUNGU', '1100000000', CONVERT(date, '1988-04-23', 23)),
    ('1165000000', N'서초구', 'SIGUNGU', '1100000000', CONVERT(date, '1988-04-23', 23)),
    ('1168000000', N'강남구', 'SIGUNGU', '1100000000', CONVERT(date, '1988-04-23', 23)),
    ('1171000000', N'송파구', 'SIGUNGU', '1100000000', CONVERT(date, '1988-04-23', 23)),
    ('1174000000', N'강동구', 'SIGUNGU', '1100000000', CONVERT(date, '1988-04-23', 23)),
    ('1211000000', N'목포시', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1213000000', N'여수시', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1215000000', N'순천시', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1217000000', N'나주시', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1219000000', N'광양시', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1221000000', N'동구', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1224000000', N'서구', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1227000000', N'남구', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1230000000', N'북구', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1233000000', N'광산구', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1271000000', N'담양군', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1272000000', N'곡성군', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1273000000', N'구례군', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1274000000', N'고흥군', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1275000000', N'보성군', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1276000000', N'화순군', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1277000000', N'장흥군', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1278000000', N'강진군', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1279000000', N'해남군', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1280000000', N'영암군', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1281000000', N'무안군', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1282000000', N'함평군', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1283000000', N'영광군', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1284000000', N'장성군', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1285000000', N'완도군', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1286000000', N'진도군', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('1287000000', N'신안군', 'SIGUNGU', '1200000000', CONVERT(date, '2026-06-30', 23)),
    ('2611000000', N'중구', 'SIGUNGU', '2600000000', CONVERT(date, '1995-01-01', 23)),
    ('2614000000', N'서구', 'SIGUNGU', '2600000000', CONVERT(date, '1995-01-01', 23)),
    ('2617000000', N'동구', 'SIGUNGU', '2600000000', CONVERT(date, '1995-01-01', 23)),
    ('2620000000', N'영도구', 'SIGUNGU', '2600000000', CONVERT(date, '1995-01-01', 23)),
    ('2623000000', N'부산진구', 'SIGUNGU', '2600000000', CONVERT(date, '1995-01-01', 23)),
    ('2626000000', N'동래구', 'SIGUNGU', '2600000000', CONVERT(date, '1995-01-01', 23)),
    ('2629000000', N'남구', 'SIGUNGU', '2600000000', CONVERT(date, '1995-01-01', 23)),
    ('2632000000', N'북구', 'SIGUNGU', '2600000000', CONVERT(date, '1995-01-01', 23)),
    ('2635000000', N'해운대구', 'SIGUNGU', '2600000000', CONVERT(date, '1995-01-01', 23)),
    ('2638000000', N'사하구', 'SIGUNGU', '2600000000', CONVERT(date, '1995-01-01', 23)),
    ('2641000000', N'금정구', 'SIGUNGU', '2600000000', CONVERT(date, '1995-01-01', 23)),
    ('2644000000', N'강서구', 'SIGUNGU', '2600000000', CONVERT(date, '1995-01-01', 23)),
    ('2647000000', N'연제구', 'SIGUNGU', '2600000000', CONVERT(date, '1995-03-01', 23)),
    ('2650000000', N'수영구', 'SIGUNGU', '2600000000', CONVERT(date, '1995-03-01', 23)),
    ('2653000000', N'사상구', 'SIGUNGU', '2600000000', CONVERT(date, '1995-03-01', 23)),
    ('2671000000', N'기장군', 'SIGUNGU', '2600000000', CONVERT(date, '1995-03-01', 23)),
    ('2711000000', N'중구', 'SIGUNGU', '2700000000', CONVERT(date, '1995-01-01', 23)),
    ('2714000000', N'동구', 'SIGUNGU', '2700000000', CONVERT(date, '1995-01-01', 23)),
    ('2717000000', N'서구', 'SIGUNGU', '2700000000', CONVERT(date, '1995-01-01', 23)),
    ('2720000000', N'남구', 'SIGUNGU', '2700000000', CONVERT(date, '1995-01-01', 23)),
    ('2723000000', N'북구', 'SIGUNGU', '2700000000', CONVERT(date, '1995-01-01', 23)),
    ('2726000000', N'수성구', 'SIGUNGU', '2700000000', CONVERT(date, '1995-01-01', 23)),
    ('2729000000', N'달서구', 'SIGUNGU', '2700000000', CONVERT(date, '1995-01-01', 23)),
    ('2771000000', N'달성군', 'SIGUNGU', '2700000000', CONVERT(date, '1995-03-01', 23)),
    ('2772000000', N'군위군', 'SIGUNGU', '2700000000', CONVERT(date, '2023-06-30', 23)),
    ('2812500000', N'제물포구', 'SIGUNGU', '2800000000', CONVERT(date, '2026-06-30', 23)),
    ('2815500000', N'영종구', 'SIGUNGU', '2800000000', CONVERT(date, '2026-06-30', 23)),
    ('2817700000', N'미추홀구', 'SIGUNGU', '2800000000', CONVERT(date, '2018-07-01', 23)),
    ('2818500000', N'연수구', 'SIGUNGU', '2800000000', CONVERT(date, '1995-03-01', 23)),
    ('2820000000', N'남동구', 'SIGUNGU', '2800000000', CONVERT(date, '1995-01-01', 23)),
    ('2823700000', N'부평구', 'SIGUNGU', '2800000000', CONVERT(date, '1995-03-01', 23)),
    ('2824500000', N'계양구', 'SIGUNGU', '2800000000', CONVERT(date, '1995-03-01', 23)),
    ('2827500000', N'서해구', 'SIGUNGU', '2800000000', CONVERT(date, '2026-06-30', 23)),
    ('2829000000', N'검단구', 'SIGUNGU', '2800000000', CONVERT(date, '2026-06-30', 23)),
    ('2871000000', N'강화군', 'SIGUNGU', '2800000000', CONVERT(date, '1995-03-01', 23)),
    ('2872000000', N'옹진군', 'SIGUNGU', '2800000000', CONVERT(date, '1995-03-01', 23)),
    ('3011000000', N'동구', 'SIGUNGU', '3000000000', CONVERT(date, '1995-01-01', 23)),
    ('3014000000', N'중구', 'SIGUNGU', '3000000000', CONVERT(date, '1995-01-01', 23)),
    ('3017000000', N'서구', 'SIGUNGU', '3000000000', CONVERT(date, '1995-01-01', 23)),
    ('3020000000', N'유성구', 'SIGUNGU', '3000000000', CONVERT(date, '1995-01-01', 23)),
    ('3023000000', N'대덕구', 'SIGUNGU', '3000000000', CONVERT(date, '1995-01-01', 23)),
    ('3111000000', N'중구', 'SIGUNGU', '3100000000', CONVERT(date, '1997-07-15', 23)),
    ('3114000000', N'남구', 'SIGUNGU', '3100000000', CONVERT(date, '1997-07-15', 23)),
    ('3117000000', N'동구', 'SIGUNGU', '3100000000', CONVERT(date, '1997-07-15', 23)),
    ('3120000000', N'북구', 'SIGUNGU', '3100000000', CONVERT(date, '1997-07-15', 23)),
    ('3171000000', N'울주군', 'SIGUNGU', '3100000000', CONVERT(date, '1997-07-15', 23)),
    ('3611000000', N'세종시', 'SIGUNGU', '3600000000', CONVERT(date, '2012-07-01', 23)),
    ('4111000000', N'수원시', 'SIGUNGU', '4100000000', CONVERT(date, '1988-04-23', 23)),
    ('4113000000', N'성남시', 'SIGUNGU', '4100000000', CONVERT(date, '1988-04-23', 23)),
    ('4115000000', N'의정부시', 'SIGUNGU', '4100000000', CONVERT(date, '1988-04-23', 23)),
    ('4117000000', N'안양시', 'SIGUNGU', '4100000000', CONVERT(date, '1988-04-23', 23)),
    ('4119000000', N'부천시', 'SIGUNGU', '4100000000', CONVERT(date, '1988-04-23', 23)),
    ('4121000000', N'광명시', 'SIGUNGU', '4100000000', CONVERT(date, '1988-04-23', 23)),
    ('4122000000', N'평택시', 'SIGUNGU', '4100000000', CONVERT(date, '1995-05-10', 23)),
    ('4125000000', N'동두천시', 'SIGUNGU', '4100000000', CONVERT(date, '1988-04-23', 23)),
    ('4127000000', N'안산시', 'SIGUNGU', '4100000000', CONVERT(date, '1988-04-23', 23)),
    ('4128000000', N'고양시', 'SIGUNGU', '4100000000', CONVERT(date, '1992-05-04', 23)),
    ('4129000000', N'과천시', 'SIGUNGU', '4100000000', CONVERT(date, '1988-04-23', 23)),
    ('4131000000', N'구리시', 'SIGUNGU', '4100000000', CONVERT(date, '1988-04-23', 23)),
    ('4136000000', N'남양주시', 'SIGUNGU', '4100000000', CONVERT(date, '1995-01-01', 23)),
    ('4137000000', N'오산시', 'SIGUNGU', '4100000000', CONVERT(date, '1989-01-01', 23)),
    ('4139000000', N'시흥시', 'SIGUNGU', '4100000000', CONVERT(date, '1989-01-01', 23)),
    ('4141000000', N'군포시', 'SIGUNGU', '4100000000', CONVERT(date, '1989-01-01', 23)),
    ('4143000000', N'의왕시', 'SIGUNGU', '4100000000', CONVERT(date, '1989-01-01', 23)),
    ('4145000000', N'하남시', 'SIGUNGU', '4100000000', CONVERT(date, '1989-01-01', 23)),
    ('4146000000', N'용인시', 'SIGUNGU', '4100000000', CONVERT(date, '1996-03-01', 23)),
    ('4148000000', N'파주시', 'SIGUNGU', '4100000000', CONVERT(date, '1996-03-01', 23)),
    ('4150000000', N'이천시', 'SIGUNGU', '4100000000', CONVERT(date, '1996-03-01', 23)),
    ('4155000000', N'안성시', 'SIGUNGU', '4100000000', CONVERT(date, '1998-04-01', 23)),
    ('4157000000', N'김포시', 'SIGUNGU', '4100000000', CONVERT(date, '1998-04-01', 23)),
    ('4159000000', N'화성시', 'SIGUNGU', '4100000000', CONVERT(date, '2001-03-21', 23)),
    ('4161000000', N'광주시', 'SIGUNGU', '4100000000', CONVERT(date, '2001-03-21', 23)),
    ('4163000000', N'양주시', 'SIGUNGU', '4100000000', CONVERT(date, '2003-10-19', 23)),
    ('4165000000', N'포천시', 'SIGUNGU', '4100000000', CONVERT(date, '2003-10-19', 23)),
    ('4167000000', N'여주시', 'SIGUNGU', '4100000000', CONVERT(date, '2013-09-23', 23)),
    ('4180000000', N'연천군', 'SIGUNGU', '4100000000', CONVERT(date, '1988-04-23', 23)),
    ('4182000000', N'가평군', 'SIGUNGU', '4100000000', CONVERT(date, '1988-04-23', 23)),
    ('4183000000', N'양평군', 'SIGUNGU', '4100000000', CONVERT(date, '1988-04-23', 23)),
    ('4311000000', N'청주시', 'SIGUNGU', '4300000000', CONVERT(date, '1988-04-23', 23)),
    ('4313000000', N'충주시', 'SIGUNGU', '4300000000', CONVERT(date, '1988-04-23', 23)),
    ('4315000000', N'제천시', 'SIGUNGU', '4300000000', CONVERT(date, '1988-04-23', 23)),
    ('4372000000', N'보은군', 'SIGUNGU', '4300000000', CONVERT(date, '1988-04-23', 23)),
    ('4373000000', N'옥천군', 'SIGUNGU', '4300000000', CONVERT(date, '1988-04-23', 23)),
    ('4374000000', N'영동군', 'SIGUNGU', '4300000000', CONVERT(date, '1988-04-23', 23)),
    ('4374500000', N'증평군', 'SIGUNGU', '4300000000', CONVERT(date, '2003-08-30', 23)),
    ('4375000000', N'진천군', 'SIGUNGU', '4300000000', CONVERT(date, '1988-04-23', 23)),
    ('4376000000', N'괴산군', 'SIGUNGU', '4300000000', CONVERT(date, '1988-04-23', 23)),
    ('4377000000', N'음성군', 'SIGUNGU', '4300000000', CONVERT(date, '1988-04-23', 23)),
    ('4380000000', N'단양군', 'SIGUNGU', '4300000000', CONVERT(date, '1988-04-23', 23)),
    ('4413000000', N'천안시', 'SIGUNGU', '4400000000', CONVERT(date, '1988-04-23', 23)),
    ('4415000000', N'공주시', 'SIGUNGU', '4400000000', CONVERT(date, '1988-04-23', 23)),
    ('4418000000', N'보령시', 'SIGUNGU', '4400000000', CONVERT(date, '1995-01-01', 23)),
    ('4420000000', N'아산시', 'SIGUNGU', '4400000000', CONVERT(date, '1995-01-01', 23)),
    ('4421000000', N'서산시', 'SIGUNGU', '4400000000', CONVERT(date, '1989-01-01', 23)),
    ('4423000000', N'논산시', 'SIGUNGU', '4400000000', CONVERT(date, '1996-03-01', 23)),
    ('4425000000', N'계룡시', 'SIGUNGU', '4400000000', CONVERT(date, '2003-09-19', 23)),
    ('4427000000', N'당진시', 'SIGUNGU', '4400000000', CONVERT(date, '2012-01-01', 23)),
    ('4471000000', N'금산군', 'SIGUNGU', '4400000000', CONVERT(date, '1988-04-23', 23)),
    ('4476000000', N'부여군', 'SIGUNGU', '4400000000', CONVERT(date, '1988-04-23', 23)),
    ('4477000000', N'서천군', 'SIGUNGU', '4400000000', CONVERT(date, '1988-04-23', 23)),
    ('4479000000', N'청양군', 'SIGUNGU', '4400000000', CONVERT(date, '1988-04-23', 23)),
    ('4480000000', N'홍성군', 'SIGUNGU', '4400000000', CONVERT(date, '1988-04-23', 23)),
    ('4481000000', N'예산군', 'SIGUNGU', '4400000000', CONVERT(date, '1988-04-23', 23)),
    ('4482500000', N'태안군', 'SIGUNGU', '4400000000', CONVERT(date, '1989-01-01', 23)),
    ('4711000000', N'포항시', 'SIGUNGU', '4700000000', CONVERT(date, '1988-04-23', 23)),
    ('4713000000', N'경주시', 'SIGUNGU', '4700000000', CONVERT(date, '1988-04-23', 23)),
    ('4715000000', N'김천시', 'SIGUNGU', '4700000000', CONVERT(date, '1988-04-23', 23)),
    ('4717000000', N'안동시', 'SIGUNGU', '4700000000', CONVERT(date, '1988-04-23', 23)),
    ('4719000000', N'구미시', 'SIGUNGU', '4700000000', CONVERT(date, '1988-04-23', 23)),
    ('4721000000', N'영주시', 'SIGUNGU', '4700000000', CONVERT(date, '1988-04-23', 23)),
    ('4723000000', N'영천시', 'SIGUNGU', '4700000000', CONVERT(date, '1988-04-23', 23)),
    ('4725000000', N'상주시', 'SIGUNGU', '4700000000', CONVERT(date, '1988-04-23', 23)),
    ('4728000000', N'문경시', 'SIGUNGU', '4700000000', CONVERT(date, '1995-01-01', 23)),
    ('4729000000', N'경산시', 'SIGUNGU', '4700000000', CONVERT(date, '1989-01-01', 23)),
    ('4773000000', N'의성군', 'SIGUNGU', '4700000000', CONVERT(date, '1988-04-23', 23)),
    ('4775000000', N'청송군', 'SIGUNGU', '4700000000', CONVERT(date, '1988-04-23', 23)),
    ('4776000000', N'영양군', 'SIGUNGU', '4700000000', CONVERT(date, '1988-04-23', 23)),
    ('4777000000', N'영덕군', 'SIGUNGU', '4700000000', CONVERT(date, '1988-04-23', 23)),
    ('4782000000', N'청도군', 'SIGUNGU', '4700000000', CONVERT(date, '1988-04-23', 23)),
    ('4783000000', N'고령군', 'SIGUNGU', '4700000000', CONVERT(date, '1988-04-23', 23)),
    ('4784000000', N'성주군', 'SIGUNGU', '4700000000', CONVERT(date, '1988-04-23', 23)),
    ('4785000000', N'칠곡군', 'SIGUNGU', '4700000000', CONVERT(date, '1988-04-23', 23)),
    ('4790000000', N'예천군', 'SIGUNGU', '4700000000', CONVERT(date, '1988-04-23', 23)),
    ('4792000000', N'봉화군', 'SIGUNGU', '4700000000', CONVERT(date, '1988-04-23', 23)),
    ('4793000000', N'울진군', 'SIGUNGU', '4700000000', CONVERT(date, '1988-04-23', 23)),
    ('4794000000', N'울릉군', 'SIGUNGU', '4700000000', CONVERT(date, '1988-04-23', 23)),
    ('4812000000', N'창원시', 'SIGUNGU', '4800000000', CONVERT(date, '2010-07-01', 23)),
    ('4817000000', N'진주시', 'SIGUNGU', '4800000000', CONVERT(date, '1988-04-23', 23)),
    ('4822000000', N'통영시', 'SIGUNGU', '4800000000', CONVERT(date, '1995-01-01', 23)),
    ('4824000000', N'사천시', 'SIGUNGU', '4800000000', CONVERT(date, '1995-05-10', 23)),
    ('4825000000', N'김해시', 'SIGUNGU', '4800000000', CONVERT(date, '1988-04-23', 23)),
    ('4827000000', N'밀양시', 'SIGUNGU', '4800000000', CONVERT(date, '1989-01-01', 23)),
    ('4831000000', N'거제시', 'SIGUNGU', '4800000000', CONVERT(date, '1995-01-01', 23)),
    ('4833000000', N'양산시', 'SIGUNGU', '4800000000', CONVERT(date, '1996-03-01', 23)),
    ('4872000000', N'의령군', 'SIGUNGU', '4800000000', CONVERT(date, '1988-04-23', 23)),
    ('4873000000', N'함안군', 'SIGUNGU', '4800000000', CONVERT(date, '1988-04-23', 23)),
    ('4874000000', N'창녕군', 'SIGUNGU', '4800000000', CONVERT(date, '1988-04-23', 23)),
    ('4882000000', N'고성군', 'SIGUNGU', '4800000000', CONVERT(date, '1988-04-23', 23)),
    ('4884000000', N'남해군', 'SIGUNGU', '4800000000', CONVERT(date, '1988-04-23', 23)),
    ('4885000000', N'하동군', 'SIGUNGU', '4800000000', CONVERT(date, '1988-04-23', 23)),
    ('4886000000', N'산청군', 'SIGUNGU', '4800000000', CONVERT(date, '1988-04-23', 23)),
    ('4887000000', N'함양군', 'SIGUNGU', '4800000000', CONVERT(date, '1988-04-23', 23)),
    ('4888000000', N'거창군', 'SIGUNGU', '4800000000', CONVERT(date, '1988-04-23', 23)),
    ('4889000000', N'합천군', 'SIGUNGU', '4800000000', CONVERT(date, '1988-04-23', 23)),
    ('5011000000', N'제주시', 'SIGUNGU', '5000000000', CONVERT(date, '2006-07-01', 23)),
    ('5013000000', N'서귀포시', 'SIGUNGU', '5000000000', CONVERT(date, '2006-07-01', 23)),
    ('5111000000', N'춘천시', 'SIGUNGU', '5100000000', CONVERT(date, '2023-06-09', 23)),
    ('5113000000', N'원주시', 'SIGUNGU', '5100000000', CONVERT(date, '2023-06-09', 23)),
    ('5115000000', N'강릉시', 'SIGUNGU', '5100000000', CONVERT(date, '2023-06-09', 23)),
    ('5117000000', N'동해시', 'SIGUNGU', '5100000000', CONVERT(date, '2023-06-09', 23)),
    ('5119000000', N'태백시', 'SIGUNGU', '5100000000', CONVERT(date, '2023-06-09', 23)),
    ('5121000000', N'속초시', 'SIGUNGU', '5100000000', CONVERT(date, '2023-06-09', 23)),
    ('5123000000', N'삼척시', 'SIGUNGU', '5100000000', CONVERT(date, '2023-06-09', 23)),
    ('5172000000', N'홍천군', 'SIGUNGU', '5100000000', CONVERT(date, '2023-06-09', 23)),
    ('5173000000', N'횡성군', 'SIGUNGU', '5100000000', CONVERT(date, '2023-06-09', 23)),
    ('5175000000', N'영월군', 'SIGUNGU', '5100000000', CONVERT(date, '2023-06-09', 23)),
    ('5176000000', N'평창군', 'SIGUNGU', '5100000000', CONVERT(date, '2023-06-09', 23)),
    ('5177000000', N'정선군', 'SIGUNGU', '5100000000', CONVERT(date, '2023-06-09', 23)),
    ('5178000000', N'철원군', 'SIGUNGU', '5100000000', CONVERT(date, '2023-06-09', 23)),
    ('5179000000', N'화천군', 'SIGUNGU', '5100000000', CONVERT(date, '2023-06-09', 23)),
    ('5180000000', N'양구군', 'SIGUNGU', '5100000000', CONVERT(date, '2023-06-09', 23)),
    ('5181000000', N'인제군', 'SIGUNGU', '5100000000', CONVERT(date, '2023-06-09', 23)),
    ('5182000000', N'고성군', 'SIGUNGU', '5100000000', CONVERT(date, '2023-06-09', 23)),
    ('5183000000', N'양양군', 'SIGUNGU', '5100000000', CONVERT(date, '2023-06-09', 23)),
    ('5211000000', N'전주시', 'SIGUNGU', '5200000000', CONVERT(date, '2024-01-18', 23)),
    ('5213000000', N'군산시', 'SIGUNGU', '5200000000', CONVERT(date, '2024-01-18', 23)),
    ('5214000000', N'익산시', 'SIGUNGU', '5200000000', CONVERT(date, '2024-01-18', 23)),
    ('5218000000', N'정읍시', 'SIGUNGU', '5200000000', CONVERT(date, '2024-01-18', 23)),
    ('5219000000', N'남원시', 'SIGUNGU', '5200000000', CONVERT(date, '2024-01-18', 23)),
    ('5221000000', N'김제시', 'SIGUNGU', '5200000000', CONVERT(date, '2024-01-18', 23)),
    ('5271000000', N'완주군', 'SIGUNGU', '5200000000', CONVERT(date, '2024-01-18', 23)),
    ('5272000000', N'진안군', 'SIGUNGU', '5200000000', CONVERT(date, '2024-01-18', 23)),
    ('5273000000', N'무주군', 'SIGUNGU', '5200000000', CONVERT(date, '2024-01-18', 23)),
    ('5274000000', N'장수군', 'SIGUNGU', '5200000000', CONVERT(date, '2024-01-18', 23)),
    ('5275000000', N'임실군', 'SIGUNGU', '5200000000', CONVERT(date, '2024-01-18', 23)),
    ('5277000000', N'순창군', 'SIGUNGU', '5200000000', CONVERT(date, '2024-01-18', 23)),
    ('5279000000', N'고창군', 'SIGUNGU', '5200000000', CONVERT(date, '2024-01-18', 23)),
    ('5280000000', N'부안군', 'SIGUNGU', '5200000000', CONVERT(date, '2024-01-18', 23));

IF (SELECT COUNT(*) FROM @Source WHERE area_level_code = 'SIDO') <> @ExpectedSido
    THROW 51001, 'Unexpected SIDO source count.', 1;
IF (SELECT COUNT(*) FROM @Source WHERE area_level_code = 'SIGUNGU') <> @ExpectedSigungu
    THROW 51002, 'Unexpected SIGUNGU source count.', 1;
IF EXISTS
(
    SELECT 1
    FROM @Source child
    LEFT JOIN @Source parent ON parent.area_code = child.parent_area_code AND parent.area_level_code = 'SIDO'
    WHERE child.area_level_code = 'SIGUNGU' AND parent.area_code IS NULL
)
    THROW 51003, 'SIGUNGU source contains an invalid SIDO parent.', 1;

BEGIN TRANSACTION;

-- Preserve IDs and references when updating an existing active official code.
UPDATE target
SET source_system_code = 'MOIS_STANDARD_CODE',
    area_name = source.area_name,
    area_level_code = source.area_level_code,
    source_parent_area_code = source.parent_area_code,
    source_created_date = source.source_created_date,
    source_abolished_date = NULL,
    abolition_type_code = NULL,
    effective_to = NULL,
    is_active = 1,
    updated_at = @Now
FROM dbo.administrative_areas target
JOIN @Source source ON source.area_code = target.area_code
WHERE target.is_active = 1;

-- Reactivate the exact historical version instead of creating a duplicate.
UPDATE target
SET source_system_code = 'MOIS_STANDARD_CODE',
    area_name = source.area_name,
    area_level_code = source.area_level_code,
    source_parent_area_code = source.parent_area_code,
    source_created_date = source.source_created_date,
    source_abolished_date = NULL,
    abolition_type_code = NULL,
    effective_to = NULL,
    is_active = 1,
    updated_at = @Now
FROM dbo.administrative_areas target
JOIN @Source source
  ON source.area_code = target.area_code
 AND source.source_created_date = target.effective_from
WHERE target.is_active = 0
  AND NOT EXISTS
      (SELECT 1 FROM dbo.administrative_areas active_row
       WHERE active_row.area_code = source.area_code AND active_row.is_active = 1);

INSERT INTO dbo.administrative_areas
(
    public_id, source_system_code, area_code, area_name, area_level_code,
    parent_area_id, source_parent_area_code, source_created_date,
    source_abolished_date, abolition_type_code, effective_from, effective_to,
    is_active, created_at, updated_at
)
SELECT NEWID(), 'MOIS_STANDARD_CODE', source.area_code, source.area_name, source.area_level_code,
       NULL, source.parent_area_code, source.source_created_date,
       NULL, NULL, source.source_created_date, NULL,
       1, @Now, @Now
FROM @Source source
WHERE NOT EXISTS
      (SELECT 1 FROM dbo.administrative_areas target
       WHERE target.area_code = source.area_code AND target.is_active = 1);

-- Resolve the SIDO parent foreign key after inserting both levels.
UPDATE child
SET parent_area_id = parent.id,
    updated_at = @Now
FROM dbo.administrative_areas child
JOIN @Source source ON source.area_code = child.area_code AND source.area_level_code = 'SIGUNGU'
JOIN dbo.administrative_areas parent ON parent.area_code = source.parent_area_code AND parent.is_active = 1
WHERE child.is_active = 1;

-- Retire development-only placeholders without deleting history or references.
UPDATE dbo.administrative_areas
SET is_active = 0,
    effective_to = COALESCE(effective_to, @RetiredAt),
    source_abolished_date = COALESCE(source_abolished_date, @RetiredAt),
    abolition_type_code = COALESCE(abolition_type_code, 'DEV_REFERENCE_RETIRED'),
    updated_at = @Now
WHERE source_system_code = 'SOODAL_DEV_TEST' AND is_active = 1;

IF (SELECT COUNT(*) FROM dbo.administrative_areas WHERE is_active = 1 AND area_level_code = 'SIDO') <> @ExpectedSido
    THROW 51004, 'Active SIDO count verification failed.', 1;
IF (SELECT COUNT(*) FROM dbo.administrative_areas WHERE is_active = 1 AND area_level_code = 'SIGUNGU') <> @ExpectedSigungu
    THROW 51005, 'Active SIGUNGU count verification failed.', 1;
IF EXISTS
(
    SELECT 1
    FROM dbo.administrative_areas child
    LEFT JOIN dbo.administrative_areas parent ON parent.id = child.parent_area_id
    WHERE child.is_active = 1 AND child.area_level_code = 'SIGUNGU'
      AND (parent.id IS NULL OR parent.is_active = 0 OR parent.area_level_code <> 'SIDO')
)
    THROW 51006, 'Active SIGUNGU parent verification failed.', 1;
IF EXISTS
(
    SELECT area_code
    FROM dbo.administrative_areas
    WHERE is_active = 1
    GROUP BY area_code
    HAVING COUNT(*) > 1
)
    THROW 51007, 'Duplicate active area codes were found.', 1;

COMMIT TRANSACTION;

SELECT area_level_code, COUNT(*) AS ActiveCount
FROM dbo.administrative_areas
WHERE is_active = 1
GROUP BY area_level_code
ORDER BY area_level_code;

SELECT parent.area_name AS SidoName, COUNT(*) AS ActiveSigunguCount
FROM dbo.administrative_areas child
JOIN dbo.administrative_areas parent ON parent.id = child.parent_area_id
WHERE child.is_active = 1 AND child.area_level_code = 'SIGUNGU'
GROUP BY parent.area_name
ORDER BY parent.area_name;

SELECT COUNT(*) AS RetiredDevelopmentAreaCount
FROM dbo.administrative_areas
WHERE source_system_code = 'SOODAL_DEV_TEST' AND is_active = 0;