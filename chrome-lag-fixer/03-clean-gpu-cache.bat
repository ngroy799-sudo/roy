@echo off
setlocal EnableExtensions
cd /d "%~dp0"
chcp 65001 >nul

echo.
echo  === 清除 Chrome GPU / Shader / Code Cache ===
echo.
echo  會刪 Default（同 Profile 1-3）入面嘅：
echo    GPUCache, ShaderCache, GrShaderCache, GraphiteDawnCache, Code Cache
echo  書籤、密碼、分頁唔會刪。必須先完全關閉 Chrome。
echo.
choice /C YN /M "關閉 Chrome 並清除 GPU cache"
if errorlevel 2 goto :cancel

echo 關閉 chrome.exe ...
taskkill /IM chrome.exe /F >nul 2>&1
timeout /t 2 /nobreak >nul

set "UD=%LOCALAPPDATA%\Google\Chrome\User Data"
if not exist "%UD%" (
  echo [!] 搵唔到 %UD%
  pause
  exit /b 1
)

for %%P in ("Default" "Profile 1" "Profile 2" "Profile 3") do (
  if exist "%UD%\%%~P" (
    echo 清 %%~P ...
    rmdir /s /q "%UD%\%%~P\GPUCache" 2>nul
    rmdir /s /q "%UD%\%%~P\ShaderCache" 2>nul
    rmdir /s /q "%UD%\%%~P\GrShaderCache" 2>nul
    rmdir /s /q "%UD%\%%~P\GraphiteDawnCache" 2>nul
    rmdir /s /q "%UD%\%%~P\Code Cache" 2>nul
  )
)
rmdir /s /q "%UD%\ShaderCache" 2>nul
rmdir /s /q "%UD%\GrShaderCache" 2>nul
rmdir /s /q "%UD%\GraphiteDawnCache" 2>nul

echo.
echo 完成。重新開 Chrome 試下係咪順啲。
echo 第一次開可能會再編譯 shader，短暫頓一下屬正常。
echo.
pause
exit /b 0

:cancel
echo 已取消。
pause
endlocal
