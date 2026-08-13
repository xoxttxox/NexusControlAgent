# GitHub-Veröffentlichung

Diese Angaben können beim Erstellen und Einrichten des GitHub-Repositories
direkt übernommen werden.

## Repository

**Empfohlener Name**

```text
NexusControlAgent
```

**Beschreibung für den „About“-Bereich**

```text
Windows-Agent für Nexus Control: sicheres Pairing, PC-Telemetrie, Remote-Steuerung, Medien, Bildschirmübertragung und Dateiübertragung im LAN oder über Tailscale.
```

**Topics**

```text
nexus-control
windows
windows-11
dotnet
csharp
winforms
remote-control
pc-control
websocket
tailscale
telemetry
screen-streaming
```

## Empfohlene Repository-Einstellungen

- Standardbranch: `main`
- Issues aktivieren
- Private Vulnerability Reporting aktivieren
- Discussions nur aktivieren, wenn eine Community aufgebaut werden soll
- Actions erlauben, damit `.github/workflows/build.yml` Builds prüfen kann
- Branchschutz für `main`: Pull Request und erfolgreicher Build erforderlich
- Wiki deaktivieren, solange die Dokumentation im Repository gepflegt wird

## Erstes Hochladen

Im Projektordner ausführen:

```powershell
git init
git add .
git commit -m "Initial public release"
git branch -M main
git remote add origin https://github.com/DEIN-NAME/NexusControlAgent.git
git push -u origin main
```

`DEIN-NAME` muss durch den eigenen GitHub-Benutzernamen oder die Organisation
ersetzt werden.

## Erste GitHub Release

**Tag**

```text
v0.11.4
```

**Titel**

```text
Nexus Control Agent 0.11.4
```

**Dateien für die Release-Assets**

```text
NexusControlAgent-Setup-v0.11.4-win-x64.msi
```

Die Release-Beschreibung kann aus dem Abschnitt `0.11.4` in `CHANGELOG.md`
übernommen werden. Der Agent besitzt keinen integrierten Updater; Benutzer laden
und installieren neue MSI-Versionen manuell.

## Checkliste vor „Public“

- Projekt im Release-Modus erfolgreich bauen
- MSI auf einem sauberen Windows-System testen
- EXE und MSI digital signieren
- keine Tokens, Zertifikate, privaten IP-Adressen oder Logs einchecken
- `git status` und den vollständigen Commit-Inhalt prüfen
- GitHub Private Vulnerability Reporting aktivieren
- gewünschte Quellcode-Lizenz auswählen und als `LICENSE` hinzufügen
- MSI als Release-Asset hochladen

Ohne `LICENSE` ist ein öffentlich sichtbares Repository nicht automatisch Open
Source; die normalen Urheberrechte bleiben bestehen.
