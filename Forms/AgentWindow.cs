using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using NexusControl.Agent.Configuration;
using NexusControl.Agent.Networking;
using NexusControl.Agent.Pairing;
using NexusControl.Agent.Services;
using NexusControl.Agent.UI;
using QRCoder;

namespace NexusControl.Agent.Forms;

[DesignerCategory("Form")]
internal sealed partial class AgentWindow : Form
{
    private readonly PairingService _pairing;
    private readonly DeviceStore _devices;
    private readonly AgentOptions _options;
    private readonly AutoStartService _autoStart;
    private readonly FirewallService _firewall;
    private readonly ConnectionDiagnosticsService _connectionDiagnostics;
    private readonly ActivityLogService _activityLog;
    private DateTimeOffset _lastNetworkRefresh = DateTimeOffset.MinValue;
    private string? _lastQrPayload;
    private string _connectionMode = "LAN";
    private bool _syncingAutoStart;
    private bool _firewallCheckStarted;

    /// <summary>
    /// Konstruktor ausschließlich für den Visual-Studio-WinForms-Designer.
    /// Zur Laufzeit verwendet der Agent den Konstruktor mit seinen Diensten.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public AgentWindow()
    {
        _pairing = null!;
        _devices = null!;
        _options = null!;
        _autoStart = null!;
        _firewall = null!;
        _connectionDiagnostics = null!;
        _activityLog = null!;
        InitializeComponent();
        WinFormsTheme.Apply(this);
        ConfigureToolTips();
    }

    public AgentWindow(
        PairingService pairing,
        DeviceStore devices,
        AgentOptions options,
        AutoStartService autoStart,
        FirewallService firewall,
        ActivityLogService activityLog)
    {
        _pairing = pairing;
        _devices = devices;
        _options = options;
        _autoStart = autoStart;
        _firewall = firewall;
        _activityLog = activityLog;
        _connectionDiagnostics = new ConnectionDiagnosticsService(
            options.Port,
            devices,
            firewall,
            autoStart);

        InitializeComponent();
        versionStatusLabel.Text = $"Version {TelemetryService.AgentVersion}";
        serverPortValueLabel.Text = _options.Port.ToString();
        TrySetApplicationIcon();
        WinFormsTheme.Apply(this);
        ConfigureToolTips();

        RefreshAutoStartState();
        RefreshStatus(forceNetworkRefresh: true);
        refreshTimer.Start();
    }

    public event EventHandler? HideRequested;

    public void RotatePairingCode()
    {
        _pairing.RotateCodeNow();
        RefreshStatus(forceNetworkRefresh: true);
    }

    public void RefreshAutoStartState()
    {
        _syncingAutoStart = true;
        try
        {
            autoStartCheckBox.Checked = _autoStart.IsEnabled();
        }
        finally
        {
            _syncingAutoStart = false;
        }
    }

    protected override async void OnShown(EventArgs eventArgs)
    {
        base.OnShown(eventArgs);

        if (_firewallCheckStarted)
        {
            return;
        }

        _firewallCheckStarted = true;
        await EnsureFirewallConfiguredAsync();
    }

    private async Task EnsureFirewallConfiguredAsync()
    {
        bool isConfigured;
        try
        {
            isConfigured = await _firewall.IsConfiguredAsync();
        }
        catch
        {
            isConfigured = false;
        }

        if (isConfigured)
        {
            return;
        }

        NexusDialog.Show(
            this,
            "Damit dein Smartphone den PC im lokalen Netzwerk erreichen kann, richtet Nexus Control jetzt einmalig eine Windows-Firewall-Regel ein.\r\n\r\n" +
            $"Freigegeben wird ausschließlich TCP-Port {_options.Port} für Geräte aus deinem lokalen Subnetz. Danach erscheint die Windows-Administratorabfrage.",
            "Windows-Firewall einrichten",
            NexusDialogKind.Information);

        statusLabel.Text = "Windows-Firewall wird eingerichtet …";
        var result = await _firewall.InstallElevatedAsync();
        if (result.Succeeded)
        {
            statusLabel.Text = "Windows-Firewall wurde erfolgreich eingerichtet.";
            return;
        }

        var message = result.Cancelled
            ? "Die Windows-Administratorabfrage wurde abgebrochen. Ohne Firewall-Regel kann dein Smartphone den Agent möglicherweise nicht erreichen. Beim nächsten Start wird die Einrichtung erneut angeboten."
            : "Die Windows-Firewall konnte nicht eingerichtet werden.\r\n\r\n" +
              (result.Error ?? "Unbekannter Fehler.");

        NexusDialog.Show(
            this,
            message,
            "Firewall-Einrichtung",
            NexusDialogKind.Warning);
    }

