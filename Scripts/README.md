# Nexus Control Agent – Skripte 0.11.4

- `build-msi.bat`: veröffentlicht den Windows-x64-Agent und baut das WiX-MSI.
- `publish-agent.bat`: erstellt nur die selbstständige Desktop-App.
- `start-agent.bat`: baut und startet den Agent lokal.
- `install-firewall.bat` / `install-firewall.ps1`: richtet Port `5188` für
  privates LAN und Tailscale ein.
- `install-msi-test.bat`: installiert das gebaute MSI mit ausführlichem Log.
- `validate-installer.ps1`: prüft die WiX-Quelldatei vor dem Build.
- `verify-install.ps1`: prüft Agent, Firewall und die Entfernung alter
  Entsperrkomponenten nach der Installation.
- `sign-release.ps1`: signiert Agent und MSI mit SHA-256 und Zeitstempel.

MSI-Ausgabe:

```text
artifacts\installer\NexusControlAgent-Setup-v0.11.4-win-x64.msi
```
