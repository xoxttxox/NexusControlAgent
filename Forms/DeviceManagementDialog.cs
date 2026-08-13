using System.ComponentModel;
using System.Windows.Forms;
using NexusControl.Agent.Models;
using NexusControl.Agent.Pairing;
using NexusControl.Agent.Services;
using NexusControl.Agent.UI;

namespace NexusControl.Agent.Forms;

/// <summary>
/// Kleine lokale Verwaltung für gekoppelte Smartphones. Das Fenster verwendet
/// ausschließlich das bestehende WinForms-Theme und verändert das Hauptdesign
/// nicht.
/// </summary>
[DesignerCategory("Form")]
internal sealed partial class DeviceManagementDialog : Form
{
    private readonly DeviceStore _devices;
    private readonly ActivityLogService _activityLog;
    private bool _loading;
    private int _knownDeviceCount;

    /// <summary>
    /// Konstruktor ausschließlich für den Visual-Studio-WinForms-Designer.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public DeviceManagementDialog()
    {
        _devices = null!;
        _activityLog = null!;
        InitializeComponent();
        WinFormsTheme.Apply(this);
    }

    public DeviceManagementDialog(
        DeviceStore devices,
        ActivityLogService activityLog)
    {
        _devices = devices;
        _activityLog = activityLog;
        InitializeComponent();
        WinFormsTheme.Apply(this);
        LoadDevices();
        _refreshTimer.Start();
    }

    private DeviceChoice? SelectedChoice =>
        _deviceComboBox.SelectedItem as DeviceChoice;

    private void LoadDevices(string? preferredDeviceId = null)
    {
        var selectedId = preferredDeviceId ?? SelectedChoice?.DeviceId;
        var devices = _devices.ListDevices();
        _knownDeviceCount = devices.Count;

        _loading = true;
        try
        {
            _deviceComboBox.BeginUpdate();
            _deviceComboBox.Items.Clear();
            foreach (var device in devices)
            {
                _deviceComboBox.Items.Add(new DeviceChoice(device));
            }
            _deviceComboBox.EndUpdate();

            var selectedIndex = devices
                .Select((device, index) => (device, index))
                .Where(item => string.Equals(
                    item.device.DeviceId,
                    selectedId,
                    StringComparison.Ordinal))
                .Select(item => item.index)
                .DefaultIfEmpty(devices.Count > 0 ? 0 : -1)
                .First();
            _deviceComboBox.SelectedIndex = selectedIndex;
            PopulateSelectedDevice();
        }
        finally
        {
            _loading = false;
        }
    }

    private void PopulateSelectedDevice()
    {
        var choice = SelectedChoice;
        var enabled = choice is not null;
        _deviceNameTextBox.Enabled = enabled;
        _remoteAccessCheckBox.Enabled = enabled;
        _saveButton.Enabled = enabled;
        _removeButton.Enabled = enabled;

        if (choice is null)
        {
            _deviceNameTextBox.Text = "";
            _deviceInfoLabel.Text = "Noch kein Smartphone gekoppelt.";
            _deviceInfoLabel.ForeColor = WinFormsTheme.TextMuted;
            _remoteAccessCheckBox.Checked = false;
            SetPermissionFlags(DevicePermission.None);
            SetPermissionControlsEnabled(false);
            _statusLabel.Text = "Über den QR-Code kann ein Gerät gekoppelt werden.";
            return;
        }

        var device = choice.Device;
        _deviceNameTextBox.Text = device.DeviceName;
        _deviceInfoLabel.Text = FormatDeviceInfo(device);
        _deviceInfoLabel.ForeColor = device.IsOnline
            ? WinFormsTheme.Success
            : WinFormsTheme.TextMuted;
        _remoteAccessCheckBox.Checked = device.RemoteAccessEnabled;
        SetPermissionFlags(device.Permissions.ToFlags());
        SetPermissionControlsEnabled(device.RemoteAccessEnabled);
        _statusLabel.Text = device.RemoteAccessEnabled
            ? "Änderungen gelten sofort für neue Befehle."
            : "Remote-Zugriff ist für dieses Gerät pausiert.";
    }

    private void DeviceSelectionChanged(object? sender, EventArgs eventArgs)
    {
        if (_loading)
        {
            return;
        }

        _loading = true;
        try
        {
            PopulateSelectedDevice();
        }
        finally
        {
            _loading = false;
        }
    }

    private void SettingsChanged(object? sender, EventArgs eventArgs)
    {
        if (_loading)
        {
            return;
        }

        SetPermissionControlsEnabled(_remoteAccessCheckBox.Checked);
        _statusLabel.Text = "Nicht gespeicherte Änderungen.";
    }

    private void SaveButtonClicked(object? sender, EventArgs eventArgs)
    {
        var choice = SelectedChoice;
        if (choice is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_deviceNameTextBox.Text))
        {
            _statusLabel.Text = "Bitte einen Gerätenamen eingeben.";
            _deviceNameTextBox.Focus();
            return;
        }

        var saved = _devices.UpdateDevice(
            choice.DeviceId,
            _deviceNameTextBox.Text,
            _remoteAccessCheckBox.Checked,
            GetPermissionFlags());
        if (!saved)
        {
            _statusLabel.Text = "Das Gerät wurde nicht mehr gefunden.";
            LoadDevices();
            return;
        }

