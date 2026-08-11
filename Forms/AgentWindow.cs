using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using NexusControl.Agent.Configuration;
using NexusControl.Agent.Models;
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
    private readonly UpdateService _updates;
    private DateTimeOffset _lastNetworkRefresh = DateTimeOffset.MinValue;
    private string? _lastQrPayload;
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
        _updates = null!;
        InitializeComponent();
        WinFormsTheme.Apply(this);
    }

    public AgentWindow(
        PairingService pairing,
        DeviceStore devices,
        AgentOptions options,
        AutoStartService autoStart,
        FirewallService firewall,
        UpdateService updates)
    {
        _pairing = pairing;
        _devices = devices;
        _options = options;
        _autoStart = autoStart;
        _firewall = firewall;
        _updates = updates;

        InitializeComponent();
        versionStatusLabel.Text = $"Version {TelemetryService.AgentVersion}";
        serverPortValueLabel.Text = _options.Port.ToString();
        TrySetApplicationIcon();
        WinFormsTheme.Apply(this);
        _updates.SnapshotChanged += UpdateSnapshotChanged;
        ApplyUpdateSnapshot(_updates.Snapshot);

        RefreshAutoStartState();
        RefreshStatus(forceNetworkRefresh: true);
        refreshTimer.Start();
    }

    public event EventHandler? HideRequested;

    public void ShowUpdateWindow(bool forceCheck)
    {
        var existingWindow = System.Windows.Forms.Application.OpenForms
            .OfType<UpdateWindow>()
            .FirstOrDefault();
        if (existingWindow is not null)
        {
            existingWindow.Activate();
            existingWindow.BringToFront();
            if (forceCheck)
            {
                _ = _updates.CheckNowAsync();
            }

            return;
        }

        using var updateWindow = new UpdateWindow(_updates, forceCheck);
        updateWindow.ShowDialog(this);
    }

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

    protected override void OnFormClosed(FormClosedEventArgs eventArgs)
    {
        if (_updates is not null)
        {
            _updates.SnapshotChanged -= UpdateSnapshotChanged;
        }

        base.OnFormClosed(eventArgs);
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

    private void EndpointsListBoxDoubleClick(
        object? sender,
        EventArgs eventArgs)
    {
        if (endpointsListBox.SelectedItem is string endpoint)
        {
            CopyText(endpoint);
        }
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
    }

    private void HideButtonClick(object? sender, EventArgs eventArgs)
    {
        HideRequested?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateButtonClick(object? sender, EventArgs eventArgs)
    {
        ShowUpdateWindow(forceCheck: false);
    }

    private void RefreshTimerTick(object? sender, EventArgs eventArgs)
    {
        RefreshStatus();
    }

    private void UpdateSnapshotChanged(UpdateSnapshot snapshot)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(new Action<UpdateSnapshot>(UpdateSnapshotChanged), snapshot);
            return;
        }

        ApplyUpdateSnapshot(snapshot);
    }

    private void ApplyUpdateSnapshot(UpdateSnapshot snapshot)
    {
        switch (snapshot.Stage)
        {
            case UpdateStage.Available:
                updateButton.Visible = true;
                updateButton.Enabled = true;
                updateButton.Text = "↓  Update";
                updateToolTip.SetToolTip(
                    updateButton,
                    $"Version {snapshot.Release?.DisplayVersion} ist verfügbar.");
                break;

            case UpdateStage.Downloading:
                updateButton.Visible = true;
                updateButton.Enabled = false;
                updateButton.Text = $"{snapshot.ProgressPercent}%";
                break;

            case UpdateStage.Verifying:
            case UpdateStage.Installing:
                updateButton.Visible = true;
                updateButton.Enabled = false;
                updateButton.Text = "Update …";
                break;

            default:
                updateButton.Visible = false;
                updateButton.Enabled = false;
                break;
        }
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

    private void RefreshStatus(bool forceNetworkRefresh = false)
    {
        if (
            forceNetworkRefresh
            || DateTimeOffset.UtcNow - _lastNetworkRefresh
                >= TimeSpan.FromSeconds(5))
        {
            _pairing.Configure(NetworkUtilities.GetReachableIPv4Addresses());
            remoteAccessValueLabel.Text =
                NetworkUtilities.GetTailscaleIPv4Addresses().Count > 0
                    ? "Tailscale verfügbar"
                    : "Nur lokales Netzwerk";
            _lastNetworkRefresh = DateTimeOffset.UtcNow;
        }

        var snapshot = _pairing.GetSnapshot();
        primaryAddressValueLabel.Text = snapshot.PreferredEndpoint;
        trustedDevicesValueLabel.Text = _devices.Count == 1
            ? "1 Gerät"
            : $"{_devices.Count} Geräte";
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
