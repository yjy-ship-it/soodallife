[CmdletBinding()]
param(
    [string]$ServerInstance = 'localhost',
    [string]$DatabaseName = 'SOODAL_LIFE_DEV'
)

$ErrorActionPreference = 'Stop'
Import-Module WebAdministration
$releaseRoot = $PSScriptRoot
$appsRoot = 'D:\SOODALLIFE\apps'
$backupParent = 'D:\SOODALLIFE\releases'
$databaseBackupRoot = 'D:\SOODALLIFE\DB\Backup'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot = Join-Path $backupParent "backup-before-subscription-termination-v101-$stamp"
$iisBackupName = "Before_SubscriptionTerminationV101_$stamp"
$databaseBackupPath = Join-Path $databaseBackupRoot "${DatabaseName}_PreSubscriptionTerminationV101_$stamp.bak"
$migrationSql = Join-Path $releaseRoot 'deployment\Apply-SubscriptionTerminationV101.sql'
$verificationSql = Join-Path $releaseRoot 'deployment\Verify-SubscriptionTerminationV101.sql'
$sources = [ordered]@{ API=Join-Path $releaseRoot 'api'; Customer=Join-Path $releaseRoot 'frontend\customer'; Partner=Join-Path $releaseRoot 'frontend\partner'; Admin=Join-Path $releaseRoot 'frontend\admin' }
$targets = [ordered]@{ API=Join-Path $appsRoot 'api'; Customer=Join-Path $appsRoot 'customer'; Partner=Join-Path $appsRoot 'partner'; Admin=Join-Path $appsRoot 'admin' }

foreach($required in @((Join-Path $sources.API 'SoodalLife.Api.dll'),(Join-Path $sources.API 'web.config'),(Join-Path $sources.Customer 'index.html'),(Join-Path $sources.Partner 'index.html'),(Join-Path $sources.Admin 'index.html'),$migrationSql,$verificationSql)){if(-not(Test-Path -LiteralPath $required -PathType Leaf)){throw "Required release file is missing: $required"}}
if(-not(Get-Command sqlcmd.exe -ErrorAction SilentlyContinue)){throw 'sqlcmd.exe is required.'}
New-Item -ItemType Directory -Force -Path $backupRoot,$databaseBackupRoot | Out-Null

function Invoke-RobocopyChecked([string]$Source,[string]$Destination,[switch]$Mirror,[switch]$PreserveAppData){New-Item -ItemType Directory -Force -Path $Destination|Out-Null;$arguments=@($Source,$Destination,($(if($Mirror){'/MIR'}else{'/E'})),'/R:2','/W:2','/NFL','/NDL','/NJH','/NJS','/NP');if($PreserveAppData){$arguments+=@('/XD','App_Data')};& robocopy.exe @arguments;if($LASTEXITCODE -gt 7){throw "Robocopy failed ($LASTEXITCODE): $Source -> $Destination"}}
function Stop-PoolSafely([string]$Name){if((Get-WebAppPoolState -Name $Name).Value -eq 'Stopped'){return};Stop-WebAppPool -Name $Name;for($attempt=0;$attempt -lt 30;$attempt++){Start-Sleep -Milliseconds 500;if((Get-WebAppPoolState -Name $Name).Value -eq 'Stopped'){return}};throw "Application pool did not stop: $Name"}
function Start-PoolSafely([string]$Name){if((Get-WebAppPoolState -Name $Name).Value -ne 'Started'){Start-WebAppPool -Name $Name}}

& "$env:windir\System32\inetsrv\appcmd.exe" add backup $iisBackupName
if($LASTEXITCODE -ne 0){throw 'IIS configuration backup failed.'}
foreach($name in $targets.Keys){if(Test-Path -LiteralPath $targets[$name]){Invoke-RobocopyChecked $targets[$name] (Join-Path $backupRoot $name)}}
$escapedBackupPath=$databaseBackupPath.Replace("'","''")
& sqlcmd.exe -S $ServerInstance -E -C -b -Q "BACKUP DATABASE [$DatabaseName] TO DISK=N'$escapedBackupPath' WITH COPY_ONLY, CHECKSUM, COMPRESSION, INIT; RESTORE VERIFYONLY FROM DISK=N'$escapedBackupPath' WITH CHECKSUM;"
if($LASTEXITCODE -ne 0){throw 'Database backup verification failed. Deployment was not started.'}

$poolNames=@('SoodalLife.Api','soodallife.kr','SoodalLife.Static')
try{
    foreach($poolName in $poolNames){Stop-PoolSafely $poolName}
    & sqlcmd.exe -S $ServerInstance -E -C -I -b -f 65001 -d $DatabaseName -i $migrationSql
    if($LASTEXITCODE -ne 0){throw 'Subscription termination V101 database migration failed.'}
    foreach($name in $sources.Keys){if($name -eq 'API'){Invoke-RobocopyChecked $sources[$name] $targets[$name] -Mirror -PreserveAppData}else{Invoke-RobocopyChecked $sources[$name] $targets[$name] -Mirror}}
    $appDataPath=Join-Path $targets.API 'App_Data';New-Item -ItemType Directory -Force -Path $appDataPath|Out-Null
}
finally{foreach($poolName in $poolNames){try{Start-PoolSafely $poolName}catch{Write-Warning $_.Exception.Message}}}

& sqlcmd.exe -S $ServerInstance -E -C -I -b -f 65001 -d $DatabaseName -i $verificationSql
if($LASTEXITCODE -ne 0){throw 'Subscription termination V101 verification failed.'}
$sites='soodallife.kr','partner.soodallife.kr','admin.soodallife.kr','api.soodallife.kr';foreach($siteName in $sites){if((Get-WebsiteState -Name $siteName).Value -ne 'Started'){Start-Website -Name $siteName}}

[PSCustomObject]@{DeploymentSucceeded=$true;IisBackup=$iisBackupName;FileBackup=$backupRoot;DatabaseBackup=$databaseBackupPath;DatabaseChanged=$true;Migration='CompleteSubscriptionTerminationV101';PgConfigurationRequired=([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable('SubscriptionPaymentGateway__SecretKey','Machine')))}|Format-List
Get-Website|Where-Object Name -In $sites|Select-Object Name,State,PhysicalPath,ApplicationPool|Format-Table -AutoSize
Write-Host 'Next: configure the protected Toss Payments key if required, then verify termination, case hold, refund, settlement, and contract closure.'
