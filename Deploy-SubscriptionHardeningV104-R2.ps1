[CmdletBinding()]
param(
    [string]$ZipPath='D:\SOODALLIFE\SoodalLife-SubscriptionHardening-20260819-9705638-v104-r2.zip',
    [string]$ReleaseBasePath='D:\SOODALLIFE\releases'
)
$ErrorActionPreference='Stop'
$expectedSha256='0AD353DEFF73BA94C59A47C8E56BE451CE390894BCCB8C5F657ADE621F84EE82'
$identity=[Security.Principal.WindowsIdentity]::GetCurrent();$principal=[Security.Principal.WindowsPrincipal]::new($identity)
if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Run PowerShell as Administrator and try again.'}
if(-not(Test-Path -LiteralPath $ZipPath -PathType Leaf)){throw "Deployment ZIP not found: $ZipPath"}
$actualSha256=(Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash;if($actualSha256 -ne $expectedSha256){throw "ZIP SHA-256 mismatch. Expected=$expectedSha256, Actual=$actualSha256"}
if(-not(Test-Path -LiteralPath $ReleaseBasePath -PathType Container)){New-Item -ItemType Directory -Path $ReleaseBasePath|Out-Null}
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss';$extractPath=Join-Path $ReleaseBasePath "subscription-hardening-v104-r2-$stamp";if(Test-Path -LiteralPath $extractPath){throw "Extraction directory already exists: $extractPath"};New-Item -ItemType Directory -Path $extractPath|Out-Null
Expand-Archive -LiteralPath $ZipPath -DestinationPath $extractPath -ErrorAction Stop
$publishScript=Join-Path $extractPath 'Publish-Production.ps1';if(-not(Test-Path -LiteralPath $publishScript -PathType Leaf)){throw "Root Publish-Production.ps1 is missing: $publishScript"};$releasePath=$extractPath
$required=@((Join-Path $releasePath 'api\SoodalLife.Api.dll'),(Join-Path $releasePath 'api\web.config'),(Join-Path $releasePath 'frontend\customer\index.html'),(Join-Path $releasePath 'frontend\partner\index.html'),(Join-Path $releasePath 'frontend\admin\index.html'),(Join-Path $releasePath 'deployment\Apply-SubscriptionHardeningV104.sql'),(Join-Path $releasePath 'deployment\Verify-SubscriptionHardeningV104.sql'),(Join-Path $releasePath 'deployment\TOSS-PRECONNECTION-GUIDE.txt'));$missing=@($required|Where-Object{-not(Test-Path -LiteralPath $_ -PathType Leaf)});if($missing.Count){throw "Required deployment files are missing: $($missing -join ', ')"}
Write-Host '1/4 ZIP integrity check passed.'
Write-Host "2/4 Extracted V104 payment-hardening deployment validated: $releasePath"
Write-Host '3/4 Disabling Toss billing, then running backup and IIS deployment.'
Push-Location $releasePath;try{& $publishScript}finally{Pop-Location}
Write-Host '4/4 V104 payment-hardening deployment completed.' -ForegroundColor Green
Write-Host "Extracted release path: $releasePath"
Write-Host 'Toss automatic billing remains disabled. Provider bank-transfer payout confirmation remains available in admin.' -ForegroundColor Yellow
