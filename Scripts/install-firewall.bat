@echo off
setlocal EnableExtensions

title Nexus Control Firewall
cd /d "%~dp0"

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0install-firewall.ps1"

if errorlevel 1 (
  echo.
  echo Die Firewall-Freigabe wurde abgebrochen oder ist fehlgeschlagen.
) else (
  echo.
  echo Firewall-Einrichtung abgeschlossen.
)
pause
