@echo off
setlocal EnableExtensions
cd /d "%~dp0"
chcp 65001 >nul

echo.
echo  === A/B 對證：關閉 GPU 加速再開 Chrome ===
echo.
echo  如果呢次 Chrome 明顯順咗，lag 就係 NVIDIA / 硬件加速衝突。
echo  呢個測試會關閉而家所有 Chrome 視窗（未儲存分頁會冇）。
echo.
choice /C YN /M "關閉現有 Chrome 然後用 --disable-gpu 重開"
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

echo 啟動 Chrome（--disable-gpu）...
start "" "%CHROME%" --disable-gpu --disable-gpu-compositing --new-window "chrome://gpu"
echo.
echo  而家用平時會 lag 嘅網站試 30 秒。
echo  順咗 → 去 chrome://settings/system 關閉「使用圖形加速功能」
echo         或者之後改用 04-launch-smooth.bat
echo  仍然 lag → 唔係硬件加速。去 index.html 睇雙芒 Hertz / 擷取卡 / 廣告。
echo.
pause
exit /b 0

:cancel
echo 已取消。
pause
endlocal
