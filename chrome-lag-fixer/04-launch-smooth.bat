@echo off
setlocal EnableExtensions
cd /d "%~dp0"
chcp 65001 >nul

echo.
echo  === 順暢模式啟動 Chrome ===
echo.
echo  參數：D3D11 + 關掉 Skia Graphite / Vulkan
echo  （保留硬件加速，但避開 NVIDIA 上最常 stutter 嗰條路徑）
echo  會關閉而家所有 Chrome 視窗。
echo.
choice /C YN /M "關閉現有 Chrome 然後用順暢參數重開"
if errorlevel 2 goto :cancel

set "CHROME="
if exist "%ProgramFiles%\Google\Chrome\Application\chrome.exe" set "CHROME=%ProgramFiles%\Google\Chrome\Application\chrome.exe"
if exist "%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe" set "CHROME=%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe"
if exist "%LocalAppData%\Google\Chrome\Application\chrome.exe" set "CHROME=%LocalAppData%\Google\Chrome\Application\chrome.exe"

if "%CHROME%"=="" (
  echo [!] 搵唔到 Google Chrome。
  pause
  exit /b 1
)

echo 關閉 chrome.exe ...
taskkill /IM chrome.exe /F >nul 2>&1
timeout /t 2 /nobreak >nul

echo 啟動 Chrome（smooth flags）...
start "" "%CHROME%" --use-angle=d3d11 --disable-features=Vulkan,SkiaGraphite,CalculateNativeWinOcclusion --new-window "chrome://gpu"
echo.
echo  捲動、拖視窗、睇片試 30 秒。
echo  仍然 lag：返去 02-test-no-gpu.bat 或者統一雙芒 Hertz。
echo.
pause
exit /b 0

:cancel
echo 已取消。
pause
endlocal
