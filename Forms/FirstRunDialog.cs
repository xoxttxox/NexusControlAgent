using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using NexusControl.Agent.Localization;
using NexusControl.Agent.Services;
using NexusControl.Agent.UI;

namespace NexusControl.Agent.Forms;

/// <summary>
/// One-time welcome flow shown before the Agent window is opened
/// interactively for the first time.
/// </summary>
[DesignerCategory("Form")]
internal sealed partial class FirstRunDialog : Form
{
    private bool _syncingLanguage;

    public FirstRunDialog()
    {
        InitializeComponent();
        InitializeLanguageSelector();
        ApplyLocalization();
        TrySetApplicationIcon();
        WinFormsTheme.Apply(this);
    }

    private void LanguageComboBoxSelectedIndexChanged(
        object? sender,
        EventArgs eventArgs)
    {
        if (
            _syncingLanguage
            || languageComboBox.SelectedItem is not LanguageOption language)
        {
            return;
        }

        LocalizationService.SetLanguage(language.Code);
        ApplyLocalization();
    }

    private void InitializeLanguageSelector()
    {
        _syncingLanguage = true;
        try
        {
            languageComboBox.BeginUpdate();
            languageComboBox.Items.Clear();
            foreach (var language in LocalizationService.SupportedLanguages)
            {
                languageComboBox.Items.Add(language);
            }

            languageComboBox.SelectedItem = LocalizationService
                .SupportedLanguages
                .First(language => string.Equals(
                    language.Code,
                    LocalizationService.CurrentLanguageCode,
                    StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            languageComboBox.EndUpdate();
            _syncingLanguage = false;
        }
    }

    private void ApplyLocalization()
    {
        LocalizationService.Apply(this, nameof(FirstRunDialog));
        versionLabel.Text = LocalizationService.Format(
            "Common.Version",
            TelemetryService.AgentVersion);
    }

    private void TrySetApplicationIcon()
    {
        try
        {
            var executablePath = Environment.ProcessPath;
            using var extracted = string.IsNullOrWhiteSpace(executablePath)
                ? null
                : System.Drawing.Icon.ExtractAssociatedIcon(executablePath);
            if (extracted is null)
            {
                Icon = SystemIcons.Application;
                logoPictureBox.Image = SystemIcons.Application.ToBitmap();
                return;
            }

            Icon = (System.Drawing.Icon)extracted.Clone();
            logoPictureBox.Image = extracted.ToBitmap();
        }
        catch
        {
            Icon = SystemIcons.Application;
            logoPictureBox.Image = SystemIcons.Application.ToBitmap();
        }
    }
}
