# Nexus Control Agent

**Der Windows-Begleiter für Nexus Control.**

![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?logo=windows&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![UI](https://img.shields.io/badge/UI-WinForms-2D7DDB)

Nexus Control Agent verbindet die mobile Nexus-Control-App mit einem
Windows-PC. Der Agent läuft kompakt im Windows-Infobereich und stellt die
lokalen Funktionen bereit, die für Statusanzeige, PC-Steuerung,
Medienbedienung, Bildschirmübertragung und Dateiübertragung benötigt werden.

Die Verbindung ist für das private Netzwerk und optional für den verschlüsselten
Fernzugriff über Tailscale ausgelegt. Es werden keine Windows-Kennwörter, PINs
oder Entsperrdaten gespeichert.

## Funktionen

- QR-Code- und Zahlencode-Pairing mit zeitlich begrenzten Codes
- vertrauenswürdige Geräte mit gehashten, zufällig erzeugten Gerätetokens
- PC-Status für CPU, GPU, RAM, Laufwerke, Netzwerk und verfügbare Temperaturen
- Sperren, Standby, Neustart und Herunterfahren des PCs
- Remote-Maus, Tastatur, Mediensteuerung und Windows-Lautstärke
- aktive Windows-Mediensitzungen, beispielsweise Browser, Spotify oder YouTube
- Bildschirmübertragung mit Auswahl mehrerer Monitore
- Dateiübertragung bis maximal 100 MB pro Datei
- Wake-on-LAN-Informationen für gekoppelte Geräte
- automatischer Start mit Windows und stiller Betrieb im Infobereich
- automatische GitHub-Release-Updates mit geprüftem MSI-Upgrade
- lokale Firewall-Einrichtung für den Agent-Port
- optionale Push-Benachrichtigungen für überwachte Ereignisse

## Voraussetzungen

### Für die Installation

- Windows 10 oder Windows 11
- ein PC und Smartphone im selben privaten Netzwerk oder im selben
  Tailscale-Netz
- Administratorrechte für Installation, Firewall und Windows-Steuerbefehle

Das veröffentlichte Windows-x64-Paket ist selbstständig und benötigt keine
separat installierte .NET-Laufzeit.

### Für die Entwicklung

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio mit den Workloads **.NET-Desktopentwicklung** und
  **ASP.NET und Webentwicklung**
- für den MSI-Build: WiX Toolset 7 über das enthaltene Installer-Projekt

## Installation und Pairing

1. Das aktuelle MSI-Paket aus den GitHub Releases herunterladen.
2. `NexusControlAgent-Setup-vX.Y.Z-win-x64.msi` starten.
3. Die gewünschten Installer-Optionen auswählen und die Firewall-Abfrage
   bestätigen.
4. Den Nexus Control Agent über das Startmenü öffnen.
5. In der mobilen Nexus-Control-App **PC hinzufügen** auswählen.
6. Den QR-Code scannen oder den sechsstelligen Pairing-Code eingeben.

Nach erfolgreichem Pairing bleibt der Agent im Infobereich aktiv. Das kleine
Fenster kann jederzeit ausgeblendet werden, ohne den Agent zu beenden.

> [!IMPORTANT]
> Port `5188` darf nicht direkt aus dem Internet weitergeleitet werden. Für den
> Zugriff außerhalb des Heimnetzes sollte Tailscale verwendet werden.

## Lokal entwickeln

Repository klonen und anschließend `NexusControlAgent.sln` in Visual Studio
öffnen. Als Startprojekt muss **NexusControlAgent** ausgewählt sein.

Alternativ in einer Windows-Konsole:

```powershell
dotnet restore .\NexusControlAgent.csproj
dotnet build .\NexusControlAgent.csproj --configuration Release
```

Der mitgelieferte Schnellstart führt den Build aus und startet danach den Agent:

```text
Scripts\start-agent.bat
```

## Konfiguration

Die Standardwerte befinden sich in `appsettings.json` unter `Agent` und
`Updates`.

| Einstellung | Standard | Bedeutung |
| --- | ---: | --- |
| `Port` | `5188` | Lokaler HTTP- und WebSocket-Port |
| `PairingCodeLifetimeMinutes` | `10` | Gültigkeitsdauer eines Pairing-Codes |
| `MaximumPairingAttempts` | `8` | Erlaubte Versuche pro Pairing-Code |
| `TelemetryIntervalMilliseconds` | `2000` | Intervall der normalen Telemetrie |
| `AllowedClockSkewMinutes` | `2` | Erlaubte Zeitabweichung bei Befehlen |
| `MaximumMessageSizeBytes` | `65536` | Maximale Größe einer WebSocket-Nachricht |
| `PushTemperatureThresholdCelsius` | `85` | Temperaturschwelle für Warnungen |

Damit der integrierte Updater das richtige öffentliche Repository prüft, muss
vor dem Veröffentlichen einmal `Updates:RepositoryOwner` auf den eigenen
GitHub-Benutzernamen oder die Organisation gesetzt werden. Der Platzhalter
`DEIN-GITHUB-NAME` deaktiviert die Netzwerkprüfung bewusst, bis diese Angabe
korrekt ist.

Der Updater erwartet pro Release exakt diese beiden Assets:

```text
NexusControlAgent-Setup-vX.Y.Z-win-x64.msi
NexusControlAgent-Setup-vX.Y.Z-win-x64.msi.sha256
```

Ein Update wird zunächst vollständig heruntergeladen und per SHA-256 geprüft.
Danach beendet ein separater Helfer den Agent, führt das bestehende WiX-Major-
Upgrade aus und startet den Agent erneut. Bei einem per-Machine-MSI kann Windows
dabei die Administratorbestätigung anzeigen.

Die automatische Prüfung startet standardmäßig nach acht Sekunden und danach
alle vier Stunden. Ein Download ist auf 300 MB und 15 Minuten begrenzt; alle
Werte lassen sich im Abschnitt `Updates` anpassen.

Für lokale Entwicklungswerte kann .NET User Secrets verwendet werden. Private
Tokens, Zertifikate und Zugangsdaten dürfen nicht in `appsettings.json` oder in
Git eingecheckt werden.

## Veröffentlichen

Selbstständige Windows-x64-App erstellen:

```text
Scripts\publish-agent.bat
```

App und deutsches WiX-7-MSI erstellen:

```text
Scripts\build-msi.bat
```

Die fertigen Dateien werden nicht in Git eingecheckt, sondern unter
`artifacts\` erzeugt:

```text
artifacts\publish\win-x64\NexusControlAgent.exe
artifacts\installer\NexusControlAgent-Setup-vX.Y.Z-win-x64.msi
artifacts\installer\NexusControlAgent-Setup-vX.Y.Z-win-x64.msi.sha256
```

Vor einer öffentlichen Veröffentlichung sollten EXE und MSI mit einem
vertrauenswürdigen Code-Signing-Zertifikat signiert werden. Hinweise dazu
stehen in `Installer/README.md`.

## Projektstruktur

```text
Application/         WinForms-Lifecycle und Infobereich
Configuration/       Einstellungen und Validierung
Forms/               Hauptfenster, Designer und Ressourcen
Models/              API-, Telemetrie- und Nachrichtenmodelle
Networking/          HTTP, WebSocket und Netzwerkprüfung
Pairing/             Pairing und vertrauenswürdige Geräte
Security/            Sichere lokale Pfade
Services/            Telemetrie, Medien, Bildschirm, Dateien und Autostart
UI/                  Dark-Theme und Standarddialoge
Updates/             GitHub-Release-Prüfung und MSI-Update-Helfer
Windows/             Windows-Steuerung und Audio-Integration
Installer/           WiX-7-Installer und Installer-Assets
Scripts/             Build-, Installations- und Prüfroutinen
```

## Sicherheit und Datenschutz

- Pairing-Codes sind zeitlich begrenzt und werden bei zu vielen Fehlversuchen
  erneuert.
- Gerätetokens werden lokal nur als SHA-256-Hash gespeichert.
- Befehle werden durch Zeitfenster, Whitelists und Rate-Limits geprüft.
- Verbindungen werden nur aus Loopback, privaten Netzwerken und Tailscale-Netzen
  akzeptiert.
- Es gibt keine Windows-Entsperrfunktion und keine Speicherung von Kennwörtern
  oder PINs.
- Der Agent installiert keinen Windows-Kerndienst und keinen Credential Provider.

Sicherheitsprobleme bitte nicht öffentlich als normales Issue melden. Der
verantwortungsvolle Meldeweg ist in [SECURITY.md](SECURITY.md) beschrieben.

## Mitwirken

Fehlerberichte und Verbesserungsvorschläge sind willkommen. Vor größeren
Änderungen bitte zuerst ein Issue erstellen. Weitere Hinweise stehen in
[CONTRIBUTING.md](CONTRIBUTING.md).

## Weitere Dokumente

- [Änderungsverlauf](CHANGELOG.md)
- [Mitwirken](CONTRIBUTING.md)
- [Sicherheitsrichtlinie](SECURITY.md)
- [GitHub-Veröffentlichung](GITHUB_SETUP.md)
- [Installer-Dokumentation](Installer/README.md)

## Lizenz

Für den Quellcode wurde noch keine Open-Source-Lizenz festgelegt. Solange keine
`LICENSE`-Datei vorhanden ist, bleiben alle Rechte vorbehalten. Vor einer
öffentlichen Freigabe zur Weiterverwendung sollte bewusst eine passende Lizenz
ausgewählt werden.
