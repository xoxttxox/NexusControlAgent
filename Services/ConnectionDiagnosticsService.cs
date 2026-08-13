using System.Net;
using System.Net.Sockets;
using System.Text;
using NexusControl.Agent.Localization;
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
                LocalizationService.Text("Diagnostics.AgentServer"),
                serverReady
                    ? ConnectionDiagnosticState.Success
                    : ConnectionDiagnosticState.Error,
                serverReady
                    ? LocalizationService.Format(
                        "Diagnostics.PortReady",
                        _port)
                    : LocalizationService.Format(
                        "Diagnostics.PortUnavailable",
                        _port)),
            new ConnectionDiagnosticItem(
                LocalizationService.Text("Diagnostics.LocalNetwork"),
                hasLanAddress
                    ? ConnectionDiagnosticState.Success
                    : ConnectionDiagnosticState.Error,
                hasLanAddress
                    ? string.Join(", ", localAddresses.Select(
                        address => $"{address}:{_port}"))
                    : LocalizationService.Text(
                        "Diagnostics.NoPrivateAddress")),
            new ConnectionDiagnosticItem(
                LocalizationService.Text("Diagnostics.WindowsFirewall"),
                firewallReady
                    ? ConnectionDiagnosticState.Success
                    : ConnectionDiagnosticState.Warning,
                firewallReady
                    ? LocalizationService.Format(
                        "Diagnostics.FirewallReady",
                        _port)
                    : LocalizationService.Text(
                        "Diagnostics.FirewallMissing")),
            new ConnectionDiagnosticItem(
                LocalizationService.Text("Diagnostics.PairedDevices"),
                activeDevices > 0
                    ? ConnectionDiagnosticState.Success
                    : ConnectionDiagnosticState.Information,
                deviceList.Count switch
                {
                    0 => LocalizationService.Text(
                        "Diagnostics.Devices.None"),
                    1 => LocalizationService.Format(
                        "Diagnostics.Devices.One",
                        activeDevices),
                    _ => LocalizationService.Format(
                        "Diagnostics.Devices.Many",
                        deviceList.Count,
                        activeDevices),
                }),
            new ConnectionDiagnosticItem(
                LocalizationService.Text("Diagnostics.WindowsStartup"),
                autoStartEnabled
                    ? ConnectionDiagnosticState.Success
                    : ConnectionDiagnosticState.Information,
                autoStartEnabled
                    ? LocalizationService.Text(
                        "Diagnostics.StartupEnabled")
                    : LocalizationService.Text(
                        "Diagnostics.StartupDisabled")),
            new ConnectionDiagnosticItem(
                LocalizationService.Text("Diagnostics.RemoteConnection"),
                tailscaleAddresses.Count > 0
                    ? ConnectionDiagnosticState.Success
                    : ConnectionDiagnosticState.Information,
                tailscaleAddresses.Count > 0
                    ? $"Tailscale: {string.Join(", ", tailscaleAddresses)}"
                    : LocalizationService.Text("Diagnostics.LocalOnly")),
        };

        var hasCriticalError = items.Any(item =>
            item.State == ConnectionDiagnosticState.Error);
        var hasWarning = items.Any(item =>
            item.State == ConnectionDiagnosticState.Warning);
        var summary = hasCriticalError
            ? LocalizationService.Text("Diagnostics.Summary.Error")
            : hasWarning
                ? LocalizationService.Text("Diagnostics.Summary.Warning")
                : LocalizationService.Text("Diagnostics.Summary.Success");

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
        report.AppendLine(LocalizationService.Text("Diagnostics.Report.Title"));
        report.AppendLine(LocalizationService.Format(
            "Diagnostics.Report.Time",
            capturedAt));
        report.AppendLine(LocalizationService.Format(
            "Diagnostics.Report.Pc",
            Environment.MachineName));
        report.AppendLine(LocalizationService.Format(
            "Diagnostics.Report.Agent",
            TelemetryService.AgentVersion));
        report.AppendLine(LocalizationService.Format(
            "Diagnostics.Report.Windows",
            Environment.OSVersion.VersionString));
        report.AppendLine(LocalizationService.Format(
            "Diagnostics.Report.Result",
            summary));
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
            ConnectionDiagnosticState.Success => LocalizationService.Text(
                "Diagnostics.State.Success"),
            ConnectionDiagnosticState.Warning => LocalizationService.Text(
                "Diagnostics.State.Warning"),
            ConnectionDiagnosticState.Error => LocalizationService.Text(
                "Diagnostics.State.Error"),
            _ => LocalizationService.Text(
                "Diagnostics.State.Information"),
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
