using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NexusControl.Agent.Configuration;
using NexusControl.Agent.Models;
using NexusControl.Agent.Security;
using NexusControl.Agent.Updates;

namespace NexusControl.Agent.Services;

internal sealed class UpdateService : BackgroundService
{
    private readonly UpdateOptions _options;
    private readonly GitHubReleaseClient _releaseClient;
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private readonly object _stateLock = new();
    private UpdateSnapshot _snapshot;
    private UpdateInstallResult? _lastInstallResult;

    public UpdateService(
        IOptions<UpdateOptions> options,
        GitHubReleaseClient releaseClient)
    {
        _options = options.Value;
        _releaseClient = releaseClient;
        _snapshot = !_options.Enabled
            ? new UpdateSnapshot(
                UpdateStage.Disabled,
                "Automatische Updates sind deaktiviert.")
            : !_options.IsConfigured
                ? new UpdateSnapshot(
                    UpdateStage.NotConfigured,
                    "Der GitHub-Updater ist noch nicht konfiguriert.")
                : new UpdateSnapshot(
                    UpdateStage.Idle,
                    "Updateprüfung ist bereit.");
        _lastInstallResult = UpdateInstaller.ConsumeLastResult();
        CleanupOldUpdates();
    }

    public event Action<UpdateSnapshot>? SnapshotChanged;

    public event Action? InstallationRequested;

    public UpdateSnapshot Snapshot
    {
        get
        {
            lock (_stateLock)
            {
                return _snapshot;
            }
        }
    }

    public UpdateInstallResult? ConsumeLastInstallResult() =>
        Interlocked.Exchange(ref _lastInstallResult, null);

