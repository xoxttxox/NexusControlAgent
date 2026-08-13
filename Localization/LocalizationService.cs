using System.Globalization;
using System.Resources;
using System.Text.Json;
using System.Windows.Forms;
using NexusControl.Agent.Security;

namespace NexusControl.Agent.Localization;

/// <summary>
/// Central UI localization with an English fallback and a persisted,
/// per-Windows-user language preference.
/// </summary>
internal static class LocalizationService
{
    public const string DefaultLanguageCode = "en";

    private static readonly Lock Gate = new();
    private static readonly ResourceManager Resources = new(
        "NexusControl.Agent.Localization.Strings",
        typeof(LocalizationService).Assembly);
    private static readonly LanguageOption[] Languages =
    [
        new("en", "English", "en-US"),
        new("de", "Deutsch", "de-DE"),
        new("fr", "Français", "fr-FR"),
        new("es", "Español", "es-ES"),
        new("it", "Italiano", "it-IT"),
        new("pl", "Polski", "pl-PL"),
    ];

    private static CultureInfo _culture =
        CultureInfo.GetCultureInfo("en-US");
    private static string _languageCode = DefaultLanguageCode;
    private static bool _initialized;

    public static event EventHandler? LanguageChanged;

    public static IReadOnlyList<LanguageOption> SupportedLanguages => Languages;

    public static string CurrentLanguageCode
    {
        get
        {
            lock (Gate)
            {
                return _languageCode;
            }
        }
    }

    public static CultureInfo CurrentCulture
    {
        get
        {
            lock (Gate)
            {
                return _culture;
            }
        }
    }

    /// <summary>
    /// Must run before the first user-facing text is created. English remains
    /// the default when no saved preference exists or the file is invalid.
    /// </summary>
    public static void Initialize()
    {
        lock (Gate)
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            ApplyLanguageLocked(ResolveLanguage(LoadSavedLanguage()));
        }
    }

    public static bool SetLanguage(string? languageCode)
    {
        LanguageOption language;
        bool changed;
        lock (Gate)
        {
            language = ResolveLanguage(languageCode);
            changed = !string.Equals(
                _languageCode,
                language.Code,
                StringComparison.OrdinalIgnoreCase);
            ApplyLanguageLocked(language);
            SaveLanguage(language.Code);
        }

        if (changed)
        {
            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }

        return changed;
    }

    public static string Text(string key)
    {
        CultureInfo culture;
        lock (Gate)
        {
            culture = _culture;
        }

        return Resources.GetString(key, culture)
            ?? Resources.GetString(
                key,
                CultureInfo.GetCultureInfo("en-US"))
            ?? key;
    }

    public static string Format(string key, params object?[] arguments) =>
        string.Format(CurrentCulture, Text(key), arguments);

    /// <summary>
    /// Applies static texts by convention. A form uses Scope.Title, while its
    /// controls use Scope.ControlName. Dynamic values are updated by the form.
    /// </summary>
    public static void Apply(Control root, string scope)
    {
        var title = TryText($"{scope}.Title");
        if (title is not null)
        {
            root.Text = title;
        }

        ApplyChildren(root, scope);
    }

    private static void ApplyChildren(Control parent, string scope)
    {
        foreach (Control control in parent.Controls)
        {
            var text = TryText($"{scope}.{control.Name}");
            if (text is not null)
            {
                control.Text = text;
            }

            if (control is ToolStrip toolStrip)
            {
                foreach (ToolStripItem item in toolStrip.Items)
                {
                    ApplyToolStripItem(item, scope);
                }
            }

            ApplyChildren(control, scope);
        }
    }

    private static void ApplyToolStripItem(ToolStripItem item, string scope)
    {
        var text = TryText($"{scope}.{item.Name}");
        if (text is not null)
        {
            item.Text = text;
        }

        if (item is not ToolStripDropDownItem dropDownItem)
        {
            return;
        }

        foreach (ToolStripItem child in dropDownItem.DropDownItems)
        {
            ApplyToolStripItem(child, scope);
        }
    }

    private static string? TryText(string key)
    {
        CultureInfo culture;
        lock (Gate)
        {
            culture = _culture;
        }

        return Resources.GetString(key, culture)
            ?? Resources.GetString(
                key,
                CultureInfo.GetCultureInfo("en-US"));
    }

    private static LanguageOption ResolveLanguage(string? languageCode)
    {
        var normalized = string.IsNullOrWhiteSpace(languageCode)
            ? DefaultLanguageCode
            : languageCode.Trim().Split('-', '_')[0].ToLowerInvariant();
        return Languages.FirstOrDefault(language => string.Equals(
                language.Code,
                normalized,
                StringComparison.OrdinalIgnoreCase))
            ?? Languages[0];
    }

    private static void ApplyLanguageLocked(LanguageOption language)
    {
        _languageCode = language.Code;
        _culture = CultureInfo.GetCultureInfo(language.CultureName);
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _culture;
        CultureInfo.DefaultThreadCurrentCulture = _culture;
        CultureInfo.DefaultThreadCurrentUICulture = _culture;
    }

    private static string? LoadSavedLanguage()
    {
        try
        {
            if (!File.Exists(NexusPaths.LanguagePreferencesPath))
            {
                return null;
            }

            var settings = JsonSerializer.Deserialize<LanguagePreferences>(
                File.ReadAllText(NexusPaths.LanguagePreferencesPath));
            return settings?.Language;
        }
        catch
        {
            return null;
        }
    }

    private static void SaveLanguage(string languageCode)
    {
        try
        {
            Directory.CreateDirectory(NexusPaths.UserDataDirectory);
            var temporaryPath = $"{NexusPaths.LanguagePreferencesPath}.tmp";
            File.WriteAllText(
                temporaryPath,
                JsonSerializer.Serialize(
                    new LanguagePreferences(languageCode),
                    new JsonSerializerOptions { WriteIndented = true }));
            File.Move(
                temporaryPath,
                NexusPaths.LanguagePreferencesPath,
                overwrite: true);
        }
        catch
        {
            // A language preference must never prevent the Agent from running.
        }
    }

    private sealed record LanguagePreferences(string Language);
}

internal sealed record LanguageOption(
    string Code,
    string DisplayName,
    string CultureName)
{
    public override string ToString() => DisplayName;
}
