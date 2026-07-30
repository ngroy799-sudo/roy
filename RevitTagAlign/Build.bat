@echo off
REM Build RevitTagAlign.dll for Revit 2023 (Windows + .NET Framework 4.8)
cd /d "%~dp0"

where dotnet >nul 2>&1
if errorlevel 1 (
  echo ERROR: dotnet SDK not found. Install .NET SDK 8+ from https://dotnet.microsoft.com/download
  exit /b 1
)

echo Restoring packages...
dotnet restore RevitTagAlign.csproj
if errorlevel 1 exit /b 1

echo Building Release...
dotnet build RevitTagAlign.csproj -c Release --no-restore
if errorlevel 1 exit /b 1

echo.
echo SUCCESS: DLL ready at:
echo   %~dp0bin\Release\RevitTagAlign.dll
echo   %~dp0bin\Release\RevitTagAlign.addin
echo.
echo Install: copy both files to:
echo   %%AppData%%\Autodesk\Revit\Addins\2023\
pause
