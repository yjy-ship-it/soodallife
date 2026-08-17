SET NOCOUNT ON;

DECLARE @ExpectedSido int = 16;
DECLARE @ExpectedSigungu int = 230;

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

SELECT
    (SELECT COUNT(*) FROM dbo.administrative_areas WHERE is_active = 1 AND area_level_code = 'SIDO') AS ActiveSidoCount,
    (SELECT COUNT(*) FROM dbo.administrative_areas WHERE is_active = 1 AND area_level_code = 'SIGUNGU') AS ActiveSigunguCount,
    (SELECT COUNT(*) FROM dbo.administrative_areas WHERE source_system_code = 'SOODAL_DEV_TEST' AND is_active = 1) AS ActiveDevelopmentPlaceholderCount,
    (SELECT COUNT(*) FROM dbo.administrative_areas WHERE source_system_code = 'SOODAL_DEV_TEST' AND is_active = 0) AS RetiredDevelopmentPlaceholderCount;

SELECT COUNT(*) AS DuplicateActiveCodeCount
FROM
(
    SELECT area_code
    FROM dbo.administrative_areas
    WHERE is_active = 1
    GROUP BY area_code
    HAVING COUNT(*) > 1
) duplicates;

SELECT COUNT(*) AS InvalidActiveSigunguParentCount
FROM dbo.administrative_areas child
LEFT JOIN dbo.administrative_areas parent ON parent.id = child.parent_area_id
WHERE child.is_active = 1 AND child.area_level_code = 'SIGUNGU'
  AND (parent.id IS NULL OR parent.is_active = 0 OR parent.area_level_code <> 'SIDO');

SELECT COUNT(*) AS BrokenExistingReferenceCount
FROM
(
    SELECT request.administrative_area_id
    FROM dbo.service_requests request
    LEFT JOIN dbo.administrative_areas area ON area.id = request.administrative_area_id
    WHERE request.administrative_area_id IS NOT NULL AND area.id IS NULL
    UNION ALL
    SELECT service_area.administrative_area_id
    FROM dbo.provider_service_areas service_area
    LEFT JOIN dbo.administrative_areas area ON area.id = service_area.administrative_area_id
    WHERE service_area.administrative_area_id IS NOT NULL AND area.id IS NULL
    UNION ALL
    SELECT address.administrative_area_id
    FROM dbo.customer_addresses address
    LEFT JOIN dbo.administrative_areas area ON area.id = address.administrative_area_id
    WHERE address.administrative_area_id IS NOT NULL AND area.id IS NULL
) broken;

IF (SELECT COUNT(*) FROM dbo.administrative_areas WHERE is_active = 1 AND area_level_code = 'SIDO') <> @ExpectedSido
    THROW 51101, 'Active SIDO count verification failed.', 1;
IF (SELECT COUNT(*) FROM dbo.administrative_areas WHERE is_active = 1 AND area_level_code = 'SIGUNGU') <> @ExpectedSigungu
    THROW 51102, 'Active SIGUNGU count verification failed.', 1;
IF EXISTS
(
    SELECT area_code FROM dbo.administrative_areas WHERE is_active = 1
    GROUP BY area_code HAVING COUNT(*) > 1
)
    THROW 51103, 'Duplicate active area code verification failed.', 1;
IF EXISTS
(
    SELECT 1
    FROM dbo.administrative_areas child
    LEFT JOIN dbo.administrative_areas parent ON parent.id = child.parent_area_id
    WHERE child.is_active = 1 AND child.area_level_code = 'SIGUNGU'
      AND (parent.id IS NULL OR parent.is_active = 0 OR parent.area_level_code <> 'SIDO')
)
    THROW 51104, 'Active SIGUNGU parent verification failed.', 1;
IF EXISTS
(
    SELECT 1 FROM dbo.service_requests request
    LEFT JOIN dbo.administrative_areas area ON area.id = request.administrative_area_id
    WHERE request.administrative_area_id IS NOT NULL AND area.id IS NULL
    UNION ALL
    SELECT 1 FROM dbo.provider_service_areas service_area
    LEFT JOIN dbo.administrative_areas area ON area.id = service_area.administrative_area_id
    WHERE service_area.administrative_area_id IS NOT NULL AND area.id IS NULL
    UNION ALL
    SELECT 1 FROM dbo.customer_addresses address
    LEFT JOIN dbo.administrative_areas area ON area.id = address.administrative_area_id
    WHERE address.administrative_area_id IS NOT NULL AND area.id IS NULL
)
    THROW 51105, 'Existing area reference verification failed.', 1;

SELECT 'PASS' AS VerificationResult;
