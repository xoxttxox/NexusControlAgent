using System.Text.Json;

namespace NexusControl.Agent.Models;

internal sealed record PairRequest(string Code, string? DeviceName);

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

internal sealed record TrustedDeviceInfo(
    string DeviceId,
    string DeviceName,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastSeenAt,
    bool IsCurrent);

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
