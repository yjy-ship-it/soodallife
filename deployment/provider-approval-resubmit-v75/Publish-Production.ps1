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
$backupRoot = Join-Path $backupParent "backup-before-provider-document-resubmit-v75-$stamp"
$iisBackupName = "Before_ProviderDocumentResubmitV75_$stamp"
$databaseBackupPath = Join-Path $databaseBackupRoot "${DatabaseName}_PreProviderDocumentResubmitV75_$stamp.bak"
$migrationSql = Join-Path $releaseRoot 'deployment\Apply-ProviderApprovalResubmit.sql'
$verificationSql = Join-Path $releaseRoot 'deployment\Verify-ProviderApprovalResubmit.sql'

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

foreach ($requiredFile in @(
    (Join-Path $sources.API 'SoodalLife.Api.dll'),
    (Join-Path $sources.API 'web.config'),
    (Join-Path $sources.Customer 'index.html'),
    (Join-Path $sources.Partner 'index.html'),
    (Join-Path $sources.Admin 'index.html'),
    $migrationSql,
    $verificationSql
)) {
    if (-not (Test-Path -LiteralPath $requiredFile)) { throw "Required release file is missing: $requiredFile" }
}

New-Item -ItemType Directory -Force -Path $backupRoot,$databaseBackupRoot | Out-Null
if (-not (Get-Command sqlcmd.exe -ErrorAction SilentlyContinue)) { throw 'sqlcmd.exe is required for the verified database migration.' }

function Invoke-RobocopyChecked {
    param([Parameter(Mandatory)][string]$Source,[Parameter(Mandatory)][string]$Destination,[switch]$Mirror,[switch]$PreserveAppData)
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    $mode = if ($Mirror) { '/MIR' } else { '/E' }
    $arguments = @($Source,$Destination,$mode,'/R:2','/W:2','/NFL','/NDL','/NJH','/NJS','/NP')
    if ($PreserveAppData) { $arguments += @('/XD','App_Data') }
    & robocopy.exe @arguments
    if ($LASTEXITCODE -gt 7) { throw "Robocopy failed ($LASTEXITCODE): $Source -> $Destination" }
}

function Stop-PoolSafely([string]$Name) {
    if ((Get-WebAppPoolState -Name $Name).Value -eq 'Stopped') { return }
    Stop-WebAppPool -Name $Name
    for ($attempt=0;$attempt -lt 30;$attempt++) {
        Start-Sleep -Milliseconds 500
        if ((Get-WebAppPoolState -Name $Name).Value -eq 'Stopped') { return }
    }
    throw "Application pool did not stop: $Name"
}
function Start-PoolSafely([string]$Name) { if ((Get-WebAppPoolState -Name $Name).Value -ne 'Started') { Start-WebAppPool -Name $Name } }

& "$env:windir\System32\inetsrv\appcmd.exe" add backup $iisBackupName
if ($LASTEXITCODE -ne 0) { throw 'IIS configuration backup failed.' }
foreach ($name in $targets.Keys) {
    if (Test-Path -LiteralPath $targets[$name]) { Invoke-RobocopyChecked -Source $targets[$name] -Destination (Join-Path $backupRoot $name) }
}

$escapedBackupPath = $databaseBackupPath.Replace("'", "''")
$backupQuery = "BACKUP DATABASE [$DatabaseName] TO DISK=N'$escapedBackupPath' WITH COPY_ONLY, CHECKSUM, COMPRESSION, INIT, STATS=10; RESTORE VERIFYONLY FROM DISK=N'$escapedBackupPath' WITH CHECKSUM;"
& sqlcmd.exe -S $ServerInstance -E -C -b -Q $backupQuery
if ($LASTEXITCODE -ne 0) { throw 'Database backup or verification failed. Deployment was not started.' }

$poolNames = @('SoodalLife.Api','soodallife.kr','SoodalLife.Static')
try {
    foreach ($poolName in $poolNames) { Stop-PoolSafely $poolName }
    & sqlcmd.exe -S $ServerInstance -E -C -I -b -d $DatabaseName -i $migrationSql
    if ($LASTEXITCODE -ne 0) { throw 'Provider approval RESUBMIT database migration failed.' }
    foreach ($name in $sources.Keys) {
        if ($name -eq 'API') { Invoke-RobocopyChecked -Source $sources[$name] -Destination $targets[$name] -Mirror -PreserveAppData }
        else { Invoke-RobocopyChecked -Source $sources[$name] -Destination $targets[$name] -Mirror }
    }
    $appDataPath = Join-Path $targets.API 'App_Data'
    New-Item -ItemType Directory -Force -Path $appDataPath | Out-Null
    $appDataAcl = Get-Acl -LiteralPath $appDataPath
    $appPoolIdentity = [Security.Principal.NTAccount]::new('IIS AppPool','SoodalLife.Api')
    $appDataRule = [Security.AccessControl.FileSystemAccessRule]::new($appPoolIdentity,[Security.AccessControl.FileSystemRights]::Modify,[Security.AccessControl.InheritanceFlags]'ContainerInherit, ObjectInherit',[Security.AccessControl.PropagationFlags]::None,[Security.AccessControl.AccessControlType]::Allow)
    $appDataAcl.SetAccessRule($appDataRule)
    Set-Acl -LiteralPath $appDataPath -AclObject $appDataAcl
}
finally {
    foreach ($poolName in $poolNames) { try { Start-PoolSafely $poolName } catch { Write-Warning $_.Exception.Message } }
}

& sqlcmd.exe -S $ServerInstance -E -C -I -b -d $DatabaseName -i $verificationSql
if ($LASTEXITCODE -ne 0) { throw 'Provider approval RESUBMIT database verification failed.' }

$sites = 'soodallife.kr','partner.soodallife.kr','admin.soodallife.kr','api.soodallife.kr'
foreach ($siteName in $sites) { if ((Get-WebsiteState -Name $siteName).Value -ne 'Started') { Start-Website -Name $siteName } }

[PSCustomObject]@{
    DeploymentSucceeded = $true
    IisBackup = $iisBackupName
    FileBackup = $backupRoot
    DatabaseBackup = $databaseBackupPath
    AppDataPreserved = $true
    DatabaseChanged = $true
    Migration = '20260818090000_AllowProviderApprovalResubmitAction'
    DefaultTrustReferenceData = 'Applied only by the explicit admin action'
} | Format-List

Get-Website | Where-Object Name -In $sites | Select-Object Name,State,PhysicalPath,ApplicationPool | Format-Table -AutoSize
Write-Host 'Next: verify provider document original download and approval supplement resubmission.'



