using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using NexusControl.Agent.Models;
using NexusControl.Agent.Networking;
using NexusControl.Agent.Windows;

namespace NexusControl.Agent.Services;

internal sealed class TelemetryService
{
    public const string AgentVersion = "0.11.1";

    private readonly object _gate = new();
    private readonly HardwareMonitorService _hardwareMonitor;
    private readonly WindowsAudioService _audio;
    private readonly SessionUptimeService _uptime;
    private readonly WindowsMediaSessionService _mediaSessions;
    private readonly CpuUsageSampler _cpuSampler = new();
    private readonly Dictionary<int, ProcessCpuSample> _processSamples = [];
    private long _previousReceivedBytes;
    private long _previousSentBytes;
    private DateTimeOffset _previousNetworkSample = DateTimeOffset.UtcNow;

    public TelemetryService(
        HardwareMonitorService hardwareMonitor,
        WindowsAudioService audio,
        SessionUptimeService uptime,
        WindowsMediaSessionService mediaSessions)
    {
        _hardwareMonitor = hardwareMonitor;
        _audio = audio;
        _uptime = uptime;
        _mediaSessions = mediaSessions;
        (_previousReceivedBytes, _previousSentBytes) = ReadNetworkTotals();
    }

    public DeviceSnapshot Capture()
    {
        lock (_gate)
        {
            var memory = ReadMemory();
            var network = ReadNetwork();
            var sensors = _hardwareMonitor.Capture();
            var wakeOnLan = NetworkUtilities.GetWakeOnLanInfo();
            var processor = sensors.ProcessorName ?? ReadProcessorName();
            var graphics = sensors.GraphicsName ?? ReadGraphicsName();
            var cpuUsage = sensors.CpuUsagePercent ?? _cpuSampler.Sample();

            return new DeviceSnapshot(
                Environment.MachineName,
                ReadOperatingSystemName(),
                AgentVersion,
                "running",
                _uptime.GetUptimeSeconds(),
                NetworkUtilities.GetPreferredIPv4Address(),
                new WakeOnLanSnapshot(
                    wakeOnLan.Available,
                    wakeOnLan.MacAddress,
                    wakeOnLan.BroadcastAddress,
                    wakeOnLan.Port,
                    wakeOnLan.Message),
                new HardwareSnapshot(processor, graphics),
                _audio.Capture(),
                new ProcessorSnapshot(
                    Math.Round(Math.Clamp(cpuUsage, 0, 100), 1),
                    RoundTemperature(sensors.CpuTemperatureCelsius)),
                new GraphicsSnapshot(
                    Math.Round(
                        Math.Clamp(sensors.GpuUsagePercent ?? 0, 0, 100),
                        1),
                    RoundTemperature(sensors.GpuTemperatureCelsius)),
                memory,
                network,
                ReadDrives(),
                ReadProcesses(),
                _mediaSessions.GetSnapshot());
        }
    }

    private static double? RoundTemperature(float? temperature) =>
        temperature.HasValue
            ? Math.Round(temperature.Value, 1)
            : null;

    private static MemorySnapshot ReadMemory()
    {
        var status = new MemoryStatusEx();
        if (!GlobalMemoryStatusEx(status))
        {
            return new MemorySnapshot(0, 0, 0);
        }

        var used = status.TotalPhysical - status.AvailablePhysical;
        var usage = status.TotalPhysical == 0
            ? 0
            : used * 100d / status.TotalPhysical;
        return new MemorySnapshot(Math.Round(usage, 1), used, status.TotalPhysical);
    }

    private NetworkSnapshot ReadNetwork()
    {
        var now = DateTimeOffset.UtcNow;
        var (received, sent) = ReadNetworkTotals();
        var seconds = Math.Max(0.1, (now - _previousNetworkSample).TotalSeconds);
        var download = Math.Max(0, received - _previousReceivedBytes) * 8d / seconds;
        var upload = Math.Max(0, sent - _previousSentBytes) * 8d / seconds;

        _previousReceivedBytes = received;
        _previousSentBytes = sent;
        _previousNetworkSample = now;
        return new NetworkSnapshot(
            Math.Round(download, 0),
            Math.Round(upload, 0));
    }

