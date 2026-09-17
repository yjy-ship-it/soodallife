[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [string]$ZipPath=(Join-Path $PSScriptRoot 'SoodalLife-CommunicationCentering-20260831-v177.zip'),
    [string]$ReleaseBasePath='D:\SOODALLIFE\releases',
    [string]$AppsRoot='D:\SOODALLIFE\apps',
    [switch]$ConfirmProductionDeployment
)

$ErrorActionPreference='Stop'
$expectedSha256='9AC8B8014547A8CC52147C933EC9763813EB709AE1D3B18A58FCE23466D42327'
if(-not $ConfirmProductionDeployment){throw 'Safety stop: add -ConfirmProductionDeployment only after checking the ZIP hash and target paths.'}
$identity=[Security.Principal.WindowsIdentity]::GetCurrent()
$principal=[Security.Principal.WindowsPrincipal]::new($identity)
if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Run PowerShell as Administrator and try again.'}
if(-not(Test-Path -LiteralPath $ZipPath -PathType Leaf)){throw "Deployment ZIP not found: $ZipPath"}
$actual=(Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash
if($actual-ne$expectedSha256){throw "ZIP SHA-256 mismatch. Expected=$expectedSha256, Actual=$actual"}
if(-not(Test-Path -LiteralPath $ReleaseBasePath -PathType Container)){New-Item -ItemType Directory -Path $ReleaseBasePath|Out-Null}

$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$extractPath=Join-Path $ReleaseBasePath "communication-centering-v177-$stamp"
New-Item -ItemType Directory -Path $extractPath|Out-Null
Write-Host '1/4 ZIP integrity check passed.' -ForegroundColor Green
Expand-Archive -LiteralPath $ZipPath -DestinationPath $extractPath -ErrorAction Stop
$release=Join-Path $extractPath 'communication-centering-v177-final'
$required=@(
    'frontend\customer\index.html',
    'frontend\customer\sw.js',
    'frontend\customer\web.config',
    'frontend\partner\index.html',
    'frontend\partner\sw.js',
    'frontend\partner\web.config',
    'frontend\admin\index.html',
    'frontend\admin\sw.js',
    'frontend\admin\web.config',
    'Publish-Production.ps1',
    'README.txt',
    'RELEASE-MANIFEST.txt'
)
foreach($relative in $required){if(-not(Test-Path -LiteralPath (Join-Path $release $relative) -PathType Leaf)){throw "Required V177 release file is missing: $relative"}}
if(Test-Path -LiteralPath (Join-Path $release 'api')){throw 'V177 frontend-only package unexpectedly contains an API directory.'}
$partnerJs=Get-ChildItem -LiteralPath (Join-Path $release 'frontend\partner\assets') -Filter 'index-*.js' -File
if($partnerJs.Count-ne 1){throw "Exactly one V177 partner JavaScript bundle is required. Found: $($partnerJs.Count)"}
$partnerText=[IO.File]::ReadAllText($partnerJs[0].FullName,[Text.Encoding]::UTF8)
foreach($marker in @('communicationPage','chatCommunicationTabs','communicationCards')){if(-not$partnerText.Contains($marker)){throw "V177 communication marker missing: $marker"}}
foreach($portal in @('customer','partner','admin')){
    $portalRoot=Join-Path $release "frontend\$portal"
    $sw=[IO.File]::ReadAllText((Join-Path $portalRoot 'sw.js'),[Text.Encoding]::UTF8)
    $web=[IO.File]::ReadAllText((Join-Path $portalRoot 'web.config'),[Text.Encoding]::UTF8)
    if(-not$sw.Contains('soodal-life-shell-v4')){throw "V177 service-worker cache marker missing: $portal"}
    if(-not$web.Contains('no-store, no-cache, must-revalidate')){throw "V177 no-store marker missing: $portal"}
}
Write-Host "2/4 Extracted V177 deployment validated: $release" -ForegroundColor Green

if(-not $PSCmdlet.ShouldProcess("customer, partner and admin apps at $AppsRoot",'Deploy V177 communication centering')){return}
Write-Host '3/4 Running protected frontend backup followed by IIS deployment.' -ForegroundColor Yellow
& (Join-Path $release 'Publish-Production.ps1') -ConfirmProductionDeployment -AppsRoot $AppsRoot -BackupParent $ReleaseBasePath -Confirm:$false
if(-not $?){throw 'V177 protected deployment script failed.'}
Write-Host '4/4 V177 communication centering deployment completed.' -ForegroundColor Green
Write-Host 'API, database schema/data and production settings were not changed.' -ForegroundColor Yellow
Write-Host 'Centered communication hub, cards and chat navigation tabs are enabled.' -ForegroundColor Yellow
Write-Host "Extracted release path: $release"
