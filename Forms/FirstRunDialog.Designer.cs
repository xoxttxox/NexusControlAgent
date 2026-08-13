#nullable enable

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using NexusControl.Agent.UI;

namespace NexusControl.Agent.Forms;

partial class FirstRunDialog
{
    private IContainer? components;
    private TableLayoutPanel rootLayout = null!;
    private TableLayoutPanel brandLayout = null!;
    private PictureBox logoPictureBox = null!;
    private Label brandLabel = null!;
    private Label versionLabel = null!;
    private Label welcomeLabel = null!;
    private Label descriptionLabel = null!;
    private DarkGroupBox languageGroupBox = null!;
    private TableLayoutPanel languageLayout = null!;
    private Label languageLabel = null!;
    private ComboBox languageComboBox = null!;
    private Label languageHintLabel = null!;
    private TableLayoutPanel featureLayout = null!;
    private Label featurePairingLabel = null!;
    private Label featureTrayLabel = null!;
    private Label privacyLabel = null!;
    private TableLayoutPanel footerLayout = null!;
    private Button continueButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            logoPictureBox?.Image?.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        rootLayout = new TableLayoutPanel();
        brandLayout = new TableLayoutPanel();
        logoPictureBox = new PictureBox();
        brandLabel = new Label();
        versionLabel = new Label();
        welcomeLabel = new Label();
        descriptionLabel = new Label();
        languageGroupBox = new DarkGroupBox();
        languageLayout = new TableLayoutPanel();
        languageLabel = new Label();
        languageComboBox = new ComboBox();
        languageHintLabel = new Label();
        featureLayout = new TableLayoutPanel();
        featurePairingLabel = new Label();
        featureTrayLabel = new Label();
        privacyLabel = new Label();
        footerLayout = new TableLayoutPanel();
        continueButton = new Button();
        rootLayout.SuspendLayout();
        brandLayout.SuspendLayout();
        ((ISupportInitialize)logoPictureBox).BeginInit();
        languageGroupBox.SuspendLayout();
        languageLayout.SuspendLayout();
        featureLayout.SuspendLayout();
        footerLayout.SuspendLayout();
        SuspendLayout();
        // 
        // rootLayout
        // 
        rootLayout.BackColor = Color.FromArgb(21, 23, 26);
        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(brandLayout, 0, 0);
        rootLayout.Controls.Add(welcomeLabel, 0, 1);
        rootLayout.Controls.Add(descriptionLabel, 0, 2);
        rootLayout.Controls.Add(languageGroupBox, 0, 3);
        rootLayout.Controls.Add(featureLayout, 0, 4);
        rootLayout.Controls.Add(privacyLabel, 0, 5);
        rootLayout.Controls.Add(footerLayout, 0, 6);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Location = new Point(0, 0);
        rootLayout.Margin = new Padding(0);
        rootLayout.Name = "rootLayout";
        rootLayout.Padding = new Padding(20);
        rootLayout.RowCount = 7;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 29F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        rootLayout.Size = new Size(484, 401);
        rootLayout.TabIndex = 0;
        // 
        // brandLayout
        // 
        brandLayout.ColumnCount = 3;
        brandLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56F));
        brandLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        brandLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
        brandLayout.Controls.Add(logoPictureBox, 0, 0);
        brandLayout.Controls.Add(brandLabel, 1, 0);
        brandLayout.Controls.Add(versionLabel, 2, 0);
        brandLayout.Dock = DockStyle.Fill;
        brandLayout.Location = new Point(20, 20);
        brandLayout.Margin = new Padding(0);
        brandLayout.Name = "brandLayout";
        brandLayout.RowCount = 1;
        brandLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        brandLayout.Size = new Size(444, 58);
        brandLayout.TabIndex = 0;
        // 
        // logoPictureBox
        // 
        logoPictureBox.BackColor = Color.Transparent;
        logoPictureBox.Location = new Point(0, 6);
        logoPictureBox.Margin = new Padding(0);
        logoPictureBox.Name = "logoPictureBox";
        logoPictureBox.Size = new Size(44, 44);
        logoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        logoPictureBox.TabIndex = 0;
        logoPictureBox.TabStop = false;
        // 
        // brandLabel
        // 
        brandLabel.Dock = DockStyle.Fill;
        brandLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        brandLabel.Location = new Point(56, 0);
        brandLabel.Margin = new Padding(0);
        brandLabel.Name = "brandLabel";
        brandLabel.Size = new Size(298, 58);
        brandLabel.TabIndex = 1;
        brandLabel.Text = "NEXUS CONTROL AGENT";
        brandLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // versionLabel
        // 
        versionLabel.Dock = DockStyle.Fill;
        versionLabel.Location = new Point(354, 0);
        versionLabel.Margin = new Padding(0);
        versionLabel.Name = "versionLabel";
        versionLabel.Size = new Size(90, 58);
        versionLabel.TabIndex = 2;
        versionLabel.Tag = "muted";
        versionLabel.Text = "Version 0.11.4";
        versionLabel.TextAlign = ContentAlignment.MiddleRight;
        // 
        // welcomeLabel
        // 
        welcomeLabel.Dock = DockStyle.Fill;
        welcomeLabel.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
        welcomeLabel.Location = new Point(20, 78);
        welcomeLabel.Margin = new Padding(0);
        welcomeLabel.Name = "welcomeLabel";
        welcomeLabel.Size = new Size(444, 42);
        welcomeLabel.TabIndex = 1;
        welcomeLabel.Text = "Welcome to Nexus Control";
        welcomeLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // descriptionLabel
        // 
        descriptionLabel.Dock = DockStyle.Fill;
        descriptionLabel.Font = new Font("Segoe UI", 9F);
        descriptionLabel.Location = new Point(20, 120);
        descriptionLabel.Margin = new Padding(0);
        descriptionLabel.Name = "descriptionLabel";
        descriptionLabel.Size = new Size(444, 44);
        descriptionLabel.TabIndex = 2;
        descriptionLabel.Tag = "muted";
        descriptionLabel.Text = "Control and monitor your Windows PC securely from your smartphone.";
        descriptionLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // languageGroupBox
        // 
        languageGroupBox.Controls.Add(languageLayout);
        languageGroupBox.Dock = DockStyle.Fill;
        languageGroupBox.Location = new Point(20, 164);
        languageGroupBox.Margin = new Padding(0);
        languageGroupBox.Name = "languageGroupBox";
        languageGroupBox.Padding = new Padding(10, 20, 10, 7);
        languageGroupBox.Size = new Size(444, 88);
        languageGroupBox.TabIndex = 3;
        languageGroupBox.TabStop = false;
        languageGroupBox.Text = "Language";
        // 
        // languageLayout
        // 
        languageLayout.ColumnCount = 2;
        languageLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 142F));
        languageLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        languageLayout.Controls.Add(languageLabel, 0, 0);
        languageLayout.Controls.Add(languageComboBox, 1, 0);
        languageLayout.Controls.Add(languageHintLabel, 0, 1);
        languageLayout.Dock = DockStyle.Fill;
        languageLayout.Location = new Point(10, 36);
        languageLayout.Margin = new Padding(0);
        languageLayout.Name = "languageLayout";
        languageLayout.RowCount = 2;
        languageLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 27F));
        languageLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        languageLayout.Size = new Size(424, 45);
        languageLayout.TabIndex = 0;
        // 
        // languageLabel
        // 
        languageLabel.Dock = DockStyle.Fill;
        languageLabel.Location = new Point(0, 0);
        languageLabel.Margin = new Padding(0);
        languageLabel.Name = "languageLabel";
        languageLabel.Size = new Size(142, 27);
        languageLabel.TabIndex = 0;
        languageLabel.Text = "Choose your language:";
        languageLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // languageComboBox
        // 
        languageComboBox.Dock = DockStyle.Fill;
        languageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        languageComboBox.FormattingEnabled = true;
        languageComboBox.Location = new Point(142, 2);
        languageComboBox.Margin = new Padding(0, 2, 0, 2);
        languageComboBox.Name = "languageComboBox";
        languageComboBox.Size = new Size(282, 23);
        languageComboBox.TabIndex = 1;
        languageComboBox.SelectedIndexChanged += LanguageComboBoxSelectedIndexChanged;
        // 
        // languageHintLabel
        // 
        languageLayout.SetColumnSpan(languageHintLabel, 2);
        languageHintLabel.Dock = DockStyle.Fill;
        languageHintLabel.Location = new Point(0, 27);
        languageHintLabel.Margin = new Padding(0);
        languageHintLabel.Name = "languageHintLabel";
        languageHintLabel.Size = new Size(424, 18);
        languageHintLabel.TabIndex = 2;
        languageHintLabel.Tag = "muted";
        languageHintLabel.Text = "You can change this later in Settings.";
        languageHintLabel.TextAlign = ContentAlignment.BottomLeft;
        // 
        // featureLayout
        // 
        featureLayout.ColumnCount = 1;
        featureLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        featureLayout.Controls.Add(featurePairingLabel, 0, 0);
        featureLayout.Controls.Add(featureTrayLabel, 0, 1);
        featureLayout.Dock = DockStyle.Fill;
        featureLayout.Location = new Point(20, 252);
        featureLayout.Margin = new Padding(0);
        featureLayout.Name = "featureLayout";
        featureLayout.RowCount = 2;
        featureLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        featureLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        featureLayout.Size = new Size(444, 60);
        featureLayout.TabIndex = 4;
        // 
        // featurePairingLabel
        // 
        featurePairingLabel.Dock = DockStyle.Fill;
        featurePairingLabel.Location = new Point(0, 0);
        featurePairingLabel.Margin = new Padding(0);
        featurePairingLabel.Name = "featurePairingLabel";
        featurePairingLabel.Size = new Size(444, 30);
        featurePairingLabel.TabIndex = 0;
        featurePairingLabel.Text = "• Pair your smartphone using a QR code";
        featurePairingLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // featureTrayLabel
        // 
        featureTrayLabel.Dock = DockStyle.Fill;
        featureTrayLabel.Location = new Point(0, 30);
        featureTrayLabel.Margin = new Padding(0);
        featureTrayLabel.Name = "featureTrayLabel";
        featureTrayLabel.Size = new Size(444, 30);
        featureTrayLabel.TabIndex = 1;
        featureTrayLabel.Text = "• Runs quietly in the Windows system tray";
        featureTrayLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // privacyLabel
        // 
        privacyLabel.Dock = DockStyle.Fill;
        privacyLabel.Font = new Font("Segoe UI", 8F);
        privacyLabel.Location = new Point(20, 312);
        privacyLabel.Margin = new Padding(0);
        privacyLabel.Name = "privacyLabel";
        privacyLabel.Size = new Size(444, 29);
        privacyLabel.TabIndex = 5;
        privacyLabel.Tag = "muted";
        privacyLabel.Text = "Local and secure · No Windows passwords or PINs are stored";
        privacyLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // footerLayout
        // 
        footerLayout.ColumnCount = 2;
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        footerLayout.Controls.Add(continueButton, 1, 0);
        footerLayout.Dock = DockStyle.Fill;
        footerLayout.Location = new Point(20, 341);
        footerLayout.Margin = new Padding(0);
        footerLayout.Name = "footerLayout";
        footerLayout.RowCount = 1;
        footerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        footerLayout.Size = new Size(444, 40);
        footerLayout.TabIndex = 6;
        // 
        // continueButton
        // 
        continueButton.DialogResult = DialogResult.OK;
        continueButton.Dock = DockStyle.Fill;
        continueButton.Location = new Point(294, 4);
        continueButton.Margin = new Padding(0, 4, 0, 0);
        continueButton.Name = "continueButton";
        continueButton.Size = new Size(150, 36);
        continueButton.TabIndex = 0;
        continueButton.Tag = "primary";
        continueButton.Text = "Get started";
        continueButton.UseVisualStyleBackColor = false;
        // 
        // FirstRunDialog
        // 
        AcceptButton = continueButton;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(21, 23, 26);
        ClientSize = new Size(484, 401);
        Controls.Add(rootLayout);
        Font = new Font("Segoe UI", 8.5F);
        ForeColor = Color.FromArgb(242, 242, 242);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "FirstRunDialog";
        ShowInTaskbar = true;
        Size = new Size(500, 440);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Welcome to Nexus Control";
        rootLayout.ResumeLayout(false);
        brandLayout.ResumeLayout(false);
        ((ISupportInitialize)logoPictureBox).EndInit();
        languageGroupBox.ResumeLayout(false);
        languageLayout.ResumeLayout(false);
        featureLayout.ResumeLayout(false);
        footerLayout.ResumeLayout(false);
        ResumeLayout(false);
    }
}
