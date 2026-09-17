[CmdletBinding()]
param(
    [string]$ZipPath='D:\SOODALLIFE\SoodalLife-SubscriptionManual-20260819-9705638-v103.zip',
    [string]$ReleaseBasePath='D:\SOODALLIFE\releases'
)
$ErrorActionPreference='Stop'
$expectedSha256='67D25AF96864906ECD4B97D48CD6DC516E1E83FE15B6B6989AC81A1735A9808D'
$identity=[Security.Principal.WindowsIdentity]::GetCurrent();$principal=[Security.Principal.WindowsPrincipal]::new($identity)
if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Run PowerShell as Administrator and try again.'}
if(-not(Test-Path -LiteralPath $ZipPath -PathType Leaf)){throw "Deployment ZIP not found: $ZipPath"}
$actualSha256=(Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash;if($actualSha256 -ne $expectedSha256){throw "ZIP SHA-256 mismatch. Expected=$expectedSha256, Actual=$actualSha256"}
if(-not(Test-Path -LiteralPath $ReleaseBasePath -PathType Container)){New-Item -ItemType Directory -Path $ReleaseBasePath|Out-Null}
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss';$extractPath=Join-Path $ReleaseBasePath "subscription-manual-v103-$stamp";if(Test-Path -LiteralPath $extractPath){throw "Extraction directory already exists: $extractPath"};New-Item -ItemType Directory -Path $extractPath|Out-Null
Expand-Archive -LiteralPath $ZipPath -DestinationPath $extractPath -ErrorAction Stop
$scripts=@(Get-ChildItem -LiteralPath $extractPath -Filter 'Publish-Production.ps1' -File -Recurse);if($scripts.Count -ne 1){throw "Exactly one Publish-Production.ps1 is required. Found: $($scripts.Count)"};$releasePath=$scripts[0].Directory.FullName
$required=@((Join-Path $releasePath 'api\SoodalLife.Api.dll'),(Join-Path $releasePath 'api\web.config'),(Join-Path $releasePath 'frontend\customer\index.html'),(Join-Path $releasePath 'frontend\partner\index.html'),(Join-Path $releasePath 'frontend\admin\index.html'),(Join-Path $releasePath 'deployment\Apply-SubscriptionOperationalV102.sql'),(Join-Path $releasePath 'deployment\Verify-SubscriptionOperationalV102.sql'),(Join-Path $releasePath 'deployment\MANUAL-OPERATION-GUIDE.txt'));$missing=@($required|Where-Object{-not(Test-Path -LiteralPath $_ -PathType Leaf)});if($missing.Count){throw "Required deployment files are missing: $($missing -join ', ')"}
Write-Host '1/4 ZIP integrity check passed.'
Write-Host "2/4 Extracted manual-operation deployment validated: $releasePath"
Write-Host '3/4 Disabling Toss billing, then running backup and IIS deployment.'
Push-Location $releasePath;try{& $scripts[0].FullName}finally{Pop-Location}
Write-Host '4/4 V103 manual-operation deployment completed.' -ForegroundColor Green
Write-Host "Extracted release path: $releasePath"
Write-Host 'Toss automatic billing remains disabled. Provider bank-transfer payout confirmation remains available in admin.' -ForegroundColor Yellow
