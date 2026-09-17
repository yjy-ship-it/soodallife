[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [string]$ZipPath=(Join-Path $PSScriptRoot 'SoodalLife-CustomerHomePerformance-20260831-v172.zip'),
    [string]$ReleaseBasePath='D:\SOODALLIFE\releases',
    [string]$AppsRoot='D:\SOODALLIFE\apps',
    [switch]$ConfirmProductionDeployment
)

$ErrorActionPreference='Stop'
$expectedSha256='4C9D9CB9C88FA0EC90028C5827D56DA3F29B2DD078B68DAAC65A787474E8239E'
if(-not $ConfirmProductionDeployment){throw 'Safety stop: add -ConfirmProductionDeployment only after checking the ZIP hash and target paths.'}
$identity=[Security.Principal.WindowsIdentity]::GetCurrent()
$principal=[Security.Principal.WindowsPrincipal]::new($identity)
if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Run PowerShell as Administrator and try again.'}
if(-not(Test-Path -LiteralPath $ZipPath -PathType Leaf)){throw "Deployment ZIP not found: $ZipPath"}
$actual=(Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash
if($actual-ne$expectedSha256){throw "ZIP SHA-256 mismatch. Expected=$expectedSha256, Actual=$actual"}
if(-not(Test-Path -LiteralPath $ReleaseBasePath -PathType Container)){New-Item -ItemType Directory -Path $ReleaseBasePath|Out-Null}

$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$extractPath=Join-Path $ReleaseBasePath "customer-home-performance-v172-$stamp"
New-Item -ItemType Directory -Path $extractPath|Out-Null
Write-Host '1/4 ZIP integrity check passed.' -ForegroundColor Green
Expand-Archive -LiteralPath $ZipPath -DestinationPath $extractPath -ErrorAction Stop
$release=Join-Path $extractPath 'customer-home-performance-v172-final'
$required=@(
    'api\SoodalLife.Api.dll',
    'api\web.config',
    'frontend\customer\index.html',
    'frontend\partner\index.html',
    'frontend\admin\index.html',
    'Publish-Production.ps1',
    'README.txt',
    'RELEASE-MANIFEST.txt'
)
foreach($relative in $required){if(-not(Test-Path -LiteralPath (Join-Path $release $relative) -PathType Leaf)){throw "Required V172 release file is missing: $relative"}}
if(Test-Path -LiteralPath (Join-Path $release 'api\appsettings.json')){throw 'V172 package unexpectedly contains appsettings.json.'}
$sourceJs=Get-ChildItem -LiteralPath (Join-Path $release 'frontend\customer\assets') -Filter 'index-*.js' -File
if($sourceJs.Count-ne 1){throw "Exactly one V172 customer JavaScript bundle is required. Found: $($sourceJs.Count)"}
$sourceText=[IO.File]::ReadAllText($sourceJs[0].FullName,[Text.Encoding]::UTF8)
foreach($marker in @('customerHomePerformanceV172','soodal-living-home:','visibilitychange')){if(-not$sourceText.Contains($marker)){throw "V172 frontend marker missing: $marker"}}
$partnerJs=Get-ChildItem -LiteralPath (Join-Path $release 'frontend\partner\assets') -Filter 'index-*.js' -File
if($partnerJs.Count-ne 1){throw "Exactly one V172 partner JavaScript bundle is required. Found: $($partnerJs.Count)"}
$partnerText=[IO.File]::ReadAllText($partnerJs[0].FullName,[Text.Encoding]::UTF8)
foreach($marker in @('customerHomePerformanceV172','soodal-living-home:','visibilitychange')){if(-not$partnerText.Contains($marker)){throw "V172 partner frontend marker missing: $marker"}}
$partnerCss=Get-ChildItem -LiteralPath (Join-Path $release 'frontend\partner\assets') -Filter 'index-*.css' -File
if($partnerCss.Count-ne 1){throw "Exactly one V172 partner CSS bundle is required. Found: $($partnerCss.Count)"}
$partnerCssText=[IO.File]::ReadAllText($partnerCss[0].FullName,[Text.Encoding]::UTF8)
foreach($marker in @('.livingLoad{box-sizing:border-box','.liveActivityList>p{box-sizing:border-box')){if(-not$partnerCssText.Contains($marker)){throw "V172 loading-layout CSS marker missing: $marker"}}
Write-Host "2/4 Extracted V172 deployment validated: $release" -ForegroundColor Green

if(-not $PSCmdlet.ShouldProcess("API, customer, partner and admin apps at $AppsRoot",'Deploy V172 customer home performance')){return}
Write-Host '3/4 Running protected application backup followed by IIS deployment.' -ForegroundColor Yellow
& (Join-Path $release 'Publish-Production.ps1') -ConfirmProductionDeployment -AppsRoot $AppsRoot -BackupParent $ReleaseBasePath -Confirm:$false
if(-not $?){throw 'V172 protected deployment script failed.'}
Write-Host '4/4 V172 customer home performance deployment completed.' -ForegroundColor Green
Write-Host 'Database schema/data and production appsettings were not changed.' -ForegroundColor Yellow
Write-Host 'Single living-home loading, viewport activity loading, separated caches and API timing are enabled.' -ForegroundColor Yellow
Write-Host "Extracted release path: $release"
