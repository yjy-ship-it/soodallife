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
if(-not $PSCmdlet.ShouldProcess("API, customer, partner and admin apps at $AppsRoot plus database $DatabaseName",'Deploy V144 customer live activity feed advertising')){return}
Import-Module WebAdministration
$releaseRoot=$PSScriptRoot
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot=Join-Path $BackupParent "backup-before-customer-live-feed-advertising-v144-$stamp"
$iisBackupName="Before_CustomerLiveFeedAdvertisingV144_$stamp"
$databaseBackupPath=Join-Path $DatabaseBackupRoot "${DatabaseName}_PreCustomerLiveFeedAdvertisingV144_$stamp.bak"
$applySql=Join-Path $releaseRoot 'deployment\Apply-CustomerLiveFeedAdvertisingV144.sql'
$verifySql=Join-Path $releaseRoot 'deployment\Verify-CustomerLiveFeedAdvertisingV144.sql'
$sources=[ordered]@{Api=Join-Path $releaseRoot 'api';Customer=Join-Path $releaseRoot 'frontend\customer';Partner=Join-Path $releaseRoot 'frontend\partner';Admin=Join-Path $releaseRoot 'frontend\admin'}
$targets=[ordered]@{Api=Join-Path $AppsRoot 'api';Customer=Join-Path $AppsRoot 'customer';Partner=Join-Path $AppsRoot 'partner';Admin=Join-Path $AppsRoot 'admin'}
$required=@((Join-Path $sources.Api 'SoodalLife.Api.dll'),(Join-Path $sources.Api 'web.config'),(Join-Path $sources.Customer 'index.html'),(Join-Path $sources.Partner 'index.html'),(Join-Path $sources.Admin 'index.html'),$applySql,$verifySql)
foreach($file in $required){if(-not(Test-Path -LiteralPath $file -PathType Leaf)){throw "Required release file is missing: $file"}}
if(-not(Get-Command sqlcmd.exe -ErrorAction SilentlyContinue)){throw 'sqlcmd.exe is required.'}
New-Item -ItemType Directory -Force -Path $backupRoot,$DatabaseBackupRoot|Out-Null
function Copy-Checked([string]$Source,[string]$Destination,[switch]$Mirror,[switch]$ProtectApiData){
    New-Item -ItemType Directory -Force -Path $Destination|Out-Null
    $arguments=@($Source,$Destination,($(if($Mirror){'/MIR'}else{'/E'})),'/R:2','/W:2','/NFL','/NDL','/NJH','/NJS','/NP')
    if($ProtectApiData){$arguments+=@('/XD','App_Data','/XF','appsettings.json','appsettings.Production.json')}
    & robocopy.exe @arguments
    if($LASTEXITCODE -gt 7){throw "Robocopy failed ($LASTEXITCODE): $Source -> $Destination"}
}
function Get-PoolState([string]$Name){try{return (Get-WebAppPoolState -Name $Name -ErrorAction Stop).Value}catch{return $null}}
function Stop-Pool([string]$Name){if(-not(Test-Path "IIS:\AppPools\$Name")){return};if((Get-PoolState $Name)-ne 'Stopped'){Stop-WebAppPool -Name $Name -ErrorAction Stop};for($i=0;$i-lt 40;$i++){if((Get-PoolState $Name)-eq 'Stopped'){return};Start-Sleep -Milliseconds 500};throw "Application pool did not stop: $Name"}
function Start-Pool([string]$Name){if(-not(Test-Path "IIS:\AppPools\$Name")){return};for($i=0;$i-lt 40;$i++){if((Get-PoolState $Name)-eq 'Started'){return};try{Start-WebAppPool -Name $Name -ErrorAction Stop}catch{};Start-Sleep -Milliseconds 750};throw "Application pool did not start: $Name"}

