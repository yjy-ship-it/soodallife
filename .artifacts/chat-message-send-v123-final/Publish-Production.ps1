[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param([switch]$ConfirmProductionDeployment,[string]$AppsRoot='D:\SOODALLIFE\apps',[string]$BackupParent='D:\SOODALLIFE\releases')
$ErrorActionPreference='Stop'
if(-not $ConfirmProductionDeployment){throw 'Safety stop: rerun with -ConfirmProductionDeployment after checking the ZIP hash and backup path.'}
if(-not $PSCmdlet.ShouldProcess("API, customer and partner apps at $AppsRoot",'Deploy V123 chat message send fix')){return}
Import-Module WebAdministration
$releaseRoot=$PSScriptRoot;$stamp=Get-Date -Format 'yyyyMMdd-HHmmss';$backupRoot=Join-Path $BackupParent "backup-before-chat-message-send-v123-$stamp";$iisBackupName="Before_ChatMessageSendV123_$stamp"
$sources=[ordered]@{API=Join-Path $releaseRoot 'api';Customer=Join-Path $releaseRoot 'frontend\customer';Partner=Join-Path $releaseRoot 'frontend\partner'};$targets=[ordered]@{API=Join-Path $AppsRoot 'api';Customer=Join-Path $AppsRoot 'customer';Partner=Join-Path $AppsRoot 'partner'}
$required=@((Join-Path $sources.API 'SoodalLife.Api.dll'),(Join-Path $sources.API 'web.config'),(Join-Path $sources.Customer 'index.html'),(Join-Path $sources.Customer 'web.config'),(Join-Path $sources.Partner 'index.html'),(Join-Path $sources.Partner 'web.config'));foreach($file in $required){if(-not(Test-Path -LiteralPath $file -PathType Leaf)){throw "Required release file is missing: $file"}}
New-Item -ItemType Directory -Force -Path $backupRoot|Out-Null
function Copy-Checked([string]$Source,[string]$Destination,[switch]$PreserveAppData){New-Item -ItemType Directory -Force -Path $Destination|Out-Null;$arguments=@($Source,$Destination,'/E','/R:2','/W:2','/NFL','/NDL','/NJH','/NJS','/NP');if($PreserveAppData){$arguments+=@('/XD','App_Data')};& robocopy.exe @arguments;if($LASTEXITCODE -gt 7){throw "Robocopy failed ($LASTEXITCODE): $Source -> $Destination"}}
function Stop-Pool([string]$Name){if(-not(Test-Path "IIS:\AppPools\$Name") -or (Get-WebAppPoolState -Name $Name).Value -eq 'Stopped'){return};Stop-WebAppPool -Name $Name;for($i=0;$i -lt 30;$i++){Start-Sleep -Milliseconds 500;if((Get-WebAppPoolState -Name $Name).Value -eq 'Stopped'){return}};throw "Application pool did not stop: $Name"}
function Start-Pool([string]$Name){if((Test-Path "IIS:\AppPools\$Name") -and (Get-WebAppPoolState -Name $Name).Value -ne 'Started'){Start-WebAppPool -Name $Name}}
& "$env:windir\System32\inetsrv\appcmd.exe" add backup $iisBackupName;if($LASTEXITCODE -ne 0){throw 'IIS configuration backup failed.'};foreach($name in $targets.Keys){if(Test-Path -LiteralPath $targets[$name]){New-Item -ItemType Directory -Force -Path (Join-Path $backupRoot $name)|Out-Null;& robocopy.exe $targets[$name] (Join-Path $backupRoot $name) /E /R:2 /W:2 /NFL /NDL /NJH /NJS /NP;if($LASTEXITCODE -gt 7){throw "Backup failed: $name"}}}
$pools=@('SoodalLife.Api','soodallife.kr','SoodalLife.Static');$deploymentError=$null
try{foreach($pool in $pools){Stop-Pool $pool};foreach($name in $sources.Keys){Copy-Checked $sources[$name] $targets[$name] -PreserveAppData:($name -eq 'API')};New-Item -ItemType Directory -Force -Path (Join-Path $targets.API 'App_Data')|Out-Null}catch{$deploymentError=$_;Write-Warning "V123 deployment stopped. File backup: $backupRoot"}finally{foreach($pool in $pools){try{Start-Pool $pool}catch{Write-Warning $_.Exception.Message}}}
if($deploymentError){throw $deploymentError}
$healthResponse=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/health' -TimeoutSec 30;$readinessResponse=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/ready' -TimeoutSec 30
if($healthResponse.status -ne 'ok' -or $readinessResponse.status -ne 'ready'){throw 'Post-deployment health/readiness failed.'}
$customerResponse=Invoke-WebRequest -Uri 'https://soodallife.kr/customer/messages' -UseBasicParsing -TimeoutSec 30;$partnerResponse=Invoke-WebRequest -Uri 'https://partner.soodallife.kr/provider/messages' -UseBasicParsing -TimeoutSec 30
if($customerResponse.StatusCode -ne 200 -or $partnerResponse.StatusCode -ne 200){throw 'Post-deployment chat page verification failed.'}
[PSCustomObject]@{DeploymentSucceeded=$true;IisBackup=$iisBackupName;FileBackup=$backupRoot;AppDataPreserved=$true;DatabaseChanged=$false;Health=$healthResponse.status;Readiness=$readinessResponse.status;CustomerChat='ok';PartnerChat='ok'}|Format-List
Write-Host 'V123 chat message send deployment completed.' -ForegroundColor Green
