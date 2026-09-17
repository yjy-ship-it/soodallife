[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [switch]$ConfirmProductionDeployment,
    [string]$AppsRoot='D:\SOODALLIFE\apps',
    [string]$BackupParent='D:\SOODALLIFE\releases'
)

$ErrorActionPreference='Stop'
if(-not $ConfirmProductionDeployment){throw 'Safety stop: rerun with -ConfirmProductionDeployment after checking the ZIP hash and target paths.'}
if(-not $PSCmdlet.ShouldProcess("API, customer, partner and admin apps at $AppsRoot",'Deploy V178 emergency category policy separation')){return}
$identity=[Security.Principal.WindowsIdentity]::GetCurrent()
$principal=[Security.Principal.WindowsPrincipal]::new($identity)
if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Run PowerShell as Administrator and try again.'}

Import-Module WebAdministration
$releaseRoot=$PSScriptRoot
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot=Join-Path $BackupParent "backup-before-emergency-category-policy-v178-$stamp"
$iisBackupName="Before_EmergencyCategoryPolicyV178_$stamp"
$sources=[ordered]@{API=Join-Path $releaseRoot 'api';Customer=Join-Path $releaseRoot 'frontend\customer';Partner=Join-Path $releaseRoot 'frontend\partner';Admin=Join-Path $releaseRoot 'frontend\admin'}
$targets=[ordered]@{API=Join-Path $AppsRoot 'api';Customer=Join-Path $AppsRoot 'customer';Partner=Join-Path $AppsRoot 'partner';Admin=Join-Path $AppsRoot 'admin'}
$required=@((Join-Path $sources.API 'SoodalLife.Api.dll'),(Join-Path $sources.API 'web.config'),(Join-Path $sources.Customer 'index.html'),(Join-Path $sources.Partner 'index.html'),(Join-Path $sources.Admin 'index.html'),(Join-Path $releaseRoot 'RELEASE-MANIFEST.txt'))
foreach($file in $required){if(-not(Test-Path -LiteralPath $file -PathType Leaf)){throw "Required V178 release file is missing: $file"}}
if(Test-Path -LiteralPath (Join-Path $sources.API 'appsettings.json')){throw 'V178 package unexpectedly contains appsettings.json.'}

$customerJs=Get-ChildItem -LiteralPath (Join-Path $sources.Customer 'assets') -Filter 'index-*.js' -File
if($customerJs.Count-ne 1){throw "Exactly one V178 customer JavaScript bundle is required. Found: $($customerJs.Count)"}
$customerText=[IO.File]::ReadAllText($customerJs[0].FullName,[Text.Encoding]::UTF8)
foreach($marker in @('emergencyOnly=true','SOODAL EMERGENCY')){if(-not$customerText.Contains($marker)){throw "V178 customer marker missing: $marker"}}
foreach($portal in @('customer','partner','admin')){
    $portalRoot=Join-Path $releaseRoot "frontend\$portal"
    $sw=[IO.File]::ReadAllText((Join-Path $portalRoot 'sw.js'),[Text.Encoding]::UTF8)
    $web=[IO.File]::ReadAllText((Join-Path $portalRoot 'web.config'),[Text.Encoding]::UTF8)
    if(-not$sw.Contains('soodal-life-shell-v4')){throw "V178 service-worker cache marker missing: $portal"}
    if(-not$web.Contains('no-store, no-cache, must-revalidate')){throw "V178 no-store marker missing: $portal"}
}

