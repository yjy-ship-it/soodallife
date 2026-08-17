param(
    [string]$SourceCsv = (Join-Path $PSScriptRoot 'source\MOLIT_Beopjeongdong_20260630.csv'),
    [string]$OutputSql = (Join-Path $PSScriptRoot 'Load-AdministrativeAreas-20260630.sql')
)

$ErrorActionPreference = 'Stop'

function SqlText([string]$value) {
    if ($null -eq $value) { return 'NULL' }
    return "N'$($value.Replace("'", "''"))'"
}

function SqlDate([string]$value) {
    if ([string]::IsNullOrWhiteSpace($value)) { return 'NULL' }
    return "CONVERT(date, '$value', 23)"
}

$rows = @(Get-Content -LiteralPath $SourceCsv -Encoding Default | Select-Object -Skip 1 |
    ConvertFrom-Csv -Header Code,Sido,Sigungu,Eupmyeondong,Ri,Sequence,CreatedDate)
$sidos = @($rows | Where-Object {
    [string]::IsNullOrWhiteSpace($_.Sigungu) -and
    [string]::IsNullOrWhiteSpace($_.Eupmyeondong) -and
    [string]::IsNullOrWhiteSpace($_.Ri)
})
$allSigungu = @($rows | Where-Object {
    -not [string]::IsNullOrWhiteSpace($_.Sigungu) -and
    [string]::IsNullOrWhiteSpace($_.Eupmyeondong) -and
    [string]::IsNullOrWhiteSpace($_.Ri)
})

# The source contains both autonomous cities and their non-autonomous districts.
# Service areas use base municipalities, plus Sejong and Jeju administrative cities.
$cityNamesBySido = @{}
foreach ($group in ($allSigungu | Group-Object Sido)) {
    $cityNamesBySido[$group.Name] = @($group.Group | Where-Object { $_.Sigungu.EndsWith([string][char]0xC2DC) } | ForEach-Object Sigungu)
}
$nonAutonomousDistricts = @($allSigungu | Where-Object {
    $candidate = $_
    @($cityNamesBySido[$candidate.Sido] | Where-Object {
        $candidate.Sigungu -ne $_ -and $candidate.Sigungu.StartsWith($_)
    }).Count -gt 0
})
$serviceAreas = @($allSigungu | Where-Object { $nonAutonomousDistricts.Code -notcontains $_.Code })

if ($sidos.Count -ne 16) { throw "Expected 16 SIDO rows, found $($sidos.Count)." }
if ($allSigungu.Count -ne 269) { throw "Expected 269 legal SIGUNGU rows, found $($allSigungu.Count)." }
if ($nonAutonomousDistricts.Count -ne 39) { throw "Expected 39 non-autonomous districts, found $($nonAutonomousDistricts.Count)." }
if ($serviceAreas.Count -ne 230) { throw "Expected 230 service areas, found $($serviceAreas.Count)." }

$duplicates = @($serviceAreas | Group-Object Code | Where-Object Count -gt 1)
if ($duplicates.Count -gt 0) { throw 'Duplicate service-area codes were found.' }

$sourceRows = @()
foreach ($sido in $sidos) {
    $sourceRows += [pscustomobject]@{
        AreaCode = $sido.Code
        AreaName = $sido.Sido
        Level = 'SIDO'
        ParentCode = $null
        CreatedDate = $sido.CreatedDate
    }
}
foreach ($area in $serviceAreas) {
    $parent = $sidos | Where-Object Sido -eq $area.Sido | Select-Object -First 1
    if ($null -eq $parent) { throw "No SIDO parent found for $($area.Sido) $($area.Sigungu)." }
    $sourceRows += [pscustomobject]@{
        AreaCode = $area.Code
        AreaName = $area.Sigungu
        Level = 'SIGUNGU'
        ParentCode = $parent.Code
        CreatedDate = $area.CreatedDate
    }
}

$values = for ($index = 0; $index -lt $sourceRows.Count; $index++) {
    $row = $sourceRows[$index]
    $suffix = if ($index -eq $sourceRows.Count - 1) { ';' } else { ',' }
    $parentSql = if ($null -eq $row.ParentCode) { 'NULL' } else { "'$($row.ParentCode)'" }
    "    ('{0}', {1}, '{2}', {3}, {4}){5}" -f $row.AreaCode, (SqlText $row.AreaName), $row.Level, $parentSql, (SqlDate $row.CreatedDate), $suffix
}

$template = @'
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
__VALUES__

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
'@

$sql = $template.Replace('__VALUES__', ($values -join [Environment]::NewLine))
$utf8WithBom = [System.Text.UTF8Encoding]::new($true)
[System.IO.File]::WriteAllText($OutputSql, $sql, $utf8WithBom)

[pscustomobject]@{
    SourceRows = $rows.Count
    SidoRows = $sidos.Count
    LegalSigunguRows = $allSigungu.Count
    ExcludedNonAutonomousDistricts = $nonAutonomousDistricts.Count
    ServiceAreaRows = $serviceAreas.Count
    OutputSql = (Resolve-Path $OutputSql).Path
}
