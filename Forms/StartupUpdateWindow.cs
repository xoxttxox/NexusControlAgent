using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using NexusControl.Agent.Configuration;
using NexusControl.Agent.Models;
using NexusControl.Agent.Services;
using NexusControl.Agent.UI;

namespace NexusControl.Agent.Forms;

/// <summary>
/// Kleine vorgeschaltete Update-Oberfläche. Sie prüft GitHub, bevor Server,
/// Tray-Icon und Hauptfenster gestartet werden, und blockiert den Agent bei
/// fehlender Internetverbindung nur für das konfigurierte kurze Zeitlimit.
/// </summary>
[DesignerCategory("Form")]
internal sealed partial class StartupUpdateWindow : Form
{
    private readonly UpdateService _updates;
    private readonly UpdateOptions _options;
    private readonly CancellationTokenSource _lifetime = new();
    private bool _workflowStarted;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public StartupUpdateWindow()
    {
        _updates = null!;
        _options = new UpdateOptions();
        InitializeComponent();
        WinFormsTheme.Apply(this);
    }

    public StartupUpdateWindow(
        UpdateService updates,
        UpdateOptions options)
    {
        _updates = updates;
        _options = options;
        InitializeComponent();
        TrySetApplicationIcon();
        WinFormsTheme.Apply(this);
        installedVersionLabel.Text =
            $"Version {TelemetryService.AgentVersion}";
        _updates.SnapshotChanged += UpdateSnapshotChanged;
        ApplySnapshot(_updates.Snapshot);
    }

    internal bool InstallationStarted { get; private set; }

    protected override async void OnShown(EventArgs eventArgs)
    {
        base.OnShown(eventArgs);
        if (_workflowStarted)
        {
            return;
        }

        _workflowStarted = true;
        try
        {
            await RunStartupUpdateAsync();
        }
        catch (OperationCanceledException)
        {
            if (!_lifetime.IsCancellationRequested)
            {
                await ContinueWithoutUpdateAsync(
                    "Updateprüfung nicht erreichbar",
                    "Der Agent wird ohne Updateprüfung gestartet …",
                    900);
            }
        }
        catch (Exception)
        {
            await ContinueWithoutUpdateAsync(
                "Updateprüfung nicht möglich",
                "Der Agent wird trotzdem gestartet …",
                1200);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs eventArgs)
    {
        _lifetime.Cancel();
        base.OnFormClosing(eventArgs);
    }

    protected override void OnFormClosed(FormClosedEventArgs eventArgs)
    {
        if (_updates is not null)
        {
            _updates.SnapshotChanged -= UpdateSnapshotChanged;
        }

        _lifetime.Dispose();
        base.OnFormClosed(eventArgs);
    }

    private async Task RunStartupUpdateAsync()
    {
        if (!_options.Enabled)
        {
            await ContinueWithoutUpdateAsync(
                "Updates sind deaktiviert",
                "Nexus Control Agent wird gestartet …",
                500);
            return;
        }

        if (!_options.IsConfigured)
        {
            await ContinueWithoutUpdateAsync(
                "Updateprüfung übersprungen",
                "Das GitHub-Repository ist noch nicht eingerichtet.",
                900);
            return;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
            _lifetime.Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(
            _options.StartupCheckTimeoutSeconds));
        await _updates.CheckNowAsync(timeout.Token);

        var snapshot = _updates.Snapshot;
        if (snapshot.Stage == UpdateStage.Available
            && _options.AutomaticInstallOnStartup)
        {
            await _updates.DownloadAndInstallAsync(_lifetime.Token);
            snapshot = _updates.Snapshot;
            if (snapshot.Stage == UpdateStage.Installing)
            {
                InstallationStarted = true;
                statusTitleLabel.Text = "Update wird installiert";
                detailLabel.Text =
                    "Der Agent wird geschlossen und danach neu gestartet …";
                SetProgress(100);
                await DelaySafelyAsync(450);
                Close();
                return;
            }
        }

        switch (snapshot.Stage)
        {
            case UpdateStage.UpToDate:
                statusTitleLabel.Text = "Nexus Control ist aktuell";
                detailLabel.Text = "Agent wird gestartet …";
                SetProgress(100);
                await DelaySafelyAsync(650);
                Close();
                break;

            case UpdateStage.Available:
                statusTitleLabel.Text =
                    $"Update {snapshot.Release?.DisplayVersion} verfügbar";
                detailLabel.Text =
                    "Der Agent wird gestartet. Das Update kann im Agent installiert werden.";
                SetProgress(100);
                await DelaySafelyAsync(1100);
                Close();
                break;

            case UpdateStage.Error:
                await ContinueWithoutUpdateAsync(
                    "Updateprüfung nicht möglich",
                    "Der Agent wird trotzdem gestartet …",
                    1200);
                break;

            default:
                await ContinueWithoutUpdateAsync(
                    "Updateprüfung abgeschlossen",
                    "Nexus Control Agent wird gestartet …",
                    650);
                break;
        }
    }

    private async Task ContinueWithoutUpdateAsync(
        string title,
        string detail,
        int delayMilliseconds)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        statusTitleLabel.Text = title;
        detailLabel.Text = detail;
        SetProgress(0);
        await DelaySafelyAsync(delayMilliseconds);
        if (!IsDisposed && !Disposing)
        {
            Close();
        }
    }

