[CmdletBinding()]
param(
    [string]$ZipPath = 'D:\SOODALLIFE\SoodalLife-AdvertisingContentWorkflow-20260818-9705638-v96.zip',
    [string]$ReleaseBasePath = 'D:\SOODALLIFE\releases'
)

$ErrorActionPreference = 'Stop'
$expectedSha256 = '98EFA9BC4D9F728FCB4F3E65E42F0CF9C6BA55E4651503F1CCAEB1285816C9B0'

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
$extractPath = Join-Path $ReleaseBasePath "advertising-content-workflow-v96-$stamp"
if (Test-Path -LiteralPath $extractPath) {
    throw "Extraction directory already exists: $extractPath"
}

New-Item -ItemType Directory -Path $extractPath | Out-Null

Write-Host '1/4 ZIP integrity check passed.'
Write-Host "2/4 Extracting ZIP: $extractPath"
Expand-Archive -LiteralPath $ZipPath -DestinationPath $extractPath -ErrorAction Stop

$deployScripts = @(Get-ChildItem -LiteralPath $extractPath -Filter 'Publish-Production.ps1' -File -Recurse)
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
    (Join-Path $releasePath 'frontend\admin\index.html'),
    (Join-Path $releasePath 'deployment\Apply-ReviewConversationV89.sql'),
    (Join-Path $releasePath 'deployment\Verify-ReviewConversationV89.sql'),
    (Join-Path $releasePath 'deployment\Apply-QuoteFeeReservationV93.sql'),
    (Join-Path $releasePath 'deployment\Verify-QuoteFeeReservationV93.sql'),
    (Join-Path $releasePath 'deployment\Apply-ReportTypeDefaultsV95.sql'),
    (Join-Path $releasePath 'deployment\Verify-ReportTypeDefaultsV95.sql')
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
Write-Host 'Next: verify advertising dashboard, campaign draft-material-review-approval flow, content version review, pause/resume, schedules, and placements.'
