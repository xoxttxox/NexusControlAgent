#nullable enable

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using NexusControl.Agent.UI;

namespace NexusControl.Agent.Forms;

partial class SettingsDialog
{
    private IContainer? components;
    private TableLayoutPanel rootLayout = null!;
    private Label titleLabel = null!;
    private DarkGroupBox languageGroupBox = null!;
    private TableLayoutPanel languageLayout = null!;
    private Label languageLabel = null!;
    private ComboBox languageComboBox = null!;
    private Label languageHintLabel = null!;
    private TableLayoutPanel footerLayout = null!;
    private Button closeButton = null!;

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
        titleLabel = new Label();
        languageGroupBox = new DarkGroupBox();
        languageLayout = new TableLayoutPanel();
        languageLabel = new Label();
        languageComboBox = new ComboBox();
        languageHintLabel = new Label();
        footerLayout = new TableLayoutPanel();
        closeButton = new Button();
        rootLayout.SuspendLayout();
        languageGroupBox.SuspendLayout();
        languageLayout.SuspendLayout();
        footerLayout.SuspendLayout();
        SuspendLayout();
        // 
        // rootLayout
        // 
        rootLayout.BackColor = Color.FromArgb(21, 23, 26);
        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(titleLabel, 0, 0);
        rootLayout.Controls.Add(languageGroupBox, 0, 1);
        rootLayout.Controls.Add(footerLayout, 0, 2);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Location = new Point(0, 0);
        rootLayout.Margin = new Padding(0);
        rootLayout.Name = "rootLayout";
        rootLayout.Padding = new Padding(14);
        rootLayout.RowCount = 3;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        rootLayout.Size = new Size(404, 210);
        rootLayout.TabIndex = 0;
        // 
        // titleLabel
        // 
        titleLabel.Dock = DockStyle.Fill;
        titleLabel.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
        titleLabel.Location = new Point(14, 14);
        titleLabel.Margin = new Padding(0);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(376, 36);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "Agent settings";
        titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // languageGroupBox
        // 
        languageGroupBox.Controls.Add(languageLayout);
        languageGroupBox.Dock = DockStyle.Fill;
        languageGroupBox.Location = new Point(14, 50);
        languageGroupBox.Margin = new Padding(0, 0, 0, 6);
        languageGroupBox.Name = "languageGroupBox";
        languageGroupBox.Padding = new Padding(10, 20, 10, 8);
        languageGroupBox.Size = new Size(376, 90);
        languageGroupBox.TabIndex = 1;
        languageGroupBox.TabStop = false;
        languageGroupBox.Text = "Language";
        // 
        // languageLayout
        // 
        languageLayout.ColumnCount = 2;
        languageLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
        languageLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        languageLayout.Controls.Add(languageLabel, 0, 0);
        languageLayout.Controls.Add(languageComboBox, 1, 0);
        languageLayout.Controls.Add(languageHintLabel, 0, 1);
        languageLayout.Dock = DockStyle.Fill;
        languageLayout.Location = new Point(10, 36);
        languageLayout.Margin = new Padding(0);
        languageLayout.Name = "languageLayout";
        languageLayout.RowCount = 2;
        languageLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        languageLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        languageLayout.Size = new Size(356, 46);
        languageLayout.TabIndex = 0;
        // 
        // languageLabel
        // 
        languageLabel.Dock = DockStyle.Fill;
        languageLabel.Location = new Point(0, 0);
        languageLabel.Margin = new Padding(0);
        languageLabel.Name = "languageLabel";
        languageLabel.Size = new Size(112, 28);
        languageLabel.TabIndex = 0;
        languageLabel.Text = "App language:";
        languageLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // languageComboBox
        // 
        languageComboBox.Dock = DockStyle.Fill;
        languageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        languageComboBox.FormattingEnabled = true;
        languageComboBox.Location = new Point(112, 2);
        languageComboBox.Margin = new Padding(0, 2, 0, 2);
        languageComboBox.Name = "languageComboBox";
        languageComboBox.Size = new Size(244, 23);
        languageComboBox.TabIndex = 1;
        languageComboBox.SelectedIndexChanged += LanguageComboBoxSelectedIndexChanged;
        // 
        // languageHintLabel
        // 
        languageLayout.SetColumnSpan(languageHintLabel, 2);
        languageHintLabel.Dock = DockStyle.Fill;
        languageHintLabel.Location = new Point(0, 28);
        languageHintLabel.Margin = new Padding(0);
        languageHintLabel.Name = "languageHintLabel";
        languageHintLabel.Size = new Size(356, 18);
        languageHintLabel.TabIndex = 2;
        languageHintLabel.Tag = "muted";
        languageHintLabel.Text = "Changes are applied immediately and saved automatically.";
        languageHintLabel.TextAlign = ContentAlignment.BottomLeft;
        // 
        // footerLayout
        // 
        footerLayout.ColumnCount = 2;
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        footerLayout.Controls.Add(closeButton, 1, 0);
        footerLayout.Dock = DockStyle.Fill;
        footerLayout.Location = new Point(14, 146);
        footerLayout.Margin = new Padding(0);
        footerLayout.Name = "footerLayout";
        footerLayout.RowCount = 1;
        footerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        footerLayout.Size = new Size(376, 50);
        footerLayout.TabIndex = 2;
        // 
        // closeButton
        // 
        closeButton.DialogResult = DialogResult.OK;
        closeButton.Dock = DockStyle.Fill;
        closeButton.Location = new Point(266, 12);
        closeButton.Margin = new Padding(0, 12, 0, 0);
        closeButton.Name = "closeButton";
        closeButton.Size = new Size(110, 38);
        closeButton.TabIndex = 0;
        closeButton.Tag = "primary";
        closeButton.Text = "Done";
        closeButton.UseVisualStyleBackColor = false;
        // 
        // SettingsDialog
        // 
        AcceptButton = closeButton;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(21, 23, 26);
        CancelButton = closeButton;
        ClientSize = new Size(404, 210);
        Controls.Add(rootLayout);
        Font = new Font("Segoe UI", 8.5F);
        ForeColor = Color.FromArgb(242, 242, 242);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "SettingsDialog";
        ShowInTaskbar = false;
        Size = new Size(420, 249);
        StartPosition = FormStartPosition.CenterParent;
        Text = "Nexus Control settings";
        rootLayout.ResumeLayout(false);
        languageGroupBox.ResumeLayout(false);
        languageLayout.ResumeLayout(false);
        footerLayout.ResumeLayout(false);
        ResumeLayout(false);
    }
}
