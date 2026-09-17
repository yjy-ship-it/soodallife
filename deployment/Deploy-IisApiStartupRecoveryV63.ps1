param()

$ErrorActionPreference = 'Stop'
Import-Module WebAdministration

$releaseRoot = $PSScriptRoot
$appsRoot = 'D:\SOODALLIFE\apps'
$backupParent = 'D:\SOODALLIFE\releases'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot = Join-Path $backupParent "backup-before-api-startup-recovery-v63-$stamp"
$iisBackupName = "Before_ApiStartupRecoveryV63_$stamp"

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
    (Join-Path $sources.Admin 'index.html')
)) {
    if (-not (Test-Path -LiteralPath $requiredFile)) { throw "Required release file is missing: $requiredFile" }
}

New-Item -ItemType Directory -Force -Path $backupRoot | Out-Null

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

$poolNames = @('SoodalLife.Api','soodallife.kr','SoodalLife.Static')
try {
    foreach ($poolName in $poolNames) { Stop-PoolSafely $poolName }
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

$sites = 'soodallife.kr','partner.soodallife.kr','admin.soodallife.kr','api.soodallife.kr'
foreach ($siteName in $sites) { if ((Get-WebsiteState -Name $siteName).Value -ne 'Started') { Start-Website -Name $siteName } }

[PSCustomObject]@{
    DeploymentSucceeded = $true
    IisBackup = $iisBackupName
    FileBackup = $backupRoot
    AppDataPreserved = $true
    DatabaseChanged = $false
    DefaultTrustReferenceData = 'Initialized idempotently by API startup'
} | Format-List

Get-Website | Where-Object Name -In $sites | Select-Object Name,State,PhysicalPath,ApplicationPool | Format-Table -AutoSize
Write-Host 'Next: verify API health returns 200, then verify provider login and trust policy management.'
