[CmdletBinding()]
param(
    [int]$Hours = 2,
    [string]$ServerInstance = '.',
    [string]$DatabaseName = 'SOODAL_LIFE_DEV',
    [string]$AppsRoot = 'D:\SOODALLIFE\apps',
    [string]$OutputDirectory = 'D:\SOODALLIFE\diagnostics'
)
$ErrorActionPreference='Continue'
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$outputPath=Join-Path $OutputDirectory "chat-attachment-v138-$stamp.txt"
New-Item -ItemType Directory -Path $OutputDirectory -Force|Out-Null
Start-Transcript -LiteralPath $outputPath -Force|Out-Null
try{
    Write-Host '=== CHAT ATTACHMENT V138 ROOT-CAUSE DIAGNOSTICS ==='
    Write-Host "CollectedAt: $(Get-Date -Format o)"
    Write-Host "Computer: $env:COMPUTERNAME"
    Write-Host "WindowHours: $Hours"

    Import-Module WebAdministration -ErrorAction SilentlyContinue
    Write-Host "`n=== API IIS SITE CONFIGURATION ==="
    $apiTarget=[IO.Path]::GetFullPath((Join-Path $AppsRoot 'api')).TrimEnd('\')
    $apiSite=Get-Website|Where-Object{try{[IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables([string]$_.PhysicalPath)).TrimEnd('\') -ieq $apiTarget}catch{$false}}|Select-Object -First 1
    if($apiSite){
        $readAhead=(Get-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' -Location $apiSite.Name -Filter 'system.webServer/serverRuntime' -Name 'uploadReadAheadSize' -ErrorAction SilentlyContinue).Value
        [PSCustomObject]@{Site=$apiSite.Name;PhysicalPath=$apiSite.PhysicalPath;UploadReadAheadSize=$readAhead}|Format-List
    }else{Write-Warning 'API IIS website could not be resolved.'}
    Write-Host "`n=== IIS APPLICATION POOLS ==="
    foreach($pool in @('SoodalLife.Api','soodallife.kr','SoodalLife.Static')){
        if(Test-Path "IIS:\AppPools\$pool"){
            $item=Get-Item "IIS:\AppPools\$pool"
            [PSCustomObject]@{Name=$pool;State=(Get-WebAppPoolState -Name $pool).Value;IdentityType=$item.processModel.identityType;UserName=$item.processModel.userName}|Format-List
        }else{Write-Warning "Application pool not found: $pool"}
    }

    $privateRoot=Join-Path $AppsRoot 'api\App_Data\private-files'
    Write-Host "`n=== PRIVATE STORAGE ==="
    [PSCustomObject]@{Path=$privateRoot;Exists=(Test-Path -LiteralPath $privateRoot -PathType Container)}|Format-List
    if(Test-Path -LiteralPath $privateRoot -PathType Container){
        & icacls.exe $privateRoot
        $probe=Join-Path $privateRoot ".diagnostic-write-$stamp.tmp"
        try{[IO.File]::WriteAllText($probe,'WRITE_OK',[Text.Encoding]::UTF8);Write-Host 'AdministratorWriteProbe: ok'}catch{Write-Host "AdministratorWriteProbe: failed - $($_.Exception.Message)"}finally{if(Test-Path -LiteralPath $probe){Remove-Item -LiteralPath $probe -Force}}
    }
    Write-Host "`n=== CHAT UPLOAD CHUNK DIRECTORIES ==="
    $chunkRoot=Join-Path $privateRoot 'chat-upload'
    if(Test-Path -LiteralPath $chunkRoot){
        Get-ChildItem -LiteralPath $chunkRoot -Directory -Recurse -ErrorAction SilentlyContinue|
            Where-Object{$_.Name -like 'chunks-*'}|
            ForEach-Object{$files=@(Get-ChildItem -LiteralPath $_.FullName -File -ErrorAction SilentlyContinue);[PSCustomObject]@{Path=$_.FullName;LastWriteTime=$_.LastWriteTime;ChunkCount=$files.Count;TotalBytes=($files|Measure-Object Length -Sum).Sum}}|
            Sort-Object LastWriteTime -Descending|Select-Object -First 30|Format-List
    }else{Write-Host 'No chat-upload directory exists.'}

    Write-Host "`n=== RECENT APPLICATION ERRORS ==="
    $since=(Get-Date).AddHours(-[Math]::Abs($Hours))
    Get-WinEvent -FilterHashtable @{LogName='Application';StartTime=$since;Level=2,3} -ErrorAction SilentlyContinue |
        Where-Object {$_.ProviderName -match 'IIS|IIS AspNetCore|ASP.NET|\.NET Runtime|Application Error'} |
        Select-Object -First 80 TimeCreated,ProviderName,Id,LevelDisplayName,Message | Format-List

    Write-Host "`n=== RECENT IIS CHAT REQUESTS ==="
    $iisLogRoot='C:\inetpub\logs\LogFiles'
    if(Test-Path -LiteralPath $iisLogRoot){
        Get-ChildItem -LiteralPath $iisLogRoot -Filter '*.log' -File -Recurse -ErrorAction SilentlyContinue |
            Where-Object {$_.LastWriteTime -ge $since} |
            ForEach-Object {Select-String -LiteralPath $_.FullName -Pattern '/messages/file-chunks','/messages/file-data','/messages/file' -SimpleMatch -ErrorAction SilentlyContinue} |
            Select-Object -Last 100 Path,LineNumber,Line | Format-List
    }

    Write-Host "`n=== RECENT HTTPERR ENTRIES ==="
    $httpErrRoot='C:\Windows\System32\LogFiles\HTTPERR'
    if(Test-Path -LiteralPath $httpErrRoot){
        Get-ChildItem -LiteralPath $httpErrRoot -Filter '*.log' -File -ErrorAction SilentlyContinue|
            Where-Object{$_.LastWriteTime -ge $since}|
            ForEach-Object{Get-Content -LiteralPath $_.FullName -Tail 300 -ErrorAction SilentlyContinue}|
            Select-Object -Last 300
    }

    Write-Host "`n=== CHAT ATTACHMENT DATABASE ROWS ==="
    $query=@"
SET NOCOUNT ON;
SELECT TOP (30)
    public_id, purpose_code, original_file_name, content_type, size_bytes, status_code,
    malware_scan_status_code, privacy_inspection_status_code, created_at, activated_at
FROM files
WHERE purpose_code = 'CHAT_ATTACHMENT'
ORDER BY id DESC;
"@
    & sqlcmd.exe -S $ServerInstance -E -C -I -b -f 65001 -d $DatabaseName -Q $query
    if($LASTEXITCODE -ne 0){Write-Warning "Database query failed with exit code $LASTEXITCODE"}

    Write-Host "`n=== DEPLOYED FILES ==="
    Get-Item -LiteralPath (Join-Path $AppsRoot 'api\SoodalLife.Api.dll'),(Join-Path $AppsRoot 'customer\index.html'),(Join-Path $AppsRoot 'partner\index.html') -ErrorAction SilentlyContinue |
        Select-Object FullName,Length,LastWriteTime | Format-Table -AutoSize
}finally{
    Stop-Transcript|Out-Null
}
Write-Host "Diagnostic report: $outputPath" -ForegroundColor Green
