using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using NexusControl.Agent.Configuration;
using NexusControl.Agent.Models;
using NexusControl.Agent.Services;

namespace NexusControl.Agent.Pairing;

internal sealed class PairingService
{
    private readonly Lock _gate = new();
    private readonly DeviceStore _deviceStore;
    private readonly ActivityLogService _activityLog;
    private readonly AgentOptions _options;
    private string _code = "";
    private DateTimeOffset _expiresAt;
    private int _failedAttempts;
    private int _port;
    private IReadOnlyList<string> _addresses = ["127.0.0.1"];

    public PairingService(
        DeviceStore deviceStore,
        ActivityLogService activityLog,
        IOptions<AgentOptions> options)
    {
        _deviceStore = deviceStore;
        _activityLog = activityLog;
        _options = options.Value;
        _port = _options.Port;
        RotateCode();
    }

    public void Configure(IReadOnlyList<string> addresses)
    {
        lock (_gate)
        {
            _addresses = addresses;
        }
    }

    public bool TryPair(
        string code,
        string? deviceName,
        string? platform,
        out PairingCredentials? credentials,
        out string error)
    {
        lock (_gate)
        {
            if (DateTimeOffset.UtcNow >= _expiresAt)
            {
                RotateCode();
                _activityLog.Record(
                    null,
                    null,
                    "Pairing-Code prüfen",
                    ActivityLogResult.Rejected);
                credentials = null;
                error =
                    "Der Pairing-Code ist abgelaufen. Erstelle im Agent-Fenster einen neuen Code.";
                return false;
            }

            if (!CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.UTF8.GetBytes(_code),
                    System.Text.Encoding.UTF8.GetBytes(code ?? "")))
            {
                _failedAttempts++;
                if (_failedAttempts >= _options.MaximumPairingAttempts)
                {
                    RotateCode();
                    _activityLog.Record(
                        null,
                        null,
                        "Pairing-Code prüfen",
                        ActivityLogResult.Rejected);
                    credentials = null;
                    error = "Zu viele Fehlversuche. Ein neuer Code wurde erstellt.";
                    return false;
                }

                _activityLog.Record(
                    null,
                    null,
                    "Pairing-Code prüfen",
                    ActivityLogResult.Rejected);
                credentials = null;
                error = "Der 6-stellige Pairing-Code ist falsch.";
                return false;
            }

            credentials = _deviceStore.AddDevice(deviceName, platform);
            var identity = _deviceStore.GetAuditIdentity(
                credentials.DeviceId);
            _activityLog.Record(
                identity.DeviceName,
                identity.Platform,
                "Gerät gekoppelt",
                ActivityLogResult.Success);
            error = "";
            RotateCode();
            return true;
        }
    }

    public PairingSnapshot RotateCodeNow()
    {
        lock (_gate)
        {
            RotateCode();
            return CreateSnapshot();
        }
    }

    public PairingSnapshot GetSnapshot()
    {
        lock (_gate)
        {
            if (DateTimeOffset.UtcNow >= _expiresAt)
            {
                RotateCode();
            }

            return CreateSnapshot();
        }
    }

    private void RotateCode()
    {
        _code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        _expiresAt = DateTimeOffset.UtcNow.AddMinutes(
            _options.PairingCodeLifetimeMinutes);
        _failedAttempts = 0;
    }

    private PairingSnapshot CreateSnapshot()
    {
        var endpoints = _addresses
            .Select(address => $"{address}:{_port}")
            .ToArray();
        var preferredEndpoint = endpoints[0];
        var payload =
            $"nexuscontrol://pair?address={Uri.EscapeDataString(preferredEndpoint)}&code={_code}";

        return new PairingSnapshot(
            _code,
            _expiresAt,
            preferredEndpoint,
            endpoints,
            payload);
    }
}

internal sealed record PairingSnapshot(
    string Code,
    DateTimeOffset ExpiresAt,
    string PreferredEndpoint,
    IReadOnlyList<string> Endpoints,
    string Payload);
