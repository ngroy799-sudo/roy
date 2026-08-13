@echo off
setlocal EnableExtensions
cd /d "%~dp0"

set "TARGET_URL=https://m.happymh.com/manga/quanzhiduzheshijiao"
set "WINDOW_WIDTH=390"
set "WINDOW_HEIGHT=844"
set "MOBILE_UA=Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1"

if exist "%~dp0config.env" (
  for /f "usebackq eol=# tokens=1,* delims==" %%A in ("%~dp0config.env") do (
    if /i "%%A"=="TARGET_URL" set "TARGET_URL=%%B"
    if /i "%%A"=="WINDOW_WIDTH" set "WINDOW_WIDTH=%%B"
    if /i "%%A"=="WINDOW_HEIGHT" set "WINDOW_HEIGHT=%%B"
  )
)

if not "%~1"=="" set "TARGET_URL=%~1"

set "CHROME="
if exist "%ProgramFiles%\Google\Chrome\Application\chrome.exe" set "CHROME=%ProgramFiles%\Google\Chrome\Application\chrome.exe"
if exist "%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe" set "CHROME=%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe"
if exist "%LocalAppData%\Google\Chrome\Application\chrome.exe" set "CHROME=%LocalAppData%\Google\Chrome\Application\chrome.exe"

if "%CHROME%"=="" (
  echo [!] 搵唔到 Google Chrome。請安裝 Chrome，或改用 launch-edge.bat
  pause
  exit /b 1
)

echo 啟動 Chrome（手機 UA）...
echo URL: %TARGET_URL%
echo.
echo 提示：請先安裝 uBlock Origin 攔截廣告。
echo 若排版怪：F12 -^> Ctrl+Shift+M -^> 選 iPhone -^> 重新整理
echo.

start "" "%CHROME%" --new-window --user-agent="%MOBILE_UA%" --window-size=%WINDOW_WIDTH%,%WINDOW_HEIGHT% "%TARGET_URL%"
endlocal
