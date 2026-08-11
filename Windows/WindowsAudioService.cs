using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NexusControl.Agent.Models;

namespace NexusControl.Agent.Windows;

internal sealed class WindowsAudioService
{
    private static readonly Guid AudioEndpointVolumeInterfaceId =
        new("5CDF2C82-841E-4546-9722-0CF74078229A");
    private static readonly Guid AudioSessionManagerInterfaceId =
        new("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F");

    public AudioSnapshot Capture()
    {
        try
        {
            return WithEndpoint(endpoint =>
            {
                ThrowIfFailed(endpoint.GetMasterVolumeLevelScalar(out var volume));
                ThrowIfFailed(endpoint.GetMute(out var muted));
                return new AudioSnapshot(
                    Math.Round(Math.Clamp(volume, 0f, 1f) * 100d, 1),
                    muted,
                    true);
            });
        }
        catch
        {
            return new AudioSnapshot(0, false, false);
        }
    }

    public string SetVolume(int value)
    {
        var normalized = Math.Clamp(value, 0, 100);
        WithEndpoint(endpoint =>
        {
            var eventContext = Guid.Empty;
            ThrowIfFailed(
                endpoint.SetMasterVolumeLevelScalar(
                    normalized / 100f,
                    ref eventContext));
            if (normalized > 0)
            {
                ThrowIfFailed(endpoint.SetMute(false, ref eventContext));
            }
            return true;
        });
        return $"Lautstärke wurde auf {normalized} Prozent gesetzt.";
    }

    public string ToggleMute()
    {
        var muted = WithEndpoint(endpoint =>
        {
            ThrowIfFailed(endpoint.GetMute(out var current));
            var eventContext = Guid.Empty;
            ThrowIfFailed(endpoint.SetMute(!current, ref eventContext));
            return !current;
        });
        return muted
            ? "Ton wurde stummgeschaltet."
            : "Stummschaltung wurde aufgehoben.";
    }

