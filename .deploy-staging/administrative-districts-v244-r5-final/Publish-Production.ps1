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
if(-not $ConfirmProductionDeployment){throw 'Safety stop: add -ConfirmProductionDeployment after checking the package, database and target paths.'}
$expected=[IO.Path]::GetFullPath('D:\SOODALLIFE\apps').TrimEnd('\')
$resolved=[IO.Path]::GetFullPath($AppsRoot).TrimEnd('\')
if($resolved-ne $expected){throw "Unexpected AppsRoot. Expected=$expected, Actual=$resolved"}
$identity=[Security.Principal.WindowsIdentity]::GetCurrent()
$principal=[Security.Principal.WindowsPrincipal]::new($identity)
if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Run Windows PowerShell as Administrator.'}
if(-not(Get-Command sqlcmd.exe -ErrorAction SilentlyContinue)){throw 'sqlcmd.exe is required.'}

$root=$PSScriptRoot
$sources=[ordered]@{API=Join-Path $root 'api';Customer=Join-Path $root 'frontend\customer';Partner=Join-Path $root 'frontend\partner';Admin=Join-Path $root 'frontend\admin'}
$targets=[ordered]@{API=Join-Path $resolved 'api';Customer=Join-Path $resolved 'customer';Partner=Join-Path $resolved 'partner';Admin=Join-Path $resolved 'admin'}
foreach($path in $targets.Values){$full=[IO.Path]::GetFullPath($path);if(-not $full.StartsWith($resolved+'\',[StringComparison]::OrdinalIgnoreCase)){throw "Target escaped AppsRoot: $full"}}
$applySql=Join-Path $root 'deployment\Apply-MobileCareProposalV243.sql'
$verifySql=Join-Path $root 'deployment\Verify-MobileCareProposalV243.sql'
$workflowApplySql=Join-Path $root 'deployment\Apply-ProviderWorkflowV243.sql'
$workflowVerifySql=Join-Path $root 'deployment\Verify-ProviderWorkflowV243.sql'
$districtApplySql=Join-Path $root 'deployment\Apply-AdministrativeDistrictsV243.sql'
$districtVerifySql=Join-Path $root 'deployment\Verify-AdministrativeDistrictsV243.sql'
$retireCitiesApplySql=Join-Path $root 'deployment\Apply-RetireParentCitiesV244.sql'
$retireCitiesVerifySql=Join-Path $root 'deployment\Verify-RetireParentCitiesV244.sql'
$required=@((Join-Path $sources.API 'SoodalLife.Api.dll'),(Join-Path $sources.API 'web.config'),(Join-Path $sources.API 'V244-R5-API-MARKER.txt'),(Join-Path $sources.Customer 'index.html'),(Join-Path $sources.Partner 'index.html'),(Join-Path $sources.Admin 'index.html'),$applySql,$verifySql,$workflowApplySql,$workflowVerifySql,$districtApplySql,$districtVerifySql,$retireCitiesApplySql,$retireCitiesVerifySql)
foreach($file in $required){if(-not(Test-Path -LiteralPath $file -PathType Leaf)){throw "Required V243 file is missing: $file"}}
if(Get-ChildItem -LiteralPath $sources.API -Filter 'appsettings*.json' -File -ErrorAction SilentlyContinue){throw 'Package must not contain appsettings files.'}
if(-not $PSCmdlet.ShouldProcess("Database $DatabaseName plus API, customer, partner and admin apps",'Deploy V244-R5 non-transaction request cleanup and parent city deletion')){return}

Import-Module WebAdministration
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot=Join-Path $BackupParent "backup-before-administrative-districts-v244-r5-$stamp"
$databaseBackupPath=Join-Path $DatabaseBackupRoot "${DatabaseName}_PreAdministrativeDistrictsV244R5_$stamp.bak"
$iisBackup="Before_AdministrativeDistrictsV244R5_$stamp"
New-Item -ItemType Directory -Force -Path $backupRoot,$DatabaseBackupRoot|Out-Null
function Copy-Checked([string]$Source,[string]$Destination,[switch]$Mirror,[switch]$PreserveApiData){
    New-Item -ItemType Directory -Force -Path $Destination|Out-Null
    $arguments=@($Source,$Destination,$(if($Mirror){'/MIR'}else{'/E'}),'/R:2','/W:2','/NFL','/NDL','/NJH','/NJS','/NP')
    if($PreserveApiData){$arguments+=@('/XD','App_Data','/XF','appsettings*.json')}
    & robocopy.exe @arguments
    if($LASTEXITCODE-gt 7){throw "Robocopy failed: $Source"}
}
function Get-PoolStateSafe([string]$Name){try{(Get-WebAppPoolState -Name $Name -ErrorAction Stop).Value}catch{$null}}
function Stop-PoolSafe([string]$Name){if(-not(Test-Path "IIS:\AppPools\$Name")){return};if((Get-PoolStateSafe $Name)-ne 'Stopped'){Stop-WebAppPool $Name};for($i=0;$i-lt 40;$i++){if((Get-PoolStateSafe $Name)-eq 'Stopped'){return};Start-Sleep -Milliseconds 500};throw "Pool stop timeout: $Name"}
function Start-PoolSafe([string]$Name){if(-not(Test-Path "IIS:\AppPools\$Name")){return};for($i=0;$i-lt 40;$i++){if((Get-PoolStateSafe $Name)-eq 'Started'){return};try{Start-WebAppPool $Name}catch{};Start-Sleep -Milliseconds 750};throw "Pool start timeout: $Name"}

& "$env:windir\System32\inetsrv\appcmd.exe" add backup $iisBackup
if($LASTEXITCODE-ne 0){throw 'IIS backup failed.'}
foreach($name in $targets.Keys){if(Test-Path -LiteralPath $targets[$name]){Copy-Checked $targets[$name] (Join-Path $backupRoot $name)}}
$escapedBackup=$databaseBackupPath.Replace("'","''")
& sqlcmd.exe -S $ServerInstance -E -C -b -Q "BACKUP DATABASE [$DatabaseName] TO DISK=N'$escapedBackup' WITH COPY_ONLY, CHECKSUM, COMPRESSION, INIT; RESTORE VERIFYONLY FROM DISK=N'$escapedBackup' WITH CHECKSUM;"
if($LASTEXITCODE-ne 0){throw 'Database backup verification failed.'}

$pools=@('SoodalLife.Api','SoodalLife.Static')
$failure=$null
try{
    foreach($pool in $pools){Stop-PoolSafe $pool}
    & sqlcmd.exe -S $ServerInstance -E -C -I -b -f 65001 -d $DatabaseName -i $applySql
    if($LASTEXITCODE-ne 0){throw 'V243 database apply failed.'}
    & sqlcmd.exe -S $ServerInstance -E -C -I -b -f 65001 -d $DatabaseName -i $workflowApplySql
    if($LASTEXITCODE-ne 0){throw 'V243 workflow and catalog data apply failed.'}
    & sqlcmd.exe -S $ServerInstance -E -C -I -b -f 65001 -d $DatabaseName -i $districtApplySql
    if($LASTEXITCODE-ne 0){throw 'V243 administrative district apply failed.'}
    & sqlcmd.exe -S $ServerInstance -E -C -I -b -f 65001 -d $DatabaseName -i $retireCitiesApplySql
    if($LASTEXITCODE-ne 0){throw 'V244 parent city retirement apply failed.'}
    Copy-Checked $sources.API $targets.API -Mirror -PreserveApiData
    Copy-Checked $sources.Customer $targets.Customer -Mirror
    Copy-Checked $sources.Partner $targets.Partner -Mirror
    Copy-Checked $sources.Admin $targets.Admin -Mirror
    New-Item -ItemType Directory -Force -Path (Join-Path $targets.API 'App_Data\diagnostics')|Out-Null
}catch{$failure=$_;Write-Warning "V243 deployment stopped. File backup: $backupRoot. Database backup: $databaseBackupPath"}
finally{foreach($pool in $pools){try{Start-PoolSafe $pool}catch{Write-Warning $_.Exception.Message}}}
if($failure){throw $failure}

& sqlcmd.exe -S $ServerInstance -E -C -I -b -f 65001 -d $DatabaseName -i $verifySql
if($LASTEXITCODE-ne 0){throw 'V243 database verification failed.'}
& sqlcmd.exe -S $ServerInstance -E -C -I -b -f 65001 -d $DatabaseName -i $workflowVerifySql
if($LASTEXITCODE-ne 0){throw 'V243 workflow and catalog verification failed.'}
& sqlcmd.exe -S $ServerInstance -E -C -I -b -f 65001 -d $DatabaseName -i $districtVerifySql
if($LASTEXITCODE-ne 0){throw 'V243 administrative district verification failed.'}
& sqlcmd.exe -S $ServerInstance -E -C -I -b -f 65001 -d $DatabaseName -i $retireCitiesVerifySql
if($LASTEXITCODE-ne 0){throw 'V244 parent city retirement verification failed.'}
$health=Invoke-RestMethod 'https://api.soodallife.kr/api/v1/system/health' -TimeoutSec 30
$ready=Invoke-RestMethod 'https://api.soodallife.kr/api/v1/system/ready' -TimeoutSec 30
$customer=Invoke-WebRequest 'https://soodallife.kr/' -UseBasicParsing -TimeoutSec 30
$partner=Invoke-WebRequest 'https://partner.soodallife.kr/' -UseBasicParsing -TimeoutSec 30
$admin=Invoke-WebRequest 'https://admin.soodallife.kr/' -UseBasicParsing -TimeoutSec 30
$marker=(Get-Content -LiteralPath (Join-Path $targets.API 'V244-R5-API-MARKER.txt') -Raw -Encoding UTF8).Contains('V244-R5 non-transaction request data cleaned; parent cities deleted; districts preserved: enabled')
if($health.status-ne 'ok'-or $ready.status-ne 'ready'-or $customer.StatusCode-ne 200-or $partner.StatusCode-ne 200-or $admin.StatusCode-ne 200-or -not $marker){throw 'Post-deployment verification failed.'}
[PSCustomObject]@{DeploymentSucceeded=$true;IisBackup=$iisBackup;FileBackup=$backupRoot;DatabaseBackup=$databaseBackupPath;DatabaseChanged=$true;ApiChanged=$true;CustomerFrontendChanged=$true;PartnerFrontendChanged=$true;AdminFrontendChanged=$true;DistanceDecline='fixed';DesiredBudget='formatted-won';ImageCopy='pdf-removed';CareRecurrence='validated-and-explained';ProposalApplication='visible-and-tracked';ProviderMobileSchedule='enabled';MobileExitConfirmation='soodal-dialog';ProviderQuoteTemplates='included';AdministrativeDistricts='39-preserved';ParentCityRows='13-deleted';SavedAddressRows='preserved-area-link-cleared';NonTransactionRequestData='cleaned';RequestHistoryGuard='quotes-transactions-site-work-protected';ForeignKeyGuard='enabled';AppDataPreserved=$true;AppSettingsPreserved=$true;CustomerApp='ok';PartnerApp='ok';AdminApp='ok';Health=$health.status;Readiness=$ready.status}|Format-List
Write-Host 'V244-R5 parent city deletion deployment completed.' -ForegroundColor Green
Write-Host 'Database schema changed after a verified backup. API App_Data and production settings were preserved.' -ForegroundColor Yellow