    private async Task DelaySafelyAsync(int milliseconds)
    {
        try
        {
            await Task.Delay(milliseconds, _lifetime.Token);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            // Das Fenster oder Windows wird gerade beendet.
        }
    }

    private void UpdateSnapshotChanged(UpdateSnapshot snapshot)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(
                new Action<UpdateSnapshot>(UpdateSnapshotChanged),
                snapshot);
            return;
        }

        ApplySnapshot(snapshot);
    }

    private void ApplySnapshot(UpdateSnapshot snapshot)
    {
        switch (snapshot.Stage)
        {
            case UpdateStage.Checking:
                statusTitleLabel.Text = "Suche nach Updates …";
                detailLabel.Text =
                    "Die installierte Version wird mit GitHub verglichen.";
                SetMarqueeProgress();
                break;

            case UpdateStage.Available:
                statusTitleLabel.Text =
                    $"Update {snapshot.Release?.DisplayVersion} gefunden";
                detailLabel.Text = "Download wird vorbereitet …";
                SetMarqueeProgress();
                break;

            case UpdateStage.Downloading:
                statusTitleLabel.Text = "Update wird heruntergeladen …";
                detailLabel.Text = $"Fortschritt: {snapshot.ProgressPercent}%";
                SetProgress(snapshot.ProgressPercent);
                break;

            case UpdateStage.Verifying:
                statusTitleLabel.Text = "Update wird überprüft …";
                detailLabel.Text = "Prüfsumme und Installer werden kontrolliert.";
                SetMarqueeProgress();
                break;

            case UpdateStage.Installing:
                statusTitleLabel.Text = "Update wird installiert …";
                detailLabel.Text = "Nexus Control startet danach automatisch neu.";
                SetProgress(100);
                break;

            case UpdateStage.UpToDate:
                statusTitleLabel.Text = "Nexus Control ist aktuell";
                detailLabel.Text = snapshot.Message;
                SetProgress(100);
                break;

            case UpdateStage.Error:
                statusTitleLabel.Text = "Updateprüfung nicht möglich";
                detailLabel.Text = "Der Agent wird trotzdem gestartet …";
                SetProgress(0);
                break;

            default:
                statusTitleLabel.Text = "Suche nach Updates …";
                detailLabel.Text = "Bitte einen Moment warten.";
                SetMarqueeProgress();
                break;
        }
    }

    private void SetMarqueeProgress()
    {
        progressBar.Style = ProgressBarStyle.Marquee;
        progressBar.MarqueeAnimationSpeed = 22;
    }

    private void SetProgress(int value)
    {
        progressBar.MarqueeAnimationSpeed = 0;
        progressBar.Style = ProgressBarStyle.Continuous;
        progressBar.Value = Math.Clamp(value, 0, 100);
    }

    private void TrySetApplicationIcon()
    {
        try
        {
            var executablePath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                Icon = Icon.ExtractAssociatedIcon(executablePath)
                    ?? SystemIcons.Application;
            }
        }
        catch
        {
            Icon = SystemIcons.Application;
        }
    }
}
