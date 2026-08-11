@echo off
setlocal EnableExtensions

title Nexus Control Agent Build
for %%I in ("%~dp0..") do set "PROJECT_ROOT=%%~fI"
cd /d "%PROJECT_ROOT%"

dotnet publish "%PROJECT_ROOT%\NexusControlAgent.csproj" ^
  --configuration Release ^
  --runtime win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:DebugType=None ^
  -p:DebugSymbols=false ^
  --output "%PROJECT_ROOT%\artifacts\publish\win-x64"

if errorlevel 1 (
  echo.
  echo Build fehlgeschlagen.
  pause
  exit /b 1
)

echo.
echo Fertig: %PROJECT_ROOT%\artifacts\publish\win-x64\NexusControlAgent.exe
pause