    public async Task<UpdateSnapshot> CheckNowAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || !_options.IsConfigured)
        {
            Publish(!_options.Enabled
                ? new UpdateSnapshot(
                    UpdateStage.Disabled,
                    "Automatische Updates sind deaktiviert.")
                : new UpdateSnapshot(
                    UpdateStage.NotConfigured,
                    "Trage zuerst den GitHub-Benutzernamen unter Updates:RepositoryOwner in appsettings.json ein."));
            return Snapshot;
        }

        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            if (Snapshot.Stage is UpdateStage.Downloading
                or UpdateStage.Verifying
                or UpdateStage.Installing)
            {
                return Snapshot;
            }

            Publish(new UpdateSnapshot(
                UpdateStage.Checking,
                "GitHub wird nach einer neuen Version durchsucht …"));
            var release = await _releaseClient.GetLatestReleaseAsync(
                _options,
                cancellationToken);
            var currentVersion = ParseCurrentVersion();
            if (release.Version <= currentVersion)
            {
                Publish(new UpdateSnapshot(
                    UpdateStage.UpToDate,
                    $"Version {UpdateOptions.ToReleaseVersion(currentVersion)} ist aktuell.",
                    release));
            }
            else
            {
                Publish(new UpdateSnapshot(
                    UpdateStage.Available,
                    $"Version {release.DisplayVersion} ist verfügbar.",
                    release));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Publish(new UpdateSnapshot(
                UpdateStage.Error,
                "Die Updateprüfung wurde abgebrochen.",
                Error: "Das Zeitlimit für die Updateprüfung wurde erreicht."));
            throw;
        }
        catch (Exception error)
        {
            Publish(new UpdateSnapshot(
                UpdateStage.Error,
                "Die Updateprüfung ist fehlgeschlagen.",
                Error: error.Message));
        }
        finally
        {
            _operationLock.Release();
        }

        return Snapshot;
    }

    public async Task DownloadAndInstallAsync(
        CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            var release = Snapshot.Release;
            if (release is null || release.Version <= ParseCurrentVersion())
            {
                throw new InvalidOperationException(
                    "Es steht derzeit kein neueres Update zur Installation bereit.");
            }

            // Jeder Versuch bekommt einen eigenen Ordner. Dadurch können weder
            // ein früherer Updater noch Windows Installer oder ein Virenscanner
            // die neue Download-Datei durch einen alten Dateihandle blockieren.
            var updateDirectory = Path.Combine(
                NexusPaths.UpdatesDirectory,
                release.DisplayVersion,
                $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}");
            var installerPath = Path.Combine(
                updateDirectory,
                release.InstallerFileName);
            var maximumBytes = _options.MaximumInstallerSizeMegabytes
                * 1024L
                * 1024L;

            Publish(new UpdateSnapshot(
                UpdateStage.Downloading,
                $"Version {release.DisplayVersion} wird heruntergeladen …",
                release));
            using (var downloadTimeout =
                   CancellationTokenSource.CreateLinkedTokenSource(
                       cancellationToken))
            {
                downloadTimeout.CancelAfter(TimeSpan.FromMinutes(
                    _options.DownloadTimeoutMinutes));
                try
                {
                    await _releaseClient.DownloadInstallerAsync(
                        release,
                        installerPath,
                        maximumBytes,
                        progress => Publish(new UpdateSnapshot(
                            UpdateStage.Downloading,
                            $"Update wird heruntergeladen … {progress}%",
                            release,
                            progress)),
                        downloadTimeout.Token);
                }
                catch (OperationCanceledException)
                    when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException(
                        $"Der Update-Download wurde nach {_options.DownloadTimeoutMinutes} Minuten ohne Abschluss beendet.");
                }
            }

            Publish(new UpdateSnapshot(
                UpdateStage.Verifying,
                "Download und Herausgeber werden überprüft …",
                release,
                100));
            await VerifyInstallerAsync(
                release,
                installerPath,
                cancellationToken);

            UpdateInstaller.Launch(installerPath, release.DisplayVersion);
            Publish(new UpdateSnapshot(
                UpdateStage.Installing,
                "Der Agent wird geschlossen und das Update installiert …",
                release,
                100));
            InstallationRequested?.Invoke();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            Publish(new UpdateSnapshot(
                UpdateStage.Error,
                "Das Update konnte nicht vorbereitet werden.",
                Snapshot.Release,
                Error: error.Message));
        }
        finally
        {
            _operationLock.Release();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || !_options.IsConfigured)
        {
            return;
        }

        try
        {
            await Task.Delay(
                TimeSpan.FromSeconds(_options.InitialCheckDelaySeconds),
                stoppingToken);

            // Die sichtbare Startprüfung läuft vor dem eigentlichen Hoststart.
            // Nur wenn sie deaktiviert oder übersprungen wurde, ist der Zustand
            // hier noch Idle und die erste Hintergrundprüfung wird nachgeholt.
            if (Snapshot.Stage == UpdateStage.Idle)
            {
                await CheckNowAsync(stoppingToken);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(
                    TimeSpan.FromMinutes(_options.CheckIntervalMinutes),
                    stoppingToken);
                await CheckNowAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normaler Host-Shutdown.
        }
    }

    private async Task VerifyInstallerAsync(
        UpdateRelease release,
        string installerPath,
        CancellationToken cancellationToken)
    {
        var expectedHash = await _releaseClient.GetExpectedSha256Async(
            release,
            cancellationToken);
        if (_options.RequireSha256 && string.IsNullOrWhiteSpace(expectedHash))
        {
            throw new InvalidDataException(
                "Das GitHub Release enthält weder einen SHA-256-Digest noch eine passende .sha256-Datei.");
        }

        if (!string.IsNullOrWhiteSpace(expectedHash))
        {
            byte[] actualHash;
            await using (var installer = new FileStream(
                             installerPath,
                             FileMode.Open,
                             FileAccess.Read,
                             FileShare.Read,
                             81_920,
                             FileOptions.Asynchronous
                                 | FileOptions.SequentialScan))
            {
                actualHash = await SHA256.HashDataAsync(
                    installer,
                    cancellationToken);
            }

            var expectedHashBytes = Convert.FromHexString(expectedHash);
            if (!CryptographicOperations.FixedTimeEquals(
                    actualHash,
                    expectedHashBytes))
            {
                TryDeleteInvalidInstaller(installerPath);
                throw new InvalidDataException(
                    "Die SHA-256-Prüfsumme des Installers stimmt nicht. Der Download wurde verworfen.");
            }
        }

        if (_options.RequireTrustedSignature)
        {
            await AuthenticodeVerifier.VerifyAsync(
                installerPath,
                _options.TrustedPublisherSubject,
                cancellationToken);
        }
    }

    private void Publish(UpdateSnapshot snapshot)
    {
        lock (_stateLock)
        {
            _snapshot = snapshot;
        }

        var handlers = SnapshotChanged;
        if (handlers is null)
        {
            return;
        }

        foreach (Action<UpdateSnapshot> handler in handlers.GetInvocationList())
        {
            try
            {
                handler(snapshot);
            }
            catch
            {
                // Ein geschlossenes UI darf weder andere Anzeigen noch den
                // Update-Hintergrunddienst beenden.
            }
        }
    }

    private static void TryDeleteInvalidInstaller(string installerPath)
    {
        try
        {
            File.Delete(installerPath);
        }
        catch (IOException)
        {
            // Die Datei wird niemals gestartet. Ein Virenscanner darf die
            // verständliche Prüfsummenmeldung nicht durch einen Lock-Fehler
            // ersetzen; der eindeutige Ordner wird später aufgeräumt.
        }
        catch (UnauthorizedAccessException)
        {
            // Siehe oben: Sicherheitsprüfung bleibt fehlgeschlagen.
        }
    }

    private static Version ParseCurrentVersion()
    {
        if (!Version.TryParse(TelemetryService.AgentVersion, out var version))
        {
            return new Version(0, 0, 0, 0);
        }

        return new Version(
            version.Major,
            version.Minor,
            Math.Max(version.Build, 0),
            Math.Max(version.Revision, 0));
    }

    private static void CleanupOldUpdates()
    {
        try
        {
            if (!Directory.Exists(NexusPaths.UpdatesDirectory))
            {
                return;
            }

            var cutoff = DateTime.UtcNow.AddDays(-14);
            foreach (var directory in Directory.EnumerateDirectories(
                         NexusPaths.UpdatesDirectory))
            {
                try
                {
                    if (Directory.GetLastWriteTimeUtc(directory) < cutoff)
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                }
                catch
                {
                    // Ein noch laufender Helfer oder Virenscanner darf den Start nicht stören.
                }
            }
        }
        catch
        {
            // Aufräumen ist Best Effort.
        }
    }
}
