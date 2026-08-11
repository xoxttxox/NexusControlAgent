using System.ComponentModel;
using System.Diagnostics;

namespace NexusControl.Agent.Services;

internal sealed class FirewallService
{
    private const string ScriptRelativePath = "Scripts\\install-firewall.ps1";
    private readonly int _port;

    public FirewallService(int port)
    {
        if (port is <= 0 or > 65535)
        {
            throw new ArgumentOutOfRangeException(
                nameof(port),
                port,
                "Der Firewall-Port muss zwischen 1 und 65535 liegen.");
        }

        _port = port;
    }

    public async Task<bool> IsConfiguredAsync(
        CancellationToken cancellationToken = default)
    {
        var scriptPath = GetScriptPath();
        if (!File.Exists(scriptPath))
        {
            return false;
        }

        using var process = new Process
        {
            StartInfo = CreatePowerShellStartInfo(
                scriptPath,
                checkOnly: true,
                elevated: false),
        };

        if (!process.Start())
        {
            return false;
        }

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode == 0;
    }

    public async Task<FirewallSetupResult> InstallElevatedAsync(
        CancellationToken cancellationToken = default)
    {
        var scriptPath = GetScriptPath();
        if (!File.Exists(scriptPath))
        {
            return FirewallSetupResult.Failed(
                $"Das Firewall-Skript wurde nicht gefunden: {scriptPath}");
        }

        try
        {
            using var process = new Process
            {
                StartInfo = CreatePowerShellStartInfo(
                    scriptPath,
                    checkOnly: false,
                    elevated: true),
            };

            if (!process.Start())
            {
                return FirewallSetupResult.Failed(
                    "Die Windows-Administratorabfrage konnte nicht gestartet werden.");
            }

            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode == 0
                ? FirewallSetupResult.Success()
                : FirewallSetupResult.Failed(
                    $"Windows konnte die Firewall-Regel nicht erstellen (Fehlercode {process.ExitCode}).");
        }
        catch (Win32Exception exception)
            when (exception.NativeErrorCode == 1223)
        {
            return FirewallSetupResult.CancelledByUser();
        }
        catch (Exception exception)
        {
            return FirewallSetupResult.Failed(exception.Message);
        }
    }

    private ProcessStartInfo CreatePowerShellStartInfo(
        string scriptPath,
        bool checkOnly,
        bool elevated)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = elevated,
            CreateNoWindow = !elevated,
            WindowStyle = ProcessWindowStyle.Hidden,
        };

        if (elevated)
        {
            startInfo.Verb = "runas";
        }
        else
        {
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
        }

        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add("-Port");
        startInfo.ArgumentList.Add(_port.ToString());
        startInfo.ArgumentList.Add("-ExecutablePath");
        startInfo.ArgumentList.Add(GetExecutablePath());

        if (checkOnly)
        {
            startInfo.ArgumentList.Add("-CheckOnly");
        }

        return startInfo;
    }

    private static string GetScriptPath() =>
        Path.Combine(AppContext.BaseDirectory, ScriptRelativePath);

    private static string GetExecutablePath()
    {
        var applicationPath = Environment.ProcessPath;
        return string.IsNullOrWhiteSpace(applicationPath)
            ? Path.Combine(AppContext.BaseDirectory, "NexusControlAgent.exe")
            : Path.GetFullPath(applicationPath);
    }
}

internal sealed record FirewallSetupResult(
    bool Succeeded,
    bool Cancelled,
    string? Error)
{
    public static FirewallSetupResult Success() =>
        new(true, false, null);

    public static FirewallSetupResult CancelledByUser() =>
        new(false, true, null);

    public static FirewallSetupResult Failed(string error) =>
        new(false, false, error);
}