    private void EndpointsListBoxMouseDoubleClick(
        object? sender,
        MouseEventArgs eventArgs)
    {
        var index = endpointsListBox.IndexFromPoint(eventArgs.Location);
        if (
            index != ListBox.NoMatches
            && endpointsListBox.Items[index] is string endpoint)
        {
            CopyText(endpoint);
        }
    }

    private void TrustedDevicesValueLabelClick(
        object? sender,
        EventArgs eventArgs)
    {
        using var dialog = new DeviceManagementDialog(
            _devices,
            _activityLog);
        dialog.ShowDialog(this);
        RefreshStatus(forceNetworkRefresh: true);
    }

    private void PairingCodeTextBoxDoubleClick(
        object? sender,
        EventArgs eventArgs)
    {
        CopyPairingCode();
    }

    private void CopyPairingCodeButtonClick(
        object? sender,
        EventArgs eventArgs)
    {
        CopyPairingCode();
    }

    private void RotatePairingCodeButtonClick(
        object? sender,
        EventArgs eventArgs)
    {
        RotatePairingCode();
    }

    private void RefreshButtonClick(object? sender, EventArgs eventArgs)
    {
        RefreshStatus(forceNetworkRefresh: true);
        using var dialog = new ConnectionDiagnosticsDialog(
            _connectionDiagnostics);
        dialog.ShowDialog(this);
    }

    private void ProtocolButtonClick(object? sender, EventArgs eventArgs)
    {
        using var dialog = new ActivityLogDialog(_activityLog);
        dialog.ShowDialog(this);
    }

    private void HideButtonClick(object? sender, EventArgs eventArgs)
    {
        HideRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshTimerTick(object? sender, EventArgs eventArgs)
    {
        RefreshStatus();
    }

    private void TrySetApplicationIcon()
    {
        try
        {
            var executablePath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(executablePath)
                    ?? SystemIcons.Application;
            }
        }
        catch
        {
            Icon = SystemIcons.Application;
        }
    }

    private void ConfigureToolTips()
    {
        var toolTip = new ToolTip(components!)
        {
            AutomaticDelay = 350,
            AutoPopDelay = 6_000,
            ShowAlways = true,
        };
        toolTip.SetToolTip(
            trustedDevicesValueLabel,
            "Gekoppelte Smartphones verwalten");
        toolTip.SetToolTip(
            refreshButton,
            "Verbindungsdiagnose öffnen");
        toolTip.SetToolTip(
            protocolButton,
            "Lokale Verbindungen und Aktionen anzeigen");
        toolTip.SetToolTip(
            endpointsListBox,
            "Adresse doppelklicken, um sie zu kopieren");
    }

