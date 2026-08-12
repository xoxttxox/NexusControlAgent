#nullable enable

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using NexusControl.Agent.UI;

namespace NexusControl.Agent.Forms;

partial class AgentWindow
{
    private IContainer? components;
    private TableLayoutPanel appLayout = null!;
    private Panel headerPanel = null!;
    private Label localAgentLabel = null!;
    private Label onlineStatusLabel = null!;
    private TableLayoutPanel contentLayout = null!;
    private DarkGroupBox connectionGroupBox = null!;
    private TableLayoutPanel connectionLayout = null!;
    private Label primaryAddressCaptionLabel = null!;
    private Label primaryAddressValueLabel = null!;
    private Label trustedDevicesCaptionLabel = null!;
    private Label trustedDevicesValueLabel = null!;
    private Label remoteAccessCaptionLabel = null!;
    private Label remoteAccessValueLabel = null!;
    private Label serverPortCaptionLabel = null!;
    private Label serverPortValueLabel = null!;
    private Label endpointsCaptionLabel = null!;
    private Panel endpointsBorderPanel = null!;
    private ListBox endpointsListBox = null!;
    private DarkGroupBox pairingGroupBox = null!;
    private TableLayoutPanel pairingLayout = null!;
    private Panel qrHostPanel = null!;
    private PictureBox qrCodePictureBox = null!;
    private TableLayoutPanel pairingDetailsLayout = null!;
    private Label pairingCodeCaptionLabel = null!;
    private TextBox pairingCodeTextBox = null!;
    private Label pairingExpiryLabel = null!;
    private ProgressBar pairingProgressBar = null!;
    private Panel pairingSpacerPanel = null!;
    private TableLayoutPanel pairingButtonLayout = null!;
    private Button copyPairingCodeButton = null!;
    private Button rotatePairingCodeButton = null!;
    private DarkGroupBox behaviorGroupBox = null!;
    private TableLayoutPanel behaviorLayout = null!;
    private CheckBox autoStartCheckBox = null!;
    private Button refreshButton = null!;
    private Button hideButton = null!;
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel statusLabel = null!;
    private ToolStripStatusLabel versionStatusLabel = null!;
    private System.Windows.Forms.Timer refreshTimer = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            refreshTimer?.Stop();
            qrCodePictureBox?.Image?.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        appLayout = new TableLayoutPanel();
        headerPanel = new Panel();
        localAgentLabel = new Label();
        onlineStatusLabel = new Label();
        contentLayout = new TableLayoutPanel();
        connectionGroupBox = new DarkGroupBox();
        connectionLayout = new TableLayoutPanel();
        primaryAddressCaptionLabel = new Label();
        primaryAddressValueLabel = new Label();
        trustedDevicesCaptionLabel = new Label();
        trustedDevicesValueLabel = new Label();
        remoteAccessCaptionLabel = new Label();
        remoteAccessValueLabel = new Label();
        serverPortCaptionLabel = new Label();
        serverPortValueLabel = new Label();
        endpointsCaptionLabel = new Label();
        endpointsBorderPanel = new Panel();
        endpointsListBox = new ListBox();
        pairingGroupBox = new DarkGroupBox();
        pairingLayout = new TableLayoutPanel();
        qrHostPanel = new Panel();
        qrCodePictureBox = new PictureBox();
        pairingDetailsLayout = new TableLayoutPanel();
        pairingCodeCaptionLabel = new Label();
        pairingCodeTextBox = new TextBox();
        pairingExpiryLabel = new Label();
        pairingProgressBar = new ProgressBar();
        pairingSpacerPanel = new Panel();
        pairingButtonLayout = new TableLayoutPanel();
        copyPairingCodeButton = new Button();
        rotatePairingCodeButton = new Button();
        behaviorGroupBox = new DarkGroupBox();
        behaviorLayout = new TableLayoutPanel();
        autoStartCheckBox = new CheckBox();
        refreshButton = new Button();
        hideButton = new Button();
        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel();
        versionStatusLabel = new ToolStripStatusLabel();
        refreshTimer = new System.Windows.Forms.Timer(components);
        appLayout.SuspendLayout();
        headerPanel.SuspendLayout();
        contentLayout.SuspendLayout();
        connectionGroupBox.SuspendLayout();
        connectionLayout.SuspendLayout();
        endpointsBorderPanel.SuspendLayout();
        pairingGroupBox.SuspendLayout();
        pairingLayout.SuspendLayout();
        qrHostPanel.SuspendLayout();
        ((ISupportInitialize)qrCodePictureBox).BeginInit();
        pairingDetailsLayout.SuspendLayout();
        pairingButtonLayout.SuspendLayout();
        behaviorGroupBox.SuspendLayout();
        behaviorLayout.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();
        // 
        // appLayout
        // 
        appLayout.BackColor = Color.FromArgb(21, 23, 26);
        appLayout.ColumnCount = 1;
        appLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        appLayout.Controls.Add(headerPanel, 0, 0);
        appLayout.Controls.Add(contentLayout, 0, 1);
        appLayout.Controls.Add(statusStrip, 0, 2);
        appLayout.Dock = DockStyle.Fill;
        appLayout.Location = new Point(0, 0);
        appLayout.Margin = new Padding(0);
        appLayout.Name = "appLayout";
        appLayout.RowCount = 3;
        appLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        appLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        appLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        appLayout.Size = new Size(484, 461);
        appLayout.TabIndex = 0;
        // 
        // headerPanel
        // 
        headerPanel.BackColor = Color.FromArgb(28, 31, 36);
        headerPanel.Controls.Add(localAgentLabel);
        headerPanel.Controls.Add(onlineStatusLabel);
        headerPanel.Dock = DockStyle.Fill;
        headerPanel.Location = new Point(0, 0);
        headerPanel.Margin = new Padding(0);
        headerPanel.Name = "headerPanel";
        headerPanel.Size = new Size(484, 28);
        headerPanel.TabIndex = 0;
        // 
        // localAgentLabel
        // 
        localAgentLabel.Dock = DockStyle.Left;
        localAgentLabel.Location = new Point(0, 0);
        localAgentLabel.Name = "localAgentLabel";
        localAgentLabel.Padding = new Padding(10, 0, 0, 0);
        localAgentLabel.Size = new Size(260, 28);
        localAgentLabel.TabIndex = 0;
        localAgentLabel.Tag = "muted";
        localAgentLabel.Text = "Lokaler PC-Agent";
        localAgentLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // onlineStatusLabel
        // 
        onlineStatusLabel.Dock = DockStyle.Right;
        onlineStatusLabel.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
        onlineStatusLabel.Location = new Point(394, 0);
        onlineStatusLabel.Name = "onlineStatusLabel";
        onlineStatusLabel.Padding = new Padding(0, 0, 10, 0);
        onlineStatusLabel.Size = new Size(90, 28);
        onlineStatusLabel.TabIndex = 1;
        onlineStatusLabel.Tag = "success";
        onlineStatusLabel.Text = "●  Online";
        onlineStatusLabel.TextAlign = ContentAlignment.MiddleRight;
        // 
        // contentLayout
        // 
        contentLayout.BackColor = Color.FromArgb(21, 23, 26);
        contentLayout.ColumnCount = 1;
        contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        contentLayout.Controls.Add(connectionGroupBox, 0, 0);
        contentLayout.Controls.Add(pairingGroupBox, 0, 2);
        contentLayout.Controls.Add(behaviorGroupBox, 0, 4);
        contentLayout.Dock = DockStyle.Fill;
        contentLayout.Location = new Point(0, 28);
        contentLayout.Margin = new Padding(0);
        contentLayout.Name = "contentLayout";
        contentLayout.Padding = new Padding(8);
        contentLayout.RowCount = 5;
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 8F));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 8F));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        contentLayout.Size = new Size(484, 409);
        contentLayout.TabIndex = 1;
        // 
        // connectionGroupBox
        // 
        connectionGroupBox.Controls.Add(connectionLayout);
        connectionGroupBox.Dock = DockStyle.Fill;
        connectionGroupBox.Location = new Point(8, 8);
        connectionGroupBox.Margin = new Padding(0);
        connectionGroupBox.Name = "connectionGroupBox";
        connectionGroupBox.Padding = new Padding(8, 18, 8, 6);
        connectionGroupBox.Size = new Size(468, 150);
        connectionGroupBox.TabIndex = 0;
        connectionGroupBox.TabStop = false;
        connectionGroupBox.Text = "Verbindung";
        // 
        // connectionLayout
        // 
        connectionLayout.ColumnCount = 4;
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55F));
        connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        connectionLayout.Controls.Add(primaryAddressCaptionLabel, 0, 0);
        connectionLayout.Controls.Add(primaryAddressValueLabel, 1, 0);
        connectionLayout.Controls.Add(trustedDevicesCaptionLabel, 2, 0);
        connectionLayout.Controls.Add(trustedDevicesValueLabel, 3, 0);
        connectionLayout.Controls.Add(remoteAccessCaptionLabel, 0, 1);
        connectionLayout.Controls.Add(remoteAccessValueLabel, 1, 1);
        connectionLayout.Controls.Add(serverPortCaptionLabel, 2, 1);
        connectionLayout.Controls.Add(serverPortValueLabel, 3, 1);
        connectionLayout.Controls.Add(endpointsCaptionLabel, 0, 2);
        connectionLayout.Controls.Add(endpointsBorderPanel, 0, 3);
        connectionLayout.Dock = DockStyle.Fill;
        connectionLayout.Location = new Point(8, 34);
        connectionLayout.Margin = new Padding(0);
        connectionLayout.Name = "connectionLayout";
        connectionLayout.RowCount = 4;
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
        connectionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        connectionLayout.Size = new Size(452, 110);
        connectionLayout.TabIndex = 0;
        // 
        // primaryAddressCaptionLabel
        // 
        primaryAddressCaptionLabel.Dock = DockStyle.Fill;
        primaryAddressCaptionLabel.Location = new Point(0, 0);
        primaryAddressCaptionLabel.Margin = new Padding(0);
        primaryAddressCaptionLabel.Name = "primaryAddressCaptionLabel";
        primaryAddressCaptionLabel.Size = new Size(92, 22);
        primaryAddressCaptionLabel.TabIndex = 0;
        primaryAddressCaptionLabel.Tag = "muted";
        primaryAddressCaptionLabel.Text = "Primäre Adresse:";
        primaryAddressCaptionLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // primaryAddressValueLabel
        // 
        primaryAddressValueLabel.AutoEllipsis = true;
        primaryAddressValueLabel.Dock = DockStyle.Fill;
        primaryAddressValueLabel.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
        primaryAddressValueLabel.Location = new Point(92, 0);
        primaryAddressValueLabel.Margin = new Padding(0);
        primaryAddressValueLabel.Name = "primaryAddressValueLabel";
        primaryAddressValueLabel.Size = new Size(183, 22);
        primaryAddressValueLabel.TabIndex = 1;
        primaryAddressValueLabel.Text = "Wird ermittelt …";
        primaryAddressValueLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // trustedDevicesCaptionLabel
        // 
        trustedDevicesCaptionLabel.Dock = DockStyle.Fill;
        trustedDevicesCaptionLabel.Location = new Point(275, 0);
        trustedDevicesCaptionLabel.Margin = new Padding(0);
        trustedDevicesCaptionLabel.Name = "trustedDevicesCaptionLabel";
        trustedDevicesCaptionLabel.Size = new Size(55, 22);
        trustedDevicesCaptionLabel.TabIndex = 2;
        trustedDevicesCaptionLabel.Tag = "muted";
        trustedDevicesCaptionLabel.Text = "Geräte:";
        trustedDevicesCaptionLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // trustedDevicesValueLabel
        // 
        trustedDevicesValueLabel.AutoEllipsis = true;
        trustedDevicesValueLabel.Dock = DockStyle.Fill;
        trustedDevicesValueLabel.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
        trustedDevicesValueLabel.Location = new Point(330, 0);
        trustedDevicesValueLabel.Margin = new Padding(0);
        trustedDevicesValueLabel.Name = "trustedDevicesValueLabel";
        trustedDevicesValueLabel.Size = new Size(122, 22);
        trustedDevicesValueLabel.TabIndex = 3;
        trustedDevicesValueLabel.Tag = "success";
        trustedDevicesValueLabel.Text = "0 Geräte";
        trustedDevicesValueLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // remoteAccessCaptionLabel
        // 
        remoteAccessCaptionLabel.Dock = DockStyle.Fill;
        remoteAccessCaptionLabel.Location = new Point(0, 22);
        remoteAccessCaptionLabel.Margin = new Padding(0);
        remoteAccessCaptionLabel.Name = "remoteAccessCaptionLabel";
        remoteAccessCaptionLabel.Size = new Size(92, 22);
        remoteAccessCaptionLabel.TabIndex = 4;
        remoteAccessCaptionLabel.Tag = "muted";
        remoteAccessCaptionLabel.Text = "Remote:";
        remoteAccessCaptionLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // remoteAccessValueLabel
        // 
        remoteAccessValueLabel.AutoEllipsis = true;
        remoteAccessValueLabel.Dock = DockStyle.Fill;
        remoteAccessValueLabel.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
        remoteAccessValueLabel.Location = new Point(92, 22);
        remoteAccessValueLabel.Margin = new Padding(0);
        remoteAccessValueLabel.Name = "remoteAccessValueLabel";
        remoteAccessValueLabel.Size = new Size(183, 22);
        remoteAccessValueLabel.TabIndex = 5;
        remoteAccessValueLabel.Text = "Wird geprüft …";
        remoteAccessValueLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // serverPortCaptionLabel
        // 
        serverPortCaptionLabel.Dock = DockStyle.Fill;
        serverPortCaptionLabel.Location = new Point(275, 22);
        serverPortCaptionLabel.Margin = new Padding(0);
        serverPortCaptionLabel.Name = "serverPortCaptionLabel";
        serverPortCaptionLabel.Size = new Size(55, 22);
        serverPortCaptionLabel.TabIndex = 6;
        serverPortCaptionLabel.Tag = "muted";
        serverPortCaptionLabel.Text = "Port:";
        serverPortCaptionLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // serverPortValueLabel
        // 
        serverPortValueLabel.Dock = DockStyle.Fill;
        serverPortValueLabel.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
        serverPortValueLabel.Location = new Point(330, 22);
        serverPortValueLabel.Margin = new Padding(0);
        serverPortValueLabel.Name = "serverPortValueLabel";
        serverPortValueLabel.Size = new Size(122, 22);
        serverPortValueLabel.TabIndex = 7;
        serverPortValueLabel.Text = "5188";
        serverPortValueLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // endpointsCaptionLabel
        // 
        connectionLayout.SetColumnSpan(endpointsCaptionLabel, 4);
        endpointsCaptionLabel.Dock = DockStyle.Fill;
        endpointsCaptionLabel.Font = new Font("Segoe UI", 8F);
        endpointsCaptionLabel.Location = new Point(0, 44);
        endpointsCaptionLabel.Margin = new Padding(0);
        endpointsCaptionLabel.Name = "endpointsCaptionLabel";
        endpointsCaptionLabel.Size = new Size(452, 18);
        endpointsCaptionLabel.TabIndex = 8;
        endpointsCaptionLabel.Tag = "muted";
        endpointsCaptionLabel.Text = "Adressen – Doppelklick kopiert";
        endpointsCaptionLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // endpointsBorderPanel
        // 
        endpointsBorderPanel.BackColor = Color.FromArgb(52, 56, 64);
        connectionLayout.SetColumnSpan(endpointsBorderPanel, 4);
        endpointsBorderPanel.Controls.Add(endpointsListBox);
        endpointsBorderPanel.Dock = DockStyle.Fill;
        endpointsBorderPanel.Location = new Point(0, 62);
        endpointsBorderPanel.Margin = new Padding(0);
        endpointsBorderPanel.Name = "endpointsBorderPanel";
        endpointsBorderPanel.Padding = new Padding(1);
        endpointsBorderPanel.Size = new Size(452, 48);
        endpointsBorderPanel.TabIndex = 9;
        endpointsBorderPanel.Tag = "border";
        // 
        // endpointsListBox
        // 
        endpointsListBox.BorderStyle = BorderStyle.None;
        endpointsListBox.Dock = DockStyle.Fill;
        endpointsListBox.Font = new Font("Consolas", 8.5F);
        endpointsListBox.FormattingEnabled = true;
        endpointsListBox.IntegralHeight = false;
        endpointsListBox.ItemHeight = 14;
        endpointsListBox.Location = new Point(1, 1);
        endpointsListBox.Margin = new Padding(0);
        endpointsListBox.Name = "endpointsListBox";
        endpointsListBox.Size = new Size(450, 46);
        endpointsListBox.TabIndex = 0;
        endpointsListBox.Tag = "borderless";
        endpointsListBox.DoubleClick += EndpointsListBoxDoubleClick;
        // 
        // pairingGroupBox
        // 
        pairingGroupBox.Controls.Add(pairingLayout);
        pairingGroupBox.Dock = DockStyle.Fill;
        pairingGroupBox.Location = new Point(8, 166);
        pairingGroupBox.Margin = new Padding(0);
        pairingGroupBox.Name = "pairingGroupBox";
        pairingGroupBox.Padding = new Padding(8, 18, 8, 6);
        pairingGroupBox.Size = new Size(468, 150);
        pairingGroupBox.TabIndex = 1;
        pairingGroupBox.TabStop = false;
        pairingGroupBox.Text = "Neues Gerät verbinden";
        // 
        // pairingLayout
        // 
        pairingLayout.ColumnCount = 3;
        pairingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        pairingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 10F));
        pairingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        pairingLayout.Controls.Add(qrHostPanel, 0, 0);
        pairingLayout.Controls.Add(pairingDetailsLayout, 2, 0);
        pairingLayout.Dock = DockStyle.Fill;
        pairingLayout.Location = new Point(8, 34);
        pairingLayout.Margin = new Padding(0);
        pairingLayout.Name = "pairingLayout";
        pairingLayout.RowCount = 1;
        pairingLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        pairingLayout.Size = new Size(452, 110);
        pairingLayout.TabIndex = 0;
        // 
        // qrHostPanel
        // 
        qrHostPanel.BackColor = Color.White;
        qrHostPanel.Controls.Add(qrCodePictureBox);
        qrHostPanel.Location = new Point(0, 0);
        qrHostPanel.Margin = new Padding(0);
        qrHostPanel.Name = "qrHostPanel";
        qrHostPanel.Padding = new Padding(4);
        qrHostPanel.Size = new Size(104, 104);
        qrHostPanel.TabIndex = 0;
        // 
        // qrCodePictureBox
        // 
        qrCodePictureBox.BackColor = Color.White;
        qrCodePictureBox.Dock = DockStyle.Fill;
        qrCodePictureBox.Location = new Point(4, 4);
        qrCodePictureBox.Name = "qrCodePictureBox";
        qrCodePictureBox.Size = new Size(96, 96);
        qrCodePictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        qrCodePictureBox.TabIndex = 0;
        qrCodePictureBox.TabStop = false;
        // 
        // pairingDetailsLayout
        // 
        pairingDetailsLayout.ColumnCount = 1;
        pairingDetailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        pairingDetailsLayout.Controls.Add(pairingCodeCaptionLabel, 0, 0);
        pairingDetailsLayout.Controls.Add(pairingCodeTextBox, 0, 1);
        pairingDetailsLayout.Controls.Add(pairingExpiryLabel, 0, 2);
        pairingDetailsLayout.Controls.Add(pairingProgressBar, 0, 3);
        pairingDetailsLayout.Controls.Add(pairingSpacerPanel, 0, 4);
        pairingDetailsLayout.Controls.Add(pairingButtonLayout, 0, 5);
        pairingDetailsLayout.Dock = DockStyle.Fill;
        pairingDetailsLayout.Location = new Point(120, 0);
        pairingDetailsLayout.Margin = new Padding(0);
        pairingDetailsLayout.Name = "pairingDetailsLayout";
        pairingDetailsLayout.RowCount = 6;
        pairingDetailsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));
        pairingDetailsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
        pairingDetailsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
        pairingDetailsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 8F));
        pairingDetailsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 5F));
        pairingDetailsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        pairingDetailsLayout.Size = new Size(332, 110);
        pairingDetailsLayout.TabIndex = 1;
        // 
        // pairingCodeCaptionLabel
        // 
        pairingCodeCaptionLabel.Dock = DockStyle.Fill;
        pairingCodeCaptionLabel.Font = new Font("Segoe UI", 8F);
        pairingCodeCaptionLabel.Location = new Point(0, 0);
        pairingCodeCaptionLabel.Margin = new Padding(0);
        pairingCodeCaptionLabel.Name = "pairingCodeCaptionLabel";
        pairingCodeCaptionLabel.Size = new Size(332, 16);
        pairingCodeCaptionLabel.TabIndex = 0;
        pairingCodeCaptionLabel.Tag = "muted";
        pairingCodeCaptionLabel.Text = "Pairing-Code";
        pairingCodeCaptionLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // pairingCodeTextBox
        // 
        pairingCodeTextBox.AutoSize = false;
        pairingCodeTextBox.Dock = DockStyle.Fill;
        pairingCodeTextBox.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
        pairingCodeTextBox.Location = new Point(0, 17);
        pairingCodeTextBox.Margin = new Padding(0, 1, 0, 2);
        pairingCodeTextBox.Name = "pairingCodeTextBox";
        pairingCodeTextBox.ReadOnly = true;
        pairingCodeTextBox.Size = new Size(332, 32);
        pairingCodeTextBox.TabIndex = 1;
        pairingCodeTextBox.TabStop = false;
        pairingCodeTextBox.Text = "000 000";
        pairingCodeTextBox.TextAlign = HorizontalAlignment.Center;
        pairingCodeTextBox.DoubleClick += PairingCodeTextBoxDoubleClick;
        // 
        // pairingExpiryLabel
        // 
        pairingExpiryLabel.Dock = DockStyle.Fill;
        pairingExpiryLabel.Font = new Font("Segoe UI", 8F);
        pairingExpiryLabel.Location = new Point(0, 51);
        pairingExpiryLabel.Margin = new Padding(0);
        pairingExpiryLabel.Name = "pairingExpiryLabel";
        pairingExpiryLabel.Size = new Size(332, 18);
        pairingExpiryLabel.TabIndex = 2;
        pairingExpiryLabel.Tag = "muted";
        pairingExpiryLabel.Text = "Noch 00:00 Minuten gültig";
        pairingExpiryLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // pairingProgressBar
        // 
        pairingProgressBar.Dock = DockStyle.Fill;
        pairingProgressBar.Location = new Point(0, 70);
        pairingProgressBar.Margin = new Padding(0, 1, 0, 1);
        pairingProgressBar.Maximum = 1000;
        pairingProgressBar.Name = "pairingProgressBar";
        pairingProgressBar.Size = new Size(332, 6);
        pairingProgressBar.Style = ProgressBarStyle.Continuous;
        pairingProgressBar.TabIndex = 3;
        // 
        // pairingSpacerPanel
        // 
        pairingSpacerPanel.Dock = DockStyle.Fill;
        pairingSpacerPanel.Location = new Point(0, 77);
        pairingSpacerPanel.Margin = new Padding(0);
        pairingSpacerPanel.Name = "pairingSpacerPanel";
        pairingSpacerPanel.Size = new Size(332, 5);
        pairingSpacerPanel.TabIndex = 4;
        // 
        // pairingButtonLayout
        // 
        pairingButtonLayout.ColumnCount = 2;
        pairingButtonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        pairingButtonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        pairingButtonLayout.Controls.Add(copyPairingCodeButton, 0, 0);
        pairingButtonLayout.Controls.Add(rotatePairingCodeButton, 1, 0);
        pairingButtonLayout.Dock = DockStyle.Fill;
        pairingButtonLayout.Location = new Point(0, 82);
        pairingButtonLayout.Margin = new Padding(0);
        pairingButtonLayout.Name = "pairingButtonLayout";
        pairingButtonLayout.RowCount = 1;
        pairingButtonLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        pairingButtonLayout.Size = new Size(332, 28);
        pairingButtonLayout.TabIndex = 5;
        // 
        // copyPairingCodeButton
        // 
        copyPairingCodeButton.Dock = DockStyle.Fill;
        copyPairingCodeButton.Location = new Point(0, 0);
        copyPairingCodeButton.Margin = new Padding(0, 0, 4, 0);
        copyPairingCodeButton.Name = "copyPairingCodeButton";
        copyPairingCodeButton.Size = new Size(162, 28);
        copyPairingCodeButton.TabIndex = 0;
        copyPairingCodeButton.Tag = "primary";
        copyPairingCodeButton.Text = "Code kopieren";
        copyPairingCodeButton.UseVisualStyleBackColor = false;
        copyPairingCodeButton.Click += CopyPairingCodeButtonClick;
        // 
        // rotatePairingCodeButton
        // 
        rotatePairingCodeButton.Dock = DockStyle.Fill;
        rotatePairingCodeButton.Location = new Point(170, 0);
        rotatePairingCodeButton.Margin = new Padding(4, 0, 0, 0);
        rotatePairingCodeButton.Name = "rotatePairingCodeButton";
        rotatePairingCodeButton.Size = new Size(162, 28);
        rotatePairingCodeButton.TabIndex = 1;
        rotatePairingCodeButton.Text = "Neuer Code";
        rotatePairingCodeButton.UseVisualStyleBackColor = false;
        rotatePairingCodeButton.Click += RotatePairingCodeButtonClick;
        // 
        // behaviorGroupBox
        // 
        behaviorGroupBox.Controls.Add(behaviorLayout);
        behaviorGroupBox.Dock = DockStyle.Fill;
        behaviorGroupBox.Location = new Point(8, 324);
        behaviorGroupBox.Margin = new Padding(0);
        behaviorGroupBox.Name = "behaviorGroupBox";
        behaviorGroupBox.Padding = new Padding(8, 18, 8, 6);
        behaviorGroupBox.Size = new Size(468, 77);
        behaviorGroupBox.TabIndex = 2;
        behaviorGroupBox.TabStop = false;
        behaviorGroupBox.Text = "Verhalten";
        // 
        // behaviorLayout
        // 
        behaviorLayout.ColumnCount = 4;
        behaviorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        behaviorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96F));
        behaviorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 6F));
        behaviorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82F));
        behaviorLayout.Controls.Add(autoStartCheckBox, 0, 0);
        behaviorLayout.Controls.Add(refreshButton, 1, 0);
        behaviorLayout.Controls.Add(hideButton, 3, 0);
        behaviorLayout.Dock = DockStyle.Fill;
        behaviorLayout.Location = new Point(8, 34);
        behaviorLayout.Margin = new Padding(0);
        behaviorLayout.Name = "behaviorLayout";
        behaviorLayout.RowCount = 1;
        behaviorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        behaviorLayout.Size = new Size(452, 37);
        behaviorLayout.TabIndex = 0;
        // 
        // autoStartCheckBox
        // 
        autoStartCheckBox.Dock = DockStyle.Fill;
        autoStartCheckBox.Location = new Point(0, 0);
        autoStartCheckBox.Margin = new Padding(0);
        autoStartCheckBox.Name = "autoStartCheckBox";
        autoStartCheckBox.Size = new Size(268, 37);
        autoStartCheckBox.TabIndex = 0;
        autoStartCheckBox.Text = "Mit Windows starten";
        autoStartCheckBox.UseVisualStyleBackColor = false;
        autoStartCheckBox.CheckedChanged += AutoStartCheckedChanged;
        // 
        // refreshButton
        // 
        refreshButton.Dock = DockStyle.Fill;
        refreshButton.Location = new Point(268, 0);
        refreshButton.Margin = new Padding(0);
        refreshButton.Name = "refreshButton";
        refreshButton.Size = new Size(96, 37);
        refreshButton.TabIndex = 1;
        refreshButton.Text = "Aktualisieren";
        refreshButton.UseVisualStyleBackColor = false;
        refreshButton.Click += RefreshButtonClick;
        // 
        // hideButton
        // 
        hideButton.Dock = DockStyle.Fill;
        hideButton.Location = new Point(370, 0);
        hideButton.Margin = new Padding(0);
        hideButton.Name = "hideButton";
        hideButton.Size = new Size(82, 37);
        hideButton.TabIndex = 2;
        hideButton.Tag = "primary";
        hideButton.Text = "Ausblenden";
        hideButton.UseVisualStyleBackColor = false;
        hideButton.Click += HideButtonClick;
        // 
        // statusStrip
        // 
        statusStrip.AutoSize = false;
        statusStrip.BackColor = Color.FromArgb(28, 31, 36);
        statusStrip.Dock = DockStyle.Fill;
        statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, versionStatusLabel });
        statusStrip.Location = new Point(0, 437);
        statusStrip.Name = "statusStrip";
        statusStrip.Padding = new Padding(6, 0, 6, 0);
        statusStrip.Size = new Size(484, 24);
        statusStrip.SizingGrip = false;
        statusStrip.TabIndex = 2;
        // 
        // statusLabel
        // 
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(401, 19);
        statusLabel.Spring = true;
        statusLabel.Text = "Agent wird gestartet …";
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // versionStatusLabel
        // 
        versionStatusLabel.Name = "versionStatusLabel";
        versionStatusLabel.Size = new Size(65, 19);
        versionStatusLabel.Text = "Version 0.11.3";
        versionStatusLabel.TextAlign = ContentAlignment.MiddleRight;
        // 
        // refreshTimer
        // 
        refreshTimer.Interval = 1000;
        refreshTimer.Tick += RefreshTimerTick;
        // 
        // AgentWindow
        // 
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(21, 23, 26);
        ClientSize = new Size(484, 461);
        Controls.Add(appLayout);
        DoubleBuffered = true;
        Font = new Font("Segoe UI", 8.5F);
        ForeColor = Color.FromArgb(242, 242, 242);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MaximumSize = new Size(500, 500);
        MinimumSize = new Size(500, 500);
        MinimizeBox = true;
        Name = "AgentWindow";
        ShowInTaskbar = true;
        Size = new Size(500, 500);
        SizeGripStyle = SizeGripStyle.Hide;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Nexus Control Agent";
        appLayout.ResumeLayout(false);
        headerPanel.ResumeLayout(false);
        contentLayout.ResumeLayout(false);
        connectionGroupBox.ResumeLayout(false);
        connectionLayout.ResumeLayout(false);
        endpointsBorderPanel.ResumeLayout(false);
        pairingGroupBox.ResumeLayout(false);
        pairingLayout.ResumeLayout(false);
        qrHostPanel.ResumeLayout(false);
        ((ISupportInitialize)qrCodePictureBox).EndInit();
        pairingDetailsLayout.ResumeLayout(false);
        pairingDetailsLayout.PerformLayout();
        pairingButtonLayout.ResumeLayout(false);
        behaviorGroupBox.ResumeLayout(false);
        behaviorLayout.ResumeLayout(false);
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
    }
}
