@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo === Install Mirror to Assets\Mirror ===
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0InstallMirror.ps1" %*
if errorlevel 1 echo FAILED - see message above.
pause
