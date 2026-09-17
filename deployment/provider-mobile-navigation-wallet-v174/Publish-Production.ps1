[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param([switch]$ConfirmProductionDeployment,[string]$AppsRoot='D:\SOODALLIFE\apps',[string]$BackupParent='D:\SOODALLIFE\releases')
$ErrorActionPreference='Stop'
if(-not $ConfirmProductionDeployment){throw 'Safety stop: rerun with -ConfirmProductionDeployment after checking the ZIP hash and target paths.'}
if(-not $PSCmdlet.ShouldProcess("partner app at $AppsRoot",'Deploy V174 provider mobile navigation and wallet header')){return}
$identity=[Security.Principal.WindowsIdentity]::GetCurrent();$principal=[Security.Principal.WindowsPrincipal]::new($identity)
if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Run this script from an elevated Administrator PowerShell.'}
Import-Module WebAdministration
$releaseRoot=$PSScriptRoot;$source=Join-Path $releaseRoot 'frontend\partner';$target=Join-Path $AppsRoot 'partner';$stamp=Get-Date -Format 'yyyyMMdd-HHmmss';$backupRoot=Join-Path $BackupParent "backup-before-provider-mobile-navigation-wallet-v174-$stamp";$iisBackupName="Before_ProviderMobileNavigationWalletV174_$stamp"
$required=@((Join-Path $source 'index.html'),(Join-Path $source 'sw.js'),(Join-Path $source 'web.config'),(Join-Path $releaseRoot 'RELEASE-MANIFEST.txt'));foreach($file in $required){if(-not(Test-Path -LiteralPath $file -PathType Leaf)){throw "Required V174 release file is missing: $file"}}
$js=Get-ChildItem -LiteralPath (Join-Path $source 'assets') -Filter 'index-*.js' -File;$css=Get-ChildItem -LiteralPath (Join-Path $source 'assets') -Filter 'index-*.css' -File
if($js.Count-ne 1-or$css.Count-ne 1){throw "Exactly one partner JS and CSS bundle is required. JS=$($js.Count), CSS=$($css.Count)"}
$jsText=[IO.File]::ReadAllText($js[0].FullName,[Text.Encoding]::UTF8);$cssText=[IO.File]::ReadAllText($css[0].FullName,[Text.Encoding]::UTF8)
foreach($marker in @('providerBottomNavIcon','providerBrandMobileWallet','providerBottomNavLabel')){if(-not$jsText.Contains($marker)){throw "V174 partner JavaScript marker missing: $marker"};if(-not$cssText.Contains($marker)){throw "V174 partner CSS marker missing: $marker"}}
$sw=[IO.File]::ReadAllText((Join-Path $source 'sw.js'),[Text.Encoding]::UTF8);$web=[IO.File]::ReadAllText((Join-Path $source 'web.config'),[Text.Encoding]::UTF8)
if(-not$sw.Contains('soodal-life-shell-v4')){throw 'V174 service-worker cache marker missing.'};if(-not$web.Contains('no-store, no-cache, must-revalidate')){throw 'V174 no-store marker missing.'}
New-Item -ItemType Directory -Force -Path $backupRoot|Out-Null
function Copy-Checked([string]$Source,[string]$Destination,[switch]$Mirror){New-Item -ItemType Directory -Force -Path $Destination|Out-Null;$arguments=@($Source,$Destination,($(if($Mirror){'/MIR'}else{'/E'})),'/R:2','/W:2','/NFL','/NDL','/NJH','/NJS','/NP');& robocopy.exe @arguments;if($LASTEXITCODE-gt 7){throw "Robocopy failed ($LASTEXITCODE): $Source -> $Destination"}}
function Get-PoolState([string]$Name){try{return(Get-WebAppPoolState -Name $Name -ErrorAction Stop).Value}catch{return $null}}
function Stop-Pool([string]$Name){if(-not(Test-Path "IIS:\AppPools\$Name")){return};if((Get-PoolState $Name)-ne 'Stopped'){Stop-WebAppPool -Name $Name};for($i=0;$i-lt 40;$i++){if((Get-PoolState $Name)-eq 'Stopped'){return};Start-Sleep -Milliseconds 500};throw "Application pool did not stop: $Name"}
function Start-Pool([string]$Name){if(-not(Test-Path "IIS:\AppPools\$Name")){return};for($i=0;$i-lt 40;$i++){if((Get-PoolState $Name)-eq 'Started'){return};try{Start-WebAppPool -Name $Name}catch{};Start-Sleep -Milliseconds 750};throw "Application pool did not start: $Name"}
& "$env:windir\System32\inetsrv\appcmd.exe" add backup $iisBackupName;if($LASTEXITCODE-ne 0){throw 'IIS configuration backup failed.'}
if(Test-Path -LiteralPath $target){Copy-Checked $target (Join-Path $backupRoot 'partner')}
$pool='SoodalLife.Static';$deploymentError=$null
try{Stop-Pool $pool;Copy-Checked $source $target -Mirror}catch{$deploymentError=$_;Write-Warning "V174 deployment stopped. File backup: $backupRoot";$backup=Join-Path $backupRoot 'partner';if(Test-Path -LiteralPath $backup){Copy-Checked $backup $target -Mirror}}finally{try{Start-Pool $pool}catch{Write-Warning $_.Exception.Message}}
if($deploymentError){throw $deploymentError}
$health=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/health' -TimeoutSec 30;$ready=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/ready' -TimeoutSec 30;$partner=Invoke-WebRequest -Uri 'https://partner.soodallife.kr/provider' -UseBasicParsing -TimeoutSec 30
$deployedJs=Get-ChildItem -LiteralPath (Join-Path $target 'assets') -Filter 'index-*.js' -File;$deployedCss=Get-ChildItem -LiteralPath (Join-Path $target 'assets') -Filter 'index-*.css' -File
if($deployedJs.Count-ne 1-or$deployedCss.Count-ne 1){throw 'Deployed V174 bundle count is invalid.'};$deployedJsText=[IO.File]::ReadAllText($deployedJs[0].FullName,[Text.Encoding]::UTF8);$deployedCssText=[IO.File]::ReadAllText($deployedCss[0].FullName,[Text.Encoding]::UTF8);foreach($marker in @('providerBottomNavIcon','providerBrandMobileWallet','providerBottomNavLabel')){if(-not$deployedJsText.Contains($marker)-or-not$deployedCssText.Contains($marker)){throw "V174 deployed marker missing: $marker"}}
if($health.status-ne'ok'-or$ready.status-ne'ready'-or$partner.StatusCode-ne 200){throw 'Post-deployment verification failed.'}
[PSCustomObject]@{DeploymentSucceeded=$true;IisBackup=$iisBackupName;FileBackup=$backupRoot;DatabaseChanged=$false;ApiChanged=$false;Health=$health.status;Readiness=$ready.status;ProviderMobileNavigation='five-svg-icons';ProviderMobileBrand='approved-wallet-balance';PartnerApp='ok'}|Format-List
Write-Host 'V174 provider mobile navigation and wallet header deployment completed.' -ForegroundColor Green
Write-Host 'API, database schema/data and production settings were not changed.' -ForegroundColor Yellow
