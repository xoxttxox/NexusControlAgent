using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using NexusControl.Agent.Services;
using NexusControl.Agent.Localization;
using NexusControl.Agent.UI;

namespace NexusControl.Agent.Forms;

/// <summary>
/// Zeigt die lokalen Verbindungstests im gleichen kompakten Dark-WinForms-Stil
/// wie das Agent-Hauptfenster. Es werden keine geheimen Daten dargestellt.
/// </summary>
[DesignerCategory("Form")]
internal sealed partial class ConnectionDiagnosticsDialog : Form
{
    private readonly ConnectionDiagnosticsService _diagnostics;
    private CancellationTokenSource? _runCancellation;
    private string? _report;

    /// <summary>
    /// Konstruktor ausschließlich für den Visual-Studio-WinForms-Designer.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ConnectionDiagnosticsDialog()
    {
        _diagnostics = null!;
        InitializeComponent();
        LocalizationService.Apply(
            this,
            nameof(ConnectionDiagnosticsDialog));
        WinFormsTheme.Apply(this);
    }

    public ConnectionDiagnosticsDialog(
        ConnectionDiagnosticsService diagnostics)
    {
        _diagnostics = diagnostics;
        InitializeComponent();
        LocalizationService.Apply(
            this,
            nameof(ConnectionDiagnosticsDialog));
        WinFormsTheme.Apply(this);
    }

    private void DialogFormClosed(object? sender, FormClosedEventArgs eventArgs) =>
        _runCancellation?.Cancel();

    private async void DialogShown(object? sender, EventArgs eventArgs)
    {
        await RunDiagnosticsAsync();
    }

    private async void RunButtonClicked(object? sender, EventArgs eventArgs)
    {
        await RunDiagnosticsAsync();
    }

    private async Task RunDiagnosticsAsync()
    {
        _runCancellation?.Cancel();
        _runCancellation?.Dispose();
        _runCancellation = new CancellationTokenSource();
        var cancellationToken = _runCancellation.Token;

        _runButton.Enabled = false;
        _copyButton.Enabled = false;
        _report = null;
        _summaryLabel.Text = LocalizationService.Text(
            "ConnectionDiagnosticsDialog.Checking");
        _summaryLabel.ForeColor = WinFormsTheme.TextMuted;
        ShowProgressRows();

        try
        {
            var snapshot = await _diagnostics.RunAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested || IsDisposed)
            {
                return;
            }

            _report = snapshot.Report;
            _summaryLabel.Text = snapshot.Summary;
            _summaryLabel.ForeColor = snapshot.Items.Any(item =>
                item.State == ConnectionDiagnosticState.Error)
                ? WinFormsTheme.Error
                : snapshot.Items.Any(item =>
                    item.State == ConnectionDiagnosticState.Warning)
                    ? WinFormsTheme.Warning
                    : WinFormsTheme.Success;
            ShowResults(snapshot.Items);
            _copyButton.Enabled = true;
        }
        catch (OperationCanceledException)
        {
            // Beim Schließen oder einer erneuten Prüfung ist kein Hinweis nötig.
        }
        catch (Exception error)
        {
            _summaryLabel.Text = LocalizationService.Format(
                "ConnectionDiagnosticsDialog.Failed",
                error.Message);
            _summaryLabel.ForeColor = WinFormsTheme.Error;
        }
        finally
        {
            if (!IsDisposed)
            {
                _runButton.Enabled = true;
            }
        }
    }

    private void ShowProgressRows()
    {
        _resultsLayout.SuspendLayout();
        ClearResultControls();
        for (var row = 0; row < 6; row++)
        {
            var placeholder = new Label
            {
                Dock = DockStyle.Fill,
                Text = row == 0
                    ? LocalizationService.Text(
                        "ConnectionDiagnosticsDialog.Running")
                    : "",
                Tag = "muted",
                TextAlign = ContentAlignment.MiddleLeft,
            };
            _resultsLayout.SetColumnSpan(placeholder, 2);
            _resultsLayout.Controls.Add(placeholder, 0, row);
        }
        _resultsLayout.ResumeLayout();
    }

    private void ShowResults(IReadOnlyList<ConnectionDiagnosticItem> items)
    {
        _resultsLayout.SuspendLayout();
        ClearResultControls();
        for (var row = 0; row < 6; row++)
        {
            var item = items[row];
            var color = StateColor(item.State);
            var stateLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = $"●  {item.Name}",
                ForeColor = color,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0),
            };
            var messageLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                Text = item.Message,
                ForeColor = WinFormsTheme.TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(4, 0, 0, 0),
            };
            _resultsLayout.Controls.Add(stateLabel, 0, row);
            _resultsLayout.Controls.Add(messageLabel, 1, row);
        }
        _resultsLayout.ResumeLayout();
    }

    private void ClearResultControls()
    {
        while (_resultsLayout.Controls.Count > 0)
        {
            var control = _resultsLayout.Controls[0];
            _resultsLayout.Controls.RemoveAt(0);
            control.Dispose();
        }
    }

    private void CopyButtonClicked(object? sender, EventArgs eventArgs)
    {
        if (string.IsNullOrWhiteSpace(_report))
        {
            return;
        }

        try
        {
            Clipboard.SetText(_report);
            _summaryLabel.Text = LocalizationService.Text(
                "ConnectionDiagnosticsDialog.ReportCopied");
        }
        catch (ExternalException)
        {
            NexusDialog.Show(
                this,
                LocalizationService.Text("Common.ClipboardUnavailable"),
                LocalizationService.Text(
                    "ConnectionDiagnosticsDialog.Title"),
                NexusDialogKind.Information);
        }
    }

    private static Color StateColor(ConnectionDiagnosticState state) =>
        state switch
        {
            ConnectionDiagnosticState.Success => WinFormsTheme.Success,
            ConnectionDiagnosticState.Warning => WinFormsTheme.Warning,
            ConnectionDiagnosticState.Error => WinFormsTheme.Error,
            _ => WinFormsTheme.TextMuted,
        };
}
