[CmdletBinding()]
param(
    [string]$ZipPath = 'D:\SOODALLIFE\SoodalLife-ProviderProfile-20260817-2e647b5-v46.zip',
    [string]$ReleaseBasePath = 'D:\SOODALLIFE\releases'
)

$ErrorActionPreference = 'Stop'
$expectedSha256 = '75320B898E5FEE6BB4AAF3B6504419351648DAAF55B02AB84FF3218486063C0F'

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run PowerShell as Administrator and try again.'
}

if (-not (Test-Path -LiteralPath $ZipPath -PathType Leaf)) {
    throw "Deployment ZIP not found: $ZipPath"
}

$actualSha256 = (Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash
if ($actualSha256 -ne $expectedSha256) {
    throw "ZIP SHA-256 mismatch. Expected=$expectedSha256, Actual=$actualSha256"
}

if (-not (Test-Path -LiteralPath $ReleaseBasePath -PathType Container)) {
    New-Item -ItemType Directory -Path $ReleaseBasePath | Out-Null
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$extractPath = Join-Path $ReleaseBasePath "provider-profile-v46-$stamp"
if (Test-Path -LiteralPath $extractPath) {
    throw "Extraction directory already exists: $extractPath"
}

New-Item -ItemType Directory -Path $extractPath | Out-Null

Write-Host '1/4 ZIP integrity check passed.'
Write-Host "2/4 Extracting ZIP: $extractPath"
Expand-Archive -LiteralPath $ZipPath -DestinationPath $extractPath -ErrorAction Stop

$deployScripts = @(
    Get-ChildItem -LiteralPath $extractPath -Filter 'Publish-Production.ps1' -File -Recurse
)
if ($deployScripts.Count -ne 1) {
    throw "Exactly one Publish-Production.ps1 is required. Found: $($deployScripts.Count)"
}

$deployScript = $deployScripts[0]
$releasePath = $deployScript.Directory.FullName
$requiredFiles = @(
    (Join-Path $releasePath 'api\SoodalLife.Api.dll'),
    (Join-Path $releasePath 'api\web.config'),
    (Join-Path $releasePath 'frontend\customer\index.html'),
    (Join-Path $releasePath 'frontend\partner\index.html'),
    (Join-Path $releasePath 'frontend\admin\index.html')
)

$missingFiles = @($requiredFiles | Where-Object { -not (Test-Path -LiteralPath $_ -PathType Leaf) })
if ($missingFiles.Count -gt 0) {
    throw "Required deployment files are missing: $($missingFiles -join ', ')"
}

Write-Host "3/4 Extracted deployment validated: $releasePath"
Write-Host '4/4 Running IIS deployment script.'

Push-Location $releasePath
try {
    & $deployScript.FullName
}
finally {
    Pop-Location
}

Write-Host ''
Write-Host 'Deployment completed.' -ForegroundColor Green
Write-Host "Extracted release path: $releasePath"
Write-Host 'Next: verify profile steps, image upload, previews, API health and readiness.'
