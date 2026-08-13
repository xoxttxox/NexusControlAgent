using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using NexusControl.Agent.Configuration;
using NexusControl.Agent.Localization;
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
    private bool _languageEventsSubscribed;
    private bool _firewallCheckStarted;
    private ToolTip? _toolTip;

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
        ApplyLocalization();
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
        SubscribeLanguageEvents();
        ApplyLocalization();
        versionStatusLabel.Text = LocalizationService.Format(
            "Common.Version",
            TelemetryService.AgentVersion);
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
            LocalizationService.Format(
                "AgentWindow.Firewall.Intro",
                Environment.NewLine,
                _options.Port),
            LocalizationService.Text("AgentWindow.Firewall.Title"),
            NexusDialogKind.Information);

        statusLabel.Text = LocalizationService.Text(
            "AgentWindow.Firewall.Installing");
        var result = await _firewall.InstallElevatedAsync();
        if (result.Succeeded)
        {
            statusLabel.Text = LocalizationService.Text(
                "AgentWindow.Firewall.Success");
            return;
        }

        var message = result.Cancelled
            ? LocalizationService.Text("AgentWindow.Firewall.Cancelled")
            : LocalizationService.Format(
                "AgentWindow.Firewall.Failed",
                Environment.NewLine,
                result.Error
                    ?? LocalizationService.Text("Common.UnknownError"));

        NexusDialog.Show(
            this,
            message,
            LocalizationService.Text("AgentWindow.Firewall.SetupTitle"),
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

    private void SettingsButtonClick(object? sender, EventArgs eventArgs)
    {
        using var dialog = new SettingsDialog();
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

    private void ApplicationLanguageChanged(
        object? sender,
        EventArgs eventArgs)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(new Action(
                () => ApplicationLanguageChanged(sender, eventArgs)));
            return;
        }

        ApplyLocalization();
        ConfigureToolTips();
        RefreshStatus(forceNetworkRefresh: true);
    }

    private void SubscribeLanguageEvents()
    {
        if (_languageEventsSubscribed)
        {
            return;
        }

        LocalizationService.LanguageChanged += ApplicationLanguageChanged;
        _languageEventsSubscribed = true;
    }

    private void UnsubscribeLanguageEvents()
    {
        if (!_languageEventsSubscribed)
        {
            return;
        }

        LocalizationService.LanguageChanged -= ApplicationLanguageChanged;
        _languageEventsSubscribed = false;
    }

    private void ApplyLocalization()
    {
        LocalizationService.Apply(this, nameof(AgentWindow));
        versionStatusLabel.Text = LocalizationService.Format(
            "Common.Version",
            TelemetryService.AgentVersion);
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
        _toolTip ??= new ToolTip(components!)
        {
            AutomaticDelay = 350,
            AutoPopDelay = 6_000,
            ShowAlways = true,
        };
        _toolTip.SetToolTip(
            trustedDevicesValueLabel,
            LocalizationService.Text("AgentWindow.ToolTip.Devices"));
        _toolTip.SetToolTip(
            refreshButton,
            LocalizationService.Text("AgentWindow.ToolTip.Diagnostics"));
        _toolTip.SetToolTip(
            protocolButton,
            LocalizationService.Text("AgentWindow.ToolTip.ActivityLog"));
        _toolTip.SetToolTip(
            endpointsListBox,
            LocalizationService.Text("AgentWindow.ToolTip.Endpoints"));
        _toolTip.SetToolTip(
            settingsButton,
            LocalizationService.Text("AgentWindow.ToolTip.Settings"));
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
                    ? LocalizationService.Text(
                        "AgentWindow.Remote.TailscaleAvailable")
                    : LocalizationService.Text(
                        "AgentWindow.Remote.LocalOnly");
            _connectionMode = tailscaleAvailable ? "Tailscale" : "LAN";
            _lastNetworkRefresh = DateTimeOffset.UtcNow;
        }

        var snapshot = _pairing.GetSnapshot();
        var trustedDevices = _devices.ListDevices();
        var onlineDevices = trustedDevices.Count(device => device.IsOnline);
        primaryAddressValueLabel.Text = snapshot.PreferredEndpoint;
        trustedDevicesValueLabel.Text = trustedDevices.Count == 1
            ? LocalizationService.Text("AgentWindow.Devices.One")
            : LocalizationService.Format(
                "AgentWindow.Devices.Many",
                trustedDevices.Count);
        onlineStatusLabel.Text = onlineDevices switch
        {
            0 => LocalizationService.Format(
                "AgentWindow.Header.None",
                _connectionMode),
            1 => LocalizationService.Format(
                "AgentWindow.Header.One",
                _connectionMode),
            _ => LocalizationService.Format(
                "AgentWindow.Header.Many",
                onlineDevices,
                _connectionMode),
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

        pairingExpiryLabel.Text = LocalizationService.Format(
            "AgentWindow.Pairing.Expires",
            remaining.Minutes,
            remaining.Seconds);
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

        statusLabel.Text = LocalizationService.Format(
            "AgentWindow.Status.Running",
            _options.Port,
            DateTime.Now);

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
            result.Error
                ?? LocalizationService.Text(
                    "AgentWindow.AutoStartChangeFailed"),
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
            statusLabel.Text = LocalizationService.Text(
                "AgentWindow.Status.Copied");
        }
        catch (ExternalException)
        {
            NexusDialog.Show(
                this,
                LocalizationService.Text("Common.ClipboardUnavailable"),
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
