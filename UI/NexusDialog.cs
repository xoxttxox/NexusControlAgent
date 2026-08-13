using System.Drawing;
using System.Windows.Forms;
using NexusControl.Agent.Localization;

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
    private const int DialogWidth = 440;
    private const int MinimumDialogHeight = 180;
    private const int HorizontalPadding = 32;
    private const int IconColumnWidth = 42;
    private const int ButtonRowHeight = 34;
    private const int MessageBottomMargin = 10;
    private const int VerticalPadding = 32;

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
        NexusDialogKind kind) =>
        ShowCore(
            owner,
            text,
            caption,
            kind,
            confirmation: false,
            confirmText: LocalizationService.Text("Common.Ok"));

    public static DialogResult Confirm(
        IWin32Window? owner,
        string text,
        string caption,
        NexusDialogKind kind,
        string? confirmText = null) =>
        ShowCore(
            owner,
            text,
            caption,
            kind,
            confirmation: true,
            confirmText: confirmText
                ?? LocalizationService.Text("Common.Confirm"));

    private static DialogResult ShowCore(
        IWin32Window? owner,
        string text,
        string caption,
        NexusDialogKind kind,
        bool confirmation,
        string confirmText)
    {
        var useOwner = owner is not null
            && (owner is not Control ownerControl || ownerControl.Visible);
        using var dialogFont = new Font("Segoe UI", 9F);
        var screen = useOwner
            ? Screen.FromHandle(owner!.Handle)
            : Screen.PrimaryScreen;
        var maximumDialogHeight = Math.Max(
            MinimumDialogHeight,
            (screen?.WorkingArea.Height ?? 720) - 96);
        var dialogHeight = CalculateDialogHeight(
            text,
            dialogFont,
            maximumDialogHeight);

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
            ClientSize = new Size(DialogWidth, dialogHeight),
            Font = dialogFont,
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

        var messageLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            UseMnemonic = false,
            TextAlign = ContentAlignment.TopLeft,
            Text = text,
            BackColor = WinFormsTheme.Background,
            ForeColor = WinFormsTheme.TextPrimary,
            Margin = new Padding(0, 0, 0, 10),
        };
        layout.Controls.Add(messageLabel, 1, 0);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(0),
            BackColor = WinFormsTheme.Background,
        };
        layout.Controls.Add(buttonPanel, 1, 1);

        var okButton = new Button
        {
            Text = confirmText,
            Width = 88,
            Height = 28,
            DialogResult = DialogResult.OK,
            Tag = "primary",
            Margin = new Padding(0),
        };
        buttonPanel.Controls.Add(okButton);

        Button? cancelButton = null;
        if (confirmation)
        {
            cancelButton = new Button
            {
                Text = LocalizationService.Text("Common.Cancel"),
                Width = 88,
                Height = 28,
                DialogResult = DialogResult.Cancel,
                Margin = new Padding(8, 0, 0, 0),
            };
            buttonPanel.Controls.Add(cancelButton);
        }

        dialog.AcceptButton = okButton;
        dialog.CancelButton = cancelButton ?? okButton;

        WinFormsTheme.Apply(dialog);
        return useOwner
            ? dialog.ShowDialog(owner!)
            : dialog.ShowDialog();
    }

    private static int CalculateDialogHeight(
        string text,
        Font font,
        int maximumDialogHeight)
    {
        var availableTextWidth =
            DialogWidth - HorizontalPadding - IconColumnWidth;
        var measuredText = TextRenderer.MeasureText(
            string.IsNullOrWhiteSpace(text) ? " " : text,
            font,
            new Size(availableTextWidth, int.MaxValue),
            TextFormatFlags.WordBreak
            | TextFormatFlags.TextBoxControl
            | TextFormatFlags.NoPrefix);

        var requiredHeight = measuredText.Height
            + MessageBottomMargin
            + ButtonRowHeight
            + VerticalPadding;
        return Math.Clamp(
            requiredHeight,
            MinimumDialogHeight,
            maximumDialogHeight);
    }

    private static Icon GetIcon(NexusDialogKind kind) => kind switch
    {
        NexusDialogKind.Warning => SystemIcons.Warning,
        NexusDialogKind.Error => SystemIcons.Error,
        _ => SystemIcons.Information,
    };
}
