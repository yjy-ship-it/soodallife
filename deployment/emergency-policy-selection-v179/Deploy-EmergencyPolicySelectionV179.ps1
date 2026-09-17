[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [string]$ZipPath=(Join-Path $PSScriptRoot 'SoodalLife-EmergencyPolicySelection-20260831-v179.zip'),
    [string]$ReleaseBasePath='D:\SOODALLIFE\releases',
    [string]$AppsRoot='D:\SOODALLIFE\apps',
    [switch]$ConfirmProductionDeployment
)

$ErrorActionPreference='Stop'
$expectedSha256='6AF1D6CECDA9C8F924EC51E96EBBF3F74AC85F2AA30B2BA8E22891A0E3BFEC24'
if(-not $ConfirmProductionDeployment){throw 'Safety stop: add -ConfirmProductionDeployment only after checking the ZIP hash and target paths.'}
$identity=[Security.Principal.WindowsIdentity]::GetCurrent()
$principal=[Security.Principal.WindowsPrincipal]::new($identity)
if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Run PowerShell as Administrator and try again.'}
if(-not(Test-Path -LiteralPath $ZipPath -PathType Leaf)){throw "Deployment ZIP not found: $ZipPath"}
$actual=(Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash
if($actual-ne$expectedSha256){throw "ZIP SHA-256 mismatch. Expected=$expectedSha256, Actual=$actual"}
if(-not(Test-Path -LiteralPath $ReleaseBasePath -PathType Container)){New-Item -ItemType Directory -Path $ReleaseBasePath|Out-Null}

$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$extractPath=Join-Path $ReleaseBasePath "emergency-policy-selection-v179-$stamp"
New-Item -ItemType Directory -Path $extractPath|Out-Null
Write-Host '1/4 ZIP integrity check passed.' -ForegroundColor Green
Expand-Archive -LiteralPath $ZipPath -DestinationPath $extractPath -ErrorAction Stop
$release=Join-Path $extractPath 'emergency-policy-selection-v179-final'
$required=@('api\SoodalLife.Api.dll','api\web.config','Publish-Production.ps1','README.txt','RELEASE-MANIFEST.txt')
foreach($relative in $required){if(-not(Test-Path -LiteralPath (Join-Path $release $relative) -PathType Leaf)){throw "Required V179 release file is missing: $relative"}}
if(Test-Path -LiteralPath (Join-Path $release 'api\appsettings.json')){throw 'V179 package unexpectedly contains appsettings.json.'}
$apiHash=(Get-FileHash -LiteralPath (Join-Path $release 'api\SoodalLife.Api.dll') -Algorithm SHA256).Hash
if($apiHash-ne'03B3B33B289BC364D644ED6162D4923649D76F37B0740CAF841FEFB5D2051897'){throw "V179 API hash mismatch: $apiHash"}
Write-Host "2/4 Extracted V179 deployment validated: $release" -ForegroundColor Green

if(-not $PSCmdlet.ShouldProcess("API app at $AppsRoot",'Deploy V179 emergency policy selection fix')){return}
Write-Host '3/4 Running protected API backup followed by IIS deployment.' -ForegroundColor Yellow
& (Join-Path $release 'Publish-Production.ps1') -ConfirmProductionDeployment -AppsRoot $AppsRoot -BackupParent $ReleaseBasePath -Confirm:$false
if(-not $?){throw 'V179 protected deployment script failed.'}
Write-Host '4/4 V179 emergency policy selection deployment completed.' -ForegroundColor Green
Write-Host 'Database, frontend files and production settings were not changed.' -ForegroundColor Yellow
Write-Host 'Emergency catalog, detail and request save now use the same allowed current policy.' -ForegroundColor Yellow
Write-Host "Extracted release path: $release"
