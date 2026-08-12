@echo off
chcp 65001 >nul
setlocal EnableExtensions

for %%I in ("%~dp0..") do set "PROJECT_ROOT=%%~fI"
set "MSI=%PROJECT_ROOT%\artifacts\installer\NexusControlAgent-Setup-v0.11.3-win-x64.msi"
set "LOG=%PROJECT_ROOT%\artifacts\logs\msi-install.log"

if not exist "%MSI%" (
  echo FEHLER: MSI nicht gefunden:
  echo "%MSI%"
  pause
  exit /b 1
)

if not exist "%PROJECT_ROOT%\artifacts\logs" mkdir "%PROJECT_ROOT%\artifacts\logs"

echo Nexus Control Agent wird mit vollstaendigem MSI-Protokoll installiert.
echo Logdatei: "%LOG%"
echo.
msiexec.exe /i "%MSI%" /L*V "%LOG%"
set "EXITCODE=%ERRORLEVEL%"

echo.
if "%EXITCODE%"=="0" (
  echo Installation erfolgreich.
  powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass ^
    -File "%PROJECT_ROOT%\Scripts\verify-install.ps1"
  if errorlevel 1 (
    echo Installationspruefung fehlgeschlagen.
    set "EXITCODE=1"
  )
) else (
  echo Installation fehlgeschlagen. Windows-Installer-Code: %EXITCODE%
  echo Logdatei: "%LOG%"
)
pause
exit /b %EXITCODE%
