[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [string]$ZipPath=(Join-Path $PSScriptRoot 'SoodalLife-ExternalIntegrations-20260829-v157.zip'),
    [string]$ReleaseBasePath='D:\SOODALLIFE\releases',
    [switch]$ConfirmProductionDeployment
)
$ErrorActionPreference='Stop'
$expectedSha256='472872784DC852DF490197A1C2B5E51E699360DA455615ED253BECAF8BF4222E'
if(-not $ConfirmProductionDeployment){throw 'Safety stop: add -ConfirmProductionDeployment only after checking the ZIP hash, external integration modes and target paths.'}
$identity=[Security.Principal.WindowsIdentity]::GetCurrent();$principal=[Security.Principal.WindowsPrincipal]::new($identity)
if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Run PowerShell as Administrator and try again.'}
if(-not(Test-Path -LiteralPath $ZipPath -PathType Leaf)){throw "Deployment ZIP not found: $ZipPath"}
$actual=(Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash;if($actual-ne $expectedSha256){throw "ZIP SHA-256 mismatch. Expected=$expectedSha256, Actual=$actual"}
if(-not(Test-Path -LiteralPath $ReleaseBasePath -PathType Container)){New-Item -ItemType Directory -Path $ReleaseBasePath|Out-Null}
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss';$extractPath=Join-Path $ReleaseBasePath "external-integrations-v157-$stamp";New-Item -ItemType Directory -Path $extractPath|Out-Null
Write-Host '1/4 ZIP integrity check passed.'
Expand-Archive -LiteralPath $ZipPath -DestinationPath $extractPath -ErrorAction Stop
$scripts=@(Get-ChildItem -LiteralPath $extractPath -Filter 'Publish-Production.ps1' -File -Recurse);if($scripts.Count-ne 1){throw "Exactly one Publish-Production.ps1 is required. Found: $($scripts.Count)"}
$deploy=$scripts[0];$release=$deploy.Directory.FullName
$required=@((Join-Path $release 'api\SoodalLife.Api.dll'),(Join-Path $release 'api\web.config'),(Join-Path $release 'frontend\customer\index.html'),(Join-Path $release 'frontend\partner\index.html'),(Join-Path $release 'frontend\admin\index.html'),(Join-Path $release 'RELEASE-MANIFEST.txt'),(Join-Path $release 'EXTERNAL-INTEGRATION-SETTINGS.example.json'))
$missing=@($required|Where-Object{-not(Test-Path -LiteralPath $_ -PathType Leaf)});if($missing.Count){throw "Required deployment files are missing: $($missing-join ', ')"}
Write-Host "2/4 Extracted V157 deployment validated: $release"
Write-Host '3/4 Running protected database, API and frontend backup followed by IIS deployment.'
Push-Location $release
try{& $deploy.FullName -ConfirmProductionDeployment -WhatIf:$WhatIfPreference}finally{Pop-Location}
if($WhatIfPreference){Write-Host '4/4 WhatIf validation completed. No production deployment was performed.' -ForegroundColor Yellow;return}
Write-Host '4/4 V157 gated external integrations deployment completed.' -ForegroundColor Green
Write-Host 'External modes remain controlled by preserved server settings. Start with TEST and activate channels sequentially.' -ForegroundColor Yellow
Write-Host "Extracted release path: $release"
