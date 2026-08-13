using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.Win32;
using NexusControl.Agent.Localization;

namespace NexusControl.Agent.Services;

internal sealed class AutoStartService
{
    private const string TaskName = "NexusControlAgent";
    private const string InstallerRegistryKey =
        @"Software\Nexus Control\Nexus Control Agent";
    private const string InstallerAutoStartValue = "AutoStartInstalled";
    private const string PreferenceInitializedValue =
        "AutoStartPreferenceInitialized";
    private const string PreferenceEnabledValue = "AutoStartPreferenceEnabled";

    /// <summary>
    /// Applies the option selected in the MSI after the application is started
    /// for the first time. The application already runs elevated because its
    /// manifest requires administrator rights, so Task Scheduler can create an
    /// interactive, highest-privilege logon task for the actual user.
    /// </summary>
    public AutoStartResult ApplyInstallerPreference()
    {
        var preference = ReadSavedPreference();
        if (preference is not null)
        {
            if (preference.Value)
            {
                // Ältere Agent-Versionen konnten bereits eine Aufgabe ohne
                // --tray hinterlassen. Nur das Vorhandensein der Aufgabe zu
                // prüfen reicht deshalb nicht: Eine veraltete Aktion wird hier
                // einmalig mit dem aktuellen, unsichtbaren Tray-Start ersetzt.
                return IsConfiguredForTrayStart()
                    ? AutoStartResult.Success()
                    : SetEnabledCore(true);
            }

            return IsEnabled()
                ? SetEnabledCore(false)
                : AutoStartResult.Success();
        }

        if (!InstallerRequestedAutoStart())
        {
            return AutoStartResult.Success();
        }

        var result = SetEnabledCore(true);
        if (result.Succeeded)
        {
            SavePreference(true);
        }

        return result;
    }

    public bool IsEnabled()
    {
        var result = RunTaskScheduler(
            ["/Query", "/TN", TaskName]);
        return result.ExitCode == 0;
    }

    /// <summary>
    /// Changes autostart from the app UI and remembers the user's choice so a
    /// later app start does not silently undo it.
    /// </summary>
    public AutoStartResult SetEnabled(bool enabled)
    {
        var result = SetEnabledCore(enabled);
        if (result.Succeeded)
        {
            SavePreference(enabled);
        }

        return result;
    }

    /// <summary>
    /// Removes the scheduled task and installer preference during uninstall.
    /// This method is intentionally separate from the normal user toggle.
    /// </summary>
    public AutoStartResult RemoveForUninstall()
    {
        var result = SetEnabledCore(false);
        ClearPreference();
        return result;
    }

    private AutoStartResult SetEnabledCore(bool enabled)
    {
        if (!enabled)
        {
            var deleteResult = RunTaskScheduler(
                ["/Delete", "/TN", TaskName, "/F"]);

            return deleteResult.ExitCode == 0 || TaskDoesNotExist(deleteResult)
                ? AutoStartResult.Success()
                : AutoStartResult.Failure(
                    BuildError(
                        deleteResult,
                        LocalizationService.Text(
                            "Service.AutoStart.RemoveFailed")));
        }

        var executablePath = Environment.ProcessPath;
        if (
            string.IsNullOrWhiteSpace(executablePath)
            || !File.Exists(executablePath)
            || !string.Equals(
                Path.GetExtension(executablePath),
                ".exe",
                StringComparison.OrdinalIgnoreCase))
        {
            return AutoStartResult.Failure(
                LocalizationService.Text(
                    "Service.AutoStart.RequiresBuiltExe"));
        }

        // schtasks receives the complete action as one /TR argument. The quotes
        // are required because the installation path normally contains spaces.
        var taskAction = $"\"{executablePath}\" --tray";
        var createResult = RunTaskScheduler(
            [
                "/Create",
                "/SC",
                "ONLOGON",
                "/TN",
                TaskName,
                "/TR",
                taskAction,
                "/RL",
                "HIGHEST",
                "/IT",
                "/F",
            ]);

        return createResult.ExitCode == 0
            ? AutoStartResult.Success()
            : AutoStartResult.Failure(
                BuildError(
                    createResult,
                    LocalizationService.Text(
                        "Service.AutoStart.SetupFailed")));
    }

