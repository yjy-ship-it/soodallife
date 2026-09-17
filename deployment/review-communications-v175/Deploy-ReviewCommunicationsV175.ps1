[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [string]$ZipPath=(Join-Path $PSScriptRoot 'SoodalLife-ReviewCommunications-20260831-v175.zip'),
    [string]$ReleaseBasePath='D:\SOODALLIFE\releases',
    [string]$AppsRoot='D:\SOODALLIFE\apps',
    [switch]$ConfirmProductionDeployment
)

$ErrorActionPreference='Stop'
$expectedSha256='3B36BDA38B2B8C986A797F2FBC1BD9089B60A2725296B641A4DABAD3574EE584'
if(-not $ConfirmProductionDeployment){throw 'Safety stop: add -ConfirmProductionDeployment only after checking the ZIP hash and target paths.'}
$identity=[Security.Principal.WindowsIdentity]::GetCurrent()
$principal=[Security.Principal.WindowsPrincipal]::new($identity)
if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Run PowerShell as Administrator and try again.'}
if(-not(Test-Path -LiteralPath $ZipPath -PathType Leaf)){throw "Deployment ZIP not found: $ZipPath"}
$actual=(Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash
if($actual-ne$expectedSha256){throw "ZIP SHA-256 mismatch. Expected=$expectedSha256, Actual=$actual"}
if(-not(Test-Path -LiteralPath $ReleaseBasePath -PathType Container)){New-Item -ItemType Directory -Path $ReleaseBasePath|Out-Null}

$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$extractPath=Join-Path $ReleaseBasePath "review-communications-v175-$stamp"
New-Item -ItemType Directory -Path $extractPath|Out-Null
Write-Host '1/4 ZIP integrity check passed.' -ForegroundColor Green
Expand-Archive -LiteralPath $ZipPath -DestinationPath $extractPath -ErrorAction Stop
$release=Join-Path $extractPath 'review-communications-v175-final'
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
foreach($relative in $required){if(-not(Test-Path -LiteralPath (Join-Path $release $relative) -PathType Leaf)){throw "Required V175 release file is missing: $relative"}}
if(Test-Path -LiteralPath (Join-Path $release 'api\appsettings.json')){throw 'V175 package unexpectedly contains appsettings.json.'}
$sourceJs=Get-ChildItem -LiteralPath (Join-Path $release 'frontend\customer\assets') -Filter 'index-*.js' -File
if($sourceJs.Count-ne 1){throw "Exactly one V175 customer JavaScript bundle is required. Found: $($sourceJs.Count)"}
$sourceText=[IO.File]::ReadAllText($sourceJs[0].FullName,[Text.Encoding]::UTF8)
foreach($marker in @('/customer/communications','review-conversations/unread-count','soodal-review-conversations-read')){if(-not$sourceText.Contains($marker)){throw "V175 frontend marker missing: $marker"}}
$partnerJs=Get-ChildItem -LiteralPath (Join-Path $release 'frontend\partner\assets') -Filter 'index-*.js' -File
if($partnerJs.Count-ne 1){throw "Exactly one V175 partner JavaScript bundle is required. Found: $($partnerJs.Count)"}
$partnerText=[IO.File]::ReadAllText($partnerJs[0].FullName,[Text.Encoding]::UTF8)
foreach($marker in @('/provider/communications','review-conversations/unread-count','soodal-review-conversations-read')){if(-not$partnerText.Contains($marker)){throw "V175 partner frontend marker missing: $marker"}}
$partnerCss=Get-ChildItem -LiteralPath (Join-Path $release 'frontend\partner\assets') -Filter 'index-*.css' -File
if($partnerCss.Count-ne 1){throw "Exactly one V175 partner CSS bundle is required. Found: $($partnerCss.Count)"}
$partnerCssText=[IO.File]::ReadAllText($partnerCss[0].FullName,[Text.Encoding]::UTF8)
foreach($marker in @('.communicationTabs','.communicationCards','.reviewUnreadBadge')){if(-not$partnerCssText.Contains($marker)){throw "V175 communication CSS marker missing: $marker"}}
Write-Host "2/4 Extracted V175 deployment validated: $release" -ForegroundColor Green

if(-not $PSCmdlet.ShouldProcess("API, customer, partner and admin apps at $AppsRoot",'Deploy V175 review communications')){return}
Write-Host '3/4 Running protected application backup followed by IIS deployment.' -ForegroundColor Yellow
& (Join-Path $release 'Publish-Production.ps1') -ConfirmProductionDeployment -AppsRoot $AppsRoot -BackupParent $ReleaseBasePath -Confirm:$false
if(-not $?){throw 'V175 protected deployment script failed.'}
Write-Host '4/4 V175 review communications deployment completed.' -ForegroundColor Green
Write-Host 'Database schema/data and production appsettings were not changed.' -ForegroundColor Yellow
Write-Host 'Communication hubs, review reply notifications, unread badges and latest-activity ordering are enabled.' -ForegroundColor Yellow
Write-Host "Extracted release path: $release"
