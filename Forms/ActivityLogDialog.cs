using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using NexusControl.Agent.Services;
using NexusControl.Agent.UI;

namespace NexusControl.Agent.Forms;

/// <summary>
/// Zeigt das begrenzte lokale Aktivitätsprotokoll in einem eigenen kleinen
/// Fenster, ohne die kompakte Hauptansicht zu verändern.
/// </summary>
[DesignerCategory("Form")]
internal sealed partial class ActivityLogDialog : Form
{
    private readonly ActivityLogService _activityLog;
    private long _lastRevision = -1;

    /// <summary>
    /// Konstruktor ausschließlich für den Visual-Studio-WinForms-Designer.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ActivityLogDialog()
    {
        _activityLog = null!;
        InitializeComponent();
        WinFormsTheme.Apply(this);
    }

    public ActivityLogDialog(ActivityLogService activityLog)
    {
        _activityLog = activityLog;
        InitializeComponent();
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
                _entriesListBox.Items.Add(
                    "Noch keine Verbindungen oder Aktionen protokolliert.");
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
            0 => "Keine Einträge",
            1 => "1 Eintrag · nur lokal",
            _ => $"{entries.Count} Einträge · nur lokal",
        };
        _lastRevision = revision;
    }

    private void CopyReport()
    {
        try
        {
            Clipboard.SetText(_activityLog.BuildReport());
            _statusLabel.Text = "Protokoll wurde kopiert.";
        }
        catch (ExternalException)
        {
            NexusDialog.Show(
                this,
                "Windows konnte die Zwischenablage gerade nicht öffnen. Bitte versuche es erneut.",
                "Nexus Control Protokoll",
                NexusDialogKind.Information);
        }
    }

    private void ClearLog()
    {
        var result = NexusDialog.Confirm(
            this,
            "Soll das lokale Aktivitätsprotokoll wirklich vollständig geleert werden?",
            "Protokoll leeren",
            NexusDialogKind.Warning,
            "Leeren");
        if (result != DialogResult.OK)
        {
            return;
        }

        if (!_activityLog.Clear())
        {
            NexusDialog.Show(
                this,
                "Das Protokoll konnte nicht geleert werden.",
                "Nexus Control Protokoll",
                NexusDialogKind.Warning);
            return;
        }

        RefreshEntries(force: true);
    }

    private static string FormatEntry(ActivityLogEntry entry)
    {
        var deviceName = entry.DeviceName[
            ..Math.Min(entry.DeviceName.Length, 18)];
        var action = entry.Action[
            ..Math.Min(entry.Action.Length, 36)];
        return $"{entry.Timestamp.ToLocalTime():dd.MM. HH:mm:ss}  ·  "
            + $"{deviceName}  ·  {action}  ·  "
            + ActivityLogService.ResultText(entry.Result);
    }
}
