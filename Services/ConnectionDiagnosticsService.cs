using System.Net;
using System.Net.Sockets;
using System.Text;
using NexusControl.Agent.Networking;
using NexusControl.Agent.Pairing;

namespace NexusControl.Agent.Services;

/// <summary>
/// Führt ausschließlich lokale, nicht verändernde Prüfungen für das kompakte
/// Diagnosefenster aus. Der Bericht enthält keine Tokens oder Gerätegeheimnisse.
/// </summary>
internal sealed class ConnectionDiagnosticsService
{
    private readonly int _port;
    private readonly DeviceStore _devices;
    private readonly FirewallService _firewall;
    private readonly AutoStartService _autoStart;

    public ConnectionDiagnosticsService(
        int port,
        DeviceStore devices,
        FirewallService firewall,
        AutoStartService autoStart)
    {
        _port = port;
        _devices = devices;
        _firewall = firewall;
        _autoStart = autoStart;
    }

    public async Task<ConnectionDiagnosticsSnapshot> RunAsync(
        CancellationToken cancellationToken = default)
    {
        var capturedAt = DateTimeOffset.Now;
        var localAddresses = NetworkUtilities.GetPrivateIPv4Addresses();
        var tailscaleAddresses = NetworkUtilities.GetTailscaleIPv4Addresses();
        var deviceList = _devices.ListDevices();
        var activeDevices = deviceList.Count(device => device.IsOnline);

        var serverReady = await CanConnectToAgentAsync(cancellationToken);

        bool firewallReady;
        try
        {
            firewallReady = await _firewall.IsConfiguredAsync(cancellationToken);
        }
        catch
        {
            firewallReady = false;
        }

        bool autoStartEnabled;
        try
        {
            autoStartEnabled = await Task.Run(
                () => _autoStart.IsEnabled(),
                cancellationToken);
        }
        catch
        {
            autoStartEnabled = false;
        }

        var hasLanAddress = localAddresses.Any(address =>
            !string.Equals(address, "127.0.0.1", StringComparison.Ordinal));
        var items = new[]
        {
            new ConnectionDiagnosticItem(
                "Agent-Server",
                serverReady
                    ? ConnectionDiagnosticState.Success
                    : ConnectionDiagnosticState.Error,
                serverReady
                    ? $"Port {_port} antwortet lokal."
                    : $"Port {_port} ist lokal nicht erreichbar."),
            new ConnectionDiagnosticItem(
                "Lokales Netzwerk",
                hasLanAddress
                    ? ConnectionDiagnosticState.Success
                    : ConnectionDiagnosticState.Error,
                hasLanAddress
                    ? string.Join(", ", localAddresses.Select(
                        address => $"{address}:{_port}"))
                    : "Keine aktive private IPv4-Adresse gefunden."),
            new ConnectionDiagnosticItem(
                "Windows-Firewall",
                firewallReady
                    ? ConnectionDiagnosticState.Success
                    : ConnectionDiagnosticState.Warning,
                firewallReady
                    ? $"Freigabe für TCP-Port {_port} ist vorhanden."
                    : "Die lokale Firewall-Freigabe fehlt oder konnte nicht geprüft werden."),
            new ConnectionDiagnosticItem(
                "Gekoppelte Geräte",
                activeDevices > 0
                    ? ConnectionDiagnosticState.Success
                    : ConnectionDiagnosticState.Information,
                deviceList.Count switch
                {
                    0 => "Noch kein Smartphone gekoppelt.",
                    1 => $"1 Gerät gekoppelt, {activeDevices} aktuell online.",
                    _ => $"{deviceList.Count} Geräte gekoppelt, {activeDevices} aktuell online.",
                }),
            new ConnectionDiagnosticItem(
                "Windows-Autostart",
                autoStartEnabled
                    ? ConnectionDiagnosticState.Success
                    : ConnectionDiagnosticState.Information,
                autoStartEnabled
                    ? "Aktiv – der Agent startet unsichtbar im Infobereich."
                    : "Deaktiviert – der Agent muss manuell gestartet werden."),
            new ConnectionDiagnosticItem(
                "Remote-Verbindung",
                tailscaleAddresses.Count > 0
                    ? ConnectionDiagnosticState.Success
                    : ConnectionDiagnosticState.Information,
                tailscaleAddresses.Count > 0
                    ? $"Tailscale: {string.Join(", ", tailscaleAddresses)}"
                    : "Nur lokales Netzwerk – das ist ohne Tailscale normal."),
        };

        var hasCriticalError = items.Any(item =>
            item.State == ConnectionDiagnosticState.Error);
        var hasWarning = items.Any(item =>
            item.State == ConnectionDiagnosticState.Warning);
        var summary = hasCriticalError
            ? "Mindestens eine wichtige Verbindungskomponente ist nicht bereit."
            : hasWarning
                ? "Der Agent läuft, aber mindestens eine Einstellung sollte geprüft werden."
                : "Alle wichtigen Verbindungstests waren erfolgreich.";

        return new ConnectionDiagnosticsSnapshot(
            capturedAt,
            summary,
            items,
            BuildReport(capturedAt, summary, items));
    }

    private async Task<bool> CanConnectToAgentAsync(
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(
                IPAddress.Loopback,
                _port,
                timeout.Token);
            return client.Connected;
        }
        catch (Exception error) when (
            error is SocketException or OperationCanceledException)
        {
            return false;
        }
    }

    private static string BuildReport(
        DateTimeOffset capturedAt,
        string summary,
        IReadOnlyList<ConnectionDiagnosticItem> items)
    {
        var report = new StringBuilder();
        report.AppendLine("Nexus Control Agent – Verbindungsdiagnose");
        report.AppendLine($"Zeit: {capturedAt:yyyy-MM-dd HH:mm:ss zzz}");
        report.AppendLine($"PC: {Environment.MachineName}");
        report.AppendLine($"Agent: {TelemetryService.AgentVersion}");
        report.AppendLine($"Windows: {Environment.OSVersion.VersionString}");
        report.AppendLine($"Ergebnis: {summary}");
        report.AppendLine();
        foreach (var item in items)
        {
            report.AppendLine(
                $"[{StateText(item.State)}] {item.Name}: {item.Message}");
        }

        return report.ToString().TrimEnd();
    }

    private static string StateText(ConnectionDiagnosticState state) =>
        state switch
        {
            ConnectionDiagnosticState.Success => "OK",
            ConnectionDiagnosticState.Warning => "WARNUNG",
            ConnectionDiagnosticState.Error => "FEHLER",
            _ => "INFO",
        };
}

internal enum ConnectionDiagnosticState
{
    Success,
    Information,
    Warning,
    Error,
}

internal sealed record ConnectionDiagnosticItem(
    string Name,
    ConnectionDiagnosticState State,
    string Message);

internal sealed record ConnectionDiagnosticsSnapshot(
    DateTimeOffset CapturedAt,
    string Summary,
    IReadOnlyList<ConnectionDiagnosticItem> Items,
    string Report);