    private static bool IsConfiguredForTrayStart()
    {
        var executablePath = Environment.ProcessPath;
        if (
            string.IsNullOrWhiteSpace(executablePath)
            || !File.Exists(executablePath))
        {
            return false;
        }

        var result = RunTaskScheduler(
            ["/Query", "/TN", TaskName, "/XML"]);
        if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.Output))
        {
            return false;
        }

        try
        {
            var document = XDocument.Parse(result.Output);
            var execAction = document
                .Descendants()
                .FirstOrDefault(element =>
                    string.Equals(
                        element.Name.LocalName,
                        "Exec",
                        StringComparison.Ordinal));
            if (execAction is null)
            {
                return false;
            }

            var command = GetChildElementValue(execAction, "Command");
            var arguments = GetChildElementValue(execAction, "Arguments");
            var completeAction = $"{command} {arguments}";

            return !string.IsNullOrWhiteSpace(command)
                && command.Contains(
                    executablePath,
                    StringComparison.OrdinalIgnoreCase)
                && completeAction.Contains(
                    "--tray",
                    StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // Eine nicht lesbare oder fremd veränderte Aufgabe gilt als alt und
            // wird durch SetEnabledCore mit unserer bekannten Aktion ersetzt.
            return false;
        }
    }

    private static string GetChildElementValue(
        XElement parent,
        string localName) =>
        parent
            .Elements()
            .FirstOrDefault(element =>
                string.Equals(
                    element.Name.LocalName,
                    localName,
                    StringComparison.Ordinal))
            ?.Value
        ?? string.Empty;

    private static bool InstallerRequestedAutoStart()
    {
        try
        {
            using var localMachine = RegistryKey.OpenBaseKey(
                RegistryHive.LocalMachine,
                RegistryView.Registry64);
            using var key = localMachine.OpenSubKey(InstallerRegistryKey);
            return ReadInteger(key, InstallerAutoStartValue) == 1;
        }
        catch
        {
            // Missing access or a missing key must never prevent the agent from
            // starting. The user can still enable autostart from the app UI.
            return false;
        }
    }

    private static bool? ReadSavedPreference()
    {
        try
        {
            using var localMachine = RegistryKey.OpenBaseKey(
                RegistryHive.LocalMachine,
                RegistryView.Registry64);
            using var key = localMachine.OpenSubKey(InstallerRegistryKey);
            if (ReadInteger(key, PreferenceInitializedValue) != 1)
            {
                return null;
            }

            return ReadInteger(key, PreferenceEnabledValue) == 1;
        }
        catch
        {
            return null;
        }
    }

    private static void SavePreference(bool enabled)
    {
        try
        {
            using var localMachine = RegistryKey.OpenBaseKey(
                RegistryHive.LocalMachine,
                RegistryView.Registry64);
            using var key = localMachine.CreateSubKey(
                InstallerRegistryKey,
                writable: true);
            key?.SetValue(
                PreferenceInitializedValue,
                1,
                RegistryValueKind.DWord);
            key?.SetValue(
                PreferenceEnabledValue,
                enabled ? 1 : 0,
                RegistryValueKind.DWord);
        }
        catch
        {
            // The task state is authoritative. Failure to persist the UI choice
            // must not turn a successful Task Scheduler operation into an error.
        }
    }

    private static void ClearPreference()
    {
        try
        {
            using var localMachine = RegistryKey.OpenBaseKey(
                RegistryHive.LocalMachine,
                RegistryView.Registry64);
            using var key = localMachine.OpenSubKey(
                InstallerRegistryKey,
                writable: true);
            key?.DeleteValue(PreferenceInitializedValue, throwOnMissingValue: false);
            key?.DeleteValue(PreferenceEnabledValue, throwOnMissingValue: false);
        }
        catch
        {
            // Cleanup is best effort and must never block an uninstall.
        }
    }

    private static int? ReadInteger(RegistryKey? key, string valueName)
    {
        var value = key?.GetValue(valueName);
        return value switch
        {
            int integerValue => integerValue,
            long longValue when longValue is >= int.MinValue and <= int.MaxValue =>
                (int)longValue,
            string text when int.TryParse(text, out var parsed) => parsed,
            _ => null,
        };
    }

    private static bool TaskDoesNotExist(SchedulerResult result)
    {
        var text = $"{result.Output}{Environment.NewLine}{result.Error}";
        return text.Contains("cannot find", StringComparison.OrdinalIgnoreCase)
            || text.Contains("nicht gefunden", StringComparison.OrdinalIgnoreCase)
            || text.Contains("does not exist", StringComparison.OrdinalIgnoreCase);
    }

    private static SchedulerResult RunTaskScheduler(
        IReadOnlyList<string> arguments)
    {
        try
        {
            var executable = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "schtasks.exe");
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return new SchedulerResult(
                    -1,
                    "",
                    LocalizationService.Text(
                        "Service.AutoStart.SchedulerStartFailed"));
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(10_000))
            {
                try
                {
                    process.Kill(true);
                }
                catch
                {
                    // The process may already have ended.
                }

                return new SchedulerResult(
                    -1,
                    "",
                    LocalizationService.Text(
                        "Service.AutoStart.SchedulerTimeout"));
            }

            Task.WaitAll([outputTask, errorTask], 2_000);
            return new SchedulerResult(
                process.ExitCode,
                outputTask.IsCompletedSuccessfully ? outputTask.Result : "",
                errorTask.IsCompletedSuccessfully ? errorTask.Result : "");
        }
        catch (Exception error)
        {
            return new SchedulerResult(-1, "", error.Message);
        }
    }

    private static string BuildError(
        SchedulerResult result,
        string fallback)
    {
        var message = string.IsNullOrWhiteSpace(result.Error)
            ? result.Output
            : result.Error;
        return string.IsNullOrWhiteSpace(message)
            ? fallback
            : $"{fallback}{Environment.NewLine}{message.Trim()}";
    }

    private sealed record SchedulerResult(
        int ExitCode,
        string Output,
        string Error);
}

internal sealed record AutoStartResult(
    bool Succeeded,
    string? Error)
{
    public static AutoStartResult Success() => new(true, null);

    public static AutoStartResult Failure(string error) =>
        new(false, error);
}
