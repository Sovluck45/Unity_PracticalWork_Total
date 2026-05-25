param(
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$library = Join-Path $projectRoot "Library"

function Test-UnityEditorRunning {
    return $null -ne (Get-Process -Name "Unity" -ErrorAction SilentlyContinue)
}

if ((Test-UnityEditorRunning) -and -not $Force) {
    Write-Host "ERROR: Unity Editor is running. Close the Unity window (not only Hub)." -ForegroundColor Red
    Write-Host "Or run: powershell -ExecutionPolicy Bypass -File .\CleanPackageCache.ps1 -Force" -ForegroundColor Yellow
    exit 1
}

if (Test-Path $library) {
    Write-Host "Deleting Library ..."
    Remove-Item $library -Recurse -Force
    Write-Host "OK: Library deleted." -ForegroundColor Green
} else {
    Write-Host "Library folder not found (already clean)." -ForegroundColor Yellow
}

Write-Host "Next: run 2_InstallMirror.cmd"
Write-Host "Then open the project in Unity."
