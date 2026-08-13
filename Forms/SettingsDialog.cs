using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using NexusControl.Agent.Localization;
using NexusControl.Agent.UI;

namespace NexusControl.Agent.Forms;

[DesignerCategory("Form")]
internal sealed partial class SettingsDialog : Form
{
    private bool _syncingLanguage;

    public SettingsDialog()
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

    private void ApplyLocalization() =>
        LocalizationService.Apply(this, nameof(SettingsDialog));

    private void TrySetApplicationIcon()
    {
        try
        {
            var executablePath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                using var extracted =
                    System.Drawing.Icon.ExtractAssociatedIcon(executablePath);
                Icon = extracted is null
                    ? SystemIcons.Application
                    : (System.Drawing.Icon)extracted.Clone();
            }
        }
        catch
        {
            Icon = SystemIcons.Application;
        }
    }
}
