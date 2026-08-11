# Sicherheitsrichtlinie

## Unterstützte Versionen

Sicherheitskorrekturen werden grundsätzlich für die aktuellste veröffentlichte
Version des Nexus Control Agent bereitgestellt. Vor einer Meldung sollte geprüft
werden, ob das Problem mit der neuesten Release weiterhin auftritt.

## Sicherheitsproblem melden

Sicherheitslücken bitte nicht als öffentliches GitHub Issue veröffentlichen.
Verwende stattdessen im GitHub-Repository unter **Security** die Funktion
**Report a vulnerability**. Dafür muss Private Vulnerability Reporting in den
Repository-Einstellungen aktiviert sein.

Eine gute Meldung enthält:

- betroffene Agent-Version und Windows-Version
- Beschreibung der Schwachstelle und ihrer Auswirkungen
- nachvollziehbare Schritte oder einen minimalen Proof of Concept
- vorhandene Schutzmaßnahmen oder Voraussetzungen für einen Angriff
- mögliche Lösungsidee, falls bekannt

Bitte keine echten Gerätetokens, Push-Tokens, Kennwörter, privaten Schlüssel
oder persönlichen Daten mitsenden. Testdaten müssen vor dem Hochladen bereinigt
werden.

## Sicherer Betrieb

- Port `5188` niemals direkt aus dem öffentlichen Internet erreichbar machen.
- Für Fernzugriff ein privates Tailscale-Netz verwenden.
- Pairing nur mit Geräten durchführen, die dem Benutzer gehören und
  vertrauenswürdig sind.
- Windows, Tailscale und den Nexus Control Agent aktuell halten.
- öffentliche Builds vor der Verteilung digital signieren.
