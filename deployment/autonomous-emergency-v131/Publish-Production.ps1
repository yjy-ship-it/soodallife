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
if(-not $PSCmdlet.ShouldProcess("API, customer and partner apps at $AppsRoot plus database $DatabaseName",'Deploy V131 autonomous emergency dispatch')){return}
Import-Module WebAdministration
$releaseRoot=$PSScriptRoot
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot=Join-Path $BackupParent "backup-before-autonomous-emergency-v131-$stamp"
$iisBackupName="Before_AutonomousEmergencyV131_$stamp"
$databaseBackupPath=Join-Path $DatabaseBackupRoot "${DatabaseName}_PreAutonomousEmergencyV131_$stamp.bak"
$applySql=Join-Path $releaseRoot 'deployment\Apply-AutonomousEmergencyDispatchV131.sql'
$verifySql=Join-Path $releaseRoot 'deployment\Verify-AutonomousEmergencyDispatchV131.sql'
$sources=[ordered]@{API=Join-Path $releaseRoot 'api';Customer=Join-Path $releaseRoot 'frontend\customer';Partner=Join-Path $releaseRoot 'frontend\partner'}
$targets=[ordered]@{API=Join-Path $AppsRoot 'api';Customer=Join-Path $AppsRoot 'customer';Partner=Join-Path $AppsRoot 'partner'}
$required=@((Join-Path $sources.API 'SoodalLife.Api.dll'),(Join-Path $sources.API 'web.config'),(Join-Path $sources.Customer 'index.html'),(Join-Path $sources.Partner 'index.html'),$applySql,$verifySql)
foreach($file in $required){if(-not(Test-Path -LiteralPath $file -PathType Leaf)){throw "Required release file is missing: $file"}}
if(-not(Get-Command sqlcmd.exe -ErrorAction SilentlyContinue)){throw 'sqlcmd.exe is required.'}
New-Item -ItemType Directory -Force -Path $backupRoot,$DatabaseBackupRoot|Out-Null
function Copy-Checked([string]$Source,[string]$Destination,[switch]$PreserveAppData){New-Item -ItemType Directory -Force -Path $Destination|Out-Null;$arguments=@($Source,$Destination,'/E','/R:2','/W:2','/NFL','/NDL','/NJH','/NJS','/NP');if($PreserveAppData){$arguments+=@('/XD','App_Data')};& robocopy.exe @arguments;if($LASTEXITCODE -gt 7){throw "Robocopy failed ($LASTEXITCODE): $Source -> $Destination"}}
function Stop-Pool([string]$Name){if(-not(Test-Path "IIS:\AppPools\$Name") -or (Get-WebAppPoolState -Name $Name).Value -eq 'Stopped'){return};Stop-WebAppPool -Name $Name;for($i=0;$i -lt 30;$i++){Start-Sleep -Milliseconds 500;if((Get-WebAppPoolState -Name $Name).Value -eq 'Stopped'){return}};throw "Application pool did not stop: $Name"}
function Start-Pool([string]$Name){if((Test-Path "IIS:\AppPools\$Name") -and (Get-WebAppPoolState -Name $Name).Value -ne 'Started'){Start-WebAppPool -Name $Name}}
& "$env:windir\System32\inetsrv\appcmd.exe" add backup $iisBackupName
if($LASTEXITCODE -ne 0){throw 'IIS configuration backup failed.'}
foreach($name in $targets.Keys){if(Test-Path -LiteralPath $targets[$name]){New-Item -ItemType Directory -Force -Path (Join-Path $backupRoot $name)|Out-Null;& robocopy.exe $targets[$name] (Join-Path $backupRoot $name) /E /R:2 /W:2 /NFL /NDL /NJH /NJS /NP;if($LASTEXITCODE -gt 7){throw "Backup failed: $name"}}}
$escaped=$databaseBackupPath.Replace("'","''")
& sqlcmd.exe -S $ServerInstance -E -C -b -Q "BACKUP DATABASE [$DatabaseName] TO DISK=N'$escaped' WITH COPY_ONLY, CHECKSUM, COMPRESSION, INIT; RESTORE VERIFYONLY FROM DISK=N'$escaped' WITH CHECKSUM;"
if($LASTEXITCODE -ne 0){throw 'Database backup verification failed.'}
$pools=@('SoodalLife.Api','soodallife.kr','SoodalLife.Static')
$deploymentError=$null
try{
    foreach($pool in $pools){Stop-Pool $pool}
    & sqlcmd.exe -S $ServerInstance -E -C -I -b -f 65001 -d $DatabaseName -i $applySql
    if($LASTEXITCODE -ne 0){throw 'V131 database apply failed.'}
    foreach($name in $sources.Keys){Copy-Checked $sources[$name] $targets[$name] -PreserveAppData:($name -eq 'API')}
    New-Item -ItemType Directory -Force -Path (Join-Path $targets.API 'App_Data')|Out-Null
}catch{$deploymentError=$_;Write-Warning "V131 deployment stopped. Database backup: $databaseBackupPath"}
finally{foreach($pool in $pools){try{Start-Pool $pool}catch{Write-Warning $_.Exception.Message}}}
if($deploymentError){throw $deploymentError}
& sqlcmd.exe -S $ServerInstance -E -C -I -b -f 65001 -d $DatabaseName -i $verifySql
if($LASTEXITCODE -ne 0){throw 'V131 database verification failed.'}
$health=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/health' -TimeoutSec 30
$ready=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/ready' -TimeoutSec 30
if($health.status -ne 'ok' -or $ready.status -ne 'ready'){throw 'Post-deployment health/readiness failed.'}
$customer=Invoke-WebRequest -Uri 'https://soodallife.kr/emergency' -UseBasicParsing -TimeoutSec 30
$partner=Invoke-WebRequest -Uri 'https://partner.soodallife.kr/provider/emergency' -UseBasicParsing -TimeoutSec 30
if($customer.StatusCode -ne 200 -or $partner.StatusCode -ne 200){throw 'Post-deployment emergency page verification failed.'}
[PSCustomObject]@{DeploymentSucceeded=$true;IisBackup=$iisBackupName;FileBackup=$backupRoot;DatabaseBackup=$databaseBackupPath;DatabaseChanged=$true;Health=$health.status;Readiness=$ready.status;CustomerEmergency='ok';PartnerEmergency='ok'}|Format-List
Write-Host 'V131 autonomous emergency dispatch deployment completed.' -ForegroundColor Green
Write-Host 'Bank transfer is a direct customer-provider arrangement. The platform records declarations but does not verify deposits.' -ForegroundColor Yellow
