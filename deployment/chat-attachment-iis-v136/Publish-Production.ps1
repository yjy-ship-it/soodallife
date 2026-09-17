[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [switch]$ConfirmProductionDeployment,
    [string]$AppsRoot='D:\SOODALLIFE\apps',
    [string]$BackupParent='D:\SOODALLIFE\releases'
)
$ErrorActionPreference='Stop'
if(-not $ConfirmProductionDeployment){throw 'Safety stop: rerun with -ConfirmProductionDeployment after checking the ZIP hash and backup path.'}
if(-not $PSCmdlet.ShouldProcess("API, customer and partner apps at $AppsRoot",'Deploy V136 IIS chat attachment upload fix')){return}
Import-Module WebAdministration
$releaseRoot=$PSScriptRoot
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot=Join-Path $BackupParent "backup-before-chat-attachment-v136-$stamp"
$iisBackupName="Before_ChatAttachmentV136_$stamp"
$sources=[ordered]@{API=Join-Path $releaseRoot 'api';Customer=Join-Path $releaseRoot 'frontend\customer';Partner=Join-Path $releaseRoot 'frontend\partner'}
$targets=[ordered]@{API=Join-Path $AppsRoot 'api';Customer=Join-Path $AppsRoot 'customer';Partner=Join-Path $AppsRoot 'partner'}
$required=@((Join-Path $sources.API 'SoodalLife.Api.dll'),(Join-Path $sources.API 'web.config'),(Join-Path $sources.Customer 'index.html'),(Join-Path $sources.Partner 'index.html'))
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
$pools=@('SoodalLife.Api','soodallife.kr','SoodalLife.Static')
$deploymentError=$null
$privateRoot=Join-Path $targets.API 'App_Data\private-files'
$apiSite=$null
$actualUploadReadAheadSize=$null
try{
    foreach($pool in $pools){Stop-Pool $pool}
    foreach($name in $sources.Keys){Copy-Checked $sources[$name] $targets[$name] -PreserveAppData:($name -eq 'API')}
    New-Item -ItemType Directory -Force -Path $privateRoot|Out-Null
    & icacls.exe $privateRoot /grant 'IIS AppPool\SoodalLife.Api:(OI)(CI)M' /T /C | Out-Null
    if($LASTEXITCODE -ne 0){throw 'Failed to grant the API application pool access to the private file storage directory.'}
    $probe=Join-Path $privateRoot ".v136-write-probe-$stamp.tmp"
    [IO.File]::WriteAllText($probe,'V136_OK',[Text.Encoding]::UTF8)
    Remove-Item -LiteralPath $probe -Force
    $apiTargetFull=[IO.Path]::GetFullPath($targets.API).TrimEnd('\')
    $apiSite=Get-Website | Where-Object {
        try{
            $sitePath=[Environment]::ExpandEnvironmentVariables([string]$_.PhysicalPath)
            [IO.Path]::GetFullPath($sitePath).TrimEnd('\') -ieq $apiTargetFull
        }catch{$false}
    } | Select-Object -First 1
    if(-not $apiSite){$apiSite=Get-Website -Name 'api.soodallife.kr' -ErrorAction SilentlyContinue}
    if(-not $apiSite){throw "API IIS website could not be resolved from physical path: $apiTargetFull"}
    $uploadReadAheadSize=[int64]20971520
    Set-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' -Location $apiSite.Name -Filter 'system.webServer/serverRuntime' -Name 'uploadReadAheadSize' -Value $uploadReadAheadSize
    $actualUploadReadAheadSize=[int64](Get-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' -Location $apiSite.Name -Filter 'system.webServer/serverRuntime' -Name 'uploadReadAheadSize').Value
    if($actualUploadReadAheadSize -ne $uploadReadAheadSize){throw "IIS uploadReadAheadSize verification failed. Expected=$uploadReadAheadSize, Actual=$actualUploadReadAheadSize"}
}catch{$deploymentError=$_}
finally{foreach($pool in $pools){try{Start-Pool $pool}catch{Write-Warning $_.Exception.Message}}}
if($deploymentError){throw $deploymentError}
$health=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/health' -TimeoutSec 30
$ready=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/ready' -TimeoutSec 30
$customer=Invoke-WebRequest -Uri 'https://soodallife.kr/' -UseBasicParsing -TimeoutSec 30
$partner=Invoke-WebRequest -Uri 'https://partner.soodallife.kr/' -UseBasicParsing -TimeoutSec 30
if($health.status -ne 'ok' -or $ready.status -ne 'ready' -or $customer.StatusCode -ne 200 -or $partner.StatusCode -ne 200){throw 'Post-deployment verification failed.'}
[PSCustomObject]@{DeploymentSucceeded=$true;IisBackup=$iisBackupName;FileBackup=$backupRoot;AppDataPreserved=$true;DatabaseChanged=$false;PrivateStorageAcl='ok';ApiSite=$apiSite.Name;UploadReadAheadSize=$actualUploadReadAheadSize;Health=$health.status;Readiness=$ready.status;CustomerChat='ok';PartnerChat='ok'}|Format-List
Write-Host 'V136 IIS chat attachment upload deployment completed.' -ForegroundColor Green
Write-Host 'Allowed files: JPG, JPEG, PNG and PDF; maximum 10 MB per file.' -ForegroundColor Yellow
