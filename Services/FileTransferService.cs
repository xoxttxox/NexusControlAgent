using Microsoft.AspNetCore.StaticFiles;

namespace NexusControl.Agent.Services;

internal sealed class FileTransferService
{
    public const long MaximumFileSizeBytes = 100L * 1024 * 1024;

    private readonly FileExtensionContentTypeProvider _contentTypes = new();

    public FileTransferService()
    {
        SharedDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            "Nexus Control");
        Directory.CreateDirectory(SharedDirectory);
    }

    public string SharedDirectory { get; }

    public IReadOnlyList<SharedFile> ListFiles() =>
        new DirectoryInfo(SharedDirectory)
            .EnumerateFiles()
            .Where(file => file.Length <= MaximumFileSizeBytes)
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Take(100)
            .Select(file => new SharedFile(
                file.Name,
                file.Length,
                file.LastWriteTimeUtc,
                GetContentType(file.Name)))
            .ToArray();

    public async Task<SharedFile> SaveAsync(
        Stream source,
        string requestedName,
        long length,
        CancellationToken cancellationToken)
    {
        if (length <= 0)
        {
            throw new InvalidOperationException("Die Datei ist leer.");
        }
        if (length > MaximumFileSizeBytes)
        {
            throw new InvalidOperationException(
                "Dateien dürfen maximal 100 MB groß sein.");
        }

        var fileName = CreateSafeFileName(requestedName);
        var destination = CreateUniquePath(fileName);
        await using var output = new FileStream(
            destination,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81_920,
            FileOptions.Asynchronous);
        await source.CopyToAsync(output, cancellationToken);

        var info = new FileInfo(destination);
        return new SharedFile(
            info.Name,
            info.Length,
            info.LastWriteTimeUtc,
            GetContentType(info.Name));
    }

    public (string Path, string ContentType, string Name)? Resolve(
        string requestedName)
    {
        var name = Path.GetFileName(requestedName);
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var fullPath = Path.GetFullPath(Path.Combine(SharedDirectory, name));
        var root = Path.GetFullPath(SharedDirectory)
            .TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            || !File.Exists(fullPath))
        {
            return null;
        }

        var info = new FileInfo(fullPath);
        if (info.Length > MaximumFileSizeBytes)
        {
            return null;
        }

        return (fullPath, GetContentType(info.Name), info.Name);
    }

    private string GetContentType(string fileName) =>
        _contentTypes.TryGetContentType(fileName, out var contentType)
            ? contentType
            : "application/octet-stream";

    private string CreateUniquePath(string fileName)
    {
        var initial = Path.Combine(SharedDirectory, fileName);
        if (!File.Exists(initial))
        {
            return initial;
        }

        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        for (var index = 1; index <= 999; index++)
        {
            var candidate = Path.Combine(
                SharedDirectory,
                $"{name} ({index}){extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new IOException("Für diese Datei konnte kein freier Name erstellt werden.");
    }

    private static string CreateSafeFileName(string requestedName)
    {
        var name = Path.GetFileName(requestedName.Trim());
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '_');
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"Datei-{DateTimeOffset.Now:yyyyMMdd-HHmmss}";
        }

        return name[..Math.Min(name.Length, 180)];
    }
}

internal sealed record SharedFile(
    string Name,
    long SizeBytes,
    DateTime LastModified,
    string MimeType);
