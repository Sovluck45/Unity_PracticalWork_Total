@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo === Clean Library (Unity Hub can stay open) ===
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0CleanPackageCache.ps1" %*
if errorlevel 1 echo FAILED - see message above.
pause
