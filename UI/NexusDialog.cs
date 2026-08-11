using System.Drawing;
using System.Windows.Forms;

namespace NexusControl.Agent.UI;

internal enum NexusDialogKind
{
    Information,
    Warning,
    Error,
}

/// <summary>
/// Kompakter WinForms-Dialog für Meldungen vor und während des UI-Starts.
/// </summary>
internal static class NexusDialog
{
    public static DialogResult ShowStandalone(
        string text,
        string caption,
        NexusDialogKind kind)
    {
        var result = DialogResult.Cancel;
        var thread = new Thread(() =>
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            result = Show(null, text, caption, kind);
        })
        {
            IsBackground = false,
            Name = "NexusControlAgent.Dialog",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        return result;
    }

    public static DialogResult Show(
        IWin32Window? owner,
        string text,
        string caption,
        NexusDialogKind kind)
    {
        var useOwner = owner is not null
            && (owner is not Control ownerControl || ownerControl.Visible);
        using var dialog = new Form
        {
            Text = caption,
            StartPosition = useOwner
                ? FormStartPosition.CenterParent
                : FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            ShowInTaskbar = false,
            MinimizeBox = false,
            MaximizeBox = false,
            AutoScaleMode = AutoScaleMode.Dpi,
            ClientSize = new Size(440, 205),
            Font = new Font("Segoe UI", 9F),
            BackColor = WinFormsTheme.Background,
            ForeColor = WinFormsTheme.TextPrimary,
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(16),
            BackColor = WinFormsTheme.Background,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        dialog.Controls.Add(layout);

        using var iconImage = GetIcon(kind).ToBitmap();
        var iconBox = new PictureBox
        {
            Width = 32,
            Height = 32,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = iconImage,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 2, 8, 0),
        };
        layout.Controls.Add(iconBox, 0, 0);

        var messageBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            ScrollBars = ScrollBars.Vertical,
            TabStop = false,
            Text = text,
            BackColor = WinFormsTheme.Background,
            ForeColor = WinFormsTheme.TextPrimary,
            Tag = "plain",
            Margin = new Padding(0, 0, 0, 10),
        };
        layout.Controls.Add(messageBox, 1, 0);

        var okButton = new Button
        {
            Text = "OK",
            Width = 88,
            Height = 28,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            DialogResult = DialogResult.OK,
            Tag = "primary",
            Margin = new Padding(0),
        };
        layout.Controls.Add(okButton, 1, 1);
        dialog.AcceptButton = okButton;
        dialog.CancelButton = okButton;

        WinFormsTheme.Apply(dialog);
        return useOwner
            ? dialog.ShowDialog(owner!)
            : dialog.ShowDialog();
    }

    private static Icon GetIcon(NexusDialogKind kind) => kind switch
    {
        NexusDialogKind.Warning => SystemIcons.Warning,
        NexusDialogKind.Error => SystemIcons.Error,
        _ => SystemIcons.Information,
    };
}
