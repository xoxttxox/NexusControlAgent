using NexusControl.Agent.Configuration;
using NexusControl.Agent.Forms;
using NexusControl.Agent.Localization;
using NexusControl.Agent.Pairing;
using NexusControl.Agent.Services;
using NexusControl.Agent.UI;
using Drawing = global::System.Drawing;
using WinForms = global::System.Windows.Forms;

namespace NexusControl.Agent.Application;

/// <summary>
/// Verbindet das kompakte WinForms-Hauptfenster mit dem Windows-Infobereich.
/// Der Agent bleibt ein einzelner Prozess und läuft beim Ausblenden weiter.
/// </summary>
internal sealed class AgentApplicationContext : WinForms.ApplicationContext
{
    private readonly AgentWindow _window;
    private readonly WinForms.NotifyIcon _trayIcon;
    private readonly Drawing.Icon _trayIconImage;
    private readonly AutoStartService _autoStart;
    private readonly FirstRunService _firstRun;
    private readonly WinForms.ToolStripMenuItem _openMenuItem;
    private readonly WinForms.ToolStripMenuItem _rotateMenuItem;
    private readonly WinForms.ToolStripMenuItem _autoStartMenuItem;
    private readonly WinForms.ToolStripMenuItem _exitMenuItem;
    private bool _exiting;
    private bool _minimizeHintShown;
    private bool _updatingAutoStart;
    private bool _showingFirstRun;
    private Action? _balloonClickAction;

    public AgentApplicationContext(
        PairingService pairing,
        DeviceStore devices,
        ActivityLogService activityLog,
        AgentOptions options,
        bool startInTray)
    {
        _autoStart = new AutoStartService();
        _firstRun = new FirstRunService();
        var firewall = new FirewallService(options.Port);
        _window = new AgentWindow(
            pairing,
            devices,
            options,
            _autoStart,
            firewall,
            activityLog);

        // Do not register the Agent window as ApplicationContext.MainForm.
        // WinForms automatically makes MainForm visible when Application.Run
        // starts its message loop, which previously undid the hidden --tray
        // state. The context itself owns the lifetime and exits explicitly via
        // ExitThread(), so the Agent can run with no visible top-level window.
        _window.ShowInTaskbar = !startInTray;

        // Der unsichtbare Handle macht InvokeRequired auch beim Tray-Start
        // zuverlässig, ohne das Hauptfenster kurz aufblitzen zu lassen.
        _ = _window.Handle;

        var menu = CreateContextMenu();
        _openMenuItem = new WinForms.ToolStripMenuItem(
            LocalizationService.Text("Tray.Open"),
            null,
            (_, _) => ShowWindow())
        {
            Font = new Drawing.Font(
                "Segoe UI Semibold",
                9F,
                Drawing.FontStyle.Bold),
        };
        _rotateMenuItem = new WinForms.ToolStripMenuItem(
            LocalizationService.Text("Tray.NewPairingCode"),
            null,
            (_, _) =>
            {
                _window.RotatePairingCode();
                ShowWindow();
            });
        _autoStartMenuItem = new WinForms.ToolStripMenuItem(
            LocalizationService.Text("Tray.StartWithWindows"))
        {
            CheckOnClick = true,
        };
        _autoStartMenuItem.Click += AutoStartMenuItemClicked;
        menu.Opening += (_, _) =>
        {
            if (!_updatingAutoStart)
            {
                _autoStartMenuItem.Checked = _autoStart.IsEnabled();
            }
        };

        _exitMenuItem = new WinForms.ToolStripMenuItem(
            LocalizationService.Text("Tray.Exit"),
            null,
            (_, _) => ExitAgent());

        menu.Items.Add(_openMenuItem);
        menu.Items.Add(_rotateMenuItem);
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(_autoStartMenuItem);
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(_exitMenuItem);

        foreach (WinForms.ToolStripItem item in menu.Items)
        {
            item.ForeColor = WinFormsTheme.TextPrimary;
        }

        _trayIconImage = LoadTrayIcon();
        _trayIcon = new WinForms.NotifyIcon
        {
            Icon = _trayIconImage,
            Text = NotifyIconText(LocalizationService.Text("Tray.Running")),
            ContextMenuStrip = menu,
            Visible = true,
        };
        _trayIcon.DoubleClick += (_, _) => ShowWindow();
        _trayIcon.BalloonTipClicked += (_, _) =>
        {
            var action = Interlocked.Exchange(
                ref _balloonClickAction,
                null);
            action?.Invoke();
        };

        _window.HideRequested += (_, _) => HideWindow();
        _window.Resize += (_, _) =>
        {
            if (_window.WindowState == WinForms.FormWindowState.Minimized)
            {
                HideWindow();
            }
        };
        _window.FormClosing += WindowFormClosing;
        LocalizationService.LanguageChanged += LanguageChanged;

        if (startInTray)
        {
            // Beim Windows-Autostart darf das Hauptfenster weder sichtbar
            // werden noch kurz in der Taskleiste aufblitzen. Es wird erst über
            // das Tray-Menü oder einen Doppelklick auf das Symbol geöffnet.
            _window.WindowState = WinForms.FormWindowState.Normal;
            _minimizeHintShown = true;
        }
        else
        {
            ShowWindow();
        }
    }

