#nullable enable

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace NexusControl.Agent.Forms;

partial class UpdateWindow
{
    private IContainer? components;
    private TableLayoutPanel rootLayout = null!;
    private Panel headerPanel = null!;
    private Label headerTitleLabel = null!;
    private Label installedVersionLabel = null!;
    private TableLayoutPanel contentLayout = null!;
    private Label titleLabel = null!;
    private Label versionLabel = null!;
    private TextBox notesTextBox = null!;
    private Label statusLabel = null!;
    private ProgressBar progressBar = null!;
    private TableLayoutPanel footerLayout = null!;
    private LinkLabel releaseLinkLabel = null!;
    private Button closeButton = null!;
    private Button actionButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        rootLayout = new TableLayoutPanel();
        headerPanel = new Panel();
        headerTitleLabel = new Label();
        installedVersionLabel = new Label();
        contentLayout = new TableLayoutPanel();
        titleLabel = new Label();
        versionLabel = new Label();
        notesTextBox = new TextBox();
        statusLabel = new Label();
        progressBar = new ProgressBar();
        footerLayout = new TableLayoutPanel();
        releaseLinkLabel = new LinkLabel();
        closeButton = new Button();
        actionButton = new Button();
        rootLayout.SuspendLayout();
        headerPanel.SuspendLayout();
        contentLayout.SuspendLayout();
        footerLayout.SuspendLayout();
        SuspendLayout();
        // 
        // rootLayout
        // 
        rootLayout.BackColor = Color.FromArgb(21, 23, 26);
        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(headerPanel, 0, 0);
        rootLayout.Controls.Add(contentLayout, 0, 1);
        rootLayout.Controls.Add(footerLayout, 0, 2);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Location = new Point(0, 0);
        rootLayout.Margin = new Padding(0);
        rootLayout.Name = "rootLayout";
        rootLayout.RowCount = 3;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        rootLayout.Size = new Size(440, 320);
        rootLayout.TabIndex = 0;
        // 
        // headerPanel
        // 
        headerPanel.BackColor = Color.FromArgb(28, 31, 36);
        headerPanel.Controls.Add(headerTitleLabel);
        headerPanel.Controls.Add(installedVersionLabel);
        headerPanel.Dock = DockStyle.Fill;
        headerPanel.Location = new Point(0, 0);
        headerPanel.Margin = new Padding(0);
        headerPanel.Name = "headerPanel";
        headerPanel.Size = new Size(440, 48);
        headerPanel.TabIndex = 0;
        // 
        // headerTitleLabel
        // 
        headerTitleLabel.Dock = DockStyle.Left;
        headerTitleLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        headerTitleLabel.Location = new Point(0, 0);
        headerTitleLabel.Name = "headerTitleLabel";
        headerTitleLabel.Padding = new Padding(14, 0, 0, 0);
        headerTitleLabel.Size = new Size(245, 48);
        headerTitleLabel.TabIndex = 0;
        headerTitleLabel.Text = "Nexus Control Updater";
        headerTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // installedVersionLabel
        // 
        installedVersionLabel.Dock = DockStyle.Right;
        installedVersionLabel.Location = new Point(250, 0);
        installedVersionLabel.Name = "installedVersionLabel";
        installedVersionLabel.Padding = new Padding(0, 0, 14, 0);
        installedVersionLabel.Size = new Size(190, 48);
        installedVersionLabel.TabIndex = 1;
        installedVersionLabel.Tag = "muted";
        installedVersionLabel.Text = "Installiert: 0.11.1";
        installedVersionLabel.TextAlign = ContentAlignment.MiddleRight;
        // 
        // contentLayout
        // 
        contentLayout.ColumnCount = 1;
        contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        contentLayout.Controls.Add(titleLabel, 0, 0);
        contentLayout.Controls.Add(versionLabel, 0, 1);
        contentLayout.Controls.Add(notesTextBox, 0, 2);
        contentLayout.Controls.Add(statusLabel, 0, 3);
        contentLayout.Controls.Add(progressBar, 0, 4);
        contentLayout.Dock = DockStyle.Fill;
        contentLayout.Location = new Point(0, 48);
        contentLayout.Margin = new Padding(0);
        contentLayout.Name = "contentLayout";
        contentLayout.Padding = new Padding(16, 14, 16, 10);
        contentLayout.RowCount = 5;
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10F));
        contentLayout.Size = new Size(440, 224);
        contentLayout.TabIndex = 1;
        // 
        // titleLabel
        // 
        titleLabel.Dock = DockStyle.Fill;
        titleLabel.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
        titleLabel.Location = new Point(16, 14);
        titleLabel.Margin = new Padding(0);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(408, 32);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "Nexus Control Updates";
        titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // versionLabel
        // 
        versionLabel.Dock = DockStyle.Fill;
        versionLabel.AutoEllipsis = true;
        versionLabel.Location = new Point(16, 46);
        versionLabel.Margin = new Padding(0);
        versionLabel.Name = "versionLabel";
        versionLabel.Size = new Size(408, 24);
        versionLabel.TabIndex = 1;
        versionLabel.Tag = "muted";
        versionLabel.Text = "Bereit für die Updateprüfung";
        versionLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // notesTextBox
        // 
        notesTextBox.BorderStyle = BorderStyle.None;
        notesTextBox.Dock = DockStyle.Fill;
        notesTextBox.Location = new Point(16, 73);
        notesTextBox.Margin = new Padding(0, 3, 0, 5);
        notesTextBox.Multiline = true;
        notesTextBox.Name = "notesTextBox";
        notesTextBox.ReadOnly = true;
        notesTextBox.ScrollBars = ScrollBars.Vertical;
        notesTextBox.Size = new Size(408, 117);
        notesTextBox.TabIndex = 2;
        notesTextBox.TabStop = false;
        notesTextBox.Tag = "plain";
        notesTextBox.Text = "Neue Versionen werden sicher über GitHub Releases geladen.";
        // 
        // statusLabel
        // 
        statusLabel.Dock = DockStyle.Fill;
        statusLabel.AutoEllipsis = true;
        statusLabel.Location = new Point(16, 195);
        statusLabel.Margin = new Padding(0);
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(408, 26);
        statusLabel.TabIndex = 3;
        statusLabel.Tag = "muted";
        statusLabel.Text = "Updateprüfung ist bereit.";
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // progressBar
        // 
        progressBar.Dock = DockStyle.Fill;
        progressBar.Location = new Point(16, 223);
        progressBar.Margin = new Padding(0, 2, 0, 0);
        progressBar.Name = "progressBar";
        progressBar.Size = new Size(408, 8);
        progressBar.Style = ProgressBarStyle.Continuous;
        progressBar.TabIndex = 4;
        // 
        // footerLayout
        // 
        footerLayout.BackColor = Color.FromArgb(28, 31, 36);
        footerLayout.ColumnCount = 5;
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84F));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 8F));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132F));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12F));
        footerLayout.Controls.Add(releaseLinkLabel, 0, 0);
        footerLayout.Controls.Add(closeButton, 1, 0);
        footerLayout.Controls.Add(actionButton, 3, 0);
        footerLayout.Dock = DockStyle.Fill;
        footerLayout.Location = new Point(0, 272);
        footerLayout.Margin = new Padding(0);
        footerLayout.Name = "footerLayout";
        footerLayout.Padding = new Padding(12, 8, 0, 8);
        footerLayout.RowCount = 1;
        footerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        footerLayout.Size = new Size(440, 48);
        footerLayout.TabIndex = 2;
        // 
        // releaseLinkLabel
        // 
        releaseLinkLabel.ActiveLinkColor = Color.FromArgb(59, 140, 232);
        releaseLinkLabel.Dock = DockStyle.Fill;
        releaseLinkLabel.LinkColor = Color.FromArgb(45, 125, 219);
        releaseLinkLabel.Location = new Point(12, 8);
        releaseLinkLabel.Margin = new Padding(0);
        releaseLinkLabel.Name = "releaseLinkLabel";
        releaseLinkLabel.Size = new Size(204, 32);
        releaseLinkLabel.TabIndex = 0;
        releaseLinkLabel.TabStop = true;
        releaseLinkLabel.Text = "Release auf GitHub ansehen";
        releaseLinkLabel.TextAlign = ContentAlignment.MiddleLeft;
        releaseLinkLabel.Visible = false;
        releaseLinkLabel.VisitedLinkColor = Color.FromArgb(45, 125, 219);
        releaseLinkLabel.LinkClicked += ReleaseLinkClicked;
        // 
        // closeButton
        // 
        closeButton.DialogResult = DialogResult.Cancel;
        closeButton.Dock = DockStyle.Fill;
        closeButton.Location = new Point(216, 8);
        closeButton.Margin = new Padding(0);
        closeButton.Name = "closeButton";
        closeButton.Size = new Size(84, 32);
        closeButton.TabIndex = 1;
        closeButton.Text = "Später";
        closeButton.UseVisualStyleBackColor = false;
        // 
        // actionButton
        // 
        actionButton.Dock = DockStyle.Fill;
        actionButton.Location = new Point(308, 8);
        actionButton.Margin = new Padding(0);
        actionButton.Name = "actionButton";
        actionButton.Size = new Size(132, 32);
        actionButton.TabIndex = 2;
        actionButton.Tag = "primary";
        actionButton.Text = "Jetzt prüfen";
        actionButton.UseVisualStyleBackColor = false;
        actionButton.Click += ActionButtonClick;
        // 
        // UpdateWindow
        // 
        AcceptButton = actionButton;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(21, 23, 26);
        CancelButton = closeButton;
        ClientSize = new Size(440, 320);
        Controls.Add(rootLayout);
        DoubleBuffered = true;
        Font = new Font("Segoe UI", 9F);
        ForeColor = Color.FromArgb(242, 242, 242);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "UpdateWindow";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Nexus Control Update";
        rootLayout.ResumeLayout(false);
        headerPanel.ResumeLayout(false);
        contentLayout.ResumeLayout(false);
        contentLayout.PerformLayout();
        footerLayout.ResumeLayout(false);
        ResumeLayout(false);
    }
}