    private void RefreshStatus(bool forceNetworkRefresh = false)
    {
        if (
            forceNetworkRefresh
            || DateTimeOffset.UtcNow - _lastNetworkRefresh
                >= TimeSpan.FromSeconds(5))
        {
            _pairing.Configure(NetworkUtilities.GetReachableIPv4Addresses());
            var tailscaleAvailable =
                NetworkUtilities.GetTailscaleIPv4Addresses().Count > 0;
            remoteAccessValueLabel.Text =
                tailscaleAvailable
                    ? "Tailscale verfügbar"
                    : "Nur lokales Netzwerk";
            _connectionMode = tailscaleAvailable ? "Tailscale" : "LAN";
            _lastNetworkRefresh = DateTimeOffset.UtcNow;
        }

        var snapshot = _pairing.GetSnapshot();
        var trustedDevices = _devices.ListDevices();
        var onlineDevices = trustedDevices.Count(device => device.IsOnline);
        primaryAddressValueLabel.Text = snapshot.PreferredEndpoint;
        trustedDevicesValueLabel.Text = trustedDevices.Count == 1
            ? "1 Gerät  ›"
            : $"{trustedDevices.Count} Geräte  ›";
        onlineStatusLabel.Text = onlineDevices switch
        {
            0 => $"Online · kein Gerät verbunden · {_connectionMode}",
            1 => $"Online · 1 Gerät verbunden · {_connectionMode}",
            _ => $"Online · {onlineDevices} Geräte verbunden · {_connectionMode}",
        };
        UpdateEndpointList(snapshot.Endpoints);

        pairingCodeTextBox.Text = snapshot.Code.Length == 6
            ? $"{snapshot.Code[..3]} {snapshot.Code[3..]}"
            : snapshot.Code;

        var remaining = snapshot.ExpiresAt - DateTimeOffset.UtcNow;
        if (remaining < TimeSpan.Zero)
        {
            remaining = TimeSpan.Zero;
        }

        pairingExpiryLabel.Text =
            $"Noch {remaining.Minutes:00}:{remaining.Seconds:00} Minuten gültig";
        var lifetimeSeconds = TimeSpan
            .FromMinutes(_options.PairingCodeLifetimeMinutes)
            .TotalSeconds;
        var progress = lifetimeSeconds <= 0
            ? 0D
            : Math.Clamp(remaining.TotalSeconds / lifetimeSeconds, 0D, 1D);
        pairingProgressBar.Value = Math.Clamp(
            (int)Math.Round(progress * pairingProgressBar.Maximum),
            pairingProgressBar.Minimum,
            pairingProgressBar.Maximum);

        statusLabel.Text =
            $"Agent läuft auf Port {_options.Port}  ·  {DateTime.Now:HH:mm:ss}";

        if (!string.Equals(
                _lastQrPayload,
                snapshot.Payload,
                StringComparison.Ordinal))
        {
            var nextImage = CreateQrBitmap(snapshot.Payload);
            var previousImage = qrCodePictureBox.Image;
            qrCodePictureBox.Image = nextImage;
            previousImage?.Dispose();
            _lastQrPayload = snapshot.Payload;
        }
    }

    private void UpdateEndpointList(IReadOnlyList<string> endpoints)
    {
        if (endpointsListBox.Items.Count == endpoints.Count)
        {
            var unchanged = true;
            for (var index = 0; index < endpoints.Count; index++)
            {
                if (!string.Equals(
                        endpointsListBox.Items[index] as string,
                        endpoints[index],
                        StringComparison.Ordinal))
                {
                    unchanged = false;
                    break;
                }
            }

            if (unchanged)
            {
                return;
            }
        }

        endpointsListBox.BeginUpdate();
        try
        {
            endpointsListBox.Items.Clear();
            foreach (var endpoint in endpoints)
            {
                endpointsListBox.Items.Add(endpoint);
            }
        }
        finally
        {
            endpointsListBox.EndUpdate();
        }
    }

    private async void AutoStartCheckedChanged(
        object? sender,
        EventArgs eventArgs)
    {
        if (_syncingAutoStart)
        {
            return;
        }

        var requestedState = autoStartCheckBox.Checked;
        autoStartCheckBox.Enabled = false;
        var result = await Task.Run(
            () => _autoStart.SetEnabled(requestedState));
        autoStartCheckBox.Enabled = true;

        if (result.Succeeded)
        {
            return;
        }

        _syncingAutoStart = true;
        autoStartCheckBox.Checked = !requestedState;
        _syncingAutoStart = false;
        NexusDialog.Show(
            this,
            result.Error ?? "Der Autostart konnte nicht geändert werden.",
            "Nexus Control Agent",
            NexusDialogKind.Warning);
    }

    private void CopyPairingCode()
    {
        CopyText(pairingCodeTextBox.Text.Replace(
            " ",
            "",
            StringComparison.Ordinal));
    }

    private void CopyText(string value)
    {
        try
        {
            Clipboard.SetText(value);
            statusLabel.Text = "In die Zwischenablage kopiert.";
        }
        catch (ExternalException)
        {
            NexusDialog.Show(
                this,
                "Windows konnte die Zwischenablage gerade nicht öffnen. Bitte versuche es erneut.",
                "Nexus Control Agent",
                NexusDialogKind.Information);
        }
    }

    private Bitmap CreateQrBitmap(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(
            payload,
            QRCodeGenerator.ECCLevel.Q);
        var availableSize = Math.Max(
            1,
            Math.Min(
                qrCodePictureBox.ClientSize.Width,
                qrCodePictureBox.ClientSize.Height));
        var pixelsPerModule = Math.Max(
            1,
            availableSize / data.ModuleMatrix.Count);
        using var qrCode = new QRCode(data);
        return qrCode.GetGraphic(
            pixelsPerModule,
            Color.Black,
            Color.White,
            true);
    }
}
