using System.Buffers;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using NexusControl.Agent.Configuration;
using NexusControl.Agent.Models;

namespace NexusControl.Agent.Updates;

internal sealed partial class GitHubReleaseClient
{
    private readonly HttpClient _client;

    public GitHubReleaseClient(IHttpClientFactory httpClientFactory)
    {
        _client = httpClientFactory.CreateClient("GitHubUpdates");
    }

    public async Task<UpdateRelease> GetLatestReleaseAsync(
        UpdateOptions options,
        CancellationToken cancellationToken)
    {
        var owner = Uri.EscapeDataString(options.RepositoryOwner.Trim());
        var repository = Uri.EscapeDataString(options.RepositoryName.Trim());
        var endpoint = options.IncludePrereleases
            ? $"repos/{owner}/{repository}/releases?per_page=20"
            : $"repos/{owner}/{repository}/releases/latest";

        using var response = await _client.GetAsync(endpoint, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException(
                "GitHub hat noch kein passendes Release gefunden. Prüfe Repository, Sichtbarkeit und Release-Status.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"GitHub-Updateprüfung fehlgeschlagen (HTTP {(int)response.StatusCode}).");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(
            cancellationToken);
        var release = options.IncludePrereleases
            ? (await JsonSerializer.DeserializeAsync<List<GitHubRelease>>(
                    stream,
                    JsonOptions,
                    cancellationToken))
                ?.Where(candidate => !candidate.Draft)
                .OrderByDescending(candidate =>
                    candidate.PublishedAt ?? DateTimeOffset.MinValue)
                .FirstOrDefault()
            : await JsonSerializer.DeserializeAsync<GitHubRelease>(
                stream,
                JsonOptions,
                cancellationToken);

        if (release is null || release.Draft)
        {
            throw new InvalidOperationException(
                "Die GitHub-Antwort enthält kein veröffentlichtes Release.");
        }

        var version = ParseVersion(release.TagName);
        if (version is null)
        {
            throw new InvalidOperationException(
                $"Der Release-Tag '{release.TagName}' ist keine gültige Version. Erwartet wird zum Beispiel v0.11.0.");
        }

        var installerName = options.GetInstallerAssetName(version);
        var installer = release.Assets.FirstOrDefault(asset =>
            string.Equals(
                asset.Name,
                installerName,
                StringComparison.OrdinalIgnoreCase));
        if (installer is null || !Uri.TryCreate(
                installer.BrowserDownloadUrl,
                UriKind.Absolute,
                out var installerUri)
            || !IsTrustedGitHubUri(installerUri))
        {
            throw new InvalidOperationException(
                $"Im Release {release.TagName} fehlt das Installer-Asset '{installerName}'.");
        }

        var checksum = release.Assets.FirstOrDefault(asset =>
            string.Equals(
                asset.Name,
                installerName + ".sha256",
                StringComparison.OrdinalIgnoreCase));
        Uri? checksumUri = null;
        if (checksum is not null)
        {
            if (!Uri.TryCreate(
                    checksum.BrowserDownloadUrl,
                    UriKind.Absolute,
                    out checksumUri)
                || !IsTrustedGitHubUri(checksumUri))
            {
                throw new InvalidOperationException(
                    "Das SHA-256-Asset enthält keine vertrauenswürdige GitHub-Adresse.");
            }
        }

        if (!Uri.TryCreate(release.HtmlUrl, UriKind.Absolute, out var releaseUri)
            || !IsTrustedGitHubUri(releaseUri))
        {
            throw new InvalidOperationException(
                "Die GitHub-Antwort enthält keine gültige Release-Adresse.");
        }

        return new UpdateRelease(
            version,
            UpdateOptions.ToReleaseVersion(version),
            string.IsNullOrWhiteSpace(release.Name)
                ? $"Nexus Control Agent {UpdateOptions.ToReleaseVersion(version)}"
                : release.Name,
            release.Body,
            releaseUri,
            installerUri,
            installer.Name,
            installer.Size,
            installer.Digest,
            checksumUri,
            release.PublishedAt);
    }

    public async Task DownloadInstallerAsync(
        UpdateRelease release,
        string destinationPath,
        long maximumBytes,
        Action<int> progressChanged,
        CancellationToken cancellationToken)
    {
        if (release.InstallerSizeBytes <= 0
            || release.InstallerSizeBytes > maximumBytes)
        {
            throw new InvalidDataException(
                "Die von GitHub gemeldete Installer-Größe ist ungültig oder überschreitet das erlaubte Limit.");
        }

        using var response = await _client.GetAsync(
            release.InstallerDownload,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var contentLength = response.Content.Headers.ContentLength;
        if (contentLength is > 0 && contentLength > maximumBytes)
        {
            throw new InvalidDataException(
                "Der heruntergeladene Installer überschreitet das erlaubte Größenlimit.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        var temporaryPath = destinationPath + ".download";
        File.Delete(temporaryPath);

        try
        {
            await using var source = await response.Content.ReadAsStreamAsync(
                cancellationToken);
            await using var destination = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81_920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            var buffer = ArrayPool<byte>.Shared.Rent(81_920);
            try
            {
                long totalBytes = 0;
                var lastProgress = -1;
                while (true)
                {
                    var bytesRead = await source.ReadAsync(
                        buffer.AsMemory(0, buffer.Length),
                        cancellationToken);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    totalBytes += bytesRead;
                    if (totalBytes > maximumBytes)
                    {
                        throw new InvalidDataException(
                            "Der heruntergeladene Installer überschreitet das erlaubte Größenlimit.");
                    }

                    await destination.WriteAsync(
                        buffer.AsMemory(0, bytesRead),
                        cancellationToken);
                    var expectedBytes = contentLength is > 0
                        ? contentLength.Value
                        : release.InstallerSizeBytes;
                    var progress = expectedBytes <= 0
                        ? 0
                        : Math.Clamp(
                            (int)Math.Round(totalBytes * 100D / expectedBytes),
                            0,
                            100);
                    if (progress != lastProgress)
                    {
                        lastProgress = progress;
                        progressChanged(progress);
                    }
                }

                await destination.FlushAsync(cancellationToken);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            File.Move(temporaryPath, destinationPath, overwrite: true);
            progressChanged(100);
        }
        catch
        {
            File.Delete(temporaryPath);
            throw;
        }
    }

    public async Task<string?> GetExpectedSha256Async(
        UpdateRelease release,
        CancellationToken cancellationToken)
    {
        var digestMatch = Sha256DigestRegex().Match(
            release.InstallerDigest ?? string.Empty);
        if (digestMatch.Success)
        {
            return digestMatch.Groups[1].Value.ToUpperInvariant();
        }

        if (release.ChecksumDownload is null)
        {
            return null;
        }

        using var response = await _client.GetAsync(
            release.ChecksumDownload,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        const int maximumChecksumCharacters = 16_384;
        if (response.Content.Headers.ContentLength
            is > maximumChecksumCharacters)
        {
            throw new InvalidDataException(
                "Die SHA-256-Datei ist unerwartet groß.");
        }

        var checksumText = await ReadLimitedTextAsync(
            response.Content,
            maximumChecksumCharacters,
            cancellationToken);

        var checksumMatch = Sha256ValueRegex().Match(checksumText);
        return checksumMatch.Success
            ? checksumMatch.Groups[1].Value.ToUpperInvariant()
            : null;
    }

    private static async Task<string> ReadLimitedTextAsync(
        HttpContent content,
        int maximumCharacters,
        CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(
            cancellationToken);
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 4096,
            leaveOpen: false);
        var builder = new StringBuilder();
        var buffer = new char[4096];
        while (true)
        {
            var charactersRead = await reader.ReadAsync(
                buffer.AsMemory(),
                cancellationToken);
            if (charactersRead == 0)
            {
                return builder.ToString();
            }

            if (builder.Length + charactersRead > maximumCharacters)
            {
                throw new InvalidDataException(
                    "Die SHA-256-Datei ist unerwartet groß.");
            }

            builder.Append(buffer, 0, charactersRead);
        }
    }

    private static Version? ParseVersion(string? tagName)
    {
        var value = tagName?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value.StartsWith('v') || value.StartsWith('V'))
        {
            value = value[1..];
        }

        value = value.Split('-', '+')[0];
        if (!Version.TryParse(value, out var parsed)
            || parsed.Build < 0
            || parsed.Revision >= 0)
        {
            return null;
        }

        return new Version(
            parsed.Major,
            parsed.Minor,
            Math.Max(parsed.Build, 0),
            Math.Max(parsed.Revision, 0));
    }

    private static bool IsTrustedGitHubUri(Uri uri) =>
        string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)
        && string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(
        @"^sha256:([A-Fa-f0-9]{64})$",
        RegexOptions.CultureInvariant)]
    private static partial Regex Sha256DigestRegex();

    [GeneratedRegex(
        @"\b([A-Fa-f0-9]{64})\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex Sha256ValueRegex();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private sealed record GitHubRelease(
        [property: JsonPropertyName("tag_name")] string TagName,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("body")] string? Body,
        [property: JsonPropertyName("html_url")] string HtmlUrl,
        [property: JsonPropertyName("draft")] bool Draft,
        [property: JsonPropertyName("published_at")] DateTimeOffset? PublishedAt,
        [property: JsonPropertyName("assets")] List<GitHubReleaseAsset> Assets);

    private sealed record GitHubReleaseAsset(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl,
        [property: JsonPropertyName("size")] long Size,
        [property: JsonPropertyName("digest")] string? Digest);
}
