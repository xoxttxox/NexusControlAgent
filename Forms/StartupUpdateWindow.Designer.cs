#nullable enable

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace NexusControl.Agent.Forms;

partial class StartupUpdateWindow
{
    private IContainer? components;
    private TableLayoutPanel rootLayout = null!;
    private Label appTitleLabel = null!;
    private Label installedVersionLabel = null!;
    private Label statusTitleLabel = null!;
    private Label detailLabel = null!;
    private ProgressBar progressBar = null!;

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
        appTitleLabel = new Label();
        installedVersionLabel = new Label();
        statusTitleLabel = new Label();
        detailLabel = new Label();
        progressBar = new ProgressBar();
        rootLayout.SuspendLayout();
        SuspendLayout();
        // 
        // rootLayout
        // 
        rootLayout.BackColor = Color.FromArgb(21, 23, 26);
        rootLayout.ColumnCount = 2;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125F));
        rootLayout.Controls.Add(appTitleLabel, 0, 0);
        rootLayout.Controls.Add(installedVersionLabel, 1, 0);
        rootLayout.Controls.Add(statusTitleLabel, 0, 1);
        rootLayout.Controls.Add(detailLabel, 0, 2);
        rootLayout.Controls.Add(progressBar, 0, 3);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Location = new Point(0, 0);
        rootLayout.Margin = new Padding(0);
        rootLayout.Name = "rootLayout";
        rootLayout.Padding = new Padding(20, 14, 20, 16);
        rootLayout.RowCount = 4;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.Size = new Size(420, 150);
        rootLayout.TabIndex = 0;
        // 
        // appTitleLabel
        // 
        appTitleLabel.Dock = DockStyle.Fill;
        appTitleLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        appTitleLabel.Location = new Point(20, 14);
        appTitleLabel.Margin = new Padding(0);
        appTitleLabel.Name = "appTitleLabel";
        appTitleLabel.Size = new Size(255, 30);
        appTitleLabel.TabIndex = 0;
        appTitleLabel.Text = "Nexus Control Agent";
        appTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // installedVersionLabel
        // 
        installedVersionLabel.Dock = DockStyle.Fill;
        installedVersionLabel.Location = new Point(275, 14);
        installedVersionLabel.Margin = new Padding(0);
        installedVersionLabel.Name = "installedVersionLabel";
        installedVersionLabel.Size = new Size(125, 30);
        installedVersionLabel.TabIndex = 1;
        installedVersionLabel.Tag = "muted";
        installedVersionLabel.Text = "Version 0.11.2";
        installedVersionLabel.TextAlign = ContentAlignment.MiddleRight;
        // 
        // statusTitleLabel
        // 
        rootLayout.SetColumnSpan(statusTitleLabel, 2);
        statusTitleLabel.Dock = DockStyle.Fill;
        statusTitleLabel.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
        statusTitleLabel.Location = new Point(20, 44);
        statusTitleLabel.Margin = new Padding(0);
        statusTitleLabel.Name = "statusTitleLabel";
        statusTitleLabel.Size = new Size(380, 38);
        statusTitleLabel.TabIndex = 2;
        statusTitleLabel.Text = "Suche nach Updates …";
        statusTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // detailLabel
        // 
        rootLayout.SetColumnSpan(detailLabel, 2);
        detailLabel.AutoEllipsis = true;
        detailLabel.Dock = DockStyle.Fill;
        detailLabel.Location = new Point(20, 82);
        detailLabel.Margin = new Padding(0);
        detailLabel.Name = "detailLabel";
        detailLabel.Size = new Size(380, 30);
        detailLabel.TabIndex = 3;
        detailLabel.Tag = "muted";
        detailLabel.Text = "Bitte einen Moment warten.";
        detailLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // progressBar
        // 
        rootLayout.SetColumnSpan(progressBar, 2);
        progressBar.Dock = DockStyle.Top;
        progressBar.Location = new Point(20, 120);
        progressBar.Margin = new Padding(0, 8, 0, 0);
        progressBar.MarqueeAnimationSpeed = 22;
        progressBar.Name = "progressBar";
        progressBar.Size = new Size(380, 8);
        progressBar.Style = ProgressBarStyle.Marquee;
        progressBar.TabIndex = 4;
        // 
        // StartupUpdateWindow
        // 
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(21, 23, 26);
        ClientSize = new Size(420, 150);
        ControlBox = false;
        Controls.Add(rootLayout);
        DoubleBuffered = true;
        Font = new Font("Segoe UI", 9F);
        ForeColor = Color.FromArgb(242, 242, 242);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "StartupUpdateWindow";
        ShowInTaskbar = true;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Nexus Control Update";
        rootLayout.ResumeLayout(false);
        ResumeLayout(false);
    }
}
