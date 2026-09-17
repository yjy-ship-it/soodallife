[CmdletBinding()]
param([string]$AppsRoot='D:\SOODALLIFE\apps',[string]$BackupParent='D:\SOODALLIFE\releases')
$ErrorActionPreference='Stop'
Import-Module WebAdministration

$releaseRoot=$PSScriptRoot
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot=Join-Path $BackupParent "backup-before-customer-service-card-v113-$stamp"
$iisBackupName="Before_CustomerServiceCardV113_$stamp"
$sources=[ordered]@{
    Customer=Join-Path $releaseRoot 'frontend\customer'
    Partner=Join-Path $releaseRoot 'frontend\partner'
    Admin=Join-Path $releaseRoot 'frontend\admin'
}
$targets=[ordered]@{
    Customer=Join-Path $AppsRoot 'customer'
    Partner=Join-Path $AppsRoot 'partner'
    Admin=Join-Path $AppsRoot 'admin'
}

foreach($name in $sources.Keys){
    foreach($requiredName in @('index.html','web.config','manifest.webmanifest','sw.js')){
        $requiredFile=Join-Path $sources[$name] $requiredName
        if(-not(Test-Path -LiteralPath $requiredFile -PathType Leaf)){throw "Required frontend file is missing: $requiredFile"}
    }
}
New-Item -ItemType Directory -Force -Path $backupRoot|Out-Null

function Copy-Checked([string]$Source,[string]$Destination,[switch]$Mirror){
    New-Item -ItemType Directory -Force -Path $Destination|Out-Null
    $mode=if($Mirror){'/MIR'}else{'/E'}
    & robocopy.exe $Source $Destination $mode /R:2 /W:2 /NFL /NDL /NJH /NJS /NP
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
foreach($name in $targets.Keys){if(Test-Path -LiteralPath $targets[$name]){Copy-Checked $targets[$name] (Join-Path $backupRoot $name)}}

$pools=@('soodallife.kr','SoodalLife.Static')
$deploymentError=$null
try{
    foreach($pool in $pools){Stop-Pool $pool}
    foreach($name in $sources.Keys){Copy-Checked $sources[$name] $targets[$name] -Mirror}
}catch{
    $deploymentError=$_
    Write-Warning 'Frontend deployment failed. Restoring the pre-deployment file backup.'
    foreach($name in $targets.Keys){$backup=Join-Path $backupRoot $name;if(Test-Path -LiteralPath $backup){Copy-Checked $backup $targets[$name] -Mirror}}
}finally{
    foreach($pool in $pools){try{Start-Pool $pool}catch{Write-Warning $_.Exception.Message}}
}
if($deploymentError){throw $deploymentError}

$checks=[ordered]@{
    Customer='https://soodallife.kr/'
    CustomerServices='https://soodallife.kr/services'
    CustomerCompany='https://soodallife.kr/company'
    CustomerFaq='https://soodallife.kr/faq'
    Partner='https://partner.soodallife.kr/'
    PartnerHome='https://partner.soodallife.kr/provider'
    PartnerCompany='https://partner.soodallife.kr/company'
    Admin='https://admin.soodallife.kr/'
    AdminNotifications='https://admin.soodallife.kr/admin/notifications'
}
foreach($name in $checks.Keys){$response=Invoke-WebRequest -Uri $checks[$name] -UseBasicParsing -TimeoutSec 30;if($response.StatusCode -ne 200){throw "$name site check failed: HTTP $($response.StatusCode)"}}

[PSCustomObject]@{
    DeploymentSucceeded=$true
    IisBackup=$iisBackupName
    FileBackup=$backupRoot
    ApiPreserved=$true
    DatabaseChanged=$false
    CustomerSite='ok'
    PartnerSite='ok'
    AdminSite='ok'
}|Format-List
Write-Host 'V113 customer service card deployment completed.' -ForegroundColor Green
