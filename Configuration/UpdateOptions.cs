namespace NexusControl.Agent.Configuration;

internal sealed class UpdateOptions
{
    public const string SectionName = "Updates";

    public bool Enabled { get; set; } = true;

    public string RepositoryOwner { get; set; } = string.Empty;

    public string RepositoryName { get; set; } = "NexusControlAgent";

    public bool IncludePrereleases { get; set; }

    public int InitialCheckDelaySeconds { get; set; } = 8;

    public int CheckIntervalMinutes { get; set; } = 240;

    public int MaximumInstallerSizeMegabytes { get; set; } = 300;

    public int DownloadTimeoutMinutes { get; set; } = 15;

    public string InstallerAssetNamePattern { get; set; } =
        "NexusControlAgent-Setup-v{version}-win-x64.msi";

    public bool RequireSha256 { get; set; } = true;

    public bool RequireTrustedSignature { get; set; }

    public string TrustedPublisherSubject { get; set; } = string.Empty;

    public bool IsConfigured =>
        Enabled
        && IsRealValue(RepositoryOwner)
        && IsRealValue(RepositoryName)
        && InstallerAssetNamePattern.Contains(
            "{version}",
            StringComparison.OrdinalIgnoreCase);

    public string GetInstallerAssetName(Version version) =>
        InstallerAssetNamePattern.Replace(
            "{version}",
            ToReleaseVersion(version),
            StringComparison.OrdinalIgnoreCase);

    public static string ToReleaseVersion(Version version)
    {
        var build = Math.Max(version.Build, 0);
        return $"{version.Major}.{version.Minor}.{build}";
    }

    private static bool IsRealValue(string? value)
    {
        var normalized = value?.Trim();
        return !string.IsNullOrWhiteSpace(normalized)
            && !normalized.Equals(
                "OWNER",
                StringComparison.OrdinalIgnoreCase)
            && !normalized.StartsWith(
                "DEIN-",
                StringComparison.OrdinalIgnoreCase)
            && !normalized.StartsWith(
                "YOUR-",
                StringComparison.OrdinalIgnoreCase);
    }
}
