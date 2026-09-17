[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [switch]$ConfirmProductionDeployment,
    [string]$AppsRoot='D:\SOODALLIFE\apps',
    [string]$BackupParent='D:\SOODALLIFE\releases'
)
$ErrorActionPreference='Stop'
if(-not $ConfirmProductionDeployment){throw 'Safety stop: rerun with -ConfirmProductionDeployment after checking the ZIP hash and target paths.'}
if(-not $PSCmdlet.ShouldProcess("customer, partner and admin apps at $AppsRoot",'Deploy V166 navigation menu reliability')){return}
Import-Module WebAdministration
$releaseRoot=$PSScriptRoot;$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot=Join-Path $BackupParent "backup-before-navigation-menu-reliability-v166-$stamp"
$iisBackupName="Before_NavigationMenuReliabilityV166_$stamp"
$sources=[ordered]@{Customer=Join-Path $releaseRoot 'frontend\customer';Partner=Join-Path $releaseRoot 'frontend\partner';Admin=Join-Path $releaseRoot 'frontend\admin'}
$targets=[ordered]@{Customer=Join-Path $AppsRoot 'customer';Partner=Join-Path $AppsRoot 'partner';Admin=Join-Path $AppsRoot 'admin'}
$required=@((Join-Path $sources.Customer 'index.html'),(Join-Path $sources.Partner 'index.html'),(Join-Path $sources.Admin 'index.html'),(Join-Path $releaseRoot 'RELEASE-MANIFEST.txt'))
$missing=@($required|Where-Object{-not(Test-Path -LiteralPath $_ -PathType Leaf)});if($missing.Count){throw "Required release files are missing: $($missing-join ', ')"}
$sourceJs=Get-ChildItem -LiteralPath (Join-Path $sources.Customer 'assets') -Filter 'index-*.js' -File
if($sourceJs.Count-ne 1){throw "Exactly one customer application JavaScript file is required. Found: $($sourceJs.Count)"}
$sourceText=[IO.File]::ReadAllText($sourceJs[0].FullName,[Text.Encoding]::UTF8)
foreach($marker in @('providerLogoutButton','customerNavGroup','providerMenuBackdrop','customerMenuBackdrop')){if(-not $sourceText.Contains($marker)){throw "V166-R2 frontend marker missing: $marker"}}
New-Item -ItemType Directory -Force -Path $backupRoot|Out-Null
function Copy-Checked([string]$Source,[string]$Destination,[switch]$Mirror){New-Item -ItemType Directory -Force -Path $Destination|Out-Null;$arguments=@($Source,$Destination,($(if($Mirror){'/MIR'}else{'/E'})),'/R:2','/W:2','/NFL','/NDL','/NJH','/NJS','/NP');& robocopy.exe @arguments;if($LASTEXITCODE-gt 7){throw "Robocopy failed ($LASTEXITCODE): $Source -> $Destination"}}
function Get-PoolState([string]$Name){try{return(Get-WebAppPoolState -Name $Name -ErrorAction Stop).Value}catch{return $null}}
function Stop-Pool([string]$Name){if(-not(Test-Path "IIS:\AppPools\$Name")){return};if((Get-PoolState $Name)-ne 'Stopped'){Stop-WebAppPool -Name $Name};for($i=0;$i-lt 40;$i++){if((Get-PoolState $Name)-eq 'Stopped'){return};Start-Sleep -Milliseconds 500};throw "Application pool did not stop: $Name"}
function Start-Pool([string]$Name){if(-not(Test-Path "IIS:\AppPools\$Name")){return};for($i=0;$i-lt 40;$i++){if((Get-PoolState $Name)-eq 'Started'){return};try{Start-WebAppPool -Name $Name}catch{};Start-Sleep -Milliseconds 750};throw "Application pool did not start: $Name"}
& "$env:windir\System32\inetsrv\appcmd.exe" add backup $iisBackupName;if($LASTEXITCODE-ne 0){throw 'IIS configuration backup failed.'}
foreach($name in $targets.Keys){if(Test-Path -LiteralPath $targets[$name]){Copy-Checked $targets[$name] (Join-Path $backupRoot $name)}}
$pools=@('soodallife.kr','SoodalLife.Static');$deploymentError=$null
try{
    foreach($pool in $pools){Stop-Pool $pool}
    foreach($name in $targets.Keys){Copy-Checked $sources[$name] $targets[$name] -Mirror}
}catch{
    $deploymentError=$_;Write-Warning "V166 deployment stopped. File backup: $backupRoot"
    foreach($name in $targets.Keys){$backup=Join-Path $backupRoot $name;if(Test-Path $backup){Copy-Checked $backup $targets[$name] -Mirror}}
}finally{foreach($pool in $pools){try{Start-Pool $pool}catch{Write-Warning $_.Exception.Message}}}
if($deploymentError){throw $deploymentError}
$health=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/health' -TimeoutSec 30;$ready=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/ready' -TimeoutSec 30
$customer=Invoke-WebRequest -Uri 'https://soodallife.kr' -UseBasicParsing -TimeoutSec 30;$partner=Invoke-WebRequest -Uri 'https://partner.soodallife.kr/provider' -UseBasicParsing -TimeoutSec 30;$helpRoom=Invoke-WebRequest -Uri 'https://soodallife.kr/help-room' -UseBasicParsing -TimeoutSec 30;$admin=Invoke-WebRequest -Uri 'https://admin.soodallife.kr' -UseBasicParsing -TimeoutSec 30
$deployedJs=Get-ChildItem -LiteralPath (Join-Path $targets.Customer 'assets') -Filter 'index-*.js' -File
if($deployedJs.Count-ne 1){throw "Exactly one deployed customer application JavaScript file is required. Found: $($deployedJs.Count)"}
$deployedText=[IO.File]::ReadAllText($deployedJs[0].FullName,[Text.Encoding]::UTF8)
foreach($marker in @('providerLogoutButton','customerNavGroup','providerMenuBackdrop','customerMenuBackdrop')){if(-not $deployedText.Contains($marker)){throw "V166-R2 deployed frontend marker missing: $marker"}}
if($health.status-ne 'ok'-or $ready.status-ne 'ready'-or $customer.StatusCode-ne 200-or $partner.StatusCode-ne 200-or $helpRoom.StatusCode-ne 200-or $admin.StatusCode-ne 200){throw 'Post-deployment verification failed.'}
[PSCustomObject]@{DeploymentSucceeded=$true;IisBackup=$iisBackupName;FileBackup=$backupRoot;DatabaseChanged=$false;ApiChanged=$false;Health=$health.status;Readiness=$ready.status;PopupDismissal='outside-click-x-escape-route';CustomerHelpRoom='community-support';ProviderHelpRoom='community-support';ProviderDesktopLogout='ok'}|Format-List
Write-Host 'V166 customer and provider navigation menu reliability deployment completed.' -ForegroundColor Green
Write-Host 'Help room and suggestion box are under Community and Support. Database, API and production settings were not changed.' -ForegroundColor Yellow
