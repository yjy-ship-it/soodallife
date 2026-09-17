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
if(-not $PSCmdlet.ShouldProcess("API, customer, partner and admin apps at $AppsRoot plus database $DatabaseName",'Deploy V124 search, tax ledger and advertising targeting')){return}
Import-Module WebAdministration
$releaseRoot=$PSScriptRoot;$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot=Join-Path $BackupParent "backup-before-search-tax-advertising-v124-$stamp"
$iisBackupName="Before_SearchTaxAdvertisingV124_$stamp"
$databaseBackupPath=Join-Path $DatabaseBackupRoot "${DatabaseName}_PreSearchTaxAdvertisingV124_$stamp.bak"
$applySql=Join-Path $releaseRoot 'deployment\Apply-SearchTaxAdvertisingV124.sql'
$verifySql=Join-Path $releaseRoot 'deployment\Verify-SearchTaxAdvertisingV124.sql'
$sources=[ordered]@{API=Join-Path $releaseRoot 'api';Customer=Join-Path $releaseRoot 'frontend\customer';Partner=Join-Path $releaseRoot 'frontend\partner';Admin=Join-Path $releaseRoot 'frontend\admin'}
$targets=[ordered]@{API=Join-Path $AppsRoot 'api';Customer=Join-Path $AppsRoot 'customer';Partner=Join-Path $AppsRoot 'partner';Admin=Join-Path $AppsRoot 'admin'}
$required=@((Join-Path $sources.API 'SoodalLife.Api.dll'),(Join-Path $sources.API 'web.config'),(Join-Path $sources.Customer 'index.html'),(Join-Path $sources.Partner 'index.html'),(Join-Path $sources.Admin 'index.html'),$applySql,$verifySql)
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
$pools=@('SoodalLife.Api','soodallife.kr','SoodalLife.Static');$deploymentError=$null
try{
    foreach($pool in $pools){Stop-Pool $pool}
    & sqlcmd.exe -S $ServerInstance -E -C -I -b -f 65001 -d $DatabaseName -i $applySql
    if($LASTEXITCODE -ne 0){throw 'V124 database apply failed.'}
    foreach($name in $sources.Keys){Copy-Checked $sources[$name] $targets[$name] -PreserveAppData:($name -eq 'API')}
    New-Item -ItemType Directory -Force -Path (Join-Path $targets.API 'App_Data')|Out-Null
    $serviceLocations=& sqlcmd.exe -S $ServerInstance -E -C -h -1 -W -w 4096 -d $DatabaseName -Q "SET NOCOUNT ON; SELECT '  <url><loc>https://soodallife.kr/services/' + search_slug + '</loc><changefreq>weekly</changefreq><priority>0.8</priority></url>' FROM dbo.service_categories WHERE level_code='SERVICE' AND status_code='ACTIVE' AND is_search_indexable=1 AND search_slug IS NOT NULL ORDER BY display_order,id;"
    $sitemap=@('<?xml version="1.0" encoding="UTF-8"?>','<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">','  <url><loc>https://soodallife.kr/</loc><changefreq>daily</changefreq><priority>1.0</priority></url>','  <url><loc>https://soodallife.kr/services</loc><changefreq>daily</changefreq><priority>0.9</priority></url>') + @($serviceLocations|Where-Object{$_ -and $_.Trim()}) + @('</urlset>')
    Set-Content -LiteralPath (Join-Path $targets.Customer 'sitemap.xml') -Value $sitemap -Encoding utf8
}catch{$deploymentError=$_;Write-Warning "V124 deployment stopped. Database backup: $databaseBackupPath"}
finally{foreach($pool in $pools){try{Start-Pool $pool}catch{Write-Warning $_.Exception.Message}}}
if($deploymentError){throw $deploymentError}
& sqlcmd.exe -S $ServerInstance -E -C -I -b -f 65001 -d $DatabaseName -i $verifySql
if($LASTEXITCODE -ne 0){throw 'V124 database verification failed.'}
$health=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/health' -TimeoutSec 30
$ready=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/ready' -TimeoutSec 30
$customer=Invoke-WebRequest -Uri 'https://soodallife.kr/services' -UseBasicParsing -TimeoutSec 30
$robots=Invoke-WebRequest -Uri 'https://soodallife.kr/robots.txt' -UseBasicParsing -TimeoutSec 30
if($health.status -ne 'ok' -or $ready.status -ne 'ready' -or $customer.StatusCode -ne 200 -or $robots.StatusCode -ne 200){throw 'Post-deployment verification failed.'}
[PSCustomObject]@{DeploymentSucceeded=$true;IisBackup=$iisBackupName;FileBackup=$backupRoot;DatabaseBackup=$databaseBackupPath;DatabaseChanged=$true;Health=$health.status;Readiness=$ready.status;ServiceSearchPage='ok';Robots='ok'}|Format-List
Write-Host 'V124 search, tax ledger and advertising targeting deployment completed.' -ForegroundColor Green
Write-Host 'Register https://soodallife.kr/sitemap.xml in each portal webmaster console after deployment.' -ForegroundColor Yellow
