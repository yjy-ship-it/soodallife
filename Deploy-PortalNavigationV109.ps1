[CmdletBinding()]
param(
    [string]$ZipPath='D:\SOODALLIFE\SoodalLife-PortalNavigation-20260820-v109.zip',
    [string]$ReleaseBasePath='D:\SOODALLIFE\releases'
)
$ErrorActionPreference='Stop'
$expectedSha256='A1414FF7CC26B3C1141EADE80396751E70E92A2A757C0BAB3D1A27389FC6A8EE'
$identity=[Security.Principal.WindowsIdentity]::GetCurrent()
$principal=[Security.Principal.WindowsPrincipal]::new($identity)
if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Run PowerShell as Administrator and try again.'}
if(-not(Test-Path -LiteralPath $ZipPath -PathType Leaf)){throw "Deployment ZIP not found: $ZipPath"}
$actual=(Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash
if($actual -ne $expectedSha256){throw "ZIP SHA-256 mismatch. Expected=$expectedSha256, Actual=$actual"}
if(-not(Test-Path -LiteralPath $ReleaseBasePath -PathType Container)){New-Item -ItemType Directory -Path $ReleaseBasePath|Out-Null}
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$extractPath=Join-Path $ReleaseBasePath "portal-navigation-v109-$stamp"
New-Item -ItemType Directory -Path $extractPath|Out-Null
Write-Host '1/4 ZIP integrity check passed.'
Expand-Archive -LiteralPath $ZipPath -DestinationPath $extractPath -ErrorAction Stop
$scripts=@(Get-ChildItem -LiteralPath $extractPath -Filter 'Publish-Production.ps1' -File -Recurse)
if($scripts.Count -ne 1){throw "Exactly one Publish-Production.ps1 is required. Found: $($scripts.Count)"}
$deploy=$scripts[0]
$release=$deploy.Directory.FullName
$required=@(
    (Join-Path $release 'frontend\customer\index.html'),
    (Join-Path $release 'frontend\partner\index.html'),
    (Join-Path $release 'frontend\admin\index.html')
)
$missing=@($required|Where-Object{-not(Test-Path -LiteralPath $_ -PathType Leaf)})
if($missing.Count){throw "Required deployment files are missing: $($missing -join ', ')"}
Write-Host "2/4 Extracted deployment validated: $release"
Write-Host '3/4 Running protected frontend backup and IIS deployment.'
Push-Location $release
try{& $deploy.FullName}finally{Pop-Location}
Write-Host '4/4 V109 deployment completed.' -ForegroundColor Green
Write-Host "Extracted release path: $release"
