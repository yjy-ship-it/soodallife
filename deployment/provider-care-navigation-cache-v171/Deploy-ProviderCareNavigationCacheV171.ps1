[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [string]$ZipPath=(Join-Path $PSScriptRoot 'SoodalLife-ProviderCareNavigationCache-20260831-v171.zip'),
    [string]$ReleaseBasePath='D:\SOODALLIFE\releases',
    [string]$AppsRoot='D:\SOODALLIFE\apps',
    [switch]$ConfirmProductionDeployment
)

$ErrorActionPreference='Stop'
$expectedSha256='B3CB94468C1605CE9520B7F498E3F9760474BA92D65E18FBA68D7C5DD65927AA'
if(-not $ConfirmProductionDeployment){throw 'Safety stop: add -ConfirmProductionDeployment only after checking the ZIP hash and target paths.'}
$identity=[Security.Principal.WindowsIdentity]::GetCurrent()
$principal=[Security.Principal.WindowsPrincipal]::new($identity)
if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Run PowerShell as Administrator and try again.'}
if(-not(Test-Path -LiteralPath $ZipPath -PathType Leaf)){throw "Deployment ZIP not found: $ZipPath"}
$actual=(Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash
if($actual-ne$expectedSha256){throw "ZIP SHA-256 mismatch. Expected=$expectedSha256, Actual=$actual"}
if(-not(Test-Path -LiteralPath $ReleaseBasePath -PathType Container)){New-Item -ItemType Directory -Path $ReleaseBasePath|Out-Null}

$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$extractPath=Join-Path $ReleaseBasePath "provider-care-navigation-cache-v171-$stamp"
New-Item -ItemType Directory -Path $extractPath|Out-Null
Write-Host '1/4 ZIP integrity check passed.' -ForegroundColor Green
Expand-Archive -LiteralPath $ZipPath -DestinationPath $extractPath -ErrorAction Stop
$release=Join-Path $extractPath 'provider-care-navigation-cache-v171-final'
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
foreach($relative in $required){if(-not(Test-Path -LiteralPath (Join-Path $release $relative) -PathType Leaf)){throw "Required V171 release file is missing: $relative"}}
if(Test-Path -LiteralPath (Join-Path $release 'api')){throw 'V171 frontend-only package unexpectedly contains an API directory.'}
$partnerJs=Get-ChildItem -LiteralPath (Join-Path $release 'frontend\partner\assets') -Filter 'index-*.js' -File
if($partnerJs.Count-ne 1){throw "Exactly one V171 partner JavaScript bundle is required. Found: $($partnerJs.Count)"}
$partnerText=[IO.File]::ReadAllText($partnerJs[0].FullName,[Text.Encoding]::UTF8)
foreach($marker in @('providerCareNavigationV171','controllerchange','updateViaCache')){if(-not$partnerText.Contains($marker)){throw "V171 partner marker missing: $marker"}}
foreach($portal in @('customer','partner','admin')){
    $portalRoot=Join-Path $release "frontend\$portal"
    $sw=[IO.File]::ReadAllText((Join-Path $portalRoot 'sw.js'),[Text.Encoding]::UTF8)
    $web=[IO.File]::ReadAllText((Join-Path $portalRoot 'web.config'),[Text.Encoding]::UTF8)
    if(-not$sw.Contains('soodal-life-shell-v4')){throw "V171 service-worker cache marker missing: $portal"}
    if(-not$web.Contains('no-store, no-cache, must-revalidate')){throw "V171 no-store marker missing: $portal"}
}
Write-Host "2/4 Extracted V171 deployment validated: $release" -ForegroundColor Green

if(-not $PSCmdlet.ShouldProcess("customer, partner and admin apps at $AppsRoot",'Deploy V171 provider care navigation and cache refresh')){return}
Write-Host '3/4 Running protected frontend backup followed by IIS deployment.' -ForegroundColor Yellow
& (Join-Path $release 'Publish-Production.ps1') -ConfirmProductionDeployment -AppsRoot $AppsRoot -BackupParent $ReleaseBasePath -Confirm:$false
if(-not $?){throw 'V171 protected deployment script failed.'}
Write-Host '4/4 V171 provider care navigation and cache refresh deployment completed.' -ForegroundColor Green
Write-Host 'API, database schema/data and production settings were not changed.' -ForegroundColor Yellow
Write-Host 'Provider care request/proposal labels and automatic frontend refresh are enabled.' -ForegroundColor Yellow
Write-Host "Extracted release path: $release"
