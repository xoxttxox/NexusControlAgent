namespace NexusControl.Agent.Models;

internal enum UpdateStage
{
    Disabled,
    NotConfigured,
    Idle,
    Checking,
    UpToDate,
    Available,
    Downloading,
    Verifying,
    Installing,
    Error,
}

internal sealed record UpdateRelease(
    Version Version,
    string DisplayVersion,
    string Name,
    string? Notes,
    Uri ReleasePage,
    Uri InstallerDownload,
    string InstallerFileName,
    long InstallerSizeBytes,
    string? InstallerDigest,
    Uri? ChecksumDownload,
    DateTimeOffset? PublishedAt);

internal sealed record UpdateSnapshot(
    UpdateStage Stage,
    string Message,
    UpdateRelease? Release = null,
    int ProgressPercent = 0,
    string? Error = null)
{
    public bool IsBusy =>
        Stage is UpdateStage.Checking
            or UpdateStage.Downloading
            or UpdateStage.Verifying
            or UpdateStage.Installing;
}

internal sealed record UpdateInstallResult(
    bool Succeeded,
    int ExitCode,
    string Version,
    string LogPath,
    DateTimeOffset CompletedAt);