    private static (long Received, long Sent) ReadNetworkTotals()
    {
        long received = 0;
        long sent = 0;

        foreach (var network in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (network.OperationalStatus != OperationalStatus.Up
                || network.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }

            try
            {
                var statistics = network.GetIPStatistics();
                received += statistics.BytesReceived;
                sent += statistics.BytesSent;
            }
            catch
            {
                // A disappearing virtual adapter is skipped.
            }
        }

        return (received, sent);
    }

    private static IReadOnlyList<DriveSnapshot> ReadDrives()
    {
        var drives = new List<DriveSnapshot>();
        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (!drive.IsReady || drive.DriveType != DriveType.Fixed)
                {
                    continue;
                }

                var label = string.IsNullOrWhiteSpace(drive.VolumeLabel)
                    ? $"Laufwerk {drive.Name.TrimEnd('\\')}"
                    : $"{drive.VolumeLabel} ({drive.Name.TrimEnd('\\')})";
                drives.Add(new DriveSnapshot(
                    drive.Name.TrimEnd('\\').ToLowerInvariant(),
                    label,
                    drive.TotalSize - drive.AvailableFreeSpace,
                    drive.TotalSize));
            }
            catch
            {
                // Inaccessible drives are not exposed.
            }
        }
        return drives;
    }

    private IReadOnlyList<ProcessSnapshot> ReadProcesses()
    {
        var now = DateTimeOffset.UtcNow;
        var activeIds = new HashSet<int>();
        var snapshots = new List<ProcessSnapshot>();

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.Id == Environment.ProcessId || process.HasExited)
                {
                    continue;
                }

                activeIds.Add(process.Id);
                var totalCpu = process.TotalProcessorTime;
                var cpuUsage = 0d;

                if (_processSamples.TryGetValue(process.Id, out var previous))
                {
                    var elapsed = Math.Max(0.1, (now - previous.Timestamp).TotalSeconds);
                    var cpuSeconds = Math.Max(
                        0,
                        (totalCpu - previous.TotalProcessorTime).TotalSeconds);
                    cpuUsage = cpuSeconds
                        / elapsed
                        / Math.Max(1, Environment.ProcessorCount)
                        * 100d;
                }

                _processSamples[process.Id] = new ProcessCpuSample(totalCpu, now);
                var processName = process.ProcessName;
                snapshots.Add(new ProcessSnapshot(
                    process.Id,
                    processName,
                    Math.Round(Math.Clamp(cpuUsage, 0, 100), 1),
                    Math.Max(0, process.WorkingSet64),
                    ReadWindowTitle(process),
                    ReadThreadCount(process),
                    ReadRespondingState(process),
                    !WindowsController.IsProtectedProcessName(processName)));
            }
            catch
            {
                // Protected and short-lived processes are skipped.
            }
            finally
            {
                process.Dispose();
            }
        }

        foreach (var staleId in _processSamples.Keys.Where(id => !activeIds.Contains(id)).ToArray())
        {
            _processSamples.Remove(staleId);
        }

        return snapshots
            .OrderByDescending(process => process.CpuUsage)
            .ThenByDescending(process => process.MemoryBytes)
            .Take(40)
            .ToArray();
    }

    private static string ReadWindowTitle(Process process)
    {
        try
        {
            return process.MainWindowTitle?.Trim() ?? "";
        }
        catch
        {
            return "";
        }
    }

    private static int ReadThreadCount(Process process)
    {
        try
        {
            return Math.Max(0, process.Threads.Count);
        }
        catch
        {
            return 0;
        }
    }

    private static bool ReadRespondingState(Process process)
    {
        try
        {
            return string.IsNullOrWhiteSpace(process.MainWindowTitle)
                || process.Responding;
        }
        catch
        {
            return true;
        }
    }

    private static string ReadOperatingSystemName()
    {
        var fallback = RuntimeInformation.OSDescription.Trim();
        if (!OperatingSystem.IsWindows())
        {
            return fallback;
        }

        try
        {
            using var currentVersion = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");

            var productName = currentVersion?.GetValue("ProductName") as string;
            var editionId = currentVersion?.GetValue("EditionID") as string;
            var buildValue = currentVersion?.GetValue("CurrentBuildNumber")
                ?? currentVersion?.GetValue("CurrentBuild");
            var buildText = Convert.ToString(buildValue);
            var build = int.TryParse(buildText, out var parsedBuild)
                ? parsedBuild
                : Environment.OSVersion.Version.Build;

            if (!string.IsNullOrWhiteSpace(productName))
            {
                var normalized = productName.Trim();
                if (normalized.StartsWith(
                        "Microsoft ",
                        StringComparison.OrdinalIgnoreCase))
                {
                    normalized = normalized["Microsoft ".Length..];
                }

                if (build >= 22000
                    && normalized.StartsWith(
                        "Windows 10",
                        StringComparison.OrdinalIgnoreCase))
                {
                    normalized = $"Windows 11{normalized["Windows 10".Length..]}";
                }

                return normalized;
            }

            var family = build >= 22000 ? "Windows 11" : "Windows 10";
            var edition = GetWindowsEditionName(editionId);
            return string.IsNullOrWhiteSpace(edition)
                ? family
                : $"{family} {edition}";
        }
        catch
        {
            return NormalizeWindowsDescription(fallback);
        }
    }

    private static string NormalizeWindowsDescription(string description)
    {
        var build = Environment.OSVersion.Version.Build;
        if (build >= 22000)
        {
            return "Windows 11";
        }

        if (description.StartsWith(
                "Microsoft ",
                StringComparison.OrdinalIgnoreCase))
        {
            return description["Microsoft ".Length..];
        }

        return description;
    }

    private static string GetWindowsEditionName(string? editionId) =>
        editionId?.Trim().ToLowerInvariant() switch
        {
            "professional" => "Pro",
            "professionaln" => "Pro N",
            "professionalworkstation" => "Pro for Workstations",
            "professionalworkstationn" => "Pro N for Workstations",
            "core" => "Home",
            "coren" => "Home N",
            "coresinglelanguage" => "Home Single Language",
            "education" => "Education",
            "educationn" => "Education N",
            "enterprise" => "Enterprise",
            "enterprisen" => "Enterprise N",
            _ => string.Empty,
        };

    private static string ReadProcessorName()
    {
        try
        {
            return (Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0",
                    "ProcessorNameString",
                    null) as string)?.Trim()
                ?? "Windows-Prozessor";
        }
        catch
        {
            return "Windows-Prozessor";
        }
    }

    private static string ReadGraphicsName()
    {
        try
        {
            using var videoRoot = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Video");
            if (videoRoot is null)
            {
                return "Nicht erkannt";
            }

            foreach (var adapterId in videoRoot.GetSubKeyNames())
            {
                using var adapter = videoRoot.OpenSubKey($@"{adapterId}\0000");
                var name = adapter?.GetValue("DriverDesc") as string;
                if (!string.IsNullOrWhiteSpace(name)
                    && !name.Contains("Basic Display", StringComparison.OrdinalIgnoreCase))
                {
                    return name.Trim();
                }
            }
        }
        catch
        {
            // GPU details are optional telemetry.
        }

        return "Nicht erkannt";
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(
        [In, Out] MemoryStatusEx buffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private sealed class MemoryStatusEx
    {
        public uint Length = (uint)Marshal.SizeOf(typeof(MemoryStatusEx));
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    private sealed class CpuUsageSampler
    {
        private ulong _idle;
        private ulong _kernel;
        private ulong _user;
        private bool _initialized;

        public double Sample()
        {
            if (!GetSystemTimes(out var idle, out var kernel, out var user))
            {
                return 0;
            }

            var currentIdle = ToUInt64(idle);
            var currentKernel = ToUInt64(kernel);
            var currentUser = ToUInt64(user);
            if (!_initialized)
            {
                (_idle, _kernel, _user) = (currentIdle, currentKernel, currentUser);
                _initialized = true;
                return 0;
            }

            var idleDelta = currentIdle - _idle;
            var kernelDelta = currentKernel - _kernel;
            var userDelta = currentUser - _user;
            var total = kernelDelta + userDelta;
            (_idle, _kernel, _user) = (currentIdle, currentKernel, currentUser);

            return total == 0
                ? 0
                : Math.Round(Math.Clamp((total - idleDelta) * 100d / total, 0, 100), 1);
        }

        private static ulong ToUInt64(FileTime value) =>
            ((ulong)value.HighDateTime << 32) | value.LowDateTime;

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetSystemTimes(
            out FileTime idleTime,
            out FileTime kernelTime,
            out FileTime userTime);

        [StructLayout(LayoutKind.Sequential)]
        private struct FileTime
        {
            public uint LowDateTime;
            public uint HighDateTime;
        }
    }

    private sealed record ProcessCpuSample(
        TimeSpan TotalProcessorTime,
        DateTimeOffset Timestamp);
}
