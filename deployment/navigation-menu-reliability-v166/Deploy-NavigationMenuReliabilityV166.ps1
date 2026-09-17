[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [switch]$ConfirmProductionDeployment,
    [string]$AppsRoot='D:\SOODALLIFE\apps',
    [string]$ReleaseRoot='D:\SOODALLIFE\releases'
)
$ErrorActionPreference='Stop'
if(-not $ConfirmProductionDeployment){throw 'Safety stop: rerun with -ConfirmProductionDeployment after reviewing the package and target settings.'}
$identity=[Security.Principal.WindowsIdentity]::GetCurrent();$principal=[Security.Principal.WindowsPrincipal]::new($identity);if(-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Run this script from an elevated Administrator PowerShell.'}
$zip=Join-Path $PSScriptRoot 'SoodalLife-NavigationMenuReliability-20260830-v166.zip';if(-not(Test-Path -LiteralPath $zip -PathType Leaf)){throw "V166 ZIP is missing beside this script: $zip"}
$expected='DB07C3C99321A6DAA4E1455508E459283EA9BB98188C34EAEE0964528D374063';$actual=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash;if($actual-ne$expected){throw "V166 ZIP integrity check failed. Expected=$expected Actual=$actual"};Write-Host '1/4 ZIP integrity check passed.' -ForegroundColor Green
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss';$extractParent=Join-Path $ReleaseRoot "navigation-menu-reliability-v166-$stamp";New-Item -ItemType Directory -Force -Path $extractParent|Out-Null;Expand-Archive -LiteralPath $zip -DestinationPath $extractParent
$release=Join-Path $extractParent 'navigation-menu-reliability-v166-final';$required=@('frontend\customer\index.html','frontend\partner\index.html','frontend\admin\index.html','Publish-Production.ps1','RELEASE-MANIFEST.txt');foreach($relative in $required){if(-not(Test-Path -LiteralPath (Join-Path $release $relative) -PathType Leaf)){throw "Required V166 release file is missing: $relative"}}
$sourceJs=Get-ChildItem -LiteralPath (Join-Path $release 'frontend\customer\assets') -Filter 'index-*.js' -File;if($sourceJs.Count-ne 1){throw 'V166 frontend bundle validation failed.'};$sourceText=[IO.File]::ReadAllText($sourceJs[0].FullName,[Text.Encoding]::UTF8);foreach($marker in @('providerLogoutButton','customerNavGroup','providerMenuBackdrop','customerMenuBackdrop')){if(-not $sourceText.Contains($marker)){throw "V166 frontend marker missing: $marker"}};Write-Host "2/4 Extracted V166 deployment validated: $release" -ForegroundColor Green
if(-not $PSCmdlet.ShouldProcess("customer, partner and admin apps at $AppsRoot",'Deploy V166 navigation menu reliability')){return};Write-Host '3/4 Running protected frontend backup followed by IIS deployment.' -ForegroundColor Yellow
& (Join-Path $release 'Publish-Production.ps1') -ConfirmProductionDeployment -AppsRoot $AppsRoot -BackupParent $ReleaseRoot -Confirm:$false
if(-not $?){throw 'V166 protected deployment script failed.'};Write-Host '4/4 V166 navigation menu reliability deployment completed.' -ForegroundColor Green;Write-Host "Extracted release path: $release" -ForegroundColor Yellow
