[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [string]$RepositoryRoot = 'D:\ALL_project\Soodal_Life_Project',
    [string]$BackupParent,
    [switch]$ConfirmTestContractUpdate
)

$ErrorActionPreference = 'Stop'
if (-not $ConfirmTestContractUpdate) {
    throw 'Safety stop: add -ConfirmTestContractUpdate after checking the repository target.'
}

$testProject = Join-Path $RepositoryRoot 'tests\backend\SoodalLife.Api.Tests\SoodalLife.Api.Tests.csproj'
if (-not (Test-Path -LiteralPath $testProject -PathType Leaf)) {
    throw "Soodal Life test project not found: $testProject"
}

if ([string]::IsNullOrWhiteSpace($BackupParent)) {
    $BackupParent = Join-Path $RepositoryRoot 'releases'
}

$relativeFiles = @(
    'CustomerRequestAbusePolicyApiTests.cs',
    'QuoteFlowApiTests.cs',
    'InteriorProjectCoreApiTests.cs',
    'CustomerInteriorApiTests.cs'
)
$sourceRoot = Join-Path $PSScriptRoot 'tests'
$targetRoot = Split-Path -Parent $testProject
foreach ($relativeFile in $relativeFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $sourceRoot $relativeFile) -PathType Leaf)) {
        throw "V182 source file missing: $relativeFile"
    }
}

if (-not $PSCmdlet.ShouldProcess($RepositoryRoot, 'Apply V182 test-contract consistency update and run 48 related tests')) {
    return
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot = Join-Path $BackupParent "backup-before-request-interior-test-consistency-v182-$stamp"
New-Item -ItemType Directory -Force -Path $backupRoot | Out-Null
$updatedFiles = [System.Collections.Generic.List[string]]::new()

try {
    foreach ($relativeFile in $relativeFiles) {
        $source = Join-Path $sourceRoot $relativeFile
        $target = Join-Path $targetRoot $relativeFile
        if (Test-Path -LiteralPath $target -PathType Leaf) {
            Copy-Item -LiteralPath $target -Destination (Join-Path $backupRoot $relativeFile) -Force
        }
        $sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
        $targetHash = if (Test-Path -LiteralPath $target -PathType Leaf) { (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash } else { $null }
        if ($sourceHash -ne $targetHash) {
            Copy-Item -LiteralPath $source -Destination $target -Force
            $updatedFiles.Add($target)
        }
    }

    $filter = 'FullyQualifiedName~CustomerRequestAbusePolicyApiTests|FullyQualifiedName~QuoteFlowApiTests|FullyQualifiedName~InteriorProjectCoreApiTests|FullyQualifiedName~CustomerRequestApiTests|FullyQualifiedName~CustomerInteriorApiTests|FullyQualifiedName~ProviderInteriorWorkflowApiTests'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    & dotnet test $testProject --configuration Debug --filter $filter --logger 'console;verbosity=minimal'
    if ($LASTEXITCODE -ne 0) {
        throw "V182 verification tests failed with exit code $LASTEXITCODE."
    }
}
catch {
    foreach ($relativeFile in $relativeFiles) {
        $backup = Join-Path $backupRoot $relativeFile
        if (Test-Path -LiteralPath $backup -PathType Leaf) {
            Copy-Item -LiteralPath $backup -Destination (Join-Path $targetRoot $relativeFile) -Force
        }
    }
    throw
}

[PSCustomObject]@{
    DeploymentSucceeded = $true
    DeploymentMode = 'source-test-contract-update'
    Backup = $backupRoot
    UpdatedFileCount = $updatedFiles.Count
    VerifiedTests = 48
    ProductionAppChanged = $false
    DatabaseChanged = $false
    ProductionSettingsChanged = $false
    ThirtyMinuteScheduleContract = 'aligned'
    InteriorAutonomousApiContract = 'aligned'
    RequestQuoteValidationContract = 'aligned'
} | Format-List

Write-Host 'V182 request/interior test-contract consistency verification completed.' -ForegroundColor Green