    public IReadOnlyDictionary<string, ApplicationAudioSnapshot>
        CaptureApplicationVolumes(IEnumerable<string> sourceAppIds)
    {
        var sources = sourceAppIds
            .Where(source => !string.IsNullOrWhiteSpace(source))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (sources.Length == 0)
        {
            return new Dictionary<string, ApplicationAudioSnapshot>(
                StringComparer.OrdinalIgnoreCase);
        }

        IReadOnlyList<ApplicationAudioSession> sessions;
        try
        {
            sessions = CaptureApplicationSessions();
        }
        catch
        {
            return new Dictionary<string, ApplicationAudioSnapshot>(
                StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, ApplicationAudioSnapshot>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var source in sources)
        {
            var matches = ResolveApplicationSessions(
                source,
                sessions,
                allowSingleProcessFallback: sources.Length == 1);
            if (matches.Length == 0)
            {
                continue;
            }

            result[source] = new ApplicationAudioSnapshot(
                Math.Round(matches.Average(item => item.VolumePercent), 1),
                matches.All(item => item.IsMuted),
                true);
        }
        return result;
    }

    public string SetApplicationVolume(string sourceAppId, int value)
    {
        var normalized = Math.Clamp(value, 0, 100);
        var matches = ResolveApplicationSessions(
            sourceAppId,
            CaptureApplicationSessions(),
            allowSingleProcessFallback: true);
        var changed = VisitApplicationSessions(
            matches.Select(session => session.ProcessName),
            volume =>
            {
                var eventContext = Guid.Empty;
                ThrowIfFailed(
                    volume.SetMasterVolume(
                        normalized / 100f,
                        ref eventContext));
                if (normalized > 0)
                {
                    ThrowIfFailed(volume.SetMute(false, ref eventContext));
                }
            });
        if (changed == 0)
        {
            throw new InvalidOperationException(
                "Für diese Medienquelle ist aktuell keine Windows-Audiositzung aktiv.");
        }
        return $"Quellenlautstärke wurde auf {normalized} Prozent gesetzt.";
    }

    public string ToggleApplicationMute(string sourceAppId)
    {
        var matching = ResolveApplicationSessions(
            sourceAppId,
            CaptureApplicationSessions(),
            allowSingleProcessFallback: true);
        if (matching.Length == 0)
        {
            throw new InvalidOperationException(
                "Für diese Medienquelle ist aktuell keine Windows-Audiositzung aktiv.");
        }

        var mute = !matching.All(session => session.IsMuted);
        var changed = VisitApplicationSessions(
            matching.Select(session => session.ProcessName),
            volume =>
            {
                var eventContext = Guid.Empty;
                ThrowIfFailed(volume.SetMute(mute, ref eventContext));
            });
        if (changed == 0)
        {
            throw new InvalidOperationException(
                "Die Windows-Audiositzung wurde inzwischen beendet.");
        }
        return mute
            ? "Medienquelle wurde stummgeschaltet."
            : "Stummschaltung der Medienquelle wurde aufgehoben.";
    }

    private static T WithEndpoint<T>(
        Func<IAudioEndpointVolume, T> operation)
        => WithAudioInterface(AudioEndpointVolumeInterfaceId, operation);

    private static T WithAudioInterface<TInterface, T>(
        Guid interfaceId,
        Func<TInterface, T> operation)
        where TInterface : class
    {
        object? enumeratorObject = null;
        IMMDevice? device = null;
        object? interfaceObject = null;
        try
        {
            enumeratorObject = new MMDeviceEnumeratorComObject();
            var enumerator = (IMMDeviceEnumerator)enumeratorObject;
            ThrowIfFailed(
                enumerator.GetDefaultAudioEndpoint(
                    AudioDataFlow.Render,
                    AudioRole.Multimedia,
                    out device));
            ThrowIfFailed(
                device.Activate(
                    ref interfaceId,
                    23,
                    IntPtr.Zero,
                    out interfaceObject));
            if (interfaceObject is not TInterface audioInterface)
            {
                throw new InvalidOperationException(
                    "Windows hat die angeforderte Audioschnittstelle nicht bereitgestellt.");
            }
            return operation(audioInterface);
        }
        catch (Exception error) when (
            error is COMException or InvalidCastException)
        {
            throw new InvalidOperationException(
                "Windows Audio ist aktuell nicht verfügbar.",
                error);
        }
        finally
        {
            ReleaseComObject(interfaceObject);
            ReleaseComObject(device);
            ReleaseComObject(enumeratorObject);
        }
    }

    private static IReadOnlyList<ApplicationAudioSession>
        CaptureApplicationSessions()
    {
        return WithSessionEnumerator(enumerator =>
        {
            ThrowIfFailed(enumerator.GetCount(out var count));
            var sessions = new List<ApplicationAudioSession>(count);
            for (var index = 0; index < count; index++)
            {
                IAudioSessionControl? control = null;
                try
                {
                    ThrowIfFailed(enumerator.GetSession(index, out control));
                    ThrowIfFailed(control.GetState(out var state));
                    if (!TryGetProcessName(control, out var processName))
                    {
                        continue;
                    }

                    var volume = (ISimpleAudioVolume)control;
                    ThrowIfFailed(volume.GetMasterVolume(out var scalar));
                    ThrowIfFailed(volume.GetMute(out var muted));
                    sessions.Add(new ApplicationAudioSession(
                        processName,
                        Math.Round(Math.Clamp(scalar, 0f, 1f) * 100d, 1),
                        muted,
                        state == AudioSessionState.Active));
                }
                catch (Exception error) when (
                    error is COMException
                    or InvalidCastException
                    or ArgumentException
                    or Win32Exception
                    or UnauthorizedAccessException)
                {
                    // An audio session may disappear while Windows enumerates it.
                }
                finally
                {
                    ReleaseComObject(control);
                }
            }
            return sessions;
        });
    }

    private static int VisitApplicationSessions(
        IEnumerable<string> processNames,
        Action<ISimpleAudioVolume> operation)
    {
        var targets = processNames
            .Select(NormalizeApplicationIdentity)
            .Where(name => name.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (targets.Count == 0)
        {
            return 0;
        }

        return WithSessionEnumerator(enumerator =>
        {
            ThrowIfFailed(enumerator.GetCount(out var count));
            var changed = 0;
            for (var index = 0; index < count; index++)
            {
                IAudioSessionControl? control = null;
                try
                {
                    ThrowIfFailed(enumerator.GetSession(index, out control));
                    if (
                        !TryGetProcessName(control, out var processName)
                        || !targets.Contains(
                            NormalizeApplicationIdentity(processName))
                    )
                    {
                        continue;
                    }

                    operation((ISimpleAudioVolume)control);
                    changed++;
                }
                catch (Exception error) when (
                    error is COMException
                    or InvalidCastException
                    or ArgumentException
                    or Win32Exception
                    or UnauthorizedAccessException)
                {
                    // Continue with the remaining sessions when one closes.
                }
                finally
                {
                    ReleaseComObject(control);
                }
            }
            return changed;
        });
    }

    private static T WithSessionEnumerator<T>(
        Func<IAudioSessionEnumerator, T> operation)
    {
        return WithAudioInterface<IAudioSessionManager2, T>(
            AudioSessionManagerInterfaceId,
            manager =>
            {
                IAudioSessionEnumerator? enumerator = null;
                try
                {
                    ThrowIfFailed(manager.GetSessionEnumerator(out enumerator));
                    return operation(enumerator);
                }
                finally
                {
                    ReleaseComObject(enumerator);
                }
            });
    }

    private static bool TryGetProcessName(
        IAudioSessionControl control,
        out string processName)
    {
        processName = "";
        try
        {
            var control2 = (IAudioSessionControl2)control;
            ThrowIfFailed(control2.GetProcessId(out var processId));
            if (processId == 0)
            {
                return false;
            }

            using var process = Process.GetProcessById((int)processId);
            processName = process.ProcessName;
            return !string.IsNullOrWhiteSpace(processName);
        }
        catch (Exception error) when (
            error is COMException
            or InvalidCastException
            or ArgumentException
            or InvalidOperationException
            or Win32Exception
            or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool MatchesApplication(
        string sourceAppId,
        string processName)
    {
        var source = NormalizeApplicationIdentity(sourceAppId);
        var process = NormalizeApplicationIdentity(processName);
        if (source.Length < 3 || process.Length < 3)
        {
            return false;
        }
        if (source.Contains(process) || process.Contains(source))
        {
            return true;
        }

        return source.Contains("spotify") && process.Contains("spotify")
            || source.Contains("chrome") && process.Contains("chrome")
            || source.Contains("firefox") && process.Contains("firefox")
            || source.Contains("brave") && process.Contains("brave")
            || source.Contains("opera") && process.Contains("opera")
            || source.Contains("vlc") && process.Contains("vlc")
            || (source.Contains("microsoftedge") || source.Contains("msedge"))
                && process == "msedge"
            || source.Contains("zunemusic") && process == "musicui"
            || source.Contains("windowsmedia") &&
                process is "wmplayer" or "mediaplayer";
    }

    private static ApplicationAudioSession[] ResolveApplicationSessions(
        string sourceAppId,
        IReadOnlyList<ApplicationAudioSession> sessions,
        bool allowSingleProcessFallback)
    {
        var direct = sessions
            .Where(session => MatchesApplication(sourceAppId, session.ProcessName))
            .ToArray();
        if (direct.Length > 0 || !allowSingleProcessFallback)
        {
            return direct;
        }

        var eligible = sessions
            .Where(session => !IsSystemAudioProcess(session.ProcessName))
            .ToArray();
        var active = eligible.Where(session => session.IsActive).ToArray();
        var candidates = active.Length > 0 ? active : eligible;
        var processNames = candidates
            .Select(session => NormalizeApplicationIdentity(session.ProcessName))
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return processNames.Length == 1
            ? candidates
            : [];
    }

    private static bool IsSystemAudioProcess(string processName) =>
        NormalizeApplicationIdentity(processName) is
            "audiodg"
            or "system"
            or "svchost"
            or "explorer"
            or "shellexperiencehost"
            or "startmenuexperiencehost"
            or "searchhost";

    private static string NormalizeApplicationIdentity(string value) =>
        new(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());

    private static void ThrowIfFailed(int result)
    {
        if (result < 0)
        {
            Marshal.ThrowExceptionForHR(result);
        }
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            _ = Marshal.FinalReleaseComObject(value);
        }
    }

    private enum AudioDataFlow
    {
        Render,
        Capture,
        All,
    }

    private enum AudioRole
    {
        Console,
        Multimedia,
        Communications,
    }

    private enum AudioSessionState
    {
        Inactive,
        Active,
        Expired,
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private sealed class MMDeviceEnumeratorComObject
    {
    }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig]
        int EnumAudioEndpoints(
            AudioDataFlow dataFlow,
            uint stateMask,
            out IntPtr devices);

        [PreserveSig]
        int GetDefaultAudioEndpoint(
            AudioDataFlow dataFlow,
            AudioRole role,
            out IMMDevice device);

        [PreserveSig]
        int GetDevice(
            [MarshalAs(UnmanagedType.LPWStr)] string id,
            out IMMDevice device);

        [PreserveSig]
        int RegisterEndpointNotificationCallback(IntPtr client);

        [PreserveSig]
        int UnregisterEndpointNotificationCallback(IntPtr client);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig]
        int Activate(
            ref Guid interfaceId,
            uint classContext,
            IntPtr activationParameters,
            [MarshalAs(UnmanagedType.IUnknown)] out object interfaceObject);

        [PreserveSig]
        int OpenPropertyStore(uint access, out IntPtr properties);

        [PreserveSig]
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);

        [PreserveSig]
        int GetState(out uint state);
    }

