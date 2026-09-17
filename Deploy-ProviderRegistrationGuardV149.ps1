[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [string]$ZipPath='D:\SOODALLIFE\SoodalLife-ProviderRegistrationGuard-20260828-v149.zip',
    [string]$ReleaseBasePath='D:\SOODALLIFE\releases',
    [switch]$ConfirmProductionDeployment
)
$ErrorActionPreference='Stop'
$expectedSha256='D026D4B6883A705FD531197197733353792C199A27A832AA1A5F602B149A8187'
if(-not $ConfirmProductionDeployment){throw 'Safety stop: add -ConfirmProductionDeployment only after checking the ZIP hash and backup path.'}
$identity=[Security.Principal.WindowsIdentity]::GetCurrent()
$principal=[Security.Principal.WindowsPrincipal]::new($identity)
if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Run PowerShell as Administrator and try again.'}
if(-not(Test-Path -LiteralPath $ZipPath -PathType Leaf)){throw "Deployment ZIP not found: $ZipPath"}
$actual=(Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash
if($actual -ne $expectedSha256){throw "ZIP SHA-256 mismatch. Expected=$expectedSha256, Actual=$actual"}
if(-not(Test-Path -LiteralPath $ReleaseBasePath -PathType Container)){New-Item -ItemType Directory -Path $ReleaseBasePath|Out-Null}
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$extractPath=Join-Path $ReleaseBasePath "provider-registration-guard-v149-$stamp"
New-Item -ItemType Directory -Path $extractPath|Out-Null
Write-Host '1/4 ZIP integrity check passed.'
Expand-Archive -LiteralPath $ZipPath -DestinationPath $extractPath -ErrorAction Stop
$scripts=@(Get-ChildItem -LiteralPath $extractPath -Filter 'Publish-Production.ps1' -File -Recurse)
if($scripts.Count -ne 1){throw "Exactly one Publish-Production.ps1 is required. Found: $($scripts.Count)"}
$deploy=$scripts[0]
$release=$deploy.Directory.FullName
$required=@(
    (Join-Path $release 'api\SoodalLife.Api.dll'),
    (Join-Path $release 'api\web.config'),
    (Join-Path $release 'frontend\customer\index.html'),
    (Join-Path $release 'frontend\partner\index.html'),
    (Join-Path $release 'frontend\admin\index.html'),
    (Join-Path $release 'RELEASE-MANIFEST.txt')
)
$missing=@($required|Where-Object{-not(Test-Path -LiteralPath $_ -PathType Leaf)})
if($missing.Count){throw "Required deployment files are missing: $($missing -join ', ')"}
Write-Host "2/4 Extracted V149 deployment validated: $release"
Write-Host '3/4 Running protected API and frontend backup followed by IIS deployment.'
Push-Location $release
try{& $deploy.FullName -ConfirmProductionDeployment -WhatIf:$WhatIfPreference}finally{Pop-Location}
if($WhatIfPreference){Write-Host '4/4 WhatIf validation completed. No production deployment was performed.' -ForegroundColor Yellow;return}
Write-Host '4/4 V149 provider registration guard deployment completed.' -ForegroundColor Green
Write-Host 'Database schema and data were not changed.' -ForegroundColor Yellow
Write-Host "Extracted release path: $release"
