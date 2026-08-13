using System.Diagnostics;

namespace NexusControl.Agent.Security;

internal static class NexusPaths
{
    public static string DataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "NexusControl");

    public static string TrustedDevicesPath => Path.Combine(
        DataDirectory,
        "trusted-devices.json");

    public static string ActivityLogPath => Path.Combine(
        DataDirectory,
        "activity-history.jsonl");

    public static string DeviceStoreLockPath => Path.Combine(
        DataDirectory,
        "trusted-devices.lock");

    public static void EnsureSecureDataDirectory()
    {
        Directory.CreateDirectory(DataDirectory);
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            var systemDirectory = Environment.GetFolderPath(
                Environment.SpecialFolder.System);
            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(systemDirectory, "icacls.exe"),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            startInfo.ArgumentList.Add(DataDirectory);
            startInfo.ArgumentList.Add("/inheritance:r");
            startInfo.ArgumentList.Add("/grant:r");
            startInfo.ArgumentList.Add("*S-1-5-18:(OI)(CI)F");
            startInfo.ArgumentList.Add("*S-1-5-32-544:(OI)(CI)F");

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                throw new InvalidOperationException(
                    "Die Zugriffsrechte des Nexus-Datenordners konnten nicht gesetzt werden.");
            }

            if (!process.WaitForExit(10_000) || process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    "Windows konnte den Nexus-Datenordner nicht auf SYSTEM und Administratoren beschränken.");
            }
        }
        catch (Exception error) when (
            error is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                "Der geschützte Nexus-Datenordner konnte nicht eingerichtet werden.",
                error);
        }
    }
}
