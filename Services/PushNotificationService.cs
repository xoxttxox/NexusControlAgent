using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexusControl.Agent.Configuration;
using NexusControl.Agent.Pairing;

namespace NexusControl.Agent.Services;

internal sealed class PushNotificationService : BackgroundService
{
    private const string ExpoPushEndpoint =
        "https://exp.host/--/api/v2/push/send";

    private readonly DeviceStore _devices;
    private readonly HardwareMonitorService _hardware;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AgentOptions _options;
    private readonly ILogger<PushNotificationService> _logger;
    private bool _cpuAlertActive;
    private bool _gpuAlertActive;

    public PushNotificationService(
        DeviceStore devices,
        HardwareMonitorService hardware,
        IHttpClientFactory httpClientFactory,
        IOptions<AgentOptions> options,
        ILogger<PushNotificationService> logger)
    {
        _devices = devices;
        _hardware = hardware;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(
                _options.PushMonitoringIntervalSeconds));

        while (
            !stoppingToken.IsCancellationRequested
            && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CheckTemperaturesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception error)
            {
                _logger.LogWarning(
                    error,
                    "Push-Überwachung konnte nicht ausgeführt werden.");
            }
        }
    }

    public async Task SendRegistrationTestAsync(
        string expoPushToken,
        CancellationToken cancellationToken)
    {
        var message = new[]
        {
            new
            {
                to = expoPushToken,
                sound = "default",
                title = "Nexus Push verbunden",
                body =
                    $"{Environment.MachineName} kann jetzt Temperaturwarnungen senden, auch wenn die App geschlossen ist.",
                data = new Dictionary<string, string>
                {
                    ["kind"] = "settings",
                    ["source"] = "agent",
                },
                priority = "high",
                channelId = "nexus-alerts",
            },
        };
        var client = _httpClientFactory.CreateClient("ExpoPush");
        using var response = await client.PostAsJsonAsync(
            ExpoPushEndpoint,
            message,
            cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Expo Push antwortete mit {(int)response.StatusCode}.");
        }

        using var document = JsonDocument.Parse(responseBody);
        var data = document.RootElement.TryGetProperty(
            "data",
            out var dataElement)
            ? dataElement
            : default;
        if (
            data.ValueKind == JsonValueKind.Array
            && data.GetArrayLength() > 0
            && data[0].TryGetProperty("status", out var status)
            && string.Equals(
                status.GetString(),
                "error",
                StringComparison.OrdinalIgnoreCase))
        {
            var messageText = data[0].TryGetProperty(
                "message",
                out var messageElement)
                ? messageElement.GetString()
                : "Expo Push hat das Token abgelehnt.";
            throw new InvalidOperationException(messageText);
        }
    }

    private async Task CheckTemperaturesAsync(
        CancellationToken cancellationToken)
    {
        if (_devices.ListPushTargets().Count == 0)
        {
            return;
        }

        var snapshot = _hardware.Capture();
        await CheckSensorAsync(
            "CPU",
            "cpu",
            snapshot.CpuTemperatureCelsius,
            _cpuAlertActive,
            value => _cpuAlertActive = value,
            cancellationToken);
        await CheckSensorAsync(
            "GPU",
            "gpu",
            snapshot.GpuTemperatureCelsius,
            _gpuAlertActive,
            value => _gpuAlertActive = value,
            cancellationToken);
    }

    private async Task CheckSensorAsync(
        string label,
        string sensor,
        float? temperature,
        bool alertActive,
        Action<bool> setAlertActive,
        CancellationToken cancellationToken)
    {
        if (
            temperature.HasValue
            && temperature.Value
                >= _options.PushTemperatureThresholdCelsius
            && !alertActive)
        {
            setAlertActive(true);
            await SendToAllAsync(
                $"{label}-Temperatur zu hoch",
                $"{Environment.MachineName}: {Math.Round(temperature.Value)} °C. Prüfe Kühlung und Auslastung.",
                new Dictionary<string, string>
                {
                    ["kind"] = "temperature",
                    ["sensor"] = sensor,
                    ["value"] = temperature.Value.ToString("0.0"),
                },
                cancellationToken);
        }
        else if (
            !temperature.HasValue
            || temperature.Value
                < _options.PushTemperatureResetCelsius)
        {
            setAlertActive(false);
        }
    }

    private async Task SendToAllAsync(
        string title,
        string body,
        IReadOnlyDictionary<string, string> data,
        CancellationToken cancellationToken)
    {
        var targets = _devices.ListPushTargets();
        if (targets.Count == 0)
        {
            return;
        }

        var messages = targets
            .Select(target => new
            {
                to = target.ExpoPushToken,
                sound = "default",
                title,
                body,
                data,
                priority = "high",
                channelId = "nexus-alerts",
            })
            .ToArray();
        var client = _httpClientFactory.CreateClient("ExpoPush");
        using var response = await client.PostAsJsonAsync(
            ExpoPushEndpoint,
            messages,
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(
                cancellationToken);
            _logger.LogWarning(
                "Expo Push antwortete mit {StatusCode}: {Detail}",
                (int)response.StatusCode,
                detail);
        }
    }
}
