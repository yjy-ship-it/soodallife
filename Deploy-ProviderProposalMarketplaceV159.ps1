[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [string]$ZipPath=(Join-Path $PSScriptRoot 'SoodalLife-ProviderProposalMarketplace-20260830-v159.zip'),
    [string]$ReleaseBasePath='D:\SOODALLIFE\releases',
    [switch]$ConfirmProductionDeployment
)
$ErrorActionPreference='Stop'
$expectedSha256='66EE5DB1FBA4485D1EBA14463EEBF6BC0578C19A4D98F351AD4E7C75FF9FFDF1'
if(-not $ConfirmProductionDeployment){throw 'Safety stop: add -ConfirmProductionDeployment only after checking the ZIP hash, database and target paths.'}
$identity=[Security.Principal.WindowsIdentity]::GetCurrent();$principal=[Security.Principal.WindowsPrincipal]::new($identity)
if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Run PowerShell as Administrator and try again.'}
if(-not(Test-Path -LiteralPath $ZipPath -PathType Leaf)){throw "Deployment ZIP not found: $ZipPath"}
$actual=(Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash;if($actual-ne $expectedSha256){throw "ZIP SHA-256 mismatch. Expected=$expectedSha256, Actual=$actual"}
if(-not(Test-Path -LiteralPath $ReleaseBasePath -PathType Container)){New-Item -ItemType Directory -Path $ReleaseBasePath|Out-Null}
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss';$extractPath=Join-Path $ReleaseBasePath "provider-proposal-marketplace-v159-$stamp";New-Item -ItemType Directory -Path $extractPath|Out-Null
Write-Host '1/4 ZIP integrity check passed.'
Expand-Archive -LiteralPath $ZipPath -DestinationPath $extractPath -ErrorAction Stop
$scripts=@(Get-ChildItem -LiteralPath $extractPath -Filter 'Publish-Production.ps1' -File -Recurse);if($scripts.Count-ne 1){throw "Exactly one Publish-Production.ps1 is required. Found: $($scripts.Count)"}
$deploy=$scripts[0];$release=$deploy.Directory.FullName
$required=@((Join-Path $release 'api\SoodalLife.Api.dll'),(Join-Path $release 'api\web.config'),(Join-Path $release 'frontend\customer\index.html'),(Join-Path $release 'frontend\partner\index.html'),(Join-Path $release 'frontend\admin\index.html'),(Join-Path $release 'deployment\Apply-ProviderProposalMarketplaceV159.sql'),(Join-Path $release 'deployment\Verify-ProviderProposalMarketplaceV159.sql'),(Join-Path $release 'RELEASE-MANIFEST.txt'))
$missing=@($required|Where-Object{-not(Test-Path -LiteralPath $_ -PathType Leaf)});if($missing.Count){throw "Required deployment files are missing: $($missing-join ', ')"}
Write-Host "2/4 Extracted V159 deployment validated: $release"
Write-Host '3/4 Running protected database, API and frontend backup followed by IIS deployment.'
Push-Location $release
try{& $deploy.FullName -ConfirmProductionDeployment -WhatIf:$WhatIfPreference}finally{Pop-Location}
if($WhatIfPreference){Write-Host '4/4 WhatIf validation completed. No production deployment was performed.' -ForegroundColor Yellow;return}
Write-Host '4/4 V159 expert offer and group recruitment deployment completed.' -ForegroundColor Green
Write-Host 'External marketing and PWA PUSH remain fail-closed until their explicit operating gates are enabled.' -ForegroundColor Yellow
Write-Host "Extracted release path: $release"
