using DevDesk.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace DevDesk.Infrastructure.FileSystem;

public sealed class FileSystemService(ILogger<FileSystemService> logger) : IFileSystemService
{
    public string GetDefaultBackupDirectory()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "DevDesk",
            "Backups");
        EnsureDirectory(path);
        return path;
    }

    public string GetDefaultExportDirectory()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "DevDesk",
            "Exports");
        EnsureDirectory(path);
        return path;
    }

    public bool DirectoryExists(string path)
        => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path);

    public void EnsureDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Directory.CreateDirectory(path);
    }

    public bool IsValidWritablePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            var full = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(full);
            if (string.IsNullOrWhiteSpace(directory))
                return false;

            // Reject device paths and empty filenames when a file path is expected.
            var fileName = Path.GetFileName(full);
            if (string.IsNullOrWhiteSpace(fileName))
                return Directory.Exists(directory) || !Path.Exists(directory);

            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            logger.LogDebug(ex, "Invalid path rejected.");
            return false;
        }
    }

    public async Task WriteTextAsync(string path, string contents, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(contents);

        if (!IsValidWritablePath(path))
            throw new ArgumentException("The export path is not valid.", nameof(path));

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory))
            EnsureDirectory(directory);

        await System.IO.File.WriteAllTextAsync(path, contents, ct).ConfigureAwait(false);
        logger.LogInformation("Wrote file ({Length} chars) for import/export.", contents.Length);
    }

    public async Task<string> ReadTextAsync(string path, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!System.IO.File.Exists(path))
            throw new FileNotFoundException("The selected file was not found.", path);

        var text = await System.IO.File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        logger.LogInformation("Read import file ({Length} chars).", text.Length);
        return text;
    }
}
