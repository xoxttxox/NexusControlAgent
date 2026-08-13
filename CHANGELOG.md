# Änderungsverlauf

Alle wichtigen Änderungen am Nexus Control Agent werden in dieser Datei
dokumentiert. Die Struktur orientiert sich an
[Keep a Changelog](https://keepachangelog.com/de/1.1.0/) und die Versionierung
an [Semantic Versioning](https://semver.org/lang/de/).

## [0.11.4] – 2026-08-13

### Hinzugefügt

- eigener Button **Protokoll** unten neben **Diagnose** und **Ausblenden**
- kleines lokales Protokollfenster mit Live-Aktualisierung, Kopieren und
  sicherem Leeren
- begrenzte Historie für Smartphone-Verbindungen, Pairing, ausgeführte Befehle
  sowie erfolgreiche, abgelehnte und fehlgeschlagene Aktionen
- kompakte lokale Verwaltung gekoppelter Smartphones mit Gerätename, Plattform,
  Live-Status, letzter Aktivität, Pausieren und sicherem Entfernen
- einzeln konfigurierbare Freigaben für PC-Steuerung, Touchpad und Tastatur,
  Prozesse, Medien und Lautstärke, Bildschirmübertragung, Dateien und
  Energiebefehle
- lokale Verbindungsdiagnose für Agent-Port, Netzwerkadressen, Firewall,
  gekoppelte Geräte, stillen Windows-Autostart und Tailscale
- kopierbarer, bereinigter Diagnosebericht ohne Geräte- oder Push-Tokens

### Geändert

- die grüne Gerätezahl öffnet jetzt die Geräteverwaltung
- der bisherige Button **Aktualisieren** öffnet jetzt die Diagnose; der normale
  Status wird weiterhin automatisch aktualisiert
- Kopfzeile zeigt zusätzlich aktive Geräte und den Verbindungsmodus LAN oder
  Tailscale
- lokale Adressliste verwendet keine dauerhaft blaue Auswahlmarkierung mehr

### Sicherheit

- Protokolle enthalten ausschließlich Zeitpunkt, bereinigten Gerätenamen,
  Plattform, feste Aktionsbezeichnung und Ergebnis; Kennwörter, Tokens,
  Befehlsparameter, Texteingaben, Dateinamen und Dateiinhalte werden nicht
  gespeichert
- hochfrequente Mausbewegungen und Scrollereignisse werden bewusst nicht
  protokolliert
- Berechtigungen werden an Steuerbefehlen sowie Datei-, Bildschirm-, Medien-
  und Prozessdaten serverseitig geprüft
- das Pausieren oder Entfernen eines Geräts beendet dessen laufende Verbindung
  und verhindert neue Befehle
- alte Gerätedateien werden automatisch mit kompatiblen Standardberechtigungen in
  das erweiterte lokale Format übernommen

### Behoben

- Hinweis- und Fehlerdialoge zeigen keine dauerhaft eingeblendete helle
  Scrollleiste mehr.
- Die Dialoghöhe passt sich nun automatisch an den umgebrochenen Meldungstext
  an, damit der Inhalt ohne unnötige Scrollfläche lesbar bleibt.
- bestehende Windows-Autostart-Aufgaben werden beim Start auf den erforderlichen
  `--tray`-Parameter geprüft und bei Bedarf automatisch repariert; dadurch
  öffnet sich das Agent-Fenster nach der Windows-Anmeldung nicht mehr.

## [0.11.3] – 2026-08-12

### Entfernt

- integrierten GitHub-Updater einschließlich Startprüfung, Hintergrunddienst,
  Download, MSI-Helfer und lokaler Update-Ergebnisdateien vollständig entfernt
- Update-Fenster, Update-Button im Hauptfenster, Update-Eintrag im Tray-Menü und
  zugehörige Benachrichtigungen entfernt
- gesamten `Updates`-Abschnitt aus `appsettings.json` entfernt
- automatische `.msi.sha256`-Erzeugung aus Build- und Signierablauf entfernt

Neue Versionen werden als normales MSI manuell installiert.

## [0.11.2] – 2026-08-12

### Geändert

- Projekt-, Agent-, MSI- und Release-Version einheitlich auf 0.11.2 angehoben.

### Behoben

- Download- und Prüfsummenstreams werden vor dem Verschieben oder Löschen des
  MSI-Installers garantiert geschlossen; dadurch blockiert der Updater seine
  eigene Datei unter Windows nicht mehr.
- kurzes Wiederholen beim finalen Verschieben verhindert sporadische Fehler,
  wenn Windows Defender oder ein anderer Virenscanner die neue Datei prüft.

## [0.11.1] – 2026-08-11

### Hinzugefügt

- kleine vorgeschaltete WinForms-Updateanzeige im Discord-Stil, bevor Server,
  Tray-Icon und Hauptfenster gestartet werden
- automatische Installation eines gefundenen Updates direkt beim Start
- kurzer Offline-Timeout; der Agent startet auch ohne erreichbares GitHub weiter

### Behoben

- jeder Updateversuch verwendet einen eigenen temporären Ordner und einen
  eindeutigen Helfer, sodass alte oder noch gesperrte Dateien den Download nicht
  mehr blockieren
- Update-Helfer wartet zusätzlich auf einen lesbaren MSI-Installer, bevor
  Windows Installer gestartet wird

## [0.11.0] – 2026-08-11

### Hinzugefügt

- automatische Updateprüfung über veröffentlichte GitHub Releases beim Start und
  anschließend in einem einstellbaren Intervall
- kompaktes Update-Fenster mit Versionsvergleich, Release-Hinweisen,
  Downloadgröße und Fortschrittsanzeige
- Update-Hinweis im Kopfbereich des Hauptfensters und im Tray-Menü
- separater Update-Helfer, der den Agent sauber beendet, das WiX-MSI als
  Major Upgrade installiert und den Agent danach wieder im Infobereich startet
- automatische `.sha256`-Release-Datei im MSI-Build und nach der Signierung

### Sicherheit

- feste maximale Downloadgröße und Streaming-Download in eine temporäre Datei
- verpflichtende SHA-256-Prüfung über GitHub-Asset-Digest oder separates
  `.sha256`-Asset
- optional aktivierbare Authenticode- und Herausgeberprüfung für öffentliche,
  signierte Releases
- Windows-Installer-Protokoll und sichtbare Erfolg- oder Fehlermeldung nach dem
  Neustart

## [0.10.3] – 2026-08-11

### Geändert

- Rahmen der Verbindungs-, Pairing- und Verhaltensbereiche an das dunkle
  Nexus-Theme angepasst.
- Rahmen der Adressliste verwendet jetzt ebenfalls die zentrale Theme-Farbe.
- Button **Aktualisieren** verbreitert, damit die Beschriftung vollständig
  sichtbar bleibt.

## [0.10.2] – 2026-08-11

### Behoben

- Der Agent-Shutdown übergibt den Fünf-Sekunden-Timeout jetzt korrekt über eine
  `CancellationTokenSource` an `StopAsync`.
- Compilerfehler durch die Übergabe eines `TimeSpan` an `StopAsync` beseitigt.

## [0.10.1] – 2026-08-11

### Geändert

- Projektordner und Namespaces konsistent zusammengeführt.
- API-Routen aus `Program.cs` in eine eigene Netzwerkkomponente ausgelagert.
- nicht verwendeten und veralteten Code entfernt.
- Dokumentation und Build-Skripte auf die bereinigte Struktur umgestellt.

### Behoben

- Nullable-Kontext für automatisch generierten WinForms-Code korrigiert.
- falsche Namespace-Auflösung für `Application.EnableVisualStyles` korrigiert.

## [0.10.0] – 2026-08-11

### Geändert

- Desktop-Oberfläche vollständig auf WinForms umgestellt.
- WPF- und XAML-Abhängigkeiten aus dem Agent entfernt.
- kompaktes, nicht vergrößerbares 500 × 500-Pixel-Fenster eingeführt.
- Betrieb im Infobereich, Pairing, Server und Firewall-Einrichtung in einem
  gemeinsamen Agent-Prozess zusammengeführt.
