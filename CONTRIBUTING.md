# Mitwirken

Danke für das Interesse am Nexus Control Agent.

## Fehler und Vorschläge

- Vor dem Erstellen eines Issues nach bereits vorhandenen Meldungen suchen.
- Für Fehler die Bug-Vorlage verwenden und genaue Schritte zum Nachstellen
  angeben.
- Für neue Funktionen die Feature-Vorlage verwenden und den konkreten Nutzen
  beschreiben.
- Keine Gerätetokens, Push-Tokens, privaten IP-Adressen oder persönlichen Logs
  veröffentlichen.

Sicherheitsprobleme gehören nicht in öffentliche Issues. Dafür gilt der
Meldeweg aus `SECURITY.md`.

## Entwicklung

1. Repository forken und einen Branch von `main` erstellen.
2. Änderungen klein und thematisch zusammenhängend halten.
3. Projekt mit .NET 10 unter Windows bauen.
4. Betroffene Funktionen manuell testen.
5. Dokumentation und `CHANGELOG.md` aktualisieren, wenn sich sichtbares
   Verhalten ändert.
6. Pull Request mit verständlicher Beschreibung erstellen.

Branch-Beispiele:

```text
fix/pairing-timeout
feature/media-session-volume
docs/installation
```

## Code-Stil

- bestehende Ordner- und Namespace-Struktur beibehalten
- Nullable-Warnungen nicht durch globale Deaktivierung umgehen
- UI-Änderungen weiterhin im WinForms-Designer abbilden
- keine WPF- oder XAML-Abhängigkeiten hinzufügen
- asynchrone Vorgänge mit `CancellationToken` abbrechbar halten
- keine Zugangsdaten oder maschinenspezifischen Werte fest eintragen

Ein Pull Request sollte ohne neue Compilerfehler bauen und keine generierten
Ordner wie `bin`, `obj` oder `artifacts` enthalten.
