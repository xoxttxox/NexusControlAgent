#nullable enable

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace NexusControl.Agent.Forms;

partial class ActivityLogDialog
{
    private IContainer? components;
    private TableLayoutPanel rootLayout = null!;
    private Label titleLabel = null!;
    private Label privacyLabel = null!;
    private ListBox _entriesListBox = null!;
    private TableLayoutPanel footerLayout = null!;
    private Label _statusLabel = null!;
    private Button copyButton = null!;
    private Button clearButton = null!;
    private Button closeButton = null!;
    private System.Windows.Forms.Timer _refreshTimer = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refreshTimer?.Stop();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        rootLayout = new TableLayoutPanel();
        titleLabel = new Label();
        privacyLabel = new Label();
        _entriesListBox = new ListBox();
        footerLayout = new TableLayoutPanel();
        _statusLabel = new Label();
        copyButton = new Button();
        clearButton = new Button();
        closeButton = new Button();
        _refreshTimer = new System.Windows.Forms.Timer(components);
        rootLayout.SuspendLayout();
        footerLayout.SuspendLayout();
        SuspendLayout();
        // 
        // rootLayout
        // 
        rootLayout.BackColor = Color.FromArgb(21, 23, 26);
        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(titleLabel, 0, 0);
        rootLayout.Controls.Add(privacyLabel, 0, 1);
        rootLayout.Controls.Add(_entriesListBox, 0, 2);
        rootLayout.Controls.Add(footerLayout, 0, 3);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Location = new Point(0, 0);
        rootLayout.Name = "rootLayout";
        rootLayout.Padding = new Padding(12);
        rootLayout.RowCount = 4;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        rootLayout.Size = new Size(484, 360);
        rootLayout.TabIndex = 0;
        // 
        // titleLabel
        // 
        titleLabel.Dock = DockStyle.Fill;
        titleLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        titleLabel.Location = new Point(15, 12);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(454, 32);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "Lokales Aktivitätsprotokoll";
        titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // privacyLabel
        // 
        privacyLabel.Dock = DockStyle.Fill;
        privacyLabel.Location = new Point(15, 44);
        privacyLabel.Name = "privacyLabel";
        privacyLabel.Size = new Size(454, 46);
        privacyLabel.TabIndex = 1;
        privacyLabel.Tag = "muted";
        privacyLabel.Text = "Zeigt Verbindungen und ausgeführte Aktionen. Kennwörter, Tokens, Texteingaben, Dateinamen und Inhalte werden nicht gespeichert.";
        privacyLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _entriesListBox
        // 
        _entriesListBox.BorderStyle = BorderStyle.FixedSingle;
        _entriesListBox.Dock = DockStyle.Fill;
        _entriesListBox.Font = new Font("Segoe UI", 8.25F);
        _entriesListBox.FormattingEnabled = true;
        _entriesListBox.HorizontalScrollbar = false;
        _entriesListBox.IntegralHeight = false;
        _entriesListBox.ItemHeight = 13;
        _entriesListBox.Location = new Point(12, 90);
        _entriesListBox.Margin = new Padding(0);
        _entriesListBox.Name = "_entriesListBox";
        _entriesListBox.Size = new Size(460, 214);
        _entriesListBox.TabIndex = 2;
        // 
        // footerLayout
        // 
        footerLayout.ColumnCount = 6;
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 6F));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 74F));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 6F));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82F));
        footerLayout.Controls.Add(_statusLabel, 0, 0);
        footerLayout.Controls.Add(copyButton, 1, 0);
        footerLayout.Controls.Add(clearButton, 3, 0);
        footerLayout.Controls.Add(closeButton, 5, 0);
        footerLayout.Dock = DockStyle.Fill;
        footerLayout.Location = new Point(12, 304);
        footerLayout.Margin = new Padding(0);
        footerLayout.Name = "footerLayout";
        footerLayout.RowCount = 1;
        footerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        footerLayout.Size = new Size(460, 44);
        footerLayout.TabIndex = 3;
        // 
        // _statusLabel
        // 
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.Location = new Point(3, 0);
        _statusLabel.Name = "_statusLabel";
        _statusLabel.Size = new Size(194, 44);
        _statusLabel.TabIndex = 0;
        _statusLabel.Tag = "muted";
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // copyButton
        // 
        copyButton.Dock = DockStyle.Fill;
        copyButton.Location = new Point(200, 6);
        copyButton.Margin = new Padding(0, 6, 0, 0);
        copyButton.Name = "copyButton";
        copyButton.Size = new Size(92, 38);
        copyButton.TabIndex = 1;
        copyButton.Text = "Kopieren";
        copyButton.UseVisualStyleBackColor = true;
        copyButton.Click += CopyButtonClicked;
        // 
        // clearButton
        // 
        clearButton.Dock = DockStyle.Fill;
        clearButton.Location = new Point(298, 6);
        clearButton.Margin = new Padding(0, 6, 0, 0);
        clearButton.Name = "clearButton";
        clearButton.Size = new Size(74, 38);
        clearButton.TabIndex = 2;
        clearButton.Text = "Leeren";
        clearButton.UseVisualStyleBackColor = true;
        clearButton.Click += ClearButtonClicked;
        // 
        // closeButton
        // 
        closeButton.DialogResult = DialogResult.Cancel;
        closeButton.Dock = DockStyle.Fill;
        closeButton.Location = new Point(378, 6);
        closeButton.Margin = new Padding(0, 6, 0, 0);
        closeButton.Name = "closeButton";
        closeButton.Size = new Size(82, 38);
        closeButton.TabIndex = 3;
        closeButton.Tag = "primary";
        closeButton.Text = "Schließen";
        closeButton.UseVisualStyleBackColor = true;
        // 
        // _refreshTimer
        // 
        _refreshTimer.Interval = 1000;
        _refreshTimer.Tick += RefreshTimerTick;
        // 
        // ActivityLogDialog
        // 
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(21, 23, 26);
        CancelButton = closeButton;
        ClientSize = new Size(484, 360);
        Controls.Add(rootLayout);
        Font = new Font("Segoe UI", 8.5F);
        ForeColor = Color.FromArgb(242, 242, 242);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "ActivityLogDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Nexus Control Protokoll";
        Shown += DialogShown;
        rootLayout.ResumeLayout(false);
        footerLayout.ResumeLayout(false);
        ResumeLayout(false);
    }
}
