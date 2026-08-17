param(
    [string]$OutputRoot = (Join-Path $PSScriptRoot '..\artifacts\production'),
    [string]$ApiBaseUrl = 'https://api.soodallife.kr'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$frontendRoot = Join-Path $repositoryRoot 'src\frontend'
$apiProject = Join-Path $repositoryRoot 'src\backend\SoodalLife.Api\SoodalLife.Api.csproj'
$resolvedOutputRoot = [IO.Path]::GetFullPath($OutputRoot)
$apiOutput = Join-Path $resolvedOutputRoot 'api'
$apiBuildOutput = Join-Path $resolvedOutputRoot '_build'
$frontendOutput = Join-Path $resolvedOutputRoot 'frontend'

New-Item -ItemType Directory -Force -Path $apiOutput, $frontendOutput | Out-Null

dotnet publish $apiProject -c Release -o $apiOutput --nologo "-p:BaseOutputPath=$apiBuildOutput"
if ($LASTEXITCODE -ne 0) { throw 'API publish failed.' }

Push-Location $frontendRoot
try {
    if (-not (Test-Path (Join-Path $frontendRoot 'node_modules'))) {
        npm.cmd ci
        if ($LASTEXITCODE -ne 0) { throw 'Frontend dependency restore failed.' }
    }
    $env:VITE_API_BASE_URL = $ApiBaseUrl
    npm.cmd run build
    if ($LASTEXITCODE -ne 0) { throw 'Frontend build failed.' }
}
finally {
    Remove-Item Env:\VITE_API_BASE_URL -ErrorAction SilentlyContinue
    Pop-Location
}

foreach ($site in 'customer', 'partner', 'admin') {
    $siteOutput = Join-Path $frontendOutput $site
    New-Item -ItemType Directory -Force -Path $siteOutput | Out-Null
    Copy-Item (Join-Path $frontendRoot 'dist\*') $siteOutput -Recurse -Force
    Copy-Item (Join-Path $PSScriptRoot "iis\$site.web.config") (Join-Path $siteOutput 'web.config') -Force
}

Write-Output "Production artifacts created at $resolvedOutputRoot"
