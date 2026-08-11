using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Hosting;
using NexusControl.Agent.Models;
using NexusControl.Agent.Windows;
using Windows.Media.Control;

namespace NexusControl.Agent.Services;

internal sealed class WindowsMediaSessionService : BackgroundService
{
    private readonly WindowsAudioService _audio;
    private readonly object _gate = new();
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private IReadOnlyList<MediaSessionSnapshot> _snapshot = [];
    private Dictionary<string, GlobalSystemMediaTransportControlsSession>
        _sessions = new(StringComparer.Ordinal);
    private GlobalSystemMediaTransportControlsSessionManager? _manager;

    public WindowsMediaSessionService(WindowsAudioService audio)
    {
        _audio = audio;
    }

    public IReadOnlyList<MediaSessionSnapshot> GetSnapshot()
    {
        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;
            return _snapshot
                .Select(item => AdvancePosition(item, now))
                .ToArray();
        }
    }

    public async Task ControlAsync(string sessionId, string action)
    {
        GlobalSystemMediaTransportControlsSession? session;
        lock (_gate)
        {
            _sessions.TryGetValue(sessionId, out session);
        }

        if (session is null)
        {
            await RefreshAsync();
            lock (_gate)
            {
                _sessions.TryGetValue(sessionId, out session);
            }
        }
        if (session is null)
        {
            throw new InvalidOperationException(
                "Die ausgewählte Mediensitzung ist nicht mehr aktiv.");
        }

        var wasPlaying = session.GetPlaybackInfo().PlaybackStatus ==
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
        var accepted = action switch
        {
            "playPause" when wasPlaying => await session.TryPauseAsync(),
            "playPause" => await session.TryPlayAsync(),
            "next" => await session.TrySkipNextAsync(),
            "previous" => await session.TrySkipPreviousAsync(),
            _ => throw new InvalidOperationException(
                "Diese Medienaktion ist nicht freigegeben."),
        };
        if (!accepted)
        {
            throw new InvalidOperationException(
                "Die Medien-App hat diesen Befehl nicht angenommen.");
        }

        ApplyOptimisticControl(sessionId, action, wasPlaying);

        try
        {
            await Task.Delay(action == "playPause" ? 100 : 180);
            await RefreshAsync();
            if (action is "next" or "previous")
            {
                await Task.Delay(160);
                await RefreshAsync();
            }
        }
        catch
        {
            // The command was already accepted. The background refresh retries.
        }
    }

    public string SetVolume(string sessionId, int value)
    {
        var sourceAppId = GetSourceAppId(sessionId);
        var normalized = Math.Clamp(value, 0, 100);
        var message = _audio.SetApplicationVolume(sourceAppId, normalized);
        lock (_gate)
        {
            _snapshot = _snapshot
                .Select(item => item.Id == sessionId
                    ? item with
                    {
                        VolumePercent = normalized,
                        IsMuted = normalized > 0 ? false : item.IsMuted,
                        VolumeAvailable = true,
                    }
                    : item)
                .ToArray();
        }
        return message;
    }

    public string ToggleMute(string sessionId)
    {
        var sourceAppId = GetSourceAppId(sessionId);
        var message = _audio.ToggleApplicationMute(sourceAppId);
        lock (_gate)
        {
            _snapshot = _snapshot
                .Select(item => item.Id == sessionId
                    ? item with
                    {
                        IsMuted = !item.IsMuted,
                        VolumeAvailable = true,
                    }
                    : item)
                .ToArray();
        }
        return message;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshAsync();
            }
            catch
            {
                _manager = null;
                lock (_gate)
                {
                    _snapshot = [];
                    _sessions = new(StringComparer.Ordinal);
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(400), stoppingToken);
        }
    }

    private async Task RefreshAsync()
    {
        await _refreshLock.WaitAsync();
        try
        {
            _manager ??=
                await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            var sessions = _manager.GetSessions();
            var applicationVolumes = _audio.CaptureApplicationVolumes(
                sessions.Select(session =>
                    session.SourceAppUserModelId ?? "Windows Media"));
            var sourceOccurrences = new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);
            var nextSessions = new Dictionary<
                string,
                GlobalSystemMediaTransportControlsSession>(StringComparer.Ordinal);
            var nextSnapshot = new List<MediaSessionSnapshot>();

            foreach (var session in sessions)
            {
                try
                {
                    var sourceAppId = session.SourceAppUserModelId ?? "Windows Media";
                    sourceOccurrences.TryGetValue(sourceAppId, out var occurrence);
                    sourceOccurrences[sourceAppId] = occurrence + 1;
                    var id = CreateSessionId(sourceAppId, occurrence);
                    var properties = await session.TryGetMediaPropertiesAsync();
                    var playback = session.GetPlaybackInfo();
                    var timeline = session.GetTimelineProperties();
                    var title = properties?.Title?.Trim() ?? "";
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        continue;
                    }

                    var controls = playback.Controls;
                    applicationVolumes.TryGetValue(
                        sourceAppId,
                        out var applicationVolume);
                    var playbackStatus = NormalizePlaybackStatus(
                        playback.PlaybackStatus.ToString());
                    var playbackRate = playback.PlaybackRate.GetValueOrDefault(1d);
                    if (!double.IsFinite(playbackRate) || playbackRate <= 0)
                    {
                        playbackRate = 1d;
                    }
                    var sampledAt = DateTimeOffset.UtcNow;
                    var currentPosition = timeline.Position;
                    if (playbackStatus == "playing")
                    {
                        var elapsed = sampledAt - timeline.LastUpdatedTime;
                        if (
                            elapsed > TimeSpan.Zero
                            && elapsed < TimeSpan.FromHours(12)
                        )
                        {
                            currentPosition += TimeSpan.FromTicks(
                                (long)(elapsed.Ticks * playbackRate));
                        }
                    }
                    var effectiveEnd = timeline.EndTime > timeline.MaxSeekTime
                        ? timeline.EndTime
                        : timeline.MaxSeekTime;
                    var duration = Math.Max(
                        0,
                        (effectiveEnd - timeline.StartTime).TotalSeconds);
                    var rawPosition = Math.Max(
                        0,
                        (currentPosition - timeline.StartTime).TotalSeconds);
                    var position = Math.Clamp(
                        rawPosition,
                        0,
                        duration > 0 ? duration : rawPosition);
                    nextSessions[id] = session;
                    nextSnapshot.Add(new MediaSessionSnapshot(
                        id,
                        sourceAppId,
                        GetFriendlySourceName(sourceAppId),
                        title,
                        properties?.Artist?.Trim() ?? "",
                        properties?.AlbumTitle?.Trim() ?? "",
                        playbackStatus,
                        Math.Round(position, 2),
                        Math.Round(duration, 2),
                        playbackRate,
                        sampledAt,
                        applicationVolume?.VolumePercent ?? 0,
                        applicationVolume?.IsMuted ?? false,
                        applicationVolume?.Available ?? false,
                        controls.IsPlayPauseToggleEnabled,
                        controls.IsNextEnabled,
                        controls.IsPreviousEnabled));
                }
                catch
                {
                    // A media source may disappear while its properties are read.
                }
            }

            var ordered = nextSnapshot
                .OrderByDescending(item => item.PlaybackStatus == "playing")
                .ThenBy(item => item.SourceName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            lock (_gate)
            {
                _snapshot = ordered;
                _sessions = nextSessions;
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private void ApplyOptimisticControl(
        string sessionId,
        string action,
        bool wasPlaying)
    {
        var now = DateTimeOffset.UtcNow;
        lock (_gate)
        {
            _snapshot = _snapshot
                .Select(item =>
                {
                    if (item.Id != sessionId)
                    {
                        return item;
                    }

                    var advanced = AdvancePosition(item, now);
                    return action switch
                    {
                        "playPause" => advanced with
                        {
                            PlaybackStatus = wasPlaying ? "paused" : "playing",
                            SampledAt = now,
                        },
                        "next" or "previous" => advanced with
                        {
                            PositionSeconds = 0,
                            SampledAt = now,
                        },
                        _ => advanced,
                    };
                })
                .ToArray();
        }
    }

    private string GetSourceAppId(string sessionId)
    {
        lock (_gate)
        {
            var snapshot = _snapshot.FirstOrDefault(item => item.Id == sessionId);
            if (snapshot is not null)
            {
                return snapshot.SourceAppId;
            }
        }

        throw new InvalidOperationException(
            "Die ausgewählte Mediensitzung ist nicht mehr aktiv.");
    }

    private static MediaSessionSnapshot AdvancePosition(
        MediaSessionSnapshot item,
        DateTimeOffset now)
    {
        if (
            item.PlaybackStatus != "playing"
            || item.PlaybackRate <= 0
            || now <= item.SampledAt
        )
        {
            return item with { SampledAt = now };
        }

        var elapsed = (now - item.SampledAt).TotalSeconds;
        var position = item.PositionSeconds + elapsed * item.PlaybackRate;
        position = item.DurationSeconds > 0
            ? Math.Clamp(position, 0, item.DurationSeconds)
            : Math.Max(0, position);
        return item with
        {
            PositionSeconds = Math.Round(position, 2),
            SampledAt = now,
        };
    }

    private static string CreateSessionId(string sourceAppId, int occurrence)
    {
        var value = Encoding.UTF8.GetBytes($"{sourceAppId}|{occurrence}");
        return Convert.ToHexString(SHA256.HashData(value))[..20].ToLowerInvariant();
    }

    private static string NormalizePlaybackStatus(string status) =>
        status.ToLowerInvariant() switch
        {
            "playing" => "playing",
            "paused" => "paused",
            "stopped" => "stopped",
            _ => "unknown",
        };

    private static string GetFriendlySourceName(string sourceAppId)
    {
        var source = sourceAppId.ToLowerInvariant();
        if (source.Contains("spotify")) return "Spotify";
        if (source.Contains("youtube")) return "YouTube";
        if (source.Contains("chrome")) return "Google Chrome";
        if (source.Contains("msedge") || source.Contains("microsoftedge"))
        {
            return "Microsoft Edge";
        }
        if (source.Contains("firefox")) return "Mozilla Firefox";
        if (source.Contains("vlc")) return "VLC Media Player";

        var fileName = Path.GetFileNameWithoutExtension(sourceAppId);
        return string.IsNullOrWhiteSpace(fileName) ? "Windows Media" : fileName;
    }
}