New-Item -ItemType Directory -Force -Path $backupRoot|Out-Null
function Copy-Checked([string]$Source,[string]$Destination,[switch]$Mirror,[switch]$PreserveApiData){
    New-Item -ItemType Directory -Force -Path $Destination|Out-Null
    $arguments=@($Source,$Destination,($(if($Mirror){'/MIR'}else{'/E'})),'/R:2','/W:2','/NFL','/NDL','/NJH','/NJS','/NP')
    if($PreserveApiData){$arguments+=@('/XD','App_Data','/XF','appsettings.json','appsettings.Production.json','appsettings.Development.json')}
    & robocopy.exe @arguments
    if($LASTEXITCODE-gt 7){throw "Robocopy failed ($LASTEXITCODE): $Source -> $Destination"}
}
function Get-PoolState([string]$Name){try{return(Get-WebAppPoolState -Name $Name -ErrorAction Stop).Value}catch{return $null}}
function Stop-Pool([string]$Name){if(-not(Test-Path "IIS:\AppPools\$Name")){return};if((Get-PoolState $Name)-ne 'Stopped'){Stop-WebAppPool -Name $Name};for($i=0;$i-lt 40;$i++){if((Get-PoolState $Name)-eq 'Stopped'){return};Start-Sleep -Milliseconds 500};throw "Application pool did not stop: $Name"}
function Start-Pool([string]$Name){if(-not(Test-Path "IIS:\AppPools\$Name")){return};for($i=0;$i-lt 40;$i++){if((Get-PoolState $Name)-eq 'Started'){return};try{Start-WebAppPool -Name $Name}catch{};Start-Sleep -Milliseconds 750};throw "Application pool did not start: $Name"}

& "$env:windir\System32\inetsrv\appcmd.exe" add backup $iisBackupName
if($LASTEXITCODE-ne 0){throw 'IIS configuration backup failed.'}
foreach($name in $targets.Keys){if(Test-Path -LiteralPath $targets[$name]){Copy-Checked $targets[$name] (Join-Path $backupRoot $name)}}
$pools=@('SoodalLife.Api','soodallife.kr','SoodalLife.Static')
$deploymentError=$null
try{
    foreach($pool in $pools){Stop-Pool $pool}
    foreach($name in $targets.Keys){Copy-Checked $sources[$name] $targets[$name] -Mirror -PreserveApiData:($name-eq'API')}
    New-Item -ItemType Directory -Force -Path (Join-Path $targets.API 'App_Data')|Out-Null
}catch{
    $deploymentError=$_
    Write-Warning "V178 deployment stopped. File backup: $backupRoot"
    foreach($name in $targets.Keys){$backup=Join-Path $backupRoot $name;if(Test-Path -LiteralPath $backup){Copy-Checked $backup $targets[$name] -Mirror}}
}finally{foreach($pool in $pools){try{Start-Pool $pool}catch{Write-Warning $_.Exception.Message}}}
if($deploymentError){throw $deploymentError}

$health=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/health' -TimeoutSec 30
$ready=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/ready' -TimeoutSec 30
$customer=Invoke-WebRequest -Uri 'https://soodallife.kr/customer/requests/new?emergency=true' -UseBasicParsing -TimeoutSec 30
$partner=Invoke-WebRequest -Uri 'https://partner.soodallife.kr/provider' -UseBasicParsing -TimeoutSec 30
$admin=Invoke-WebRequest -Uri 'https://admin.soodallife.kr' -UseBasicParsing -TimeoutSec 30
if($health.status-ne'ok'-or$ready.status-ne'ready'-or$customer.StatusCode-ne 200-or$partner.StatusCode-ne 200-or$admin.StatusCode-ne 200){throw 'Post-deployment verification failed.'}
[PSCustomObject]@{DeploymentSucceeded=$true;IisBackup=$iisBackupName;FileBackup=$backupRoot;AppDataPreserved=$true;AppSettingsPreserved=$true;DatabaseChanged=$false;ApiChanged=$true;Health=$health.status;Readiness=$ready.status;EmergencyCategoryCatalog='policy-allowed-stable';EmergencyProviderAvailability='matching-time-only';EmergencyPolicyEnforcement='request-and-matching';CustomerApp='ok';PartnerApp='ok';AdminApp='ok'}|Format-List
Write-Host 'V178 emergency category policy deployment completed.' -ForegroundColor Green
Write-Host 'Policy-allowed emergency categories remain visible; provider availability is evaluated only during matching.' -ForegroundColor Yellow
Write-Host 'Database schema/data and production settings were not changed.' -ForegroundColor Yellow
