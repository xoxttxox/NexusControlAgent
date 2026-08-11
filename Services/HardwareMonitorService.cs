using System.Management;
using LibreHardwareMonitor.Hardware;

namespace NexusControl.Agent.Services;

internal sealed class HardwareMonitorService : IDisposable
{
    private readonly object _gate = new();
    private readonly Computer _computer = new()
    {
        IsCpuEnabled = true,
        IsMotherboardEnabled = true,
        IsControllerEnabled = true,
        IsGpuEnabled = true,
    };
    private bool _available;
    private DateTimeOffset _lastAcpiRead = DateTimeOffset.MinValue;
    private float? _lastAcpiTemperature;

    public HardwareMonitorService()
    {
        try
        {
            _computer.Open();
            _available = true;
        }
        catch (Exception error)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Hardware-Sensoren konnten nicht geöffnet werden: {error.Message}");
        }
    }

    public HardwareSensorSnapshot Capture()
    {
        lock (_gate)
        {
            if (!_available)
            {
                return HardwareSensorSnapshot.WithCpuTemperature(
                    ReadAcpiTemperature());
            }

            try
            {
                var allHardware = _computer.Hardware
                    .SelectMany(FlattenAndUpdate)
                    .ToArray();
                var cpu = allHardware.FirstOrDefault(
                    item => item.HardwareType == HardwareType.Cpu);
                var cpuTemperature = cpu is null
                    ? null
                    : ReadPreferredSensor(
                        cpu,
                        SensorType.Temperature,
                        "CPU Package",
                        "Core (Tctl/Tdie)",
                        "CPU Core",
                        "Core Average");
                cpuTemperature ??= ReadCpuTemperatureFromOtherHardware(
                    allHardware);
                cpuTemperature ??= ReadAcpiTemperature();
                var graphics = allHardware
                    .Where(item => item.HardwareType is
                        HardwareType.GpuAmd or
                        HardwareType.GpuIntel or
                        HardwareType.GpuNvidia)
                    .Select(item => new
                    {
                        Hardware = item,
                        Usage = ReadPreferredSensor(
                            item,
                            SensorType.Load,
                            "GPU Core",
                            "D3D 3D"),
                        Temperature = ReadPreferredSensor(
                            item,
                            SensorType.Temperature,
                            "GPU Core",
                            "GPU Hot Spot",
                            "GPU Memory Junction"),
                    })
                    .OrderByDescending(item => item.Usage ?? -1)
                    .FirstOrDefault();

                return new HardwareSensorSnapshot(
                    CpuUsagePercent: cpu is null
                        ? null
                        : ReadPreferredSensor(
                            cpu,
                            SensorType.Load,
                            "CPU Total",
                            "Total"),
                    CpuTemperatureCelsius: cpuTemperature,
                    GpuUsagePercent: graphics?.Usage,
                    GpuTemperatureCelsius: graphics?.Temperature,
                    ProcessorName: cpu?.Name,
                    GraphicsName: graphics?.Hardware.Name);
            }
            catch (Exception error)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Hardware-Sensoren konnten nicht gelesen werden: {error.Message}");
                return HardwareSensorSnapshot.WithCpuTemperature(
                    ReadAcpiTemperature());
            }
        }
    }

    public HardwareMonitorDiagnostic Diagnose()
    {
        var snapshot = Capture();
        var monitorAvailable = _available;
        var message = snapshot.CpuTemperatureCelsius.HasValue
            ? "CPU-Temperatursensor wurde erkannt."
            : monitorAvailable
                ? "Windows und die Hardware stellen keinen CPU-Temperatursensor bereit. Starte den Agent testweise als Administrator und prüfe BIOS- sowie Chipsatztreiber."
                : "LibreHardwareMonitor konnte nicht geöffnet werden. Starte den Agent als Administrator.";

        return new HardwareMonitorDiagnostic(
            monitorAvailable,
            snapshot.CpuTemperatureCelsius.HasValue,
            snapshot.GpuTemperatureCelsius.HasValue,
            message);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (!_available)
            {
                return;
            }

            _computer.Close();
            _available = false;
        }
    }

    private static IEnumerable<IHardware> FlattenAndUpdate(IHardware hardware)
    {
        hardware.Update();
        yield return hardware;

        foreach (var child in hardware.SubHardware)
        {
            foreach (var item in FlattenAndUpdate(child))
            {
                yield return item;
            }
        }
    }

    private static float? ReadPreferredSensor(
        IHardware hardware,
        SensorType type,
        params string[] preferredNames)
    {
        var sensors = hardware.Sensors
            .Where(sensor => sensor.SensorType == type && sensor.Value.HasValue)
            .ToArray();
        if (sensors.Length == 0)
        {
            return null;
        }

        foreach (var preferredName in preferredNames)
        {
            var preferred = sensors.FirstOrDefault(
                sensor => sensor.Name.Equals(
                    preferredName,
                    StringComparison.OrdinalIgnoreCase));
            if (preferred?.Value is float value)
            {
                return value;
            }
        }

        return sensors
            .Select(sensor => sensor.Value)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .Max();
    }

    private static float? ReadCpuTemperatureFromOtherHardware(
        IEnumerable<IHardware> hardware)
    {
        var candidates = hardware
            .Where(item => item.HardwareType is not (
                HardwareType.GpuAmd or
                HardwareType.GpuIntel or
                HardwareType.GpuNvidia))
            .SelectMany(item => item.Sensors)
            .Where(sensor =>
                sensor.SensorType == SensorType.Temperature
                && sensor.Value.HasValue
                && SensorLooksLikeCpu(sensor.Name)
                && sensor.Value.Value is >= 10 and <= 125)
            .Select(sensor => sensor.Value!.Value)
            .ToArray();

        return candidates.Length > 0 ? candidates.Max() : null;
    }

    private float? ReadAcpiTemperature()
    {
        if (DateTimeOffset.UtcNow - _lastAcpiRead < TimeSpan.FromSeconds(10))
        {
            return _lastAcpiTemperature;
        }

        _lastAcpiRead = DateTimeOffset.UtcNow;
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\WMI",
                "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
            var temperatures = searcher
                .Get()
                .Cast<ManagementBaseObject>()
                .Select(item => item["CurrentTemperature"])
                .Where(value => value is not null)
                .Select(value => Convert.ToSingle(value) / 10f - 273.15f)
                .Where(value => value is >= 10 and <= 125)
                .ToArray();

            _lastAcpiTemperature =
                temperatures.Length > 0 ? temperatures.Max() : null;
        }
        catch
        {
            _lastAcpiTemperature = null;
        }

        return _lastAcpiTemperature;
    }

    private static bool SensorLooksLikeCpu(string name)
    {
        var normalized = name.ToLowerInvariant();
        return normalized.Contains("cpu")
            || normalized.Contains("package")
            || normalized.Contains("tctl")
            || normalized.Contains("tdie")
            || normalized.Contains("processor");
    }
}

internal sealed record HardwareSensorSnapshot(
    float? CpuUsagePercent,
    float? CpuTemperatureCelsius,
    float? GpuUsagePercent,
    float? GpuTemperatureCelsius,
    string? ProcessorName,
    string? GraphicsName)
{
    public static HardwareSensorSnapshot WithCpuTemperature(
        float? temperature) =>
        new(null, temperature, null, null, null, null);
}

internal sealed record HardwareMonitorDiagnostic(
    bool MonitorAvailable,
    bool CpuTemperatureAvailable,
    bool GpuTemperatureAvailable,
    string Message);
