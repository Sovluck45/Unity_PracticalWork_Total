param(
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$assetsMirror = Join-Path $projectRoot "Assets\Mirror"
$tag = "v96.10.0"
$zipUrl = "https://github.com/MirrorNetworking/Mirror/archive/refs/tags/$tag.zip"
$tempZip = Join-Path $env:TEMP "Mirror-$tag.zip"
$tempExtract = Join-Path $env:TEMP "Mirror-$tag-extract"

function Test-UnityEditorRunning {
    return $null -ne (Get-Process -Name "Unity" -ErrorAction SilentlyContinue)
}

if ((Test-UnityEditorRunning) -and -not $Force) {
    Write-Host "ERROR: Unity Editor is running. Close the Unity window (not only Hub)." -ForegroundColor Red
    Write-Host "Or run: powershell -ExecutionPolicy Bypass -File .\InstallMirror.ps1 -Force" -ForegroundColor Yellow
    exit 1
}

if (Test-Path $assetsMirror) {
    Write-Host "OK: Assets\Mirror already exists - skip." -ForegroundColor Yellow
    exit 0
}

Write-Host "Downloading Mirror $tag (about 50 MB, wait 1-3 min) ..."
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest -Uri $zipUrl -OutFile $tempZip -UseBasicParsing

Write-Host "Extracting ..."
if (Test-Path $tempExtract) { Remove-Item $tempExtract -Recurse -Force }
Expand-Archive -Path $tempZip -DestinationPath $tempExtract -Force

$sourceFolder = Get-ChildItem $tempExtract -Directory | Select-Object -First 1
$sourceMirror = Join-Path $sourceFolder.FullName "Assets\Mirror"
if (-not (Test-Path $sourceMirror)) {
    Write-Host "ERROR: Assets\Mirror not found inside archive." -ForegroundColor Red
    exit 1
}

New-Item -ItemType Directory -Path (Join-Path $projectRoot "Assets") -Force | Out-Null
Write-Host "Copying to Assets\Mirror ..."
Copy-Item -Path $sourceMirror -Destination $assetsMirror -Recurse -Force

Remove-Item $tempZip -Force -ErrorAction SilentlyContinue
Remove-Item $tempExtract -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "DONE: $assetsMirror" -ForegroundColor Green
Write-Host "Open the project in Unity."
