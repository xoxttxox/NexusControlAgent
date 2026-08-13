# Nexus Control Agent – MSI-Installer 0.11.4

Der Installer basiert auf WiX Toolset 7 und installiert ausschließlich den
interaktiven Nexus Control Agent für Windows x64.

## MSI erstellen

1. Das .NET 10 SDK installieren.
2. `Scripts\build-msi.bat` starten.
3. Die WiX-v7-OSMF-EULA im Skript ausdrücklich bestätigen.

Die fertige Datei liegt hier:

```text
artifacts\installer\NexusControlAgent-Setup-v0.11.4-win-x64.msi
```

## Installierte Bestandteile

- Nexus Control Agent mit Port `5188`
- Firewall-Regeln für privates LAN und Tailscale
- Startmenüeintrag
- optionaler stiller Tray-Autostart
- optionale Desktop-Verknüpfung

Der Agent benötigt wegen der Windows-Steuerbefehle Administratorrechte. Es
werden kein Windows-Kerndienst, kein Credential Provider, kein Port `5189` und
keine Windows-Anmeldedaten installiert oder eingerichtet.

## Upgrade von 0.7.1

Beim Upgrade entfernt Version 0.8.0 automatisch die Bestandteile der früheren
Entsperrfunktion:

- Dienst `NexusControlCore`
- Credential-Provider- und COM-Registrierung
- lokal geschützte Kennwortdatei und Entsperrschlüssel
- zugehörige Registry-Werte

Die normalen Pairing-Daten unter `%ProgramData%\NexusControl` bleiben erhalten.

## Prüfen

`Scripts\build-msi.bat` führt vor dem Build
`Scripts\validate-installer.ps1` aus. Nach einer Testinstallation kann
`Scripts\verify-install.ps1` verwendet werden.

## Signieren

Für eine öffentliche Verteilung müssen EXE und MSI mit einem vertrauenswürdigen
Code-Signing-Zertifikat signiert werden:

```powershell
.\Scripts\sign-release.ps1 -CertificateThumbprint DEIN_ZERTIFIKAT_THUMBPRINT
```

Danach prüfen:

```powershell
signtool verify /pa /v .\artifacts\installer\NexusControlAgent-Setup-v0.11.4-win-x64.msi
```

Der Agent besitzt keinen integrierten Updater. Neue Versionen werden als MSI
manuell heruntergeladen und installiert; WiX führt dabei ein normales Major
Upgrade der bestehenden Installation aus.
