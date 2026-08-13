#nullable enable

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using NexusControl.Agent.UI;

namespace NexusControl.Agent.Forms;

partial class DeviceManagementDialog
{
    private IContainer? components;
    private TableLayoutPanel rootLayout = null!;
    private Label headingLabel = null!;
    private TableLayoutPanel selectorLayout = null!;
    private Label deviceSelectorLabel = null!;
    private ComboBox _deviceComboBox = null!;
    private DarkGroupBox detailsGroupBox = null!;
    private TableLayoutPanel detailsLayout = null!;
    private Label deviceNameLabel = null!;
    private TextBox _deviceNameTextBox = null!;
    private Label _deviceInfoLabel = null!;
    private CheckBox _remoteAccessCheckBox = null!;
    private Label permissionsCaptionLabel = null!;
    private TableLayoutPanel permissionGrid = null!;
    private CheckBox _systemControlCheckBox = null!;
    private CheckBox _touchpadCheckBox = null!;
    private CheckBox _processesCheckBox = null!;
    private CheckBox _mediaCheckBox = null!;
    private CheckBox _screenCheckBox = null!;
    private CheckBox _filesCheckBox = null!;
    private CheckBox _powerCheckBox = null!;
    private TableLayoutPanel footerLayout = null!;
    private Label _statusLabel = null!;
    private Button _saveButton = null!;
    private Button _removeButton = null!;
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
        headingLabel = new Label();
        selectorLayout = new TableLayoutPanel();
        deviceSelectorLabel = new Label();
        _deviceComboBox = new ComboBox();
        detailsGroupBox = new DarkGroupBox();
        detailsLayout = new TableLayoutPanel();
        deviceNameLabel = new Label();
        _deviceNameTextBox = new TextBox();
        _deviceInfoLabel = new Label();
        _remoteAccessCheckBox = new CheckBox();
        permissionsCaptionLabel = new Label();
        permissionGrid = new TableLayoutPanel();
        _systemControlCheckBox = new CheckBox();
        _touchpadCheckBox = new CheckBox();
        _processesCheckBox = new CheckBox();
        _mediaCheckBox = new CheckBox();
        _screenCheckBox = new CheckBox();
        _filesCheckBox = new CheckBox();
        _powerCheckBox = new CheckBox();
        footerLayout = new TableLayoutPanel();
        _statusLabel = new Label();
        _saveButton = new Button();
        _removeButton = new Button();
        closeButton = new Button();
        _refreshTimer = new System.Windows.Forms.Timer(components);
        rootLayout.SuspendLayout();
        selectorLayout.SuspendLayout();
        detailsGroupBox.SuspendLayout();
        detailsLayout.SuspendLayout();
        permissionGrid.SuspendLayout();
        footerLayout.SuspendLayout();
        SuspendLayout();
        // 
        // rootLayout
        // 
        rootLayout.BackColor = Color.FromArgb(21, 23, 26);
        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(headingLabel, 0, 0);
        rootLayout.Controls.Add(selectorLayout, 0, 1);
        rootLayout.Controls.Add(detailsGroupBox, 0, 2);
        rootLayout.Controls.Add(footerLayout, 0, 3);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Location = new Point(0, 0);
        rootLayout.Name = "rootLayout";
        rootLayout.Padding = new Padding(12);
        rootLayout.RowCount = 4;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        rootLayout.Size = new Size(484, 461);
        rootLayout.TabIndex = 0;
        // 
        // headingLabel
        // 
        headingLabel.Dock = DockStyle.Fill;
        headingLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        headingLabel.Location = new Point(15, 12);
        headingLabel.Name = "headingLabel";
        headingLabel.Size = new Size(454, 30);
        headingLabel.TabIndex = 0;
        headingLabel.Text = "Gekoppelte Smartphones verwalten";
        headingLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // selectorLayout
        // 
        selectorLayout.ColumnCount = 2;
        selectorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
        selectorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        selectorLayout.Controls.Add(deviceSelectorLabel, 0, 0);
        selectorLayout.Controls.Add(_deviceComboBox, 1, 0);
        selectorLayout.Dock = DockStyle.Fill;
        selectorLayout.Location = new Point(12, 42);
        selectorLayout.Margin = new Padding(0);
        selectorLayout.Name = "selectorLayout";
        selectorLayout.RowCount = 1;
        selectorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        selectorLayout.Size = new Size(460, 38);
        selectorLayout.TabIndex = 1;
        // 
        // deviceSelectorLabel
        // 
        deviceSelectorLabel.Dock = DockStyle.Fill;
        deviceSelectorLabel.Location = new Point(3, 0);
        deviceSelectorLabel.Name = "deviceSelectorLabel";
        deviceSelectorLabel.Size = new Size(94, 38);
        deviceSelectorLabel.TabIndex = 0;
        deviceSelectorLabel.Tag = "muted";
        deviceSelectorLabel.Text = "Gerät auswählen:";
        deviceSelectorLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _deviceComboBox
        // 
        _deviceComboBox.Dock = DockStyle.Fill;
        _deviceComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _deviceComboBox.FormattingEnabled = true;
        _deviceComboBox.Location = new Point(100, 5);
        _deviceComboBox.Margin = new Padding(0, 5, 0, 5);
        _deviceComboBox.Name = "_deviceComboBox";
        _deviceComboBox.Size = new Size(360, 21);
        _deviceComboBox.TabIndex = 1;
        _deviceComboBox.SelectedIndexChanged += DeviceSelectionChanged;
        // 
        // detailsGroupBox
        // 
        detailsGroupBox.Controls.Add(detailsLayout);
        detailsGroupBox.Dock = DockStyle.Fill;
        detailsGroupBox.Location = new Point(12, 86);
        detailsGroupBox.Margin = new Padding(0, 6, 0, 6);
        detailsGroupBox.Name = "detailsGroupBox";
        detailsGroupBox.Padding = new Padding(10, 22, 10, 8);
        detailsGroupBox.Size = new Size(460, 315);
        detailsGroupBox.TabIndex = 2;
        detailsGroupBox.TabStop = false;
        detailsGroupBox.Text = "Gerät und Berechtigungen";
        // 
        // detailsLayout
        // 
        detailsLayout.ColumnCount = 2;
        detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
        detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        detailsLayout.Controls.Add(deviceNameLabel, 0, 0);
        detailsLayout.Controls.Add(_deviceNameTextBox, 1, 0);
        detailsLayout.Controls.Add(_deviceInfoLabel, 0, 1);
        detailsLayout.Controls.Add(_remoteAccessCheckBox, 0, 2);
        detailsLayout.Controls.Add(permissionsCaptionLabel, 0, 3);
        detailsLayout.Controls.Add(permissionGrid, 0, 4);
        detailsLayout.Dock = DockStyle.Fill;
        detailsLayout.Location = new Point(10, 38);
        detailsLayout.Margin = new Padding(0);
        detailsLayout.Name = "detailsLayout";
        detailsLayout.RowCount = 5;
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        detailsLayout.Size = new Size(440, 269);
        detailsLayout.TabIndex = 0;
        // 
        // deviceNameLabel
        // 
        deviceNameLabel.Dock = DockStyle.Fill;
        deviceNameLabel.Location = new Point(3, 0);
        deviceNameLabel.Name = "deviceNameLabel";
        deviceNameLabel.Size = new Size(86, 32);
        deviceNameLabel.TabIndex = 0;
        deviceNameLabel.Tag = "muted";
        deviceNameLabel.Text = "Gerätename:";
        deviceNameLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _deviceNameTextBox
        // 
        _deviceNameTextBox.Dock = DockStyle.Fill;
        _deviceNameTextBox.Location = new Point(92, 4);
        _deviceNameTextBox.Margin = new Padding(0, 4, 0, 4);
        _deviceNameTextBox.MaxLength = 80;
        _deviceNameTextBox.Name = "_deviceNameTextBox";
        _deviceNameTextBox.Size = new Size(348, 23);
        _deviceNameTextBox.TabIndex = 1;
        _deviceNameTextBox.TextChanged += SettingsChanged;
        // 
        // _deviceInfoLabel
        // 
        _deviceInfoLabel.AutoEllipsis = true;
        detailsLayout.SetColumnSpan(_deviceInfoLabel, 2);
        _deviceInfoLabel.Dock = DockStyle.Fill;
        _deviceInfoLabel.Location = new Point(3, 32);
        _deviceInfoLabel.Name = "_deviceInfoLabel";
        _deviceInfoLabel.Size = new Size(434, 36);
        _deviceInfoLabel.TabIndex = 2;
        _deviceInfoLabel.Tag = "muted";
        _deviceInfoLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _remoteAccessCheckBox
        // 
        detailsLayout.SetColumnSpan(_remoteAccessCheckBox, 2);
        _remoteAccessCheckBox.Dock = DockStyle.Fill;
        _remoteAccessCheckBox.Location = new Point(0, 68);
        _remoteAccessCheckBox.Margin = new Padding(0);
        _remoteAccessCheckBox.Name = "_remoteAccessCheckBox";
        _remoteAccessCheckBox.Size = new Size(440, 32);
        _remoteAccessCheckBox.TabIndex = 3;
        _remoteAccessCheckBox.Text = "Remote-Zugriff für dieses Gerät erlauben";
        _remoteAccessCheckBox.UseVisualStyleBackColor = true;
        _remoteAccessCheckBox.CheckedChanged += SettingsChanged;
        // 
        // permissionsCaptionLabel
        // 
        detailsLayout.SetColumnSpan(permissionsCaptionLabel, 2);
        permissionsCaptionLabel.Dock = DockStyle.Fill;
        permissionsCaptionLabel.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
        permissionsCaptionLabel.Location = new Point(3, 100);
        permissionsCaptionLabel.Name = "permissionsCaptionLabel";
        permissionsCaptionLabel.Size = new Size(434, 24);
        permissionsCaptionLabel.TabIndex = 4;
        permissionsCaptionLabel.Text = "Freigegebene Funktionen";
        permissionsCaptionLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // permissionGrid
        // 
        permissionGrid.ColumnCount = 2;
        detailsLayout.SetColumnSpan(permissionGrid, 2);
        permissionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        permissionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        permissionGrid.Controls.Add(_systemControlCheckBox, 0, 0);
        permissionGrid.Controls.Add(_touchpadCheckBox, 1, 0);
        permissionGrid.Controls.Add(_processesCheckBox, 0, 1);
        permissionGrid.Controls.Add(_mediaCheckBox, 1, 1);
        permissionGrid.Controls.Add(_screenCheckBox, 0, 2);
        permissionGrid.Controls.Add(_filesCheckBox, 1, 2);
        permissionGrid.Controls.Add(_powerCheckBox, 0, 3);
        permissionGrid.Dock = DockStyle.Fill;
        permissionGrid.Location = new Point(0, 124);
        permissionGrid.Margin = new Padding(0);
        permissionGrid.Name = "permissionGrid";
        permissionGrid.RowCount = 4;
        permissionGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        permissionGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        permissionGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        permissionGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        permissionGrid.Size = new Size(440, 145);
        permissionGrid.TabIndex = 5;
        // 
        // _systemControlCheckBox
        // 
        _systemControlCheckBox.Dock = DockStyle.Fill;
        _systemControlCheckBox.Location = new Point(0, 0);
        _systemControlCheckBox.Margin = new Padding(0);
        _systemControlCheckBox.Name = "_systemControlCheckBox";
        _systemControlCheckBox.Size = new Size(220, 36);
        _systemControlCheckBox.TabIndex = 0;
        _systemControlCheckBox.Text = "PC-Steuerung";
        _systemControlCheckBox.UseVisualStyleBackColor = true;
        _systemControlCheckBox.CheckedChanged += SettingsChanged;
        // 
        // _touchpadCheckBox
        // 
        _touchpadCheckBox.Dock = DockStyle.Fill;
        _touchpadCheckBox.Location = new Point(220, 0);
        _touchpadCheckBox.Margin = new Padding(0);
        _touchpadCheckBox.Name = "_touchpadCheckBox";
        _touchpadCheckBox.Size = new Size(220, 36);
        _touchpadCheckBox.TabIndex = 1;
        _touchpadCheckBox.Text = "Touchpad && Tastatur";
        _touchpadCheckBox.UseVisualStyleBackColor = true;
        _touchpadCheckBox.CheckedChanged += SettingsChanged;
        // 
        // _processesCheckBox
        // 
        _processesCheckBox.Dock = DockStyle.Fill;
        _processesCheckBox.Location = new Point(0, 36);
        _processesCheckBox.Margin = new Padding(0);
        _processesCheckBox.Name = "_processesCheckBox";
        _processesCheckBox.Size = new Size(220, 36);
        _processesCheckBox.TabIndex = 2;
        _processesCheckBox.Text = "Prozesse";
        _processesCheckBox.UseVisualStyleBackColor = true;
        _processesCheckBox.CheckedChanged += SettingsChanged;
        // 
        // _mediaCheckBox
        // 
        _mediaCheckBox.Dock = DockStyle.Fill;
        _mediaCheckBox.Location = new Point(220, 36);
        _mediaCheckBox.Margin = new Padding(0);
        _mediaCheckBox.Name = "_mediaCheckBox";
        _mediaCheckBox.Size = new Size(220, 36);
        _mediaCheckBox.TabIndex = 3;
        _mediaCheckBox.Text = "Medien && Lautstärke";
        _mediaCheckBox.UseVisualStyleBackColor = true;
        _mediaCheckBox.CheckedChanged += SettingsChanged;
        // 
        // _screenCheckBox
        // 
        _screenCheckBox.Dock = DockStyle.Fill;
        _screenCheckBox.Location = new Point(0, 72);
        _screenCheckBox.Margin = new Padding(0);
        _screenCheckBox.Name = "_screenCheckBox";
        _screenCheckBox.Size = new Size(220, 36);
        _screenCheckBox.TabIndex = 4;
        _screenCheckBox.Text = "Bildschirmübertragung";
        _screenCheckBox.UseVisualStyleBackColor = true;
        _screenCheckBox.CheckedChanged += SettingsChanged;
        // 
        // _filesCheckBox
        // 
        _filesCheckBox.Dock = DockStyle.Fill;
        _filesCheckBox.Location = new Point(220, 72);
        _filesCheckBox.Margin = new Padding(0);
        _filesCheckBox.Name = "_filesCheckBox";
        _filesCheckBox.Size = new Size(220, 36);
        _filesCheckBox.TabIndex = 5;
        _filesCheckBox.Text = "Dateien";
        _filesCheckBox.UseVisualStyleBackColor = true;
        _filesCheckBox.CheckedChanged += SettingsChanged;
        // 
        // _powerCheckBox
        // 
        _powerCheckBox.Dock = DockStyle.Fill;
        _powerCheckBox.Location = new Point(0, 108);
        _powerCheckBox.Margin = new Padding(0);
        _powerCheckBox.Name = "_powerCheckBox";
        _powerCheckBox.Size = new Size(220, 37);
        _powerCheckBox.TabIndex = 6;
        _powerCheckBox.Text = "Energiebefehle";
        _powerCheckBox.UseVisualStyleBackColor = true;
        _powerCheckBox.CheckedChanged += SettingsChanged;
        // 
        // footerLayout
        // 
        footerLayout.ColumnCount = 6;
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88F));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 6F));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88F));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 6F));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76F));
        footerLayout.Controls.Add(_statusLabel, 0, 0);
        footerLayout.Controls.Add(_saveButton, 1, 0);
        footerLayout.Controls.Add(_removeButton, 3, 0);
        footerLayout.Controls.Add(closeButton, 5, 0);
        footerLayout.Dock = DockStyle.Fill;
        footerLayout.Location = new Point(12, 407);
        footerLayout.Margin = new Padding(0);
        footerLayout.Name = "footerLayout";
        footerLayout.RowCount = 1;
        footerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        footerLayout.Size = new Size(460, 42);
        footerLayout.TabIndex = 3;
        // 
        // _statusLabel
        // 
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.Location = new Point(3, 0);
        _statusLabel.Name = "_statusLabel";
        _statusLabel.Size = new Size(190, 42);
        _statusLabel.TabIndex = 0;
        _statusLabel.Tag = "muted";
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _saveButton
        // 
        _saveButton.Dock = DockStyle.Fill;
        _saveButton.Location = new Point(196, 4);
        _saveButton.Margin = new Padding(0, 4, 0, 4);
        _saveButton.Name = "_saveButton";
        _saveButton.Size = new Size(88, 34);
        _saveButton.TabIndex = 1;
        _saveButton.Tag = "primary";
        _saveButton.Text = "Speichern";
        _saveButton.UseVisualStyleBackColor = true;
        _saveButton.Click += SaveButtonClicked;
        // 
        // _removeButton
        // 
        _removeButton.Dock = DockStyle.Fill;
        _removeButton.Location = new Point(290, 4);
        _removeButton.Margin = new Padding(0, 4, 0, 4);
        _removeButton.Name = "_removeButton";
        _removeButton.Size = new Size(88, 34);
        _removeButton.TabIndex = 2;
        _removeButton.Text = "Entfernen";
        _removeButton.UseVisualStyleBackColor = true;
        _removeButton.Click += RemoveButtonClicked;
        // 
        // closeButton
        // 
        closeButton.DialogResult = DialogResult.Cancel;
        closeButton.Dock = DockStyle.Fill;
        closeButton.Location = new Point(384, 4);
        closeButton.Margin = new Padding(0, 4, 0, 4);
        closeButton.Name = "closeButton";
        closeButton.Size = new Size(76, 34);
        closeButton.TabIndex = 3;
        closeButton.Text = "Schließen";
        closeButton.UseVisualStyleBackColor = true;
        // 
        // _refreshTimer
        // 
        _refreshTimer.Interval = 2000;
        _refreshTimer.Tick += RefreshTimerTick;
        // 
        // DeviceManagementDialog
        // 
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(21, 23, 26);
        CancelButton = closeButton;
        ClientSize = new Size(484, 461);
        Controls.Add(rootLayout);
        Font = new Font("Segoe UI", 8.5F);
        ForeColor = Color.FromArgb(242, 242, 242);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "DeviceManagementDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Gekoppelte Geräte";
        rootLayout.ResumeLayout(false);
        selectorLayout.ResumeLayout(false);
        detailsGroupBox.ResumeLayout(false);
        detailsLayout.ResumeLayout(false);
        detailsLayout.PerformLayout();
        permissionGrid.ResumeLayout(false);
        footerLayout.ResumeLayout(false);
        ResumeLayout(false);
    }

}
