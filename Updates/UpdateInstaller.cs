using System.Diagnostics;
using System.Text.Json;
using NexusControl.Agent.Models;
using NexusControl.Agent.Security;

namespace NexusControl.Agent.Updates;

internal static class UpdateInstaller
{
    public const string ApplyCommand = "--apply-update";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public static bool IsUpdateHelper(IReadOnlyCollection<string> arguments) =>
        arguments.Any(argument => string.Equals(
            argument,
            ApplyCommand,
            StringComparison.OrdinalIgnoreCase));

    public static void Launch(
        string installerPath,
        string targetVersion)
    {
        var currentExecutable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(currentExecutable)
            || !File.Exists(currentExecutable))
        {
            throw new InvalidOperationException(
                "Der Pfad des laufenden Agenten konnte nicht ermittelt werden.");
        }

        var updateDirectory = Path.GetDirectoryName(installerPath)
            ?? throw new InvalidOperationException(
                "Der Update-Ordner konnte nicht ermittelt werden.");
        Directory.CreateDirectory(updateDirectory);
        var helperPath = Path.Combine(
            updateDirectory,
            $"NexusControlUpdater-{targetVersion}.exe");
        File.Copy(currentExecutable, helperPath, overwrite: true);

        var logPath = Path.Combine(
            updateDirectory,
            $"install-{targetVersion}.log");
        var startInfo = new ProcessStartInfo
        {
            FileName = helperPath,
            WorkingDirectory = updateDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add(ApplyCommand);
        startInfo.ArgumentList.Add($"--installer={installerPath}");
        startInfo.ArgumentList.Add($"--wait-pid={Environment.ProcessId}");
        startInfo.ArgumentList.Add($"--restart={currentExecutable}");
        startInfo.ArgumentList.Add($"--target-version={targetVersion}");
        startInfo.ArgumentList.Add($"--update-log={logPath}");

        _ = Process.Start(startInfo)
            ?? throw new InvalidOperationException(
                "Der Nexus-Control-Update-Helfer konnte nicht gestartet werden.");
    }

    public static async Task<int> RunAsync(
        IReadOnlyCollection<string> arguments,
        CancellationToken cancellationToken = default)
    {
        var installerPath = GetArgument(arguments, "--installer=");
        var restartPath = GetArgument(arguments, "--restart=");
        var targetVersion = GetArgument(arguments, "--target-version=")
            ?? "unbekannt";
        var logPath = GetArgument(arguments, "--update-log=")
            ?? Path.Combine(NexusPaths.UpdatesDirectory, "install.log");
        var waitPidText = GetArgument(arguments, "--wait-pid=");

        if (string.IsNullOrWhiteSpace(installerPath)
            || !File.Exists(installerPath)
            || !int.TryParse(waitPidText, out var waitPid)
            || waitPid <= 0)
        {
            return 87;
        }

        var exitCode = 1;
        try
        {
            await WaitForAgentExitAsync(waitPid, cancellationToken);
            await Task.Delay(750, cancellationToken);

            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            var installerStartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.System),
                    "msiexec.exe"),
                UseShellExecute = false,
                CreateNoWindow = false,
            };
            installerStartInfo.ArgumentList.Add("/i");
            installerStartInfo.ArgumentList.Add(installerPath);
            installerStartInfo.ArgumentList.Add("/passive");
            installerStartInfo.ArgumentList.Add("/norestart");
            installerStartInfo.ArgumentList.Add("/L*v");
            installerStartInfo.ArgumentList.Add(logPath);

            using var installer = Process.Start(installerStartInfo)
                ?? throw new InvalidOperationException(
                    "Windows Installer konnte nicht gestartet werden.");
            await installer.WaitForExitAsync(cancellationToken);
            exitCode = installer.ExitCode;
        }
        catch (OperationCanceledException)
        {
            exitCode = 1460;
        }
        catch (Exception error)
        {
            exitCode = 1;
            TryAppendLog(logPath, error.ToString());
        }

        var succeeded = exitCode is 0 or 1641 or 3010;
        WriteResult(new UpdateInstallResult(
            succeeded,
            exitCode,
            targetVersion,
            logPath,
            DateTimeOffset.UtcNow));

        if (!string.IsNullOrWhiteSpace(restartPath) && File.Exists(restartPath))
        {
            try
            {
                await Task.Delay(750, CancellationToken.None);
                _ = Process.Start(new ProcessStartInfo
                {
                    FileName = restartPath,
                    Arguments = "--tray",
                    UseShellExecute = true,
                });
            }
            catch (Exception error)
            {
                TryAppendLog(logPath, error.ToString());
            }
        }

        return succeeded ? 0 : exitCode;
    }

    public static UpdateInstallResult? ConsumeLastResult()
    {
        try
        {
            if (!File.Exists(NexusPaths.UpdateResultPath))
            {
                return null;
            }

            var json = File.ReadAllText(NexusPaths.UpdateResultPath);
            var result = JsonSerializer.Deserialize<UpdateInstallResult>(
                json,
                JsonOptions);
            File.Delete(NexusPaths.UpdateResultPath);
            return result;
        }
        catch
        {
            return null;
        }
    }

    private static async Task WaitForAgentExitAsync(
        int processId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMinutes(2));
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (ArgumentException)
        {
            // Der Hauptprozess wurde bereits beendet.
        }
    }

    private static string? GetArgument(
        IEnumerable<string> arguments,
        string prefix) =>
        arguments
            .FirstOrDefault(argument => argument.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase))
            ?[prefix.Length..];

    private static void WriteResult(UpdateInstallResult result)
    {
        try
        {
            Directory.CreateDirectory(NexusPaths.UpdatesDirectory);
            File.WriteAllText(
                NexusPaths.UpdateResultPath,
                JsonSerializer.Serialize(result, JsonOptions));
        }
        catch
        {
            // Das Update-Ergebnis ist Komfortfunktion und darf den Neustart nicht verhindern.
        }
    }

    private static void TryAppendLog(string logPath, string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(
                logPath,
                $"{Environment.NewLine}[Nexus Updater] {message}{Environment.NewLine}");
        }
        catch
        {
            // Ein nicht beschreibbares Log darf den Wiederanlauf nicht verhindern.
        }
    }
}
