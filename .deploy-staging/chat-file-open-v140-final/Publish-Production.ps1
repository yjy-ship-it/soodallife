[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [switch]$ConfirmProductionDeployment,
    [string]$AppsRoot='D:\SOODALLIFE\apps',
    [string]$BackupParent='D:\SOODALLIFE\releases'
)
$ErrorActionPreference='Stop'
if(-not $ConfirmProductionDeployment){throw 'Safety stop: rerun with -ConfirmProductionDeployment after checking the ZIP hash and backup path.'}
if(-not $PSCmdlet.ShouldProcess("Customer and partner apps at $AppsRoot",'Deploy V140 authenticated chat file link fix')){return}
Import-Module WebAdministration
$releaseRoot=$PSScriptRoot
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot=Join-Path $BackupParent "backup-before-chat-file-open-v140-$stamp"
$iisBackupName="Before_ChatFileOpenV140_$stamp"
$sources=[ordered]@{Customer=Join-Path $releaseRoot 'frontend\customer';Partner=Join-Path $releaseRoot 'frontend\partner'}
$targets=[ordered]@{Customer=Join-Path $AppsRoot 'customer';Partner=Join-Path $AppsRoot 'partner'}
foreach($name in $sources.Keys){foreach($requiredName in @('index.html','web.config')){$requiredFile=Join-Path $sources[$name] $requiredName;if(-not(Test-Path -LiteralPath $requiredFile -PathType Leaf)){throw "Required frontend file is missing: $requiredFile"}}}
New-Item -ItemType Directory -Force -Path $backupRoot|Out-Null
function Copy-Checked([string]$Source,[string]$Destination,[switch]$Mirror){
    New-Item -ItemType Directory -Force -Path $Destination|Out-Null
    $mode=if($Mirror){'/MIR'}else{'/E'}
    & robocopy.exe $Source $Destination $mode /R:2 /W:2 /NFL /NDL /NJH /NJS /NP
    if($LASTEXITCODE -gt 7){throw "Robocopy failed ($LASTEXITCODE): $Source -> $Destination"}
}
function Get-PoolState([string]$Name){try{return (Get-WebAppPoolState -Name $Name -ErrorAction Stop).Value}catch{return $null}}
function Stop-Pool([string]$Name){if(-not(Test-Path "IIS:\AppPools\$Name")){return};if((Get-PoolState $Name) -ne 'Stopped'){Stop-WebAppPool -Name $Name};for($i=0;$i -lt 40;$i++){if((Get-PoolState $Name) -eq 'Stopped'){return};Start-Sleep -Milliseconds 500};throw "Application pool did not stop: $Name"}
function Start-Pool([string]$Name){if(-not(Test-Path "IIS:\AppPools\$Name")){return};for($i=0;$i -lt 40;$i++){if((Get-PoolState $Name) -eq 'Started'){return};try{Start-WebAppPool -Name $Name -ErrorAction Stop}catch{};Start-Sleep -Milliseconds 750};throw "Application pool did not start: $Name"}
& "$env:windir\System32\inetsrv\appcmd.exe" add backup $iisBackupName
if($LASTEXITCODE -ne 0){throw 'IIS configuration backup failed.'}
foreach($name in $targets.Keys){if(Test-Path -LiteralPath $targets[$name]){Copy-Checked $targets[$name] (Join-Path $backupRoot $name)}}
$pools=@('soodallife.kr','SoodalLife.Static')
$deploymentError=$null
try{
    foreach($pool in $pools){Stop-Pool $pool}
    foreach($name in $sources.Keys){Copy-Checked $sources[$name] $targets[$name] -Mirror}
}catch{
    $deploymentError=$_
    Write-Warning 'V140 deployment failed. Restoring the pre-deployment frontend backup.'
    foreach($name in $targets.Keys){$backup=Join-Path $backupRoot $name;if(Test-Path -LiteralPath $backup){Copy-Checked $backup $targets[$name] -Mirror}}
}finally{foreach($pool in $pools){try{Start-Pool $pool}catch{Write-Warning $_.Exception.Message}}}
if($deploymentError){throw $deploymentError}
$customer=Invoke-WebRequest -Uri 'https://soodallife.kr/customer/messages' -UseBasicParsing -TimeoutSec 30
$partner=Invoke-WebRequest -Uri 'https://partner.soodallife.kr/provider/messages' -UseBasicParsing -TimeoutSec 30
$preflight=Invoke-WebRequest -Method Options -Uri 'https://api.soodallife.kr/api/v1/chat/rooms/00000000-0000-0000-0000-000000000000/attachments/00000000-0000-0000-0000-000000000000' -Headers @{Origin='https://soodallife.kr';'Access-Control-Request-Method'='GET'} -UseBasicParsing -TimeoutSec 30
if($customer.StatusCode -ne 200 -or $partner.StatusCode -ne 200 -or $preflight.StatusCode -ne 204){throw 'Post-deployment chat file-link verification failed.'}
[PSCustomObject]@{DeploymentSucceeded=$true;IisBackup=$iisBackupName;FileBackup=$backupRoot;ApiPreserved=$true;DatabaseChanged=$false;CustomerChat='ok';PartnerChat='ok';AttachmentPreflight='ok'}|Format-List
Write-Host 'V140 authenticated chat file link deployment completed.' -ForegroundColor Green
