@echo off
setlocal EnableExtensions

title Nexus Control Agent
for %%I in ("%~dp0..") do set "PROJECT_ROOT=%%~fI"
cd /d "%PROJECT_ROOT%"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo.
  echo .NET 10 wurde nicht gefunden.
  echo Bitte installiere zuerst das .NET 10 SDK von:
  echo https://dotnet.microsoft.com/download/dotnet/10.0
  echo.
  pause
  exit /b 1
)

dotnet build ^
  "%PROJECT_ROOT%\NexusControlAgent.csproj" ^
  --configuration Release ^
  --nologo

if errorlevel 1 (
  echo.
  echo Build fehlgeschlagen.
  pause
  exit /b 1
)

start "" "%PROJECT_ROOT%\bin\Release\net10.0-windows\NexusControlAgent.exe"
