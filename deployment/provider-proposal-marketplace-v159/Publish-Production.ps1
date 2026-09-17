[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [switch]$ConfirmProductionDeployment,
    [string]$ServerInstance='localhost',
    [string]$DatabaseName='SOODAL_LIFE_DEV',
    [string]$AppsRoot='D:\SOODALLIFE\apps',
    [string]$BackupParent='D:\SOODALLIFE\releases',
    [string]$DatabaseBackupRoot='D:\SOODALLIFE\DB\Backup'
)
$ErrorActionPreference='Stop'
if(-not $ConfirmProductionDeployment){throw 'Safety stop: rerun with -ConfirmProductionDeployment after checking the ZIP hash, database target and backup paths.'}
if(-not $PSCmdlet.ShouldProcess("API, customer, partner and admin apps at $AppsRoot plus database $DatabaseName",'Deploy V159 expert offer and group recruitment marketplace')){return}
Import-Module WebAdministration
$releaseRoot=$PSScriptRoot;$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot=Join-Path $BackupParent "backup-before-provider-proposal-marketplace-v159-$stamp"
$iisBackupName="Before_ProviderProposalMarketplaceV159_$stamp"
$databaseBackupPath=Join-Path $DatabaseBackupRoot "${DatabaseName}_PreProviderProposalMarketplaceV159_$stamp.bak"
$applySql=Join-Path $releaseRoot 'deployment\Apply-ProviderProposalMarketplaceV159.sql'
$verifySql=Join-Path $releaseRoot 'deployment\Verify-ProviderProposalMarketplaceV159.sql'
$sources=[ordered]@{Api=Join-Path $releaseRoot 'api';Customer=Join-Path $releaseRoot 'frontend\customer';Partner=Join-Path $releaseRoot 'frontend\partner';Admin=Join-Path $releaseRoot 'frontend\admin'}
$targets=[ordered]@{Api=Join-Path $AppsRoot 'api';Customer=Join-Path $AppsRoot 'customer';Partner=Join-Path $AppsRoot 'partner';Admin=Join-Path $AppsRoot 'admin'}
$required=@((Join-Path $sources.Api 'SoodalLife.Api.dll'),(Join-Path $sources.Api 'web.config'),(Join-Path $sources.Customer 'index.html'),(Join-Path $sources.Partner 'index.html'),(Join-Path $sources.Admin 'index.html'),$applySql,$verifySql,(Join-Path $releaseRoot 'RELEASE-MANIFEST.txt'))
foreach($file in $required){if(-not(Test-Path -LiteralPath $file -PathType Leaf)){throw "Required release file is missing: $file"}}
if(-not(Get-Command sqlcmd.exe -ErrorAction SilentlyContinue)){throw 'sqlcmd.exe is required.'}
New-Item -ItemType Directory -Force -Path $backupRoot,$DatabaseBackupRoot|Out-Null
function Copy-Checked([string]$Source,[string]$Destination,[switch]$Mirror,[switch]$ProtectApiData){New-Item -ItemType Directory -Force -Path $Destination|Out-Null;$arguments=@($Source,$Destination,($(if($Mirror){'/MIR'}else{'/E'})),'/R:2','/W:2','/NFL','/NDL','/NJH','/NJS','/NP');if($ProtectApiData){$arguments+=@('/XD','App_Data','/XF','appsettings.json','appsettings.Production.json')}; & robocopy.exe @arguments;if($LASTEXITCODE-gt 7){throw "Robocopy failed ($LASTEXITCODE): $Source -> $Destination"}}
function Get-PoolState([string]$Name){try{return(Get-WebAppPoolState -Name $Name -ErrorAction Stop).Value}catch{return $null}}
function Stop-Pool([string]$Name){if(-not(Test-Path "IIS:\AppPools\$Name")){return};if((Get-PoolState $Name)-ne 'Stopped'){Stop-WebAppPool -Name $Name};for($i=0;$i-lt 40;$i++){if((Get-PoolState $Name)-eq 'Stopped'){return};Start-Sleep -Milliseconds 500};throw "Application pool did not stop: $Name"}
function Start-Pool([string]$Name){if(-not(Test-Path "IIS:\AppPools\$Name")){return};for($i=0;$i-lt 40;$i++){if((Get-PoolState $Name)-eq 'Started'){return};try{Start-WebAppPool -Name $Name}catch{};Start-Sleep -Milliseconds 750};throw "Application pool did not start: $Name"}
& "$env:windir\System32\inetsrv\appcmd.exe" add backup $iisBackupName;if($LASTEXITCODE-ne 0){throw 'IIS configuration backup failed.'}
foreach($name in $targets.Keys){if(Test-Path -LiteralPath $targets[$name]){Copy-Checked $targets[$name] (Join-Path $backupRoot $name)}}
$escaped=$databaseBackupPath.Replace("'","''"); & sqlcmd.exe -S $ServerInstance -E -C -b -Q "BACKUP DATABASE [$DatabaseName] TO DISK=N'$escaped' WITH COPY_ONLY, CHECKSUM, COMPRESSION, INIT; RESTORE VERIFYONLY FROM DISK=N'$escaped' WITH CHECKSUM;";if($LASTEXITCODE-ne 0){throw 'Database backup verification failed.'}
$pools=@('SoodalLife.Api','soodallife.kr','SoodalLife.Static');$deploymentError=$null
try{
    foreach($pool in $pools){Stop-Pool $pool}
    & sqlcmd.exe -S $ServerInstance -E -C -I -b -f 65001 -d $DatabaseName -i $applySql
    if($LASTEXITCODE-ne 0){throw 'V159 database apply failed.'}
    Copy-Checked $sources.Api $targets.Api -ProtectApiData
    foreach($name in @('Customer','Partner','Admin')){Copy-Checked $sources[$name] $targets[$name] -Mirror}
    New-Item -ItemType Directory -Force -Path (Join-Path $targets.Api 'App_Data')|Out-Null
}catch{
    $deploymentError=$_;Write-Warning "V159 deployment stopped. Database backup: $databaseBackupPath"
    foreach($name in $targets.Keys){$backup=Join-Path $backupRoot $name;if(Test-Path $backup){Copy-Checked $backup $targets[$name] -Mirror}}
}finally{foreach($pool in $pools){try{Start-Pool $pool}catch{Write-Warning $_.Exception.Message}}}
if($deploymentError){throw $deploymentError}
& sqlcmd.exe -S $ServerInstance -E -C -I -b -f 65001 -d $DatabaseName -i $verifySql;if($LASTEXITCODE-ne 0){throw 'V159 database verification failed.'}
$health=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/health' -TimeoutSec 30;$ready=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/ready' -TimeoutSec 30
$policy=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/public/proposals/policy-guide' -TimeoutSec 30
$customer=Invoke-WebRequest -Uri 'https://soodallife.kr/proposals' -UseBasicParsing -TimeoutSec 30;$partner=Invoke-WebRequest -Uri 'https://partner.soodallife.kr/provider/proposals' -UseBasicParsing -TimeoutSec 30;$admin=Invoke-WebRequest -Uri 'https://admin.soodallife.kr' -UseBasicParsing -TimeoutSec 30
if($health.status-ne 'ok'-or $ready.status-ne 'ready'-or -not $policy.billingModel-or $customer.StatusCode-ne 200-or $partner.StatusCode-ne 200-or $admin.StatusCode-ne 200){throw 'Post-deployment verification failed.'}
[PSCustomObject]@{DeploymentSucceeded=$true;IisBackup=$iisBackupName;FileBackup=$backupRoot;DatabaseBackup=$databaseBackupPath;AppDataPreserved=$true;AppSettingsPreserved=$true;DatabaseChanged=$true;Health=$health.status;Readiness=$ready.status;ProposalMarketplace='ok';ExistingAdvertising='unchanged';ExternalMarketing='fail-closed';PwaPush='disabled-until-explicitly-enabled'}|Format-List
Write-Host 'V159 expert offer and group recruitment marketplace deployment completed.' -ForegroundColor Green
Write-Host 'SMS and PUSH promotion delivery remain blocked until the final marketing/PWA gates are explicitly enabled.' -ForegroundColor Yellow
