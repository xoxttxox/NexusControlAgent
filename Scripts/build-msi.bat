@echo off
chcp 65001 >nul
setlocal EnableExtensions EnableDelayedExpansion

title Nexus Control Agent - MSI erstellen

for %%I in ("%~dp0..") do set "PROJECT_ROOT=%%~fI"
cd /d "%PROJECT_ROOT%"

set "APP_PROJECT=%PROJECT_ROOT%\NexusControlAgent.csproj"
set "MSI_PROJECT=%PROJECT_ROOT%\Installer\Msi\NexusControlAgent.Installer.wixproj"
set "PACKAGE_WXS=%PROJECT_ROOT%\Installer\Msi\Package.wxs"
set "VALIDATE_SCRIPT=%PROJECT_ROOT%\Scripts\validate-installer.ps1"
set "PUBLISH_DIR=%PROJECT_ROOT%\artifacts\publish\win-x64"
set "INSTALLER_DIR=%PROJECT_ROOT%\artifacts\installer"
set "LOG_DIR=%PROJECT_ROOT%\artifacts\logs"
set "MSI_BIN_DIR=%PROJECT_ROOT%\Installer\Msi\bin"
set "MSI_OBJ_DIR=%PROJECT_ROOT%\Installer\Msi\obj"
set "MSI_FILE=%INSTALLER_DIR%\NexusControlAgent-Setup-v0.10.3-win-x64.msi"
set "LOCALIZED_MSI_FILE=%INSTALLER_DIR%\de-DE\NexusControlAgent-Setup-v0.10.3-win-x64.msi"
set "APP_LOG=%LOG_DIR%\desktop-build.log"
set "MSI_LOG=%LOG_DIR%\msi-build.log"

echo.
echo [1/7] Voraussetzungen pruefen...
where dotnet >nul 2>&1
if errorlevel 1 goto :missing_dotnet

dotnet --list-sdks | findstr /b /c:"10." >nul 2>&1
if errorlevel 1 goto :missing_sdk

if not exist "%APP_PROJECT%" goto :missing_project
if not exist "%MSI_PROJECT%" goto :missing_project
if not exist "%PACKAGE_WXS%" goto :missing_project
if not exist "%VALIDATE_SCRIPT%" goto :missing_project

if exist "%PUBLISH_DIR%" rmdir /s /q "%PUBLISH_DIR%"
if exist "%INSTALLER_DIR%" rmdir /s /q "%INSTALLER_DIR%"
if exist "%LOG_DIR%" rmdir /s /q "%LOG_DIR%"
if exist "%MSI_BIN_DIR%" rmdir /s /q "%MSI_BIN_DIR%"
if exist "%MSI_OBJ_DIR%" rmdir /s /q "%MSI_OBJ_DIR%"
mkdir "%PUBLISH_DIR%" >nul 2>&1
mkdir "%INSTALLER_DIR%" >nul 2>&1
mkdir "%LOG_DIR%" >nul 2>&1

echo.
echo [2/7] WiX-v7-Lizenz bestaetigen...
echo WiX Toolset 7 verlangt einmalig die Zustimmung zur OSMF-EULA.
echo Details: https://docs.firegiant.com/wix/osmf/
echo.
set "WIX_ACCEPT="
set /p "WIX_ACCEPT=Zum Fortfahren exakt AKZEPTIEREN eingeben: "
if /i not "!WIX_ACCEPT!"=="AKZEPTIEREN" goto :eula_declined

echo.
echo [3/7] Installer-Konfiguration pruefen...
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass ^
  -File "%VALIDATE_SCRIPT%" ^
  -PackagePath "%PACKAGE_WXS%" ^
  -WixProjectPath "%MSI_PROJECT%"
if errorlevel 1 goto :validation_failed

echo.
echo [4/7] Desktop-Begleiter veroeffentlichen...
dotnet restore "%APP_PROJECT%" >"%APP_LOG%" 2>&1
if errorlevel 1 goto :desktop_failed
dotnet publish "%APP_PROJECT%" ^
  --configuration Release ^
  --runtime win-x64 ^
  --self-contained true ^
  --no-restore ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
  -p:PublishTrimmed=false ^
  -p:DebugType=None ^
  -p:DebugSymbols=false ^
  --output "%PUBLISH_DIR%" >>"%APP_LOG%" 2>&1
if errorlevel 1 goto :desktop_failed
if not exist "%PUBLISH_DIR%\NexusControlAgent.exe" goto :desktop_failed

echo.
echo [5/7] MSI-Abhaengigkeiten wiederherstellen...
dotnet restore "%MSI_PROJECT%" -p:AcceptEula=wix7 >"%MSI_LOG%" 2>&1
if errorlevel 1 goto :wix_failed

echo.
echo [6/7] MSI mit WiX Toolset 7 erstellen...
dotnet build "%MSI_PROJECT%" ^
  --configuration Release ^
  --no-restore ^
  -p:AcceptEula=wix7 >>"%MSI_LOG%" 2>&1
if errorlevel 1 goto :wix_failed

if not exist "%MSI_FILE%" (
  if exist "%LOCALIZED_MSI_FILE%" copy /y "%LOCALIZED_MSI_FILE%" "%MSI_FILE%" >nul
)
if not exist "%MSI_FILE%" goto :wix_failed

echo.
echo [7/7] Fertiges Paket pruefen...
for %%I in ("%MSI_FILE%") do set "MSI_SIZE=%%~zI"
if "!MSI_SIZE!"=="0" goto :wix_failed

echo.
echo Fertig:
echo "%MSI_FILE%"
echo.
echo Enthalten:
echo   - stiller Desktop-Begleiter auf Port 5188
echo   - automatischer Start im Windows-Infobereich
echo   - Pairing, Telemetrie, Steuerung, Bildschirm und Dateien
echo   - kein Windows-Kennwort, kein Kerndienst und kein Credential Provider

explorer "%INSTALLER_DIR%"
pause
exit /b 0

:missing_dotnet
echo FEHLER: dotnet wurde nicht gefunden.
goto :failed

:missing_sdk
echo FEHLER: Das .NET 10 SDK wurde nicht gefunden.
goto :failed

:missing_project
echo FEHLER: Eine Projektdatei wurde nicht gefunden.
goto :failed

:eula_declined
echo Abgebrochen: Die WiX-v7-EULA wurde nicht bestaetigt.
goto :failed

:validation_failed
echo FEHLER: Die Installer-Vorabpruefung ist fehlgeschlagen.
goto :failed

:desktop_failed
echo FEHLER: Der Desktop-Begleiter konnte nicht gebaut werden.
if exist "%APP_LOG%" type "%APP_LOG%"
goto :failed

:wix_failed
echo FEHLER: Das MSI-Paket konnte nicht erstellt werden.
if exist "%MSI_LOG%" type "%MSI_LOG%"
goto :failed

:failed
pause
exit /b 1
