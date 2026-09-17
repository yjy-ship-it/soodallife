[CmdletBinding()]
param(
    [string]$ZipPath = 'D:\SOODALLIFE\SoodalLife-SubscriptionTermination-20260819-9705638-v101.zip',
    [string]$ReleaseBasePath = 'D:\SOODALLIFE\releases'
)

$ErrorActionPreference = 'Stop'
$expectedSha256 = 'F0C9C71387F93F8406047F5128B30E51732BFE46FFD3838D51D1E9753DC5FD42'
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Run PowerShell as Administrator and try again.' }
if (-not (Test-Path -LiteralPath $ZipPath -PathType Leaf)) { throw "Deployment ZIP not found: $ZipPath" }
$actualSha256 = (Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash
if ($actualSha256 -ne $expectedSha256) { throw "ZIP SHA-256 mismatch. Expected=$expectedSha256, Actual=$actualSha256" }
if (-not (Test-Path -LiteralPath $ReleaseBasePath -PathType Container)) { New-Item -ItemType Directory -Path $ReleaseBasePath | Out-Null }

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$extractPath = Join-Path $ReleaseBasePath "subscription-termination-v101-$stamp"
if (Test-Path -LiteralPath $extractPath) { throw "Extraction directory already exists: $extractPath" }
New-Item -ItemType Directory -Path $extractPath | Out-Null
Expand-Archive -LiteralPath $ZipPath -DestinationPath $extractPath -ErrorAction Stop
$deployScripts = @(Get-ChildItem -LiteralPath $extractPath -Filter 'Publish-Production.ps1' -File -Recurse)
if ($deployScripts.Count -ne 1) { throw "Exactly one Publish-Production.ps1 is required. Found: $($deployScripts.Count)" }
$deployScript = $deployScripts[0]
$releasePath = $deployScript.Directory.FullName
$requiredFiles = @((Join-Path $releasePath 'api\SoodalLife.Api.dll'),(Join-Path $releasePath 'api\web.config'),(Join-Path $releasePath 'frontend\customer\index.html'),(Join-Path $releasePath 'frontend\partner\index.html'),(Join-Path $releasePath 'frontend\admin\index.html'),(Join-Path $releasePath 'deployment\Apply-SubscriptionTerminationV101.sql'),(Join-Path $releasePath 'deployment\Verify-SubscriptionTerminationV101.sql'),(Join-Path $releasePath 'deployment\PG-Configuration-Template.txt'))
$missingFiles = @($requiredFiles | Where-Object { -not (Test-Path -LiteralPath $_ -PathType Leaf) })
if ($missingFiles.Count -gt 0) { throw "Required deployment files are missing: $($missingFiles -join ', ')" }

Write-Host '1/4 ZIP integrity check passed.'
Write-Host "2/4 Extracted deployment validated: $releasePath"
Write-Host '3/4 Running database-backup and IIS deployment.'
Push-Location $releasePath
try { & $deployScript.FullName }
finally { Pop-Location }
Write-Host '4/4 Deployment completed.' -ForegroundColor Green
Write-Host "Extracted release path: $releasePath"
Write-Host 'Next: apply protected Toss PG settings if PgConfigurationRequired is True, then verify cancellation and refund.'
