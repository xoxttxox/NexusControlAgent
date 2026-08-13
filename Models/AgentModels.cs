using System.Text.Json;

namespace NexusControl.Agent.Models;

internal sealed record PairRequest(
    string Code,
    string? DeviceName,
    string? Platform = null);

internal sealed record PairResponse(
    string DeviceId,
    string SessionToken,
    string WebSocketUrl,
    DeviceSnapshot Snapshot);

internal sealed record PairingCredentials(
    string DeviceId,
    string SessionToken);

internal sealed record PushSubscriptionRequest(string ExpoPushToken);

internal sealed record ScreenStreamSessionRequest(
    int DisplayId,
    int TargetFps,
    string? Profile);

[Flags]
internal enum DevicePermission
{
    None = 0,
    SystemControl = 1 << 0,
    Touchpad = 1 << 1,
    Processes = 1 << 2,
    Media = 1 << 3,
    Screen = 1 << 4,
    Files = 1 << 5,
    Power = 1 << 6,
    All = SystemControl
        | Touchpad
        | Processes
        | Media
        | Screen
        | Files
        | Power,
}

internal sealed record DevicePermissionsSnapshot(
    bool SystemControl,
    bool Touchpad,
    bool Processes,
    bool Media,
    bool Screen,
    bool Files,
    bool Power)
{
    public static DevicePermissionsSnapshot FromFlags(
        DevicePermission permissions) =>
        new(
            permissions.HasFlag(DevicePermission.SystemControl),
            permissions.HasFlag(DevicePermission.Touchpad),
            permissions.HasFlag(DevicePermission.Processes),
            permissions.HasFlag(DevicePermission.Media),
            permissions.HasFlag(DevicePermission.Screen),
            permissions.HasFlag(DevicePermission.Files),
            permissions.HasFlag(DevicePermission.Power));

    public DevicePermission ToFlags()
    {
        var permissions = DevicePermission.None;
        if (SystemControl)
        {
            permissions |= DevicePermission.SystemControl;
        }
        if (Touchpad)
        {
            permissions |= DevicePermission.Touchpad;
        }
        if (Processes)
        {
            permissions |= DevicePermission.Processes;
        }
        if (Media)
        {
            permissions |= DevicePermission.Media;
        }
        if (Screen)
        {
            permissions |= DevicePermission.Screen;
        }
        if (Files)
        {
            permissions |= DevicePermission.Files;
        }
        if (Power)
        {
            permissions |= DevicePermission.Power;
        }

        return permissions;
    }
}

internal sealed record TrustedDeviceInfo(
    string DeviceId,
    string DeviceName,
    string Platform,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastSeenAt,
    bool IsCurrent,
    bool IsOnline,
    bool RemoteAccessEnabled,
    DevicePermissionsSnapshot Permissions);

internal static class DevicePermissionPolicy
{
    public static DevicePermission ForCommand(string command) =>
        command switch
        {
            "system.wake" => DevicePermission.Power,
            "system.sleep" => DevicePermission.Power,
            "system.restart" => DevicePermission.Power,
            "system.shutdown" => DevicePermission.Power,
            "session.lock" => DevicePermission.SystemControl,
            _ when command.StartsWith(
                "input.",
                StringComparison.Ordinal) => DevicePermission.Touchpad,
            _ when command.StartsWith(
                "process.",
                StringComparison.Ordinal) => DevicePermission.Processes,
            _ when command.StartsWith(
                "media.",
                StringComparison.Ordinal) => DevicePermission.Media,
            _ when command.StartsWith(
                "audio.",
                StringComparison.Ordinal) => DevicePermission.Media,
            _ when command.StartsWith(
                "screen.",
                StringComparison.Ordinal) => DevicePermission.Screen,
            _ => DevicePermission.SystemControl,
        };
}

internal sealed record AuthenticationPayload(string SessionToken);

internal sealed record CommandRequest(
    string Command,
    JsonElement Parameters,
    bool? ExpectResult);

internal sealed record CommandResult(
    string RequestMessageId,
    bool Accepted,
    string Status,
    string? Message,
    string? ErrorCode);

internal sealed record ProtocolEnvelope(
    int Version,
    string Type,
    string MessageId,
    string DeviceId,
    DateTimeOffset Timestamp,
    JsonElement Payload);

internal sealed record DeviceSnapshot(
    string Name,
    string Os,
    string AgentVersion,
    string PowerState,
    long UptimeSeconds,
    string LocalIPAddress,
    WakeOnLanSnapshot WakeOnLan,
    HardwareSnapshot Hardware,
    AudioSnapshot Audio,
    ProcessorSnapshot Cpu,
    GraphicsSnapshot Gpu,
    MemorySnapshot Memory,
    NetworkSnapshot Network,
    IReadOnlyList<DriveSnapshot> Drives,
    IReadOnlyList<ProcessSnapshot> Processes,
    IReadOnlyList<MediaSessionSnapshot> MediaSessions);

internal sealed record MediaSessionsUpdate(
    DateTimeOffset CapturedAt,
    IReadOnlyList<MediaSessionSnapshot> Sessions);

internal sealed record AudioSnapshot(
    double VolumePercent,
    bool IsMuted,
    bool Available);

internal sealed record WakeOnLanSnapshot(
    bool Available,
    string? MacAddress,
    string? BroadcastAddress,
    int Port,
    string Message);

internal sealed record HardwareSnapshot(
    string Processor,
    string Graphics);

internal sealed record ProcessorSnapshot(
    double UsagePercent,
    double? TemperatureCelsius);

internal sealed record GraphicsSnapshot(
    double UsagePercent,
    double? TemperatureCelsius);

internal sealed record MemorySnapshot(
    double UsagePercent,
    ulong UsedBytes,
    ulong TotalBytes);

internal sealed record NetworkSnapshot(
    double DownloadBitsPerSecond,
    double UploadBitsPerSecond);

internal sealed record DriveSnapshot(
    string Id,
    string Name,
    long UsedBytes,
    long TotalBytes);

internal sealed record ProcessSnapshot(
    int Id,
    string Name,
    double CpuUsage,
    long MemoryBytes,
    string WindowTitle,
    int ThreadCount,
    bool IsResponding,
    bool CanTerminate);

internal sealed record MediaSessionSnapshot(
    string Id,
    string SourceAppId,
    string SourceName,
    string Title,
    string Artist,
    string AlbumTitle,
    string PlaybackStatus,
    double PositionSeconds,
    double DurationSeconds,
    double PlaybackRate,
    DateTimeOffset SampledAt,
    double VolumePercent,
    bool IsMuted,
    bool VolumeAvailable,
    bool CanPlayPause,
    bool CanNext,
    bool CanPrevious);
