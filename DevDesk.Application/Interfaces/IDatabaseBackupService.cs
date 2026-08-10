namespace DevDesk.Application.Interfaces;

public interface IDatabaseBackupService
{
    /// <summary>
    /// Attempts a SQL Server BACKUP DATABASE to the given destination path.
    /// Never logs or stores credentials from the connection string.
    /// </summary>
    Task BackupAsync(string destinationFilePath, CancellationToken ct = default);
}
