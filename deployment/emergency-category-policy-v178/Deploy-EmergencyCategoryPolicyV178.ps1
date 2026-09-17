[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [string]$ZipPath=(Join-Path $PSScriptRoot 'SoodalLife-EmergencyCategoryPolicy-20260831-v178.zip'),
    [string]$ReleaseBasePath='D:\SOODALLIFE\releases',
    [string]$AppsRoot='D:\SOODALLIFE\apps',
    [switch]$ConfirmProductionDeployment
)

$ErrorActionPreference='Stop'
$expectedSha256='51A5992E4B1856A881792289A48EE598583A7966E8B0E4E1A23CBFA5ABFFADAD'
if(-not $ConfirmProductionDeployment){throw 'Safety stop: add -ConfirmProductionDeployment only after checking the ZIP hash and target paths.'}
$identity=[Security.Principal.WindowsIdentity]::GetCurrent()
$principal=[Security.Principal.WindowsPrincipal]::new($identity)
if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Run PowerShell as Administrator and try again.'}
if(-not(Test-Path -LiteralPath $ZipPath -PathType Leaf)){throw "Deployment ZIP not found: $ZipPath"}
$actual=(Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash
if($actual-ne$expectedSha256){throw "ZIP SHA-256 mismatch. Expected=$expectedSha256, Actual=$actual"}
if(-not(Test-Path -LiteralPath $ReleaseBasePath -PathType Container)){New-Item -ItemType Directory -Path $ReleaseBasePath|Out-Null}

$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$extractPath=Join-Path $ReleaseBasePath "emergency-category-policy-v178-$stamp"
New-Item -ItemType Directory -Path $extractPath|Out-Null
Write-Host '1/4 ZIP integrity check passed.' -ForegroundColor Green
Expand-Archive -LiteralPath $ZipPath -DestinationPath $extractPath -ErrorAction Stop
$release=Join-Path $extractPath 'emergency-category-policy-v178-final'
$required=@('api\SoodalLife.Api.dll','api\web.config','frontend\customer\index.html','frontend\customer\sw.js','frontend\customer\web.config','frontend\partner\index.html','frontend\partner\sw.js','frontend\partner\web.config','frontend\admin\index.html','frontend\admin\sw.js','frontend\admin\web.config','Publish-Production.ps1','README.txt','RELEASE-MANIFEST.txt')
foreach($relative in $required){if(-not(Test-Path -LiteralPath (Join-Path $release $relative) -PathType Leaf)){throw "Required V178 release file is missing: $relative"}}
if(Test-Path -LiteralPath (Join-Path $release 'api\appsettings.json')){throw 'V178 package unexpectedly contains appsettings.json.'}
$customerJs=Get-ChildItem -LiteralPath (Join-Path $release 'frontend\customer\assets') -Filter 'index-*.js' -File
if($customerJs.Count-ne 1){throw "Exactly one V178 customer JavaScript bundle is required. Found: $($customerJs.Count)"}
$customerText=[IO.File]::ReadAllText($customerJs[0].FullName,[Text.Encoding]::UTF8)
foreach($marker in @('emergencyOnly=true','SOODAL EMERGENCY')){if(-not$customerText.Contains($marker)){throw "V178 customer marker missing: $marker"}}
foreach($portal in @('customer','partner','admin')){
    $portalRoot=Join-Path $release "frontend\$portal"
    $sw=[IO.File]::ReadAllText((Join-Path $portalRoot 'sw.js'),[Text.Encoding]::UTF8)
    $web=[IO.File]::ReadAllText((Join-Path $portalRoot 'web.config'),[Text.Encoding]::UTF8)
    if(-not$sw.Contains('soodal-life-shell-v4')){throw "V178 service-worker cache marker missing: $portal"}
    if(-not$web.Contains('no-store, no-cache, must-revalidate')){throw "V178 no-store marker missing: $portal"}
}
Write-Host "2/4 Extracted V178 deployment validated: $release" -ForegroundColor Green

if(-not $PSCmdlet.ShouldProcess("API, customer, partner and admin apps at $AppsRoot",'Deploy V178 emergency category policy separation')){return}
Write-Host '3/4 Running protected application backup followed by IIS deployment.' -ForegroundColor Yellow
& (Join-Path $release 'Publish-Production.ps1') -ConfirmProductionDeployment -AppsRoot $AppsRoot -BackupParent $ReleaseBasePath -Confirm:$false
if(-not $?){throw 'V178 protected deployment script failed.'}
Write-Host '4/4 V178 emergency category policy deployment completed.' -ForegroundColor Green
Write-Host 'Database schema/data and production settings were not changed.' -ForegroundColor Yellow
Write-Host 'Policy-allowed emergency categories remain visible; provider availability is checked during matching.' -ForegroundColor Yellow
Write-Host "Extracted release path: $release"
