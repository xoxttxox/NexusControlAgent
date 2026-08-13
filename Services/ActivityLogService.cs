using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexusControl.Agent.Localization;
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
            Sanitize(
                deviceName,
                80,
                LocalizationService.Text("ActivityLog.UnknownDevice")),
            Sanitize(
                platform,
                40,
                LocalizationService.Text("ActivityLog.UnknownPlatform")),
            Sanitize(
                action,
                100,
                "activity.unknown"),
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
        report.AppendLine(LocalizationService.Text("ActivityLog.Report.Title"));
        report.AppendLine(LocalizationService.Text(
            "ActivityLog.Report.Privacy"));
        report.AppendLine();

        foreach (var entry in entries.Reverse())
        {
            report.AppendLine(
                $"{entry.Timestamp.ToLocalTime().ToString(
                    "G",
                    LocalizationService.CurrentCulture)} | "
                + $"{entry.DeviceName} ({entry.Platform}) | "
                + $"{DisplayAction(entry.Action)} | "
                + ResultText(entry.Result));
        }

        if (entries.Count == 0)
        {
            report.AppendLine(LocalizationService.Text(
                "ActivityLog.Report.Empty"));
        }

        return report.ToString().TrimEnd();
    }

    public static string CommandAction(string command) =>
        command switch
        {
            "system.wake" => "command.system.wake",
            "system.sleep" => "command.system.sleep",
            "system.restart" => "command.system.restart",
            "system.shutdown" => "command.system.shutdown",
            "session.lock" => "command.session.lock",
            "media.playPause" => "command.media.playPause",
            "media.next" => "command.media.next",
            "media.previous" => "command.media.previous",
            "media.session.playPause" =>
                "command.media.session.playPause",
            "media.session.next" => "command.media.session.next",
            "media.session.previous" => "command.media.session.previous",
            "media.session.setVolume" => "command.media.session.setVolume",
            "media.session.toggleMute" =>
                "command.media.session.toggleMute",
            "audio.toggleMute" => "command.audio.toggleMute",
            "audio.setVolume" => "command.audio.setVolume",
            "input.pointerButton" => "command.input.pointerButton",
            "input.keyboardText" => "command.input.keyboardText",
            "process.terminate" => "command.process.terminate",
            "screen.start" => "command.screen.start",
            "screen.stop" => "command.screen.stop",
            _ => "command.unknown",
        };

    public static bool ShouldRecordCommand(string command) =>
        command is not (
            "input.pointerMove"
            or "input.pointerScroll");

    public static string ResultText(ActivityLogResult result) =>
        result switch
        {
            ActivityLogResult.Success => LocalizationService.Text(
                "ActivityLog.Result.Success"),
            ActivityLogResult.Rejected => LocalizationService.Text(
                "ActivityLog.Result.Rejected"),
            ActivityLogResult.Failed => LocalizationService.Text(
                "ActivityLog.Result.Failed"),
            _ => LocalizationService.Text(
                "ActivityLog.Result.Information"),
        };

    /// <summary>
    /// Converts stable action identifiers and entries written by older German
    /// versions into the language currently selected by the user.
    /// </summary>
    public static string DisplayAction(string action) => action switch
    {
        "connection.attempt" or "Verbindung herstellen" =>
            LocalizationService.Text("ActivityLog.Action.ConnectionAttempt"),
        "connection.established" or "Verbindung hergestellt" =>
            LocalizationService.Text(
                "ActivityLog.Action.ConnectionEstablished"),
        "connection.disconnected" or "Verbindung getrennt" =>
            LocalizationService.Text(
                "ActivityLog.Action.ConnectionDisconnected"),
        "connection.revoked" or "Verbindung widerrufen" =>
            LocalizationService.Text("ActivityLog.Action.ConnectionRevoked"),
        "pairing.verify" or "Pairing-Code prüfen" =>
            LocalizationService.Text("ActivityLog.Action.PairingVerify"),
        "device.paired" or "Gerät gekoppelt" =>
            LocalizationService.Text("ActivityLog.Action.DevicePaired"),
        "device.permissions.changed" or "Gerätefreigaben geändert" =>
            LocalizationService.Text(
                "ActivityLog.Action.PermissionsChanged"),
        "device.permission.remove" or "Gerätefreigabe entfernen" =>
            LocalizationService.Text("ActivityLog.Action.PermissionRemove"),
        "device.permission.removed" or "Gerätefreigabe entfernt" =>
            LocalizationService.Text("ActivityLog.Action.PermissionRemoved"),
        "file.upload" or "Datei hochladen" =>
            LocalizationService.Text("ActivityLog.Action.FileUpload"),
        "file.download" or "Datei herunterladen" =>
            LocalizationService.Text("ActivityLog.Action.FileDownload"),
        "command.system.wake" or "PC aufwecken" =>
            LocalizationService.Text("ActivityLog.Action.SystemWake"),
        "command.system.sleep" or "Standby" =>
            LocalizationService.Text("ActivityLog.Action.SystemSleep"),
        "command.system.restart" or "PC neu starten" =>
            LocalizationService.Text("ActivityLog.Action.SystemRestart"),
        "command.system.shutdown" or "PC herunterfahren" =>
            LocalizationService.Text("ActivityLog.Action.SystemShutdown"),
        "command.session.lock" or "PC sperren" =>
            LocalizationService.Text("ActivityLog.Action.SessionLock"),
        "command.media.playPause" or "Medien: Wiedergabe/Pause" =>
            LocalizationService.Text("ActivityLog.Action.MediaPlayPause"),
        "command.media.next" or "Medien: nächster Titel" =>
            LocalizationService.Text("ActivityLog.Action.MediaNext"),
        "command.media.previous" or "Medien: vorheriger Titel" =>
            LocalizationService.Text("ActivityLog.Action.MediaPrevious"),
        "command.media.session.playPause"
            or "Aktive Medien: Wiedergabe/Pause" =>
            LocalizationService.Text("ActivityLog.Action.SessionPlayPause"),
        "command.media.session.next"
            or "Aktive Medien: nächster Titel" =>
            LocalizationService.Text("ActivityLog.Action.SessionNext"),
        "command.media.session.previous"
            or "Aktive Medien: vorheriger Titel" =>
            LocalizationService.Text("ActivityLog.Action.SessionPrevious"),
        "command.media.session.setVolume"
            or "Aktive Medien: Lautstärke" =>
            LocalizationService.Text("ActivityLog.Action.SessionVolume"),
        "command.media.session.toggleMute"
            or "Aktive Medien: Stummschaltung" =>
            LocalizationService.Text("ActivityLog.Action.SessionMute"),
        "command.audio.toggleMute"
            or "Windows-Audio: Stummschaltung" =>
            LocalizationService.Text("ActivityLog.Action.AudioMute"),
        "command.audio.setVolume" or "Windows-Audio: Lautstärke" =>
            LocalizationService.Text("ActivityLog.Action.AudioVolume"),
        "command.input.pointerButton"
            or "Remote-Touchpad: Mausklick" =>
            LocalizationService.Text("ActivityLog.Action.PointerButton"),
        "command.input.keyboardText"
            or "Remote-Tastatur: Texteingabe" =>
            LocalizationService.Text("ActivityLog.Action.KeyboardText"),
        "command.process.terminate" or "Prozess beenden" =>
            LocalizationService.Text("ActivityLog.Action.ProcessTerminate"),
        "command.screen.start" or "Bildschirmübertragung starten" =>
            LocalizationService.Text("ActivityLog.Action.ScreenStart"),
        "command.screen.stop" or "Bildschirmübertragung beenden" =>
            LocalizationService.Text("ActivityLog.Action.ScreenStop"),
        "command.unknown" or "Unbekannter Befehl" =>
            LocalizationService.Text("ActivityLog.Action.UnknownCommand"),
        "activity.unknown" or "Unbekannte Aktion" =>
            LocalizationService.Text("ActivityLog.UnknownAction"),
        _ => action,
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