        LoadDevices(choice.DeviceId);
        var identity = _devices.GetAuditIdentity(choice.DeviceId);
        _activityLog.Record(
            identity.DeviceName,
            identity.Platform,
            "Gerätefreigaben geändert",
            ActivityLogResult.Success);
        _statusLabel.Text = "Gerät und Berechtigungen wurden gespeichert.";
    }

    private void RemoveButtonClicked(object? sender, EventArgs eventArgs)
    {
        var choice = SelectedChoice;
        if (choice is null)
        {
            return;
        }

        var confirmation = NexusDialog.Confirm(
            this,
            $"Soll „{choice.Device.DeviceName}“ wirklich entfernt werden? Das Smartphone muss danach erneut per QR- oder Pairing-Code gekoppelt werden.",
            "Gerätefreigabe entfernen",
            NexusDialogKind.Warning,
            "Entfernen");
        if (confirmation != DialogResult.OK)
        {
            return;
        }

        var identity = _devices.GetAuditIdentity(choice.DeviceId);
        if (!_devices.RemoveDevice(choice.DeviceId))
        {
            _activityLog.Record(
                identity.DeviceName,
                identity.Platform,
                "Gerätefreigabe entfernen",
                ActivityLogResult.Failed);
            LoadDevices();
            _statusLabel.Text = "Das Gerät wurde nicht mehr gefunden.";
            return;
        }
        _activityLog.Record(
            identity.DeviceName,
            identity.Platform,
            "Gerätefreigabe entfernt",
            ActivityLogResult.Success);
        LoadDevices();
        _statusLabel.Text = "Gerätefreigabe wurde entfernt.";
    }

    private void RefreshTimerTick(object? sender, EventArgs eventArgs)
    {
        var devices = _devices.ListDevices();
        if (devices.Count != _knownDeviceCount)
        {
            LoadDevices();
            return;
        }

        var choice = SelectedChoice;
        if (choice is null)
        {
            return;
        }

        var latest = devices.FirstOrDefault(device => string.Equals(
            device.DeviceId,
            choice.DeviceId,
            StringComparison.Ordinal));
        if (latest is not null)
        {
            choice.Device = latest;
            _deviceInfoLabel.Text = FormatDeviceInfo(latest);
            _deviceInfoLabel.ForeColor = latest.IsOnline
                ? WinFormsTheme.Success
                : WinFormsTheme.TextMuted;
            _deviceComboBox.Invalidate();
        }
    }

    private DevicePermission GetPermissionFlags()
    {
        var permissions = DevicePermission.None;
        if (_systemControlCheckBox.Checked)
        {
            permissions |= DevicePermission.SystemControl;
        }
        if (_touchpadCheckBox.Checked)
        {
            permissions |= DevicePermission.Touchpad;
        }
        if (_processesCheckBox.Checked)
        {
            permissions |= DevicePermission.Processes;
        }
        if (_mediaCheckBox.Checked)
        {
            permissions |= DevicePermission.Media;
        }
        if (_screenCheckBox.Checked)
        {
            permissions |= DevicePermission.Screen;
        }
        if (_filesCheckBox.Checked)
        {
            permissions |= DevicePermission.Files;
        }
        if (_powerCheckBox.Checked)
        {
            permissions |= DevicePermission.Power;
        }

        return permissions;
    }

    private void SetPermissionFlags(DevicePermission permissions)
    {
        _systemControlCheckBox.Checked = permissions.HasFlag(
            DevicePermission.SystemControl);
        _touchpadCheckBox.Checked = permissions.HasFlag(
            DevicePermission.Touchpad);
        _processesCheckBox.Checked = permissions.HasFlag(
            DevicePermission.Processes);
        _mediaCheckBox.Checked = permissions.HasFlag(DevicePermission.Media);
        _screenCheckBox.Checked = permissions.HasFlag(DevicePermission.Screen);
        _filesCheckBox.Checked = permissions.HasFlag(DevicePermission.Files);
        _powerCheckBox.Checked = permissions.HasFlag(DevicePermission.Power);
    }

    private void SetPermissionControlsEnabled(bool enabled)
    {
        _systemControlCheckBox.Enabled = enabled;
        _touchpadCheckBox.Enabled = enabled;
        _processesCheckBox.Enabled = enabled;
        _mediaCheckBox.Enabled = enabled;
        _screenCheckBox.Enabled = enabled;
        _filesCheckBox.Enabled = enabled;
        _powerCheckBox.Enabled = enabled;
    }

    private static string FormatDeviceInfo(TrustedDeviceInfo device)
    {
        var state = !device.RemoteAccessEnabled
            ? "Pausiert"
            : device.IsOnline
                ? "Online"
                : "Offline";
        var lastSeen = device.IsOnline
            ? "jetzt verbunden"
            : device.LastSeenAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss");
        return $"{device.Platform}  ·  {state}  ·  Zuletzt: {lastSeen}";
    }

    private sealed class DeviceChoice
    {
        public DeviceChoice(TrustedDeviceInfo device)
        {
            Device = device;
        }

        public TrustedDeviceInfo Device { get; set; }
        public string DeviceId => Device.DeviceId;

        public override string ToString()
        {
            var state = !Device.RemoteAccessEnabled
                ? "Pausiert"
                : Device.IsOnline
                    ? "Online"
                    : "Offline";
            return $"{Device.DeviceName} — {state}";
        }
    }
}
