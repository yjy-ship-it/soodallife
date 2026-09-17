[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param([switch]$ConfirmProductionDeployment,[string]$AppsRoot='D:\SOODALLIFE\apps',[string]$BackupParent='D:\SOODALLIFE\releases')
$ErrorActionPreference='Stop'
if(-not $ConfirmProductionDeployment){throw 'Safety stop: rerun with -ConfirmProductionDeployment after checking the ZIP hash and backup path.'}
if(-not $PSCmdlet.ShouldProcess("Partner IIS app at $AppsRoot","Deploy V119 provider advertising customer link")){return}
Import-Module WebAdministration
$releaseRoot=$PSScriptRoot;$source=Join-Path $releaseRoot 'frontend\partner';$target=Join-Path $AppsRoot 'partner';$stamp=Get-Date -Format 'yyyyMMdd-HHmmss';$backupRoot=Join-Path $BackupParent "backup-before-provider-advertising-customer-link-v119-$stamp";$iisBackupName="Before_ProviderAdvertisingCustomerLinkV119_$stamp"
foreach($name in @('index.html','web.config','manifest.webmanifest','sw.js')){$file=Join-Path $source $name;if(-not(Test-Path -LiteralPath $file -PathType Leaf)){throw "Required frontend file is missing: $file"}}
New-Item -ItemType Directory -Force -Path $backupRoot|Out-Null
function Copy-Checked([string]$From,[string]$To,[switch]$Mirror){New-Item -ItemType Directory -Force -Path $To|Out-Null;$mode=if($Mirror){'/MIR'}else{'/E'};& robocopy.exe $From $To $mode /R:2 /W:2 /NFL /NDL /NJH /NJS /NP;if($LASTEXITCODE -gt 7){throw "Robocopy failed ($LASTEXITCODE): $From -> $To"}}
function Stop-Pool([string]$Name){if(-not(Test-Path "IIS:\AppPools\$Name") -or (Get-WebAppPoolState -Name $Name).Value -eq 'Stopped'){return};Stop-WebAppPool -Name $Name;for($i=0;$i -lt 30;$i++){Start-Sleep -Milliseconds 500;if((Get-WebAppPoolState -Name $Name).Value -eq 'Stopped'){return}};throw "Application pool did not stop: $Name"}
function Start-Pool([string]$Name){if((Test-Path "IIS:\AppPools\$Name") -and (Get-WebAppPoolState -Name $Name).Value -ne 'Started'){Start-WebAppPool -Name $Name}}
& "$env:windir\System32\inetsrv\appcmd.exe" add backup $iisBackupName;if($LASTEXITCODE -ne 0){throw 'IIS configuration backup failed.'};if(Test-Path -LiteralPath $target){Copy-Checked $target $backupRoot}
$deploymentError=$null;try{Stop-Pool 'SoodalLife.Static';Copy-Checked $source $target -Mirror}catch{$deploymentError=$_;if(Test-Path -LiteralPath (Join-Path $backupRoot 'index.html')){Copy-Checked $backupRoot $target -Mirror}}finally{try{Start-Pool 'SoodalLife.Static'}catch{Write-Warning $_.Exception.Message}};if($deploymentError){throw $deploymentError}
$page=Invoke-WebRequest -Uri 'https://partner.soodallife.kr/provider/advertising' -UseBasicParsing -TimeoutSec 30;if($page.StatusCode -ne 200){throw "Provider advertising page failed: HTTP $($page.StatusCode)"}
[PSCustomObject]@{DeploymentSucceeded=$true;IisBackup=$iisBackupName;FileBackup=$backupRoot;ApiPreserved=$true;DatabaseChanged=$false;PartnerAdvertisingPage='ok'}|Format-List
Write-Host 'V119 provider advertising customer link deployment completed.' -ForegroundColor Green
