[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param([switch]$ConfirmProductionDeployment,[string]$AppsRoot='D:\SOODALLIFE\apps',[string]$BackupParent='D:\SOODALLIFE\releases')
$ErrorActionPreference='Stop'
if(-not $ConfirmProductionDeployment){throw 'Safety stop: rerun with -ConfirmProductionDeployment after checking the ZIP hash and backup path.'}
if(-not $PSCmdlet.ShouldProcess("Partner IIS app at $AppsRoot",'Deploy V122 provider role navigation')){return}
Import-Module WebAdministration
$releaseRoot=$PSScriptRoot;$source=Join-Path $releaseRoot 'frontend\partner';$target=Join-Path $AppsRoot 'partner';$stamp=Get-Date -Format 'yyyyMMdd-HHmmss';$backupRoot=Join-Path $BackupParent "backup-before-provider-role-navigation-v122-$stamp";$iisBackupName="Before_ProviderRoleNavigationV122_$stamp"
foreach($name in @('index.html','web.config','manifest.webmanifest','sw.js')){$file=Join-Path $source $name;if(-not(Test-Path -LiteralPath $file -PathType Leaf)){throw "Required frontend file is missing: $file"}}
New-Item -ItemType Directory -Force -Path $backupRoot|Out-Null
function Copy-Checked([string]$From,[string]$To){New-Item -ItemType Directory -Force -Path $To|Out-Null;& robocopy.exe $From $To /E /R:2 /W:2 /NFL /NDL /NJH /NJS /NP;if($LASTEXITCODE -gt 7){throw "Robocopy failed ($LASTEXITCODE): $From -> $To"}}
function Stop-Pool([string]$Name){if(-not(Test-Path "IIS:\AppPools\$Name") -or (Get-WebAppPoolState -Name $Name).Value -eq 'Stopped'){return};Stop-WebAppPool -Name $Name;for($i=0;$i -lt 30;$i++){Start-Sleep -Milliseconds 500;if((Get-WebAppPoolState -Name $Name).Value -eq 'Stopped'){return}};throw "Application pool did not stop: $Name"}
function Start-Pool([string]$Name){if((Test-Path "IIS:\AppPools\$Name") -and (Get-WebAppPoolState -Name $Name).Value -ne 'Started'){Start-WebAppPool -Name $Name}}
& "$env:windir\System32\inetsrv\appcmd.exe" add backup $iisBackupName
if($LASTEXITCODE -ne 0){throw 'IIS configuration backup failed.'}
if(Test-Path -LiteralPath $target){Copy-Checked $target $backupRoot}
$deploymentError=$null
try{Stop-Pool 'SoodalLife.Static';Copy-Checked $source $target}catch{$deploymentError=$_;Write-Warning "V122 deployment stopped. File backup: $backupRoot"}finally{try{Start-Pool 'SoodalLife.Static'}catch{Write-Warning $_.Exception.Message}}
if($deploymentError){throw $deploymentError}
$notification=Invoke-WebRequest -Uri 'https://partner.soodallife.kr/provider/notifications' -UseBasicParsing -TimeoutSec 30
$providerHomeResponse=Invoke-WebRequest -Uri 'https://partner.soodallife.kr/provider' -UseBasicParsing -TimeoutSec 30
if($notification.StatusCode -ne 200 -or $providerHomeResponse.StatusCode -ne 200){throw 'Provider page verification failed.'}
[PSCustomObject]@{DeploymentSucceeded=$true;IisBackup=$iisBackupName;FileBackup=$backupRoot;ApiPreserved=$true;DatabaseChanged=$false;ProviderNotifications='ok';ProviderHome='ok'}|Format-List
Write-Host 'V122 provider role navigation deployment completed.' -ForegroundColor Green
