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
if(-not $PSCmdlet.ShouldProcess("API at $AppsRoot plus self-trade data cleanup in database $DatabaseName",'Deploy V161 self-trade prevention')){return}
Import-Module WebAdministration
$releaseRoot=$PSScriptRoot;$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot=Join-Path $BackupParent "backup-before-self-trade-prevention-v161-$stamp"
$iisBackupName="Before_SelfTradePreventionV161_$stamp"
$databaseBackupPath=Join-Path $DatabaseBackupRoot "${DatabaseName}_PreSelfTradePreventionV161_$stamp.bak"
$applySql=Join-Path $releaseRoot 'deployment\Apply-SelfTradePreventionV161.sql'
$verifySql=Join-Path $releaseRoot 'deployment\Verify-SelfTradePreventionV161.sql'
$sourceApi=Join-Path $releaseRoot 'api';$targetApi=Join-Path $AppsRoot 'api'
$required=@((Join-Path $sourceApi 'SoodalLife.Api.dll'),(Join-Path $sourceApi 'web.config'),$applySql,$verifySql,(Join-Path $releaseRoot 'RELEASE-MANIFEST.txt'))
foreach($file in $required){if(-not(Test-Path -LiteralPath $file -PathType Leaf)){throw "Required release file is missing: $file"}}
if(-not(Get-Command sqlcmd.exe -ErrorAction SilentlyContinue)){throw 'sqlcmd.exe is required.'}
New-Item -ItemType Directory -Force -Path $backupRoot,$DatabaseBackupRoot|Out-Null
function Copy-Checked([string]$Source,[string]$Destination,[switch]$Mirror,[switch]$ProtectApiData){New-Item -ItemType Directory -Force -Path $Destination|Out-Null;$arguments=@($Source,$Destination,($(if($Mirror){'/MIR'}else{'/E'})),'/R:2','/W:2','/NFL','/NDL','/NJH','/NJS','/NP');if($ProtectApiData){$arguments+=@('/XD','App_Data','/XF','appsettings.json','appsettings.Production.json')}; & robocopy.exe @arguments;if($LASTEXITCODE-gt 7){throw "Robocopy failed ($LASTEXITCODE): $Source -> $Destination"}}
function Get-PoolState([string]$Name){try{return(Get-WebAppPoolState -Name $Name -ErrorAction Stop).Value}catch{return $null}}
function Stop-Pool([string]$Name){if(-not(Test-Path "IIS:\AppPools\$Name")){return};if((Get-PoolState $Name)-ne 'Stopped'){Stop-WebAppPool -Name $Name};for($i=0;$i-lt 40;$i++){if((Get-PoolState $Name)-eq 'Stopped'){return};Start-Sleep -Milliseconds 500};throw "Application pool did not stop: $Name"}
function Start-Pool([string]$Name){if(-not(Test-Path "IIS:\AppPools\$Name")){return};for($i=0;$i-lt 40;$i++){if((Get-PoolState $Name)-eq 'Started'){return};try{Start-WebAppPool -Name $Name}catch{};Start-Sleep -Milliseconds 750};throw "Application pool did not start: $Name"}
& "$env:windir\System32\inetsrv\appcmd.exe" add backup $iisBackupName;if($LASTEXITCODE-ne 0){throw 'IIS configuration backup failed.'}
if(Test-Path -LiteralPath $targetApi){Copy-Checked $targetApi (Join-Path $backupRoot 'api')}
$escaped=$databaseBackupPath.Replace("'","''"); & sqlcmd.exe -S $ServerInstance -E -C -b -Q "BACKUP DATABASE [$DatabaseName] TO DISK=N'$escaped' WITH COPY_ONLY, CHECKSUM, COMPRESSION, INIT; RESTORE VERIFYONLY FROM DISK=N'$escaped' WITH CHECKSUM;";if($LASTEXITCODE-ne 0){throw 'Database backup verification failed.'}
$deploymentError=$null
try{
    Stop-Pool 'SoodalLife.Api'
    & sqlcmd.exe -S $ServerInstance -E -C -I -b -f 65001 -d $DatabaseName -i $applySql
    if($LASTEXITCODE-ne 0){throw 'V161 self-trade data cleanup failed.'}
    Copy-Checked $sourceApi $targetApi -ProtectApiData
    New-Item -ItemType Directory -Force -Path (Join-Path $targetApi 'App_Data')|Out-Null
}catch{
    $deploymentError=$_;Write-Warning "V161 deployment stopped. Database backup: $databaseBackupPath"
    $backup=Join-Path $backupRoot 'api';if(Test-Path $backup){Copy-Checked $backup $targetApi -Mirror}
}finally{try{Start-Pool 'SoodalLife.Api'}catch{Write-Warning $_.Exception.Message}}
if($deploymentError){throw $deploymentError}
& sqlcmd.exe -S $ServerInstance -E -C -I -b -f 65001 -d $DatabaseName -i $verifySql;if($LASTEXITCODE-ne 0){throw 'V161 database verification failed.'}
$health=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/health' -TimeoutSec 30;$ready=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/ready' -TimeoutSec 30
if($health.status-ne 'ok'-or $ready.status-ne 'ready'){throw 'Post-deployment API verification failed.'}
[PSCustomObject]@{DeploymentSucceeded=$true;IisBackup=$iisBackupName;FileBackup=$backupRoot;DatabaseBackup=$databaseBackupPath;AppDataPreserved=$true;AppSettingsPreserved=$true;DatabaseSchemaChanged=$false;SelfMatching='blocked';SelfQuoteSubmission='blocked';SelfAcceptance='blocked';EmergencyAndSiteVisit='blocked';Health=$health.status;Readiness=$ready.status}|Format-List
Write-Host 'V161 self-trade prevention deployment completed.' -ForegroundColor Green
Write-Host 'Dual customer/provider roles remain allowed; only interactions with requests created by the same account are blocked.' -ForegroundColor Yellow
