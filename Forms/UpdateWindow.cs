using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using NexusControl.Agent.Models;
using NexusControl.Agent.Services;
using NexusControl.Agent.UI;

namespace NexusControl.Agent.Forms;

[DesignerCategory("Form")]
internal sealed partial class UpdateWindow : Form
{
    private readonly UpdateService _updates;
    private readonly bool _forceCheck;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public UpdateWindow()
    {
        _updates = null!;
        InitializeComponent();
        WinFormsTheme.Apply(this);
    }

    public UpdateWindow(UpdateService updates, bool forceCheck)
    {
        _updates = updates;
        _forceCheck = forceCheck;
        InitializeComponent();
        TrySetApplicationIcon();
        WinFormsTheme.Apply(this);
        _updates.SnapshotChanged += UpdateSnapshotChanged;
        ApplySnapshot(_updates.Snapshot);
    }

    protected override async void OnShown(EventArgs eventArgs)
    {
        base.OnShown(eventArgs);
        if (_forceCheck
            || _updates.Snapshot.Stage is UpdateStage.Idle
                or UpdateStage.Error)
        {
            await _updates.CheckNowAsync();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs eventArgs)
    {
        if (_updates is not null
            && eventArgs.CloseReason == CloseReason.UserClosing
            && _updates.Snapshot.Stage is (
                UpdateStage.Downloading
                or UpdateStage.Verifying
                or UpdateStage.Installing))
        {
            eventArgs.Cancel = true;
            return;
        }

        base.OnFormClosing(eventArgs);
    }

    protected override void OnFormClosed(FormClosedEventArgs eventArgs)
    {
        if (_updates is not null)
        {
            _updates.SnapshotChanged -= UpdateSnapshotChanged;
        }

        base.OnFormClosed(eventArgs);
    }

    private async void ActionButtonClick(object? sender, EventArgs eventArgs)
    {
        if (_updates.Snapshot.Stage == UpdateStage.Available)
        {
            await _updates.DownloadAndInstallAsync();
            return;
        }

        await _updates.CheckNowAsync();
    }

    private void ReleaseLinkClicked(
        object? sender,
        LinkLabelLinkClickedEventArgs eventArgs)
    {
        var release = _updates.Snapshot.Release;
        if (release is null)
        {
            return;
        }

        try
        {
            _ = Process.Start(new ProcessStartInfo
            {
                FileName = release.ReleasePage.AbsoluteUri,
                UseShellExecute = true,
            });
        }
        catch
        {
            NexusDialog.Show(
                this,
                "Die GitHub-Release-Seite konnte nicht geöffnet werden.",
                "Nexus Control Updater",
                NexusDialogKind.Warning);
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
            BeginInvoke(new Action<UpdateSnapshot>(UpdateSnapshotChanged), snapshot);
            return;
        }

        ApplySnapshot(snapshot);
    }

    private void ApplySnapshot(UpdateSnapshot snapshot)
    {
        installedVersionLabel.Text =
            $"Installiert: {TelemetryService.AgentVersion}";
        releaseLinkLabel.Visible = snapshot.Release is not null;
        progressBar.MarqueeAnimationSpeed = 0;
        progressBar.Style = ProgressBarStyle.Continuous;
        progressBar.Value = Math.Clamp(snapshot.ProgressPercent, 0, 100);
        closeButton.Enabled = snapshot.Stage is not (
            UpdateStage.Downloading
            or UpdateStage.Verifying
            or UpdateStage.Installing);
        actionButton.Enabled = !snapshot.IsBusy;

        switch (snapshot.Stage)
        {
            case UpdateStage.Disabled:
                titleLabel.Text = "Updates sind deaktiviert";
                versionLabel.Text = "Keine automatische Prüfung";
                notesTextBox.Text =
                    "Aktiviere den Updater in appsettings.json, wenn der Agent GitHub Releases automatisch prüfen soll.";
                actionButton.Text = "Erneut prüfen";
                actionButton.Enabled = false;
                break;

            case UpdateStage.NotConfigured:
                titleLabel.Text = "Updater einrichten";
                versionLabel.Text = "GitHub-Repository fehlt";
                notesTextBox.Text =
                    "Trage deinen GitHub-Benutzernamen unter Updates:RepositoryOwner in appsettings.json ein. Danach kann Nexus Control veröffentlichte Releases automatisch erkennen.";
                actionButton.Text = "Noch nicht bereit";
                actionButton.Enabled = false;
                break;

            case UpdateStage.Checking:
                titleLabel.Text = "Suche nach Updates";
                versionLabel.Text = "GitHub Releases werden geprüft …";
                notesTextBox.Text =
                    "Die installierte Version wird mit dem neuesten veröffentlichten Release verglichen.";
                SetMarqueeProgress();
                actionButton.Text = "Prüfe …";
                break;

            case UpdateStage.UpToDate:
                titleLabel.Text = "Nexus Control ist aktuell";
                versionLabel.Text = snapshot.Message;
                notesTextBox.Text =
                    "Es ist keine neuere stabile Version verfügbar. Der Agent prüft später automatisch erneut.";
                progressBar.Value = 100;
                actionButton.Text = "Erneut prüfen";
                break;

            case UpdateStage.Available:
                titleLabel.Text = "Update verfügbar";
                versionLabel.Text =
                    $"{TelemetryService.AgentVersion}  →  {snapshot.Release!.DisplayVersion}";
                notesTextBox.Text = FormatReleaseNotes(snapshot.Release);
                statusLabel.Text = FormatDownloadSize(snapshot.Release);
                actionButton.Text = "Update installieren";
                actionButton.Enabled = true;
                break;

            case UpdateStage.Downloading:
                titleLabel.Text = "Update wird heruntergeladen";
                versionLabel.Text =
                    $"Version {snapshot.Release?.DisplayVersion}";
                notesTextBox.Text =
                    "Der MSI-Installer wird in den geschützten lokalen Update-Ordner geladen. Danach wird seine SHA-256-Prüfsumme kontrolliert.";
                actionButton.Text = $"{snapshot.ProgressPercent}%";
                break;

            case UpdateStage.Verifying:
                titleLabel.Text = "Update wird überprüft";
                versionLabel.Text =
                    $"Version {snapshot.Release?.DisplayVersion}";
                notesTextBox.Text =
                    "Nexus Control überprüft Dateigröße, SHA-256-Prüfsumme und – falls aktiviert – die digitale Herausgebersignatur.";
                SetMarqueeProgress();
                actionButton.Text = "Überprüfe …";
                break;

            case UpdateStage.Installing:
                titleLabel.Text = "Installation wird gestartet";
                versionLabel.Text =
                    $"Version {snapshot.Release?.DisplayVersion}";
                notesTextBox.Text =
                    "Der Agent beendet sich jetzt. Der Update-Helfer wartet auf die Freigabe der Dateien, führt das MSI-Upgrade aus und startet Nexus Control danach erneut.";
                progressBar.Value = 100;
                actionButton.Text = "Installiere …";
                break;

            case UpdateStage.Error:
                titleLabel.Text = "Update nicht möglich";
                versionLabel.Text = snapshot.Message;
                notesTextBox.Text = snapshot.Error
                    ?? "Bei der Updateprüfung ist ein unbekannter Fehler aufgetreten.";
                actionButton.Text = "Erneut prüfen";
                actionButton.Enabled = true;
                break;

            default:
                titleLabel.Text = "Nexus Control Updates";
                versionLabel.Text = "Bereit für die Updateprüfung";
                notesTextBox.Text =
                    "Nexus Control kann neue Versionen direkt über GitHub Releases finden und als geprüftes MSI-Upgrade installieren.";
                actionButton.Text = "Jetzt prüfen";
                break;
        }

        if (snapshot.Stage != UpdateStage.Available)
        {
            statusLabel.Text = snapshot.Message;
        }
    }

    private void SetMarqueeProgress()
    {
        progressBar.Style = ProgressBarStyle.Marquee;
        progressBar.MarqueeAnimationSpeed = 25;
    }

    private static string FormatReleaseNotes(UpdateRelease release)
    {
        var notes = release.Notes?.Trim();
        if (string.IsNullOrWhiteSpace(notes))
        {
            return $"{release.Name} steht zur Installation bereit.";
        }

        const int maximumLength = 900;
        return notes.Length <= maximumLength
            ? notes
            : notes[..maximumLength].TrimEnd() + " …";
    }

    private static string FormatDownloadSize(UpdateRelease release)
    {
        var sizeMegabytes = release.InstallerSizeBytes / 1024D / 1024D;
        var published = release.PublishedAt is null
            ? string.Empty
            : $"  ·  {release.PublishedAt.Value.ToLocalTime():dd.MM.yyyy}";
        return $"Download: {sizeMegabytes:0.0} MB{published}  ·  SHA-256 wird geprüft";
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
