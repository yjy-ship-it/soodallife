param(
    [string]$ServerInstance = 'localhost',
    [string]$DatabaseName = 'SOODAL_LIFE_DEV'
)

$ErrorActionPreference = 'Stop'
Import-Module WebAdministration

$releaseRoot = $PSScriptRoot
$appsRoot = 'D:\SOODALLIFE\apps'
$backupParent = 'D:\SOODALLIFE\releases'
$databaseBackupRoot = 'D:\SOODALLIFE\DB\Backup'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot = Join-Path $backupParent "backup-before-unified-account-v34-$stamp"
$iisBackupName = "Before_UnifiedAccountV34_$stamp"
$databaseBackupPath = Join-Path $databaseBackupRoot "${DatabaseName}_PreUnifiedAccountV34_$stamp.bak"
$migrationSql = Join-Path $releaseRoot 'deployment\Apply-UnifiedAccountMigration.sql'
$verificationSql = Join-Path $releaseRoot 'deployment\Verify-UnifiedAccount.sql'

$sources = [ordered]@{
    API = Join-Path $releaseRoot 'api'
    Customer = Join-Path $releaseRoot 'frontend\customer'
    Partner = Join-Path $releaseRoot 'frontend\partner'
    Admin = Join-Path $releaseRoot 'frontend\admin'
}

$targets = [ordered]@{
    API = Join-Path $appsRoot 'api'
    Customer = Join-Path $appsRoot 'customer'
    Partner = Join-Path $appsRoot 'partner'
    Admin = Join-Path $appsRoot 'admin'
}

foreach ($name in $sources.Keys) {
    if (-not (Test-Path -LiteralPath $sources[$name])) {
        throw "Release source is missing: $($sources[$name])"
    }
}

foreach ($requiredFile in @(
    (Join-Path $sources.API 'SoodalLife.Api.dll'),
    (Join-Path $sources.API 'web.config'),
    (Join-Path $sources.Customer 'index.html'),
    (Join-Path $sources.Partner 'index.html'),
    (Join-Path $sources.Admin 'index.html'),
    $migrationSql,
    $verificationSql
)) {
    if (-not (Test-Path -LiteralPath $requiredFile)) {
        throw "Required release file is missing: $requiredFile"
    }
}

New-Item -ItemType Directory -Force -Path $backupRoot, $databaseBackupRoot | Out-Null

function Invoke-RobocopyChecked {
    param(
        [Parameter(Mandatory)] [string]$Source,
        [Parameter(Mandatory)] [string]$Destination,
        [switch]$Mirror
    )

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    $mode = if ($Mirror) { '/MIR' } else { '/E' }
    & robocopy.exe $Source $Destination $mode /R:2 /W:2 /NFL /NDL /NJH /NJS /NP
    $code = $LASTEXITCODE
    if ($code -gt 7) {
        throw "Robocopy failed ($code): $Source -> $Destination"
    }
}

function Stop-PoolSafely([string]$Name) {
    $item = Get-Item "IIS:\AppPools\$Name" -ErrorAction Stop
    if ($item.state -ne 'Stopped') {
        Stop-WebAppPool -Name $Name
        for ($attempt = 0; $attempt -lt 20; $attempt++) {
            Start-Sleep -Milliseconds 500
            if ((Get-WebAppPoolState -Name $Name).Value -eq 'Stopped') { return }
        }
        throw "Application pool did not stop: $Name"
    }
}

function Start-PoolSafely([string]$Name) {
    if ((Get-WebAppPoolState -Name $Name).Value -ne 'Started') {
        Start-WebAppPool -Name $Name
    }
}

& "$env:windir\System32\inetsrv\appcmd.exe" add backup $iisBackupName
if ($LASTEXITCODE -ne 0) { throw 'IIS configuration backup failed.' }

foreach ($name in $targets.Keys) {
    if (Test-Path -LiteralPath $targets[$name]) {
        Invoke-RobocopyChecked -Source $targets[$name] -Destination (Join-Path $backupRoot $name)
    }
}

$escapedBackupPath = $databaseBackupPath.Replace("'", "''")
$backupQuery = "BACKUP DATABASE [$DatabaseName] TO DISK=N'$escapedBackupPath' WITH COPY_ONLY, CHECKSUM, COMPRESSION, INIT, STATS=10; RESTORE VERIFYONLY FROM DISK=N'$escapedBackupPath' WITH CHECKSUM;"
& sqlcmd.exe -S $ServerInstance -E -C -b -Q $backupQuery
if ($LASTEXITCODE -ne 0) { throw 'Database backup or verification failed.' }

$poolNames = @('SoodalLife.Api', 'soodallife.kr', 'SoodalLife.Static')
try {
    foreach ($poolName in $poolNames) { Stop-PoolSafely $poolName }

    & sqlcmd.exe -S $ServerInstance -E -C -I -b -d $DatabaseName -i $migrationSql
    if ($LASTEXITCODE -ne 0) { throw 'Unified-account database migration failed.' }

    foreach ($name in $sources.Keys) {
        Invoke-RobocopyChecked -Source $sources[$name] -Destination $targets[$name] -Mirror
    }
}
finally {
    foreach ($poolName in $poolNames) {
        try { Start-PoolSafely $poolName } catch { Write-Warning $_.Exception.Message }
    }
}

& sqlcmd.exe -S $ServerInstance -E -C -I -b -d $DatabaseName -i $verificationSql
if ($LASTEXITCODE -ne 0) { throw 'Unified-account verification query failed.' }

$sites = 'soodallife.kr', 'partner.soodallife.kr', 'admin.soodallife.kr', 'api.soodallife.kr'
foreach ($siteName in $sites) {
    if ((Get-WebsiteState -Name $siteName).Value -ne 'Started') {
        Start-Website -Name $siteName
    }
}

[PSCustomObject]@{
    DeploymentSucceeded = $true
    IisBackup = $iisBackupName
    FileBackup = $backupRoot
    DatabaseBackup = $databaseBackupPath
    Migration = '20260816123000_UnifyCustomerProviderAccounts'
} | Format-List

Get-Website |
    Where-Object Name -In $sites |
    Select-Object Name, State, PhysicalPath, ApplicationPool |
    Format-Table -AutoSize
