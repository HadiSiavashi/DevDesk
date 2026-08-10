namespace DevDesk.Application.Interfaces;

/// <summary>
/// Safe file IO helpers for import/export and backup path selection.
/// </summary>
public interface IFileSystemService
{
    Task WriteTextAsync(string path, string contents, CancellationToken ct = default);
    Task<string> ReadTextAsync(string path, CancellationToken ct = default);
    bool DirectoryExists(string path);
    void EnsureDirectory(string path);
    string GetDefaultBackupDirectory();
    string GetDefaultExportDirectory();
    bool IsValidWritablePath(string? path);
}
