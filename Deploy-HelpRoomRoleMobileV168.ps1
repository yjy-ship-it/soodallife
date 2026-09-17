[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [string]$ZipPath=(Join-Path $PSScriptRoot 'SoodalLife-HelpRoomRoleMobile-20260830-v168.zip'),
    [string]$ReleaseBasePath='D:\SOODALLIFE\releases',
    [string]$AppsRoot='D:\SOODALLIFE\apps',
    [switch]$ConfirmProductionDeployment
)

$ErrorActionPreference='Stop'
$expectedSha256='F50F93E350A93550C76A2B00A97B6E083229FC8A5B49C93CD47F196B3E04976D'
if(-not $ConfirmProductionDeployment){throw 'Safety stop: add -ConfirmProductionDeployment only after checking the ZIP hash and target paths.'}
$identity=[Security.Principal.WindowsIdentity]::GetCurrent()
$principal=[Security.Principal.WindowsPrincipal]::new($identity)
if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Run PowerShell as Administrator and try again.'}
if(-not(Test-Path -LiteralPath $ZipPath -PathType Leaf)){throw "Deployment ZIP not found: $ZipPath"}
$actual=(Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash
if($actual-ne$expectedSha256){throw "ZIP SHA-256 mismatch. Expected=$expectedSha256, Actual=$actual"}
if(-not(Test-Path -LiteralPath $ReleaseBasePath -PathType Container)){New-Item -ItemType Directory -Path $ReleaseBasePath|Out-Null}

$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$extractPath=Join-Path $ReleaseBasePath "help-room-role-mobile-v168-$stamp"
New-Item -ItemType Directory -Path $extractPath|Out-Null
Write-Host '1/4 ZIP integrity check passed.' -ForegroundColor Green
Expand-Archive -LiteralPath $ZipPath -DestinationPath $extractPath -ErrorAction Stop
$release=Join-Path $extractPath 'help-room-role-mobile-v168-final'
$required=@(
    'api\SoodalLife.Api.dll',
    'api\web.config',
    'frontend\customer\index.html',
    'frontend\partner\index.html',
    'frontend\admin\index.html',
    'Publish-Production.ps1',
    'README.txt',
    'RELEASE-MANIFEST.txt'
)
foreach($relative in $required){if(-not(Test-Path -LiteralPath (Join-Path $release $relative) -PathType Leaf)){throw "Required V168 release file is missing: $relative"}}
if(Test-Path -LiteralPath (Join-Path $release 'api\appsettings.json')){throw 'V168 package unexpectedly contains appsettings.json.'}
$sourceJs=Get-ChildItem -LiteralPath (Join-Path $release 'frontend\customer\assets') -Filter 'index-*.js' -File
if($sourceJs.Count-ne 1){throw "Exactly one V168 customer JavaScript bundle is required. Found: $($sourceJs.Count)"}
$sourceText=[IO.File]::ReadAllText($sourceJs[0].FullName,[Text.Encoding]::UTF8)
foreach($marker in @('providerHelpHero','helpProviderAction','providerHelpGrid','helpForm')){if(-not$sourceText.Contains($marker)){throw "V168 frontend marker missing: $marker"}}
Write-Host "2/4 Extracted V168 deployment validated: $release" -ForegroundColor Green

if(-not $PSCmdlet.ShouldProcess("API, customer, partner and admin apps at $AppsRoot",'Deploy V168 help room role and mobile reliability')){return}
Write-Host '3/4 Running protected application backup followed by IIS deployment.' -ForegroundColor Yellow
& (Join-Path $release 'Publish-Production.ps1') -ConfirmProductionDeployment -AppsRoot $AppsRoot -BackupParent $ReleaseBasePath -Confirm:$false
if(-not $?){throw 'V168 protected deployment script failed.'}
Write-Host '4/4 V168 help room role and mobile reliability deployment completed.' -ForegroundColor Green
Write-Host 'Database schema/data and production appsettings were not changed.' -ForegroundColor Yellow
Write-Host 'Customer mobile input containment and provider-specific answer workflow are enabled.' -ForegroundColor Yellow
Write-Host "Extracted release path: $release"
