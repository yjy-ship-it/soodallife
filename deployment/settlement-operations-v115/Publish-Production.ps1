[CmdletBinding()]
param([string]$AppsRoot='D:\SOODALLIFE\apps',[string]$BackupParent='D:\SOODALLIFE\releases')
$ErrorActionPreference='Stop'
Import-Module WebAdministration

$releaseRoot=$PSScriptRoot
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot=Join-Path $BackupParent "backup-before-settlement-operations-v115-$stamp"
$iisBackupName="Before_SettlementOperationsV115_$stamp"
$sources=[ordered]@{
    API=Join-Path $releaseRoot 'api'
    Customer=Join-Path $releaseRoot 'frontend\customer'
    Partner=Join-Path $releaseRoot 'frontend\partner'
    Admin=Join-Path $releaseRoot 'frontend\admin'
}
$targets=[ordered]@{
    API=Join-Path $AppsRoot 'api'
    Customer=Join-Path $AppsRoot 'customer'
    Partner=Join-Path $AppsRoot 'partner'
    Admin=Join-Path $AppsRoot 'admin'
}
$required=@(
    (Join-Path $sources.API 'SoodalLife.Api.dll'),(Join-Path $sources.API 'web.config'),
    (Join-Path $sources.Customer 'index.html'),(Join-Path $sources.Customer 'web.config'),
    (Join-Path $sources.Partner 'index.html'),(Join-Path $sources.Partner 'web.config'),
    (Join-Path $sources.Admin 'index.html'),(Join-Path $sources.Admin 'web.config')
)
foreach($file in $required){if(-not(Test-Path -LiteralPath $file -PathType Leaf)){throw "Required release file is missing: $file"}}
New-Item -ItemType Directory -Force -Path $backupRoot|Out-Null

function Copy-Checked([string]$Source,[string]$Destination,[switch]$PreserveAppData){
    New-Item -ItemType Directory -Force -Path $Destination|Out-Null
    $args=@($Source,$Destination,'/MIR','/R:2','/W:2','/NFL','/NDL','/NJH','/NJS','/NP')
    if($PreserveAppData){$args+=@('/XD','App_Data')}
    & robocopy.exe @args
    if($LASTEXITCODE -gt 7){throw "Robocopy failed ($LASTEXITCODE): $Source -> $Destination"}
}
function Stop-Pool([string]$Name){
    if(-not(Test-Path "IIS:\AppPools\$Name")){return}
    if((Get-WebAppPoolState -Name $Name).Value -eq 'Stopped'){return}
    Stop-WebAppPool -Name $Name
    for($i=0;$i -lt 30;$i++){Start-Sleep -Milliseconds 500;if((Get-WebAppPoolState -Name $Name).Value -eq 'Stopped'){return}}
    throw "Application pool did not stop: $Name"
}
function Start-Pool([string]$Name){if((Test-Path "IIS:\AppPools\$Name") -and (Get-WebAppPoolState -Name $Name).Value -ne 'Started'){Start-WebAppPool -Name $Name}}

& "$env:windir\System32\inetsrv\appcmd.exe" add backup $iisBackupName
if($LASTEXITCODE -ne 0){throw 'IIS configuration backup failed.'}
foreach($name in $targets.Keys){if(Test-Path -LiteralPath $targets[$name]){New-Item -ItemType Directory -Force -Path (Join-Path $backupRoot $name)|Out-Null;& robocopy.exe $targets[$name] (Join-Path $backupRoot $name) /E /R:2 /W:2 /NFL /NDL /NJH /NJS /NP;if($LASTEXITCODE -gt 7){throw "Backup failed: $name"}}}

$pools=@('SoodalLife.Api','soodallife.kr','SoodalLife.Static')
$deploymentError=$null
try{
    foreach($pool in $pools){Stop-Pool $pool}
    foreach($name in $sources.Keys){Copy-Checked $sources[$name] $targets[$name] -PreserveAppData:($name -eq 'API')}
    New-Item -ItemType Directory -Force -Path (Join-Path $targets.API 'App_Data')|Out-Null
}catch{
    $deploymentError=$_
    Write-Warning 'V115 deployment failed. Restoring the pre-deployment file backup.'
    foreach($name in $targets.Keys){$backup=Join-Path $backupRoot $name;if(Test-Path -LiteralPath $backup){Copy-Checked $backup $targets[$name] -PreserveAppData:($name -eq 'API')}}
}finally{
    foreach($pool in $pools){try{Start-Pool $pool}catch{Write-Warning $_.Exception.Message}}
}
if($deploymentError){throw $deploymentError}

$health=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/health' -TimeoutSec 30
$ready=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/ready' -TimeoutSec 30
if($health.status -ne 'ok' -or $ready.status -ne 'ready'){throw 'Post-deployment API health/readiness failed.'}
$admin=Invoke-WebRequest -Uri 'https://admin.soodallife.kr/admin/settlements' -UseBasicParsing -TimeoutSec 30
if($admin.StatusCode -ne 200){throw "Admin settlement page check failed: HTTP $($admin.StatusCode)"}

[PSCustomObject]@{
    DeploymentSucceeded=$true
    IisBackup=$iisBackupName
    FileBackup=$backupRoot
    AppDataPreserved=$true
    DatabaseChanged=$false
    Health=$health.status
    Readiness=$ready.status
    SettlementPage='ok'
}|Format-List
Write-Host 'V115 settlement operations deployment completed.' -ForegroundColor Green