& "$env:windir\System32\inetsrv\appcmd.exe" add backup $iisBackupName
if($LASTEXITCODE-ne 0){throw 'IIS configuration backup failed.'}
foreach($name in $targets.Keys){if(Test-Path -LiteralPath $targets[$name]){Copy-Checked $targets[$name] (Join-Path $backupRoot $name)}}
$escapedBackup=$databaseBackupPath.Replace("'","''")
& sqlcmd.exe -S $ServerInstance -E -C -b -Q "BACKUP DATABASE [$DatabaseName] TO DISK=N'$escapedBackup' WITH COPY_ONLY, CHECKSUM, COMPRESSION, INIT; RESTORE VERIFYONLY FROM DISK=N'$escapedBackup' WITH CHECKSUM;"
if($LASTEXITCODE-ne 0){throw 'Database backup verification failed.'}

$pools=@('SoodalLife.Api','soodallife.kr','SoodalLife.Static')
$deploymentError=$null
try{
    foreach($pool in $pools){Stop-Pool $pool}
    & sqlcmd.exe -S $ServerInstance -E -C -I -b -f 65001 -d $DatabaseName -i $applySql
    if($LASTEXITCODE-ne 0){throw 'V144 database apply failed.'}
    Copy-Checked $sources.Api $targets.Api -ProtectApiData
    foreach($name in @('Customer','Partner','Admin')){Copy-Checked $sources[$name] $targets[$name] -Mirror}
    New-Item -ItemType Directory -Force -Path (Join-Path $targets.Api 'App_Data')|Out-Null
}catch{
    $deploymentError=$_
    Write-Warning "V144 deployment stopped. Database backup: $databaseBackupPath"
    foreach($name in $targets.Keys){$backup=Join-Path $backupRoot $name;if(Test-Path -LiteralPath $backup){Copy-Checked $backup $targets[$name] -Mirror}}
}finally{foreach($pool in $pools){try{Start-Pool $pool}catch{Write-Warning $_.Exception.Message}}}
if($deploymentError){throw $deploymentError}

& sqlcmd.exe -S $ServerInstance -E -C -I -b -f 65001 -d $DatabaseName -i $verifySql
if($LASTEXITCODE-ne 0){throw 'V144 database verification failed.'}
$health=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/health' -TimeoutSec 30
$readiness=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/ready' -TimeoutSec 30
$customerPage=Invoke-WebRequest -Uri 'https://soodallife.kr/customer#live-activity' -UseBasicParsing -TimeoutSec 30
$partnerPage=Invoke-WebRequest -Uri 'https://partner.soodallife.kr/provider/advertising' -UseBasicParsing -TimeoutSec 30
$adminPage=Invoke-WebRequest -Uri 'https://admin.soodallife.kr/admin/provider-campaigns' -UseBasicParsing -TimeoutSec 30
$promotionApi=Invoke-WebRequest -Uri 'https://api.soodallife.kr/api/v1/public/advertising?audience=CUSTOMER&placement=CUSTOMER_LIVE_ACTIVITY_FEED' -UseBasicParsing -TimeoutSec 30
if($health.status-ne 'ok'-or $readiness.status-ne 'ready'-or $customerPage.StatusCode-ne 200-or $partnerPage.StatusCode-ne 200-or $adminPage.StatusCode-ne 200-or $promotionApi.StatusCode-ne 200){throw 'Post-deployment health, page or live-feed advertising API verification failed.'}
[PSCustomObject]@{DeploymentSucceeded=$true;IisBackup=$iisBackupName;FileBackup=$backupRoot;DatabaseBackup=$databaseBackupPath;AppDataPreserved=$true;AppSettingsPreserved=$true;DatabaseChanged=$true;Health=$health.status;Readiness=$readiness.status;CustomerLiveFeed='ok';PartnerAdvertising='ok';AdminCampaigns='ok';LiveFeedAdvertisingApi='ok'}|Format-List
Write-Host 'V144 customer live activity feed advertising deployment completed.' -ForegroundColor Green
Write-Host 'Actual transactions remain newest-first; advertisements are separately labeled and excluded from transaction counts.' -ForegroundColor Yellow
