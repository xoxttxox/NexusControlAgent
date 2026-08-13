using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexusControl.Agent.Security;

namespace NexusControl.Agent.Services;

/// <summary>
/// Begrenztes lokales Aktivitätsprotokoll. Die API nimmt absichtlich keine
/// Tokens, Kennwörter, Befehlsparameter, Texteingaben, Dateinamen oder
/// Dateiinhalte entgegen.
/// </summary>
internal sealed class ActivityLogService
{
    private const int MaximumEntries = 250;
    private const long MaximumFileBytes = 512 * 1024;

    private readonly Lock _gate = new();
    private readonly string _path;
    private long _revision;

    public ActivityLogService()
    {
        NexusPaths.EnsureSecureDataDirectory();
        _path = NexusPaths.ActivityLogPath;
    }

    public long Revision => Interlocked.Read(ref _revision);

    public void Record(
        string? deviceName,
        string? platform,
        string action,
        ActivityLogResult result)
    {
        var entry = new ActivityLogEntry(
            DateTimeOffset.UtcNow,
            Sanitize(deviceName, 80, "Unbekanntes Gerät"),
            Sanitize(platform, 40, "Unbekannt"),
            Sanitize(action, 100, "Unbekannte Aktion"),
            result);

        lock (_gate)
        {
            try
            {
                File.AppendAllText(
                    _path,
                    JsonSerializer.Serialize(entry, JsonOptions)
                        + Environment.NewLine,
                    Encoding.UTF8);
                Interlocked.Increment(ref _revision);
                CompactIfNeeded();
            }
            catch (Exception error)
            {
                // Das optionale Protokoll darf weder Befehle noch den
                // Agent-Start blockieren.
                System.Diagnostics.Debug.WriteLine(
                    $"Activity log write failed: {error.Message}");
            }
        }
    }

    public IReadOnlyList<ActivityLogEntry> ReadRecent(int maximum = 100)
    {
        maximum = Math.Clamp(maximum, 1, MaximumEntries);
        lock (_gate)
        {
            try
            {
                return ReadAll()
                    .TakeLast(maximum)
                    .Reverse()
                    .ToArray();
            }
            catch (Exception error)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Activity log read failed: {error.Message}");
                return Array.Empty<ActivityLogEntry>();
            }
        }
    }

    public bool Clear()
    {
        lock (_gate)
        {
            try
            {
                File.WriteAllText(_path, string.Empty, Encoding.UTF8);
                Interlocked.Increment(ref _revision);
                return true;
            }
            catch (Exception error)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Activity log clear failed: {error.Message}");
                return false;
            }
        }
    }

    public string BuildReport(int maximum = 250)
    {
        var entries = ReadRecent(Math.Clamp(maximum, 1, MaximumEntries));
        var report = new StringBuilder();
        report.AppendLine("Nexus Control Agent – lokales Protokoll");
        report.AppendLine(
            "Enthält keine Kennwörter, Tokens, Befehlsparameter, Dateinamen oder Dateiinhalte.");
        report.AppendLine();

        foreach (var entry in entries.Reverse())
        {
            report.AppendLine(
                $"{entry.Timestamp.ToLocalTime():yyyy-MM-dd HH:mm:ss} | "
                + $"{entry.DeviceName} ({entry.Platform}) | "
                + $"{entry.Action} | {ResultText(entry.Result)}");
        }

        if (entries.Count == 0)
        {
            report.AppendLine("Noch keine Aktivität protokolliert.");
        }

        return report.ToString().TrimEnd();
    }

    public static string CommandAction(string command) =>
        command switch
        {
            "system.wake" => "PC aufwecken",
            "system.sleep" => "Standby",
            "system.restart" => "PC neu starten",
            "system.shutdown" => "PC herunterfahren",
            "session.lock" => "PC sperren",
            "media.playPause" => "Medien: Wiedergabe/Pause",
            "media.next" => "Medien: nächster Titel",
            "media.previous" => "Medien: vorheriger Titel",
            "media.session.playPause" =>
                "Aktive Medien: Wiedergabe/Pause",
            "media.session.next" => "Aktive Medien: nächster Titel",
            "media.session.previous" => "Aktive Medien: vorheriger Titel",
            "media.session.setVolume" => "Aktive Medien: Lautstärke",
            "media.session.toggleMute" =>
                "Aktive Medien: Stummschaltung",
            "audio.toggleMute" => "Windows-Audio: Stummschaltung",
            "audio.setVolume" => "Windows-Audio: Lautstärke",
            "input.pointerButton" => "Remote-Touchpad: Mausklick",
            "input.keyboardText" => "Remote-Tastatur: Texteingabe",
            "process.terminate" => "Prozess beenden",
            "screen.start" => "Bildschirmübertragung starten",
            "screen.stop" => "Bildschirmübertragung beenden",
            _ => "Unbekannter Befehl",
        };

    public static bool ShouldRecordCommand(string command) =>
        command is not (
            "input.pointerMove"
            or "input.pointerScroll");

    public static string ResultText(ActivityLogResult result) =>
        result switch
        {
            ActivityLogResult.Success => "Erfolgreich",
            ActivityLogResult.Rejected => "Abgelehnt",
            ActivityLogResult.Failed => "Fehlgeschlagen",
            _ => "Information",
        };

    private void CompactIfNeeded()
    {
        var file = new FileInfo(_path);
        if (!file.Exists || file.Length <= MaximumFileBytes)
        {
            return;
        }

        var retained = ReadAll().TakeLast(MaximumEntries).ToArray();
        var temporaryPath = $"{_path}.tmp";
        File.WriteAllLines(
            temporaryPath,
            retained.Select(entry =>
                JsonSerializer.Serialize(entry, JsonOptions)),
            Encoding.UTF8);
        File.Move(temporaryPath, _path, overwrite: true);
    }

    private IReadOnlyList<ActivityLogEntry> ReadAll()
    {
        if (!File.Exists(_path))
        {
            return Array.Empty<ActivityLogEntry>();
        }

        var entries = new List<ActivityLogEntry>();
        foreach (var line in File.ReadLines(_path, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var entry = JsonSerializer.Deserialize<ActivityLogEntry>(
                    line,
                    JsonOptions);
                if (entry is not null)
                {
                    entries.Add(entry);
                }
            }
            catch (JsonException)
            {
                // Eine beschädigte Einzelzeile blockiert den Agent nicht.
            }
        }

        return entries;
    }

    private static string Sanitize(
        string? value,
        int maximumLength,
        string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var cleaned = new string(value
            .Where(character => !char.IsControl(character))
            .ToArray())
            .Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return fallback;
        }

        return cleaned[..Math.Min(cleaned.Length, maximumLength)];
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };
}

internal enum ActivityLogResult
{
    Information,
    Success,
    Rejected,
    Failed,
}

internal sealed record ActivityLogEntry(
    DateTimeOffset Timestamp,
    string DeviceName,
    string Platform,
    string Action,
    ActivityLogResult Result);
