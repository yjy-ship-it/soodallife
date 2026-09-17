[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [switch]$ConfirmProductionDeployment,
    [string]$AppsRoot = 'D:\SOODALLIFE\apps',
    [string]$BackupParent = 'D:\SOODALLIFE\releases'
)

$ErrorActionPreference = 'Stop'
if (-not $ConfirmProductionDeployment) { throw 'Safety stop: add -ConfirmProductionDeployment after checking the package and target paths.' }
$expected = [IO.Path]::GetFullPath('D:\SOODALLIFE\apps').TrimEnd('\')
$resolved = [IO.Path]::GetFullPath($AppsRoot).TrimEnd('\')
if ($resolved -ne $expected) { throw "Unexpected AppsRoot. Expected=$expected, Actual=$resolved" }
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Run Windows PowerShell as Administrator.' }

$root = $PSScriptRoot
$sources = [ordered]@{
    API = Join-Path $root 'api'
    Customer = Join-Path $root 'frontend\customer'
    Partner = Join-Path $root 'frontend\partner'
    Admin = Join-Path $root 'frontend\admin'
}
$targets = [ordered]@{
    API = Join-Path $resolved 'api'
    Customer = Join-Path $resolved 'customer'
    Partner = Join-Path $resolved 'partner'
    Admin = Join-Path $resolved 'admin'
}
foreach ($path in $targets.Values) {
    $full = [IO.Path]::GetFullPath($path)
    if (-not $full.StartsWith($resolved + '\', [StringComparison]::OrdinalIgnoreCase)) { throw "Target escaped AppsRoot: $full" }
}
$required = @(
    (Join-Path $sources.API 'SoodalLife.Api.dll'),
    (Join-Path $sources.API 'web.config'),
    (Join-Path $sources.API 'V237-API-MARKER.txt'),
    (Join-Path $sources.Customer 'index.html'),
    (Join-Path $sources.Partner 'index.html'),
    (Join-Path $sources.Admin 'index.html')
)
foreach ($file in $required) { if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Required V237 file missing: $file" } }
if (Get-ChildItem -LiteralPath $sources.API -Filter 'appsettings*.json' -File -ErrorAction SilentlyContinue) { throw 'Package must not contain appsettings files.' }
if (-not $PSCmdlet.ShouldProcess("API, customer, partner and admin apps at $resolved", 'Deploy V237 image upload security policy')) { return }

Import-Module WebAdministration
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot = Join-Path $BackupParent "backup-before-image-upload-policy-v236-$stamp"
$iisBackup = "Before_ImageUploadPolicyV237_$stamp"
New-Item -ItemType Directory -Force -Path $backupRoot | Out-Null

function Copy-Checked {
    param([string]$Source, [string]$Destination, [switch]$Mirror, [switch]$PreserveApiData)
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    $arguments = @($Source, $Destination, $(if ($Mirror) { '/MIR' } else { '/E' }), '/R:2', '/W:2', '/NFL', '/NDL', '/NJH', '/NJS', '/NP')
    if ($PreserveApiData) { $arguments += @('/XD', 'App_Data', '/XF', 'appsettings*.json') }
    & robocopy.exe @arguments
    if ($LASTEXITCODE -gt 7) { throw "Robocopy failed: $Source" }
}
function Get-PoolStateSafe([string]$Name) { try { (Get-WebAppPoolState -Name $Name -ErrorAction Stop).Value } catch { $null } }
function Stop-PoolSafe([string]$Name) {
    if (-not (Test-Path "IIS:\AppPools\$Name")) { return }
    if ((Get-PoolStateSafe $Name) -ne 'Stopped') { Stop-WebAppPool $Name }
    for ($i = 0; $i -lt 40; $i++) { if ((Get-PoolStateSafe $Name) -eq 'Stopped') { return }; Start-Sleep -Milliseconds 500 }
    throw "Pool stop timeout: $Name"
}
function Start-PoolSafe([string]$Name) {
    if (-not (Test-Path "IIS:\AppPools\$Name")) { return }
    for ($i = 0; $i -lt 40; $i++) { if ((Get-PoolStateSafe $Name) -eq 'Started') { return }; try { Start-WebAppPool $Name } catch {}; Start-Sleep -Milliseconds 750 }
    throw "Pool start timeout: $Name"
}

& "$env:windir\System32\inetsrv\appcmd.exe" add backup $iisBackup
if ($LASTEXITCODE -ne 0) { throw 'IIS backup failed.' }
foreach ($name in $targets.Keys) { if (Test-Path -LiteralPath $targets[$name]) { Copy-Checked -Source $targets[$name] -Destination (Join-Path $backupRoot $name) } }
$pools = @('SoodalLife.Api', 'SoodalLife.Static')
$failure = $null
try {
    foreach ($pool in $pools) { Stop-PoolSafe $pool }
    Copy-Checked -Source $sources.API -Destination $targets.API -Mirror -PreserveApiData
    Copy-Checked -Source $sources.Customer -Destination $targets.Customer -Mirror
    Copy-Checked -Source $sources.Partner -Destination $targets.Partner -Mirror
    Copy-Checked -Source $sources.Admin -Destination $targets.Admin -Mirror
    New-Item -ItemType Directory -Force -Path (Join-Path $targets.API 'App_Data\diagnostics') | Out-Null
} catch {
    $failure = $_
    Write-Warning "V237 deployment stopped. Restoring files from $backupRoot."
    foreach ($name in $targets.Keys) {
        $saved = Join-Path $backupRoot $name
        if (Test-Path -LiteralPath $saved) { Copy-Checked -Source $saved -Destination $targets[$name] -Mirror -PreserveApiData:($name -eq 'API') }
    }
} finally {
    foreach ($pool in $pools) { try { Start-PoolSafe $pool } catch { Write-Warning $_.Exception.Message } }
}
if ($failure) { throw $failure }

$health = Invoke-RestMethod 'https://api.soodallife.kr/api/v1/system/health' -TimeoutSec 30
$ready = Invoke-RestMethod 'https://api.soodallife.kr/api/v1/system/ready' -TimeoutSec 30
$customer = Invoke-WebRequest 'https://soodallife.kr/' -UseBasicParsing -TimeoutSec 30
$partner = Invoke-WebRequest 'https://partner.soodallife.kr/' -UseBasicParsing -TimeoutSec 30
$admin = Invoke-WebRequest 'https://admin.soodallife.kr/' -UseBasicParsing -TimeoutSec 30
$marker = (Get-Content -LiteralPath (Join-Path $targets.API 'V237-API-MARKER.txt') -Raw -Encoding UTF8).Contains('Image reencoding: enabled')
if ($health.status -ne 'ok' -or $ready.status -ne 'ready' -or $customer.StatusCode -ne 200 -or $partner.StatusCode -ne 200 -or $admin.StatusCode -ne 200 -or -not $marker) { throw 'Post-deployment verification failed.' }

[PSCustomObject]@{
    DeploymentSucceeded = $true
    IisBackup = $iisBackup
    FileBackup = $backupRoot
    DatabaseChanged = $false
    ApiChanged = $true
    CustomerFrontendChanged = $true
    PartnerFrontendChanged = $true
    AdminFrontendChanged = $true
    UploadTypes = 'jpg-jpeg-png-only'
    UploadMaximum = '5MB'
    ImageReencoding = 'enabled'
    MetadataRemoval = 'enabled'
    RandomStorageNames = 'enabled'
    FullAdministrativeArea = 'province-and-district'
    AcceptedQuoteDetail = 'expanded'
    CompletionPhotos = 'before-and-after-separated'
    ChatAvailability = 'transaction-wide'
    ChatAttachmentSafety = 'fail-closed-with-guidance'
    ProviderCalendar = 'monthly-auto-schedule'
    AppDataPreserved = $true
    AppSettingsPreserved = $true
    CustomerApp = 'ok'
    PartnerApp = 'ok'
    AdminApp = 'ok'
    Health = $health.status
    Readiness = $ready.status
} | Format-List
Write-Host 'V237 image upload security policy deployment completed.' -ForegroundColor Green
Write-Host 'Database, API App_Data and production settings were not changed.' -ForegroundColor Yellow