    public void RequestExit()
    {
        if (_window.IsDisposed)
        {
            return;
        }

        if (_window.InvokeRequired)
        {
            _window.BeginInvoke(new Action(RequestExit));
            return;
        }

        ExitAgent();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            LocalizationService.LanguageChanged -= LanguageChanged;
            _trayIcon.Visible = false;
            _trayIcon.ContextMenuStrip?.Dispose();
            _trayIcon.Dispose();
            _trayIconImage.Dispose();
            if (!_window.IsDisposed)
            {
                _window.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void ShowWindow()
    {
        if (_exiting || _window.IsDisposed)
        {
            return;
        }

        if (!EnsureFirstRunCompleted())
        {
            return;
        }

        _window.ShowInTaskbar = true;
        if (!_window.Visible)
        {
            _window.Show();
        }

        if (_window.WindowState == WinForms.FormWindowState.Minimized)
        {
            _window.WindowState = WinForms.FormWindowState.Normal;
        }

        _window.RefreshAutoStartState();
        _window.Activate();
        _window.BringToFront();
    }

    private bool EnsureFirstRunCompleted()
    {
        if (_firstRun.IsCompleted())
        {
            return true;
        }

        if (_showingFirstRun)
        {
            return false;
        }

        _showingFirstRun = true;
        try
        {
            using var dialog = new FirstRunDialog();
            if (dialog.ShowDialog() != WinForms.DialogResult.OK)
            {
                return false;
            }

            if (!_firstRun.MarkCompleted())
            {
                NexusDialog.Show(
                    _window,
                    LocalizationService.Text("FirstRunDialog.SaveFailed"),
                    LocalizationService.Text("FirstRunDialog.Title"),
                    NexusDialogKind.Warning);
            }

            return true;
        }
        finally
        {
            _showingFirstRun = false;
        }
    }

    private void HideWindow()
    {
        if (_exiting || _window.IsDisposed)
        {
            return;
        }

        _window.WindowState = WinForms.FormWindowState.Normal;
        _window.Hide();
        _window.ShowInTaskbar = false;
        if (_minimizeHintShown)
        {
            return;
        }

        _minimizeHintShown = true;
        _balloonClickAction = ShowWindow;
        _trayIcon.ShowBalloonTip(
            3000,
            LocalizationService.Text("Tray.BalloonTitle"),
            LocalizationService.Text("Tray.BalloonText"),
            WinForms.ToolTipIcon.Info);
    }

    private void WindowFormClosing(
        object? sender,
        WinForms.FormClosingEventArgs eventArgs)
    {
        if (
            !_exiting
            && eventArgs.CloseReason == WinForms.CloseReason.UserClosing)
        {
            eventArgs.Cancel = true;
            HideWindow();
            return;
        }

        _trayIcon.Visible = false;
    }

    private async void AutoStartMenuItemClicked(
        object? sender,
        EventArgs eventArgs)
    {
        if (_updatingAutoStart)
        {
            return;
        }

        _updatingAutoStart = true;
        _autoStartMenuItem.Enabled = false;
        var requestedState = _autoStartMenuItem.Checked;
        var result = await Task.Run(
            () => _autoStart.SetEnabled(requestedState));
        _autoStartMenuItem.Enabled = true;
        _updatingAutoStart = false;

        if (!result.Succeeded)
        {
            _autoStartMenuItem.Checked = !requestedState;
            NexusDialog.Show(
                _window,
                result.Error
                    ?? LocalizationService.Text(
                        "Tray.AutoStartChangeFailed"),
                "Nexus Control Agent",
                NexusDialogKind.Warning);
        }

        _window.RefreshAutoStartState();
    }

    private void ExitAgent()
    {
        if (_exiting)
        {
            return;
        }

        _exiting = true;
        _trayIcon.Visible = false;
        _window.Close();
        ExitThread();
    }

    private void LanguageChanged(object? sender, EventArgs eventArgs)
    {
        if (_window.IsDisposed)
        {
            return;
        }

        if (_window.InvokeRequired)
        {
            _window.BeginInvoke(new Action(
                () => LanguageChanged(sender, eventArgs)));
            return;
        }

        _openMenuItem.Text = LocalizationService.Text("Tray.Open");
        _rotateMenuItem.Text = LocalizationService.Text("Tray.NewPairingCode");
        _autoStartMenuItem.Text = LocalizationService.Text(
            "Tray.StartWithWindows");
        _exitMenuItem.Text = LocalizationService.Text("Tray.Exit");
        _trayIcon.Text = NotifyIconText(
            LocalizationService.Text("Tray.Running"));
    }

    private static string NotifyIconText(string text) =>
        text[..Math.Min(text.Length, 63)];

    private static Drawing.Icon LoadTrayIcon()
    {
        try
        {
            var executablePath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                using var extracted =
                    Drawing.Icon.ExtractAssociatedIcon(executablePath);
                if (extracted is not null)
                {
                    return (Drawing.Icon)extracted.Clone();
                }
            }
        }
        catch
        {
            // Das eingebettete Fallback-Icon hält den Tray weiterhin nutzbar.
        }

        return (Drawing.Icon)Drawing.SystemIcons.Application.Clone();
    }

    private static WinForms.ContextMenuStrip CreateContextMenu()
    {
        return new WinForms.ContextMenuStrip
        {
            BackColor = WinFormsTheme.Surface,
            ForeColor = WinFormsTheme.TextPrimary,
            Renderer = WinFormsTheme.CreateToolStripRenderer(),
            ShowImageMargin = false,
            Padding = new WinForms.Padding(4),
        };
    }
}
