@echo off
REM Install RevitTagAlign for Revit 2023 and unblock downloaded files (fixes 0x80131515)
setlocal
cd /d "%~dp0"

set "ADDIN_DIR=%AppData%\Autodesk\Revit\Addins\2023"
if not exist "%ADDIN_DIR%" mkdir "%ADDIN_DIR%"

echo Installing to:
echo   %ADDIN_DIR%
echo.

REM Unblock files in this folder (Mark of the Web from browser download)
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Get-ChildItem -LiteralPath '%~dp0' -Include *.dll,*.addin,*.zip -Recurse -ErrorAction SilentlyContinue | Unblock-File -ErrorAction SilentlyContinue"

if not exist "%~dp0RevitTagAlign.dll" (
  echo ERROR: RevitTagAlign.dll not found next to Install.bat
  echo Extract the zip fully, then run Install.bat again.
  pause
  exit /b 1
)

copy /Y "%~dp0RevitTagAlign.dll" "%ADDIN_DIR%\RevitTagAlign.dll" >nul
copy /Y "%~dp0RevitTagAlign.addin" "%ADDIN_DIR%\RevitTagAlign.addin" >nul

REM Unblock after copy as well
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Unblock-File -LiteralPath '%ADDIN_DIR%\RevitTagAlign.dll' -ErrorAction SilentlyContinue; Unblock-File -LiteralPath '%ADDIN_DIR%\RevitTagAlign.addin' -ErrorAction SilentlyContinue"

echo Done.
echo.
echo Next: FULLY close and reopen Revit 2023
echo.
echo Check load OK:
echo   Add-Ins tab -^> External Tools -^> TagAlign Align Selected Tags
echo.
echo Keyboard shortcut:
echo   Type KS -^> search TagAlign -^> Assign TA
echo.
echo If error 0x80131515:
echo   Right-click RevitTagAlign.dll -^> Properties -^> Unblock
echo.
pause
