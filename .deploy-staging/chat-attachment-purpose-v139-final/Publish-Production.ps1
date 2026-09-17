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
if(-not $PSCmdlet.ShouldProcess("API app at $AppsRoot plus database $DatabaseName",'Deploy V139 chat attachment database constraint fix')){return}
Import-Module WebAdministration
$releaseRoot=$PSScriptRoot
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot=Join-Path $BackupParent "backup-before-chat-attachment-v139-$stamp"
$iisBackupName="Before_ChatAttachmentV139_$stamp"
$databaseBackupPath=Join-Path $DatabaseBackupRoot "${DatabaseName}_PreChatAttachmentV139_$stamp.bak"
$applySql=Join-Path $releaseRoot 'deployment\Apply-ChatAttachmentPurposeV139.sql'
$verifySql=Join-Path $releaseRoot 'deployment\Verify-ChatAttachmentPurposeV139.sql'
$apiSource=Join-Path $releaseRoot 'api'
$apiTarget=Join-Path $AppsRoot 'api'
$required=@((Join-Path $apiSource 'SoodalLife.Api.dll'),(Join-Path $apiSource 'web.config'),$applySql,$verifySql)
foreach($file in $required){if(-not(Test-Path -LiteralPath $file -PathType Leaf)){throw "Required release file is missing: $file"}}
if(-not(Get-Command sqlcmd.exe -ErrorAction SilentlyContinue)){throw 'sqlcmd.exe is required.'}
New-Item -ItemType Directory -Force -Path $backupRoot,$DatabaseBackupRoot|Out-Null
function Copy-Checked([string]$Source,[string]$Destination,[switch]$PreserveAppData){
    New-Item -ItemType Directory -Force -Path $Destination|Out-Null
    $arguments=@($Source,$Destination,'/E','/R:2','/W:2','/NFL','/NDL','/NJH','/NJS','/NP')
    if($PreserveAppData){$arguments+=@('/XD','App_Data')}
    & robocopy.exe @arguments
    if($LASTEXITCODE -gt 7){throw "Robocopy failed ($LASTEXITCODE): $Source -> $Destination"}
}
function Get-PoolState([string]$Name){try{return (Get-WebAppPoolState -Name $Name -ErrorAction Stop).Value}catch{return $null}}
function Stop-Pool([string]$Name){
    if(-not(Test-Path "IIS:\AppPools\$Name")){return}
    if((Get-PoolState $Name) -ne 'Stopped'){Stop-WebAppPool -Name $Name -ErrorAction Stop}
    for($i=0;$i -lt 40;$i++){if((Get-PoolState $Name) -eq 'Stopped'){return};Start-Sleep -Milliseconds 500}
    throw "Application pool did not stop within 20 seconds: $Name"
}
function Start-Pool([string]$Name){
    if(-not(Test-Path "IIS:\AppPools\$Name")){return}
    for($i=0;$i -lt 40;$i++){if((Get-PoolState $Name) -eq 'Started'){return};try{Start-WebAppPool -Name $Name -ErrorAction Stop}catch{};Start-Sleep -Milliseconds 750}
    throw "Application pool did not start: $Name"
}
& "$env:windir\System32\inetsrv\appcmd.exe" add backup $iisBackupName
if($LASTEXITCODE -ne 0){throw 'IIS configuration backup failed.'}
if(Test-Path -LiteralPath $apiTarget){Copy-Checked $apiTarget (Join-Path $backupRoot 'API')}
$escapedBackup=$databaseBackupPath.Replace("'","''")
& sqlcmd.exe -S $ServerInstance -E -C -b -Q "BACKUP DATABASE [$DatabaseName] TO DISK=N'$escapedBackup' WITH COPY_ONLY, CHECKSUM, COMPRESSION, INIT; RESTORE VERIFYONLY FROM DISK=N'$escapedBackup' WITH CHECKSUM;"
if($LASTEXITCODE -ne 0){throw 'Database backup verification failed.'}
$deploymentError=$null
try{
    Stop-Pool 'SoodalLife.Api'
    & sqlcmd.exe -S $ServerInstance -E -C -I -b -f 65001 -d $DatabaseName -i $applySql
    if($LASTEXITCODE -ne 0){throw 'V139 database apply failed.'}
    Copy-Checked $apiSource $apiTarget -PreserveAppData
    New-Item -ItemType Directory -Force -Path (Join-Path $apiTarget 'App_Data\private-files')|Out-Null
    & icacls.exe (Join-Path $apiTarget 'App_Data\private-files') /grant 'IIS AppPool\SoodalLife.Api:(OI)(CI)M' /T /C|Out-Null
    if($LASTEXITCODE -ne 0){throw 'Private storage ACL verification failed.'}
}catch{$deploymentError=$_;Write-Warning "V139 deployment stopped. Database backup: $databaseBackupPath"}
finally{try{Start-Pool 'SoodalLife.Api'}catch{Write-Warning $_.Exception.Message}}
if($deploymentError){throw $deploymentError}
& sqlcmd.exe -S $ServerInstance -E -C -I -b -f 65001 -d $DatabaseName -i $verifySql
if($LASTEXITCODE -ne 0){throw 'V139 database verification failed.'}
$health=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/health' -TimeoutSec 30
$ready=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/ready' -TimeoutSec 30
$preflight=Invoke-WebRequest -Method Options -Uri 'https://api.soodallife.kr/api/v1/chat/rooms/00000000-0000-0000-0000-000000000000/messages/file-chunks' -Headers @{Origin='https://partner.soodallife.kr';'Access-Control-Request-Method'='POST';'Access-Control-Request-Headers'='authorization,content-type'} -UseBasicParsing -TimeoutSec 30
if($health.status -ne 'ok' -or $ready.status -ne 'ready' -or $preflight.StatusCode -ne 204){throw 'Post-deployment API verification failed.'}
[PSCustomObject]@{DeploymentSucceeded=$true;IisBackup=$iisBackupName;FileBackup=$backupRoot;DatabaseBackup=$databaseBackupPath;DatabaseChanged=$true;ChatAttachmentPurpose='allowed';Health=$health.status;Readiness=$ready.status;ChunkPreflight='ok'}|Format-List
Write-Host 'V139 chat attachment database constraint deployment completed.' -ForegroundColor Green
Write-Host 'Root cause fixed: CK_files_purpose now permits CHAT_ATTACHMENT.' -ForegroundColor Yellow
