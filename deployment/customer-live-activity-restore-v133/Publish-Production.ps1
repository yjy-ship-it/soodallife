[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [switch]$ConfirmProductionDeployment,
    [string]$AppsRoot='D:\SOODALLIFE\apps',
    [string]$BackupParent='D:\SOODALLIFE\releases'
)
$ErrorActionPreference='Stop'
if(-not $ConfirmProductionDeployment){throw 'Safety stop: rerun with -ConfirmProductionDeployment after checking the ZIP hash and backup path.'}
if(-not $PSCmdlet.ShouldProcess("API and customer apps at $AppsRoot",'Deploy V133 customer live activity restoration')){return}
Import-Module WebAdministration
$releaseRoot=$PSScriptRoot
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot=Join-Path $BackupParent "backup-before-customer-live-activity-v133-$stamp"
$iisBackupName="Before_CustomerLiveActivityV133_$stamp"
$sources=[ordered]@{API=Join-Path $releaseRoot 'api';Customer=Join-Path $releaseRoot 'frontend\customer'}
$targets=[ordered]@{API=Join-Path $AppsRoot 'api';Customer=Join-Path $AppsRoot 'customer'}
$required=@((Join-Path $sources.API 'SoodalLife.Api.dll'),(Join-Path $sources.API 'web.config'),(Join-Path $sources.Customer 'index.html'))
foreach($file in $required){if(-not(Test-Path -LiteralPath $file -PathType Leaf)){throw "Required release file is missing: $file"}}
New-Item -ItemType Directory -Force -Path $backupRoot|Out-Null
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
    $lastError=$null
    for($i=0;$i -lt 40;$i++){
        if((Get-PoolState $Name) -eq 'Started'){return}
        try{Start-WebAppPool -Name $Name -ErrorAction Stop}catch{$lastError=$_}
        Start-Sleep -Milliseconds 750
    }
    try{& "$env:windir\System32\inetsrv\appcmd.exe" start apppool /apppool.name:$Name|Out-Null}catch{$lastError=$_}
    for($i=0;$i -lt 20;$i++){if((Get-PoolState $Name) -eq 'Started'){return};Start-Sleep -Milliseconds 750}
    if($lastError){throw $lastError};throw "Application pool did not start: $Name"
}
& "$env:windir\System32\inetsrv\appcmd.exe" add backup $iisBackupName
if($LASTEXITCODE -ne 0){throw 'IIS configuration backup failed.'}
foreach($name in $targets.Keys){
    if(Test-Path -LiteralPath $targets[$name]){
        $backupTarget=Join-Path $backupRoot $name
        New-Item -ItemType Directory -Force -Path $backupTarget|Out-Null
        & robocopy.exe $targets[$name] $backupTarget /E /R:2 /W:2 /NFL /NDL /NJH /NJS /NP
        if($LASTEXITCODE -gt 7){throw "Backup failed: $name"}
    }
}
$pools=@('SoodalLife.Api','soodallife.kr')
$deploymentError=$null
try{
    foreach($pool in $pools){Stop-Pool $pool}
    foreach($name in $sources.Keys){Copy-Checked $sources[$name] $targets[$name] -PreserveAppData:($name -eq 'API')}
    New-Item -ItemType Directory -Force -Path (Join-Path $targets.API 'App_Data')|Out-Null
}catch{$deploymentError=$_}
finally{foreach($pool in $pools){try{Start-Pool $pool}catch{Write-Warning $_.Exception.Message}}}
if($deploymentError){throw $deploymentError}
$health=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/health' -TimeoutSec 30
$ready=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/ready' -TimeoutSec 30
$feed=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/public/activity?take=5' -TimeoutSec 30
$customer=Invoke-WebRequest -Uri 'https://soodallife.kr/' -UseBasicParsing -TimeoutSec 30
if($health.status -ne 'ok' -or $ready.status -ne 'ready' -or $null -eq $feed.items -or $customer.StatusCode -ne 200){throw 'Post-deployment verification failed.'}
[PSCustomObject]@{DeploymentSucceeded=$true;IisBackup=$iisBackupName;FileBackup=$backupRoot;AppDataPreserved=$true;DatabaseChanged=$false;Health=$health.status;Readiness=$ready.status;PublicActivityApi='ok';CustomerHome='ok'}|Format-List
Write-Host 'V133 customer live activity restoration deployment completed.' -ForegroundColor Green
Write-Host 'V132 emergency dispatch and general site-visit features are preserved.' -ForegroundColor Yellow
