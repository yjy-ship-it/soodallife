[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [switch]$ConfirmProductionDeployment,
    [string]$AppsRoot='D:\SOODALLIFE\apps',
    [string]$BackupParent='D:\SOODALLIFE\releases'
)

$ErrorActionPreference='Stop'
if(-not $ConfirmProductionDeployment){throw 'Safety stop: rerun with -ConfirmProductionDeployment after checking the ZIP hash and target paths.'}
if(-not $PSCmdlet.ShouldProcess("API app at $AppsRoot",'Deploy V179 emergency policy selection fix')){return}
$identity=[Security.Principal.WindowsIdentity]::GetCurrent()
$principal=[Security.Principal.WindowsPrincipal]::new($identity)
if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Run PowerShell as Administrator and try again.'}
Import-Module WebAdministration

$releaseRoot=$PSScriptRoot
$source=Join-Path $releaseRoot 'api'
$target=Join-Path $AppsRoot 'api'
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot=Join-Path $BackupParent "backup-before-emergency-policy-selection-v179-$stamp"
$iisBackupName="Before_EmergencyPolicySelectionV179_$stamp"
$required=@((Join-Path $source 'SoodalLife.Api.dll'),(Join-Path $source 'web.config'),(Join-Path $releaseRoot 'RELEASE-MANIFEST.txt'))
foreach($file in $required){if(-not(Test-Path -LiteralPath $file -PathType Leaf)){throw "Required V179 release file is missing: $file"}}
if(Test-Path -LiteralPath (Join-Path $source 'appsettings.json')){throw 'V179 package unexpectedly contains appsettings.json.'}

function Copy-Checked([string]$Source,[string]$Destination,[switch]$Mirror,[switch]$PreserveApiData){
    New-Item -ItemType Directory -Force -Path $Destination|Out-Null
    $arguments=@($Source,$Destination,($(if($Mirror){'/MIR'}else{'/E'})),'/R:2','/W:2','/NFL','/NDL','/NJH','/NJS','/NP')
    if($PreserveApiData){$arguments+=@('/XD','App_Data','/XF','appsettings.json','appsettings.Production.json','appsettings.Development.json')}
    & robocopy.exe @arguments
    if($LASTEXITCODE-gt 7){throw "Robocopy failed ($LASTEXITCODE): $Source -> $Destination"}
}
function Get-PoolState([string]$Name){try{return(Get-WebAppPoolState -Name $Name -ErrorAction Stop).Value}catch{return $null}}
function Stop-Pool([string]$Name){if(-not(Test-Path "IIS:\AppPools\$Name")){return};if((Get-PoolState $Name)-ne 'Stopped'){Stop-WebAppPool -Name $Name};for($i=0;$i-lt 40;$i++){if((Get-PoolState $Name)-eq 'Stopped'){return};Start-Sleep -Milliseconds 500};throw "Application pool did not stop: $Name"}
function Start-Pool([string]$Name){if(-not(Test-Path "IIS:\AppPools\$Name")){return};for($i=0;$i-lt 40;$i++){if((Get-PoolState $Name)-eq 'Started'){return};try{Start-WebAppPool -Name $Name}catch{};Start-Sleep -Milliseconds 750};throw "Application pool did not start: $Name"}

New-Item -ItemType Directory -Force -Path $backupRoot|Out-Null
& "$env:windir\System32\inetsrv\appcmd.exe" add backup $iisBackupName
if($LASTEXITCODE-ne 0){throw 'IIS configuration backup failed.'}
if(Test-Path -LiteralPath $target){Copy-Checked $target (Join-Path $backupRoot 'API')}
$deploymentError=$null
try{
    Stop-Pool 'SoodalLife.Api'
    Copy-Checked $source $target -Mirror -PreserveApiData
    New-Item -ItemType Directory -Force -Path (Join-Path $target 'App_Data')|Out-Null
}catch{
    $deploymentError=$_
    Write-Warning "V179 deployment stopped. File backup: $backupRoot"
    $backup=Join-Path $backupRoot 'API'
    if(Test-Path -LiteralPath $backup){Copy-Checked $backup $target -Mirror}
}finally{try{Start-Pool 'SoodalLife.Api'}catch{Write-Warning $_.Exception.Message}}
if($deploymentError){throw $deploymentError}

$health=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/health' -TimeoutSec 30
$ready=Invoke-RestMethod -Uri 'https://api.soodallife.kr/api/v1/system/ready' -TimeoutSec 30
if($health.status-ne'ok'-or$ready.status-ne'ready'){throw 'Post-deployment API verification failed.'}
[PSCustomObject]@{DeploymentSucceeded=$true;IisBackup=$iisBackupName;FileBackup=$backupRoot;AppDataPreserved=$true;AppSettingsPreserved=$true;DatabaseChanged=$false;ApiChanged=$true;Health=$health.status;Readiness=$ready.status;EmergencyPolicySelection='allowed-current-policy';EmergencyCatalogDetail='aligned';CustomerApp='unchanged';PartnerApp='unchanged';AdminApp='unchanged'}|Format-List
Write-Host 'V179 emergency policy selection deployment completed.' -ForegroundColor Green
Write-Host 'Emergency catalog, detail and request save now use the same allowed current policy.' -ForegroundColor Yellow
Write-Host 'Database, frontend files and production settings were not changed.' -ForegroundColor Yellow
