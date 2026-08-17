param(
    [string]$SqlInstance = 'localhost',
    [string]$BackupDirectory = 'D:\SOODALLIFE\DB\Backup'
)

$ErrorActionPreference = 'Stop'
$database = 'SOODAL_LIFE_DEV'
$loadSql = Join-Path $PSScriptRoot 'Load-AdministrativeAreas-20260630.sql'
$verifySql = Join-Path $PSScriptRoot 'Verify-AdministrativeAreas.sql'
$expectedLoadHash = '85D5D351F411F7800E3EE4225B14BA235B8B9614BD03D644A8A0E30020DDB011'

$sqlcmd = (Get-Command sqlcmd.exe -ErrorAction Stop).Source
if (-not (Test-Path -LiteralPath $loadSql)) { throw "Load SQL not found: $loadSql" }
if (-not (Test-Path -LiteralPath $verifySql)) { throw "Verify SQL not found: $verifySql" }

$actualLoadHash = (Get-FileHash -LiteralPath $loadSql -Algorithm SHA256).Hash
if ($actualLoadHash -ne $expectedLoadHash) {
    throw "Load SQL hash mismatch. Expected $expectedLoadHash, found $actualLoadHash."
}

New-Item -ItemType Directory -Force -Path $BackupDirectory | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupPath = Join-Path $BackupDirectory "SOODAL_LIFE_DEV_PreAdministrativeAreas_$stamp.bak"

Write-Host '=== Target database identity ==='
& $sqlcmd -b -S $SqlInstance -E -d $database -Q "SET NOCOUNT ON; SELECT DB_NAME() AS DatabaseName;"
if ($LASTEXITCODE -ne 0) { throw 'Database identity check failed.' }

Write-Host '=== Before counts ==='
& $sqlcmd -b -S $SqlInstance -E -d $database -Q "SET NOCOUNT ON; SELECT area_level_code, COUNT(*) AS TotalCount, SUM(CASE WHEN is_active=1 THEN 1 ELSE 0 END) AS ActiveCount FROM dbo.administrative_areas GROUP BY area_level_code ORDER BY area_level_code;"
if ($LASTEXITCODE -ne 0) { throw 'Pre-load count check failed.' }

Write-Host '=== Backup and verification ==='
$escapedBackupPath = $backupPath.Replace("'", "''")
& $sqlcmd -b -S $SqlInstance -E -d master -Q "BACKUP DATABASE [SOODAL_LIFE_DEV] TO DISK=N'$escapedBackupPath' WITH COPY_ONLY, CHECKSUM, COMPRESSION, INIT, STATS=10;"
if ($LASTEXITCODE -ne 0) { throw 'Database backup failed.' }
& $sqlcmd -b -S $SqlInstance -E -d master -Q "RESTORE VERIFYONLY FROM DISK=N'$escapedBackupPath' WITH CHECKSUM;"
if ($LASTEXITCODE -ne 0) { throw 'Database backup verification failed.' }

Write-Host '=== Load administrative areas ==='
& $sqlcmd -b -S $SqlInstance -E -d $database -f 65001 -i $loadSql
if ($LASTEXITCODE -ne 0) { throw 'Administrative-area load failed. The SQL transaction was rolled back.' }

Write-Host '=== Post-load verification ==='
& $sqlcmd -b -S $SqlInstance -E -d $database -f 65001 -i $verifySql
if ($LASTEXITCODE -ne 0) { throw 'Post-load verification failed.' }

Write-Host "BackupPath: $backupPath"
Write-Host "LoadSqlHash: $actualLoadHash"
Write-Host 'Administrative-area development DB load completed.'