    [ComImport]
    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        [PreserveSig]
        int RegisterControlChangeNotify(IntPtr notify);

        [PreserveSig]
        int UnregisterControlChangeNotify(IntPtr notify);

        [PreserveSig]
        int GetChannelCount(out uint channelCount);

        [PreserveSig]
        int SetMasterVolumeLevel(float level, ref Guid eventContext);

        [PreserveSig]
        int SetMasterVolumeLevelScalar(float level, ref Guid eventContext);

        [PreserveSig]
        int GetMasterVolumeLevel(out float level);

        [PreserveSig]
        int GetMasterVolumeLevelScalar(out float level);

        [PreserveSig]
        int SetChannelVolumeLevel(
            uint channel,
            float level,
            ref Guid eventContext);

        [PreserveSig]
        int SetChannelVolumeLevelScalar(
            uint channel,
            float level,
            ref Guid eventContext);

        [PreserveSig]
        int GetChannelVolumeLevel(uint channel, out float level);

        [PreserveSig]
        int GetChannelVolumeLevelScalar(uint channel, out float level);

        [PreserveSig]
        int SetMute(
            [MarshalAs(UnmanagedType.Bool)] bool muted,
            ref Guid eventContext);

        [PreserveSig]
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool muted);

        [PreserveSig]
        int GetVolumeStepInfo(out uint step, out uint stepCount);

        [PreserveSig]
        int VolumeStepUp(ref Guid eventContext);

        [PreserveSig]
        int VolumeStepDown(ref Guid eventContext);

        [PreserveSig]
        int QueryHardwareSupport(out uint hardwareSupportMask);

        [PreserveSig]
        int GetVolumeRange(
            out float minimum,
            out float maximum,
            out float increment);
    }

    [ComImport]
    [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionManager2
    {
        [PreserveSig]
        int GetAudioSessionControl(
            ref Guid sessionGuid,
            uint streamFlags,
            out IAudioSessionControl sessionControl);

        [PreserveSig]
        int GetSimpleAudioVolume(
            ref Guid sessionGuid,
            uint streamFlags,
            out ISimpleAudioVolume audioVolume);

        [PreserveSig]
        int GetSessionEnumerator(out IAudioSessionEnumerator enumerator);

        [PreserveSig]
        int RegisterSessionNotification(IntPtr notification);

        [PreserveSig]
        int UnregisterSessionNotification(IntPtr notification);

        [PreserveSig]
        int RegisterDuckNotification(
            [MarshalAs(UnmanagedType.LPWStr)] string sessionId,
            IntPtr notification);

        [PreserveSig]
        int UnregisterDuckNotification(IntPtr notification);
    }

    [ComImport]
    [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionEnumerator
    {
        [PreserveSig]
        int GetCount(out int sessionCount);

        [PreserveSig]
        int GetSession(
            int sessionIndex,
            out IAudioSessionControl sessionControl);
    }

    [ComImport]
    [Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionControl
    {
        [PreserveSig]
        int GetState(out AudioSessionState state);

        [PreserveSig]
        int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string name);

        [PreserveSig]
        int SetDisplayName(
            [MarshalAs(UnmanagedType.LPWStr)] string name,
            ref Guid eventContext);

        [PreserveSig]
        int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string path);

        [PreserveSig]
        int SetIconPath(
            [MarshalAs(UnmanagedType.LPWStr)] string path,
            ref Guid eventContext);

        [PreserveSig]
        int GetGroupingParam(out Guid groupingId);

        [PreserveSig]
        int SetGroupingParam(ref Guid groupingId, ref Guid eventContext);

        [PreserveSig]
        int RegisterAudioSessionNotification(IntPtr events);

        [PreserveSig]
        int UnregisterAudioSessionNotification(IntPtr events);
    }

    [ComImport]
    [Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionControl2
    {
        [PreserveSig]
        int GetState(out AudioSessionState state);

        [PreserveSig]
        int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string name);

        [PreserveSig]
        int SetDisplayName(
            [MarshalAs(UnmanagedType.LPWStr)] string name,
            ref Guid eventContext);

        [PreserveSig]
        int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string path);

        [PreserveSig]
        int SetIconPath(
            [MarshalAs(UnmanagedType.LPWStr)] string path,
            ref Guid eventContext);

        [PreserveSig]
        int GetGroupingParam(out Guid groupingId);

        [PreserveSig]
        int SetGroupingParam(ref Guid groupingId, ref Guid eventContext);

        [PreserveSig]
        int RegisterAudioSessionNotification(IntPtr events);

        [PreserveSig]
        int UnregisterAudioSessionNotification(IntPtr events);

        [PreserveSig]
        int GetSessionIdentifier(
            [MarshalAs(UnmanagedType.LPWStr)] out string sessionIdentifier);

        [PreserveSig]
        int GetSessionInstanceIdentifier(
            [MarshalAs(UnmanagedType.LPWStr)] out string sessionInstanceIdentifier);

        [PreserveSig]
        int GetProcessId(out uint processId);

        [PreserveSig]
        int IsSystemSoundsSession();

        [PreserveSig]
        int SetDuckingPreference(
            [MarshalAs(UnmanagedType.Bool)] bool optOut);
    }

    [ComImport]
    [Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISimpleAudioVolume
    {
        [PreserveSig]
        int SetMasterVolume(float volume, ref Guid eventContext);

        [PreserveSig]
        int GetMasterVolume(out float volume);

        [PreserveSig]
        int SetMute(
            [MarshalAs(UnmanagedType.Bool)] bool muted,
            ref Guid eventContext);

        [PreserveSig]
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool muted);
    }

    private sealed record ApplicationAudioSession(
        string ProcessName,
        double VolumePercent,
        bool IsMuted,
        bool IsActive);
}

internal sealed record ApplicationAudioSnapshot(
    double VolumePercent,
    bool IsMuted,
    bool Available);
