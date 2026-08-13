#nullable enable

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using NexusControl.Agent.UI;

namespace NexusControl.Agent.Forms;

partial class ConnectionDiagnosticsDialog
{
    private IContainer? components;
    private TableLayoutPanel rootLayout = null!;
    private TableLayoutPanel headerLayout = null!;
    private Label titleLabel = null!;
    private Label _summaryLabel = null!;
    private DarkGroupBox resultsGroupBox = null!;
    private TableLayoutPanel _resultsLayout = null!;
    private TableLayoutPanel footerLayout = null!;
    private Button _copyButton = null!;
    private Button _runButton = null!;
    private Button closeButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _runCancellation?.Cancel();
            _runCancellation?.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        rootLayout = new TableLayoutPanel();
        headerLayout = new TableLayoutPanel();
        titleLabel = new Label();
        _summaryLabel = new Label();
        resultsGroupBox = new DarkGroupBox();
        _resultsLayout = new TableLayoutPanel();
        footerLayout = new TableLayoutPanel();
        _copyButton = new Button();
        _runButton = new Button();
        closeButton = new Button();
        rootLayout.SuspendLayout();
        headerLayout.SuspendLayout();
        resultsGroupBox.SuspendLayout();
        footerLayout.SuspendLayout();
        SuspendLayout();
        // 
        // rootLayout
        // 
        rootLayout.BackColor = Color.FromArgb(21, 23, 26);
        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(headerLayout, 0, 0);
        rootLayout.Controls.Add(resultsGroupBox, 0, 1);
        rootLayout.Controls.Add(footerLayout, 0, 2);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Location = new Point(0, 0);
        rootLayout.Name = "rootLayout";
        rootLayout.Padding = new Padding(12);
        rootLayout.RowCount = 3;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        rootLayout.Size = new Size(484, 392);
        rootLayout.TabIndex = 0;
        // 
        // headerLayout
        // 
        headerLayout.ColumnCount = 1;
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerLayout.Controls.Add(titleLabel, 0, 0);
        headerLayout.Controls.Add(_summaryLabel, 0, 1);
        headerLayout.Dock = DockStyle.Fill;
        headerLayout.Location = new Point(12, 12);
        headerLayout.Margin = new Padding(0);
        headerLayout.Name = "headerLayout";
        headerLayout.RowCount = 2;
        headerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        headerLayout.Size = new Size(460, 52);
        headerLayout.TabIndex = 0;
        // 
        // titleLabel
        // 
        titleLabel.Dock = DockStyle.Fill;
        titleLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        titleLabel.Location = new Point(3, 0);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(454, 26);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "Nexus Control connection diagnostics";
        titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _summaryLabel
        // 
        _summaryLabel.Dock = DockStyle.Fill;
        _summaryLabel.Location = new Point(3, 26);
        _summaryLabel.Name = "_summaryLabel";
        _summaryLabel.Size = new Size(454, 26);
        _summaryLabel.TabIndex = 1;
        _summaryLabel.Tag = "muted";
        _summaryLabel.Text = "Preparing checks …";
        _summaryLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // resultsGroupBox
        // 
        resultsGroupBox.Controls.Add(_resultsLayout);
        resultsGroupBox.Dock = DockStyle.Fill;
        resultsGroupBox.Location = new Point(12, 68);
        resultsGroupBox.Margin = new Padding(0, 4, 0, 6);
        resultsGroupBox.Name = "resultsGroupBox";
        resultsGroupBox.Padding = new Padding(10, 22, 10, 8);
        resultsGroupBox.Size = new Size(460, 268);
        resultsGroupBox.TabIndex = 1;
        resultsGroupBox.TabStop = false;
        resultsGroupBox.Text = "Status";
        // 
        // _resultsLayout
        // 
        _resultsLayout.ColumnCount = 2;
        _resultsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125F));
        _resultsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _resultsLayout.Dock = DockStyle.Fill;
        _resultsLayout.Location = new Point(10, 38);
        _resultsLayout.Margin = new Padding(0);
        _resultsLayout.Name = "_resultsLayout";
        _resultsLayout.RowCount = 6;
        _resultsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 16.66667F));
        _resultsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 16.66667F));
        _resultsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 16.66667F));
        _resultsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 16.66667F));
        _resultsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 16.66667F));
        _resultsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 16.66667F));
        _resultsLayout.Size = new Size(440, 222);
        _resultsLayout.TabIndex = 0;
        // 
        // footerLayout
        // 
        footerLayout.ColumnCount = 6;
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 6F));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 6F));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84F));
        footerLayout.Controls.Add(_copyButton, 1, 0);
        footerLayout.Controls.Add(_runButton, 3, 0);
        footerLayout.Controls.Add(closeButton, 5, 0);
        footerLayout.Dock = DockStyle.Fill;
        footerLayout.Location = new Point(12, 336);
        footerLayout.Margin = new Padding(0);
        footerLayout.Name = "footerLayout";
        footerLayout.RowCount = 1;
        footerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        footerLayout.Size = new Size(460, 44);
        footerLayout.TabIndex = 2;
        // 
        // _copyButton
        // 
        _copyButton.Dock = DockStyle.Fill;
        _copyButton.Enabled = false;
        _copyButton.Location = new Point(164, 5);
        _copyButton.Margin = new Padding(0, 5, 0, 3);
        _copyButton.Name = "_copyButton";
        _copyButton.Size = new Size(112, 36);
        _copyButton.TabIndex = 0;
        _copyButton.Text = "Copy report";
        _copyButton.UseVisualStyleBackColor = true;
        _copyButton.Click += CopyButtonClicked;
        // 
        // _runButton
        // 
        _runButton.Dock = DockStyle.Fill;
        _runButton.Location = new Point(282, 5);
        _runButton.Margin = new Padding(0, 5, 0, 3);
        _runButton.Name = "_runButton";
        _runButton.Size = new Size(96, 36);
        _runButton.TabIndex = 1;
        _runButton.Tag = "primary";
        _runButton.Text = "Run again";
        _runButton.UseVisualStyleBackColor = true;
        _runButton.Click += RunButtonClicked;
        // 
        // closeButton
        // 
        closeButton.DialogResult = DialogResult.Cancel;
        closeButton.Dock = DockStyle.Fill;
        closeButton.Location = new Point(384, 5);
        closeButton.Margin = new Padding(0, 5, 0, 3);
        closeButton.Name = "closeButton";
        closeButton.Size = new Size(76, 36);
        closeButton.TabIndex = 2;
        closeButton.Text = "Close";
        closeButton.UseVisualStyleBackColor = true;
        // 
        // ConnectionDiagnosticsDialog
        // 
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(21, 23, 26);
        CancelButton = closeButton;
        ClientSize = new Size(484, 392);
        Controls.Add(rootLayout);
        Font = new Font("Segoe UI", 8.5F);
        ForeColor = Color.FromArgb(242, 242, 242);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "ConnectionDiagnosticsDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Connection diagnostics";
        FormClosed += DialogFormClosed;
        Shown += DialogShown;
        rootLayout.ResumeLayout(false);
        headerLayout.ResumeLayout(false);
        resultsGroupBox.ResumeLayout(false);
        footerLayout.ResumeLayout(false);
        ResumeLayout(false);
    }
}
