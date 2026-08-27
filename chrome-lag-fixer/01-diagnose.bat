@echo off
setlocal EnableExtensions
cd /d "%~dp0"
chcp 65001 >nul

echo.
echo  === Chrome lag 診斷 ===
echo  會收集：GPU、螢幕刷新率、Chrome RAM、擷取卡、OBS / NVIDIA Overlay
echo  報告：reports\chrome-lag-report.txt
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0diagnose.ps1"
set "ERR=%ERRORLEVEL%"

echo.
if not "%ERR%"=="0" (
  echo [!] PowerShell 診斷失敗，exit code %ERR%
) else (
  echo 完成。用記事本開 reports\chrome-lag-report.txt
)
echo.
pause
endlocal
