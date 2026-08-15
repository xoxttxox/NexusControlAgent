#nullable enable

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using NexusControl.Agent.Localization;
using NexusControl.Agent.Services;
using NexusControl.Agent.UI;

namespace NexusControl.Agent.Forms;

/// <summary>
/// Shows the bounded local activity log in a separate compact window without
/// changing the main Agent layout.
/// </summary>
[DesignerCategory("Form")]
internal sealed partial class ActivityLogDialog : Form
{
    private readonly ActivityLogService _activityLog;
    private long _lastRevision = -1;

    /// <summary>
    /// Constructor used exclusively by the Visual Studio WinForms designer.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ActivityLogDialog()
    {
        _activityLog = null!;
        InitializeComponent();
        LocalizationService.Apply(this, nameof(ActivityLogDialog));
        WinFormsTheme.Apply(this);
    }

    public ActivityLogDialog(ActivityLogService activityLog)
    {
        _activityLog = activityLog;
        InitializeComponent();
        LocalizationService.Apply(this, nameof(ActivityLogDialog));
        WinFormsTheme.Apply(this);
    }

    private void DialogShown(object? sender, EventArgs eventArgs)
    {
        RefreshEntries(force: true);
        _refreshTimer.Start();
    }

    private void RefreshTimerTick(object? sender, EventArgs eventArgs) =>
        RefreshEntries();

    private void CopyButtonClicked(object? sender, EventArgs eventArgs) =>
        CopyReport();

    private void ClearButtonClicked(object? sender, EventArgs eventArgs) =>
        ClearLog();

    private void RefreshEntries(bool force = false)
    {
        var revision = _activityLog.Revision;
        if (!force && revision == _lastRevision)
        {
            return;
        }

        var entries = _activityLog.ReadRecent(100);
        _entriesListBox.BeginUpdate();
        try
        {
            _entriesListBox.Items.Clear();
            if (entries.Count == 0)
            {
                _entriesListBox.Items.Add(LocalizationService.Text(
                    "ActivityLogDialog.NoActivity"));
            }
            else
            {
                foreach (var entry in entries)
                {
                    _entriesListBox.Items.Add(FormatEntry(entry));
                }
            }
        }
        finally
        {
            _entriesListBox.EndUpdate();
        }

        _statusLabel.Text = entries.Count switch
        {
            0 => LocalizationService.Text(
                "ActivityLogDialog.Status.None"),
            1 => LocalizationService.Text(
                "ActivityLogDialog.Status.One"),
            _ => LocalizationService.Format(
                "ActivityLogDialog.Status.Many",
                entries.Count),
        };
        _lastRevision = revision;
    }

    private void CopyReport()
    {
        try
        {
            Clipboard.SetText(_activityLog.BuildReport());
            _statusLabel.Text = LocalizationService.Text(
                "ActivityLogDialog.Copied");
        }
        catch (ExternalException)
        {
            NexusDialog.Show(
                this,
                LocalizationService.Text("ActivityLogDialog.CopyFailed"),
                LocalizationService.Text("ActivityLogDialog.Title"),
                NexusDialogKind.Information);
        }
    }

    private void ClearLog()
    {
        var result = NexusDialog.Confirm(
            this,
            LocalizationService.Text("ActivityLogDialog.ClearPrompt"),
            LocalizationService.Text("ActivityLogDialog.ClearTitle"),
            NexusDialogKind.Warning,
            LocalizationService.Text("ActivityLogDialog.clearButton"));
        if (result != DialogResult.OK)
        {
            return;
        }

        if (!_activityLog.Clear())
        {
            NexusDialog.Show(
                this,
                LocalizationService.Text("ActivityLogDialog.ClearFailed"),
                LocalizationService.Text("ActivityLogDialog.Title"),
                NexusDialogKind.Warning);
            return;
        }

        RefreshEntries(force: true);
    }

    private static string FormatEntry(ActivityLogEntry entry)
    {
        var deviceName = entry.DeviceName[
            ..Math.Min(entry.DeviceName.Length, 18)];
        var displayAction = ActivityLogService.DisplayAction(entry.Action);
        var action = displayAction[
            ..Math.Min(displayAction.Length, 36)];
        var timestamp = entry.Timestamp
            .ToLocalTime()
            .ToString("g", LocalizationService.CurrentCulture);
        return $"{timestamp}  ·  {deviceName}  ·  {action}  ·  "
            + ActivityLogService.ResultText(entry.Result);
    }
}
