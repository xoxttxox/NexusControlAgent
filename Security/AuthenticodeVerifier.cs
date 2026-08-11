using System.Diagnostics;

namespace NexusControl.Agent.Security;

internal static class AuthenticodeVerifier
{
    public static async Task VerifyAsync(
        string filePath,
        string? expectedPublisherSubject,
        CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "Authenticode-Prüfungen werden nur unter Windows unterstützt.");
        }

        var powerShellPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell",
            "v1.0",
            "powershell.exe");
        if (!File.Exists(powerShellPath))
        {
            throw new FileNotFoundException(
                "Windows PowerShell wurde für die Signaturprüfung nicht gefunden.",
                powerShellPath);
        }

        const string script =
            "$signature = Get-AuthenticodeSignature -LiteralPath $env:NEXUS_UPDATE_FILE; " +
            "if ($signature.Status -ne 'Valid') { " +
            "  [Console]::Error.WriteLine('Ungültige Authenticode-Signatur: ' + $signature.StatusMessage); exit 10 }; " +
            "$publisher = $env:NEXUS_UPDATE_PUBLISHER; " +
            "if ($publisher -and $signature.SignerCertificate.Subject -notmatch [Regex]::Escape($publisher)) { " +
            "  [Console]::Error.WriteLine('Der Herausgeber der Signatur stimmt nicht überein.'); exit 11 }; " +
            "exit 0";

        var startInfo = new ProcessStartInfo
        {
            FileName = powerShellPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("-NoLogo");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(script);
        startInfo.Environment["NEXUS_UPDATE_FILE"] = filePath;
        startInfo.Environment["NEXUS_UPDATE_PUBLISHER"] =
            expectedPublisherSubject?.Trim() ?? string.Empty;

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException(
                "Die Authenticode-Prüfung konnte nicht gestartet werden.");
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        _ = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidDataException(
                string.IsNullOrWhiteSpace(error)
                    ? "Die Authenticode-Signatur des Installers ist ungültig."
                    : error.Trim());
        }
    }
}
