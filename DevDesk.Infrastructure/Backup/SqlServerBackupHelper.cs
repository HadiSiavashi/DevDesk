using DevDesk.Application.Interfaces;
using DevDesk.Application.Options;
using DevDesk.Infrastructure.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevDesk.Infrastructure.Backup;

/// <summary>
/// Attempts BACKUP DATABASE via SqlConnection when the SQL Server principal has permission.
/// Never stores or logs passwords from the connection string.
/// </summary>
public sealed class SqlServerBackupHelper : IDatabaseBackupService
{
    private readonly IConfiguration _configuration;
    private readonly IOptions<DatabaseOptions> _databaseOptions;
    private readonly IFileSystemService _fileSystem;
    private readonly ILogger<SqlServerBackupHelper> _logger;

    public SqlServerBackupHelper(
        IConfiguration configuration,
        IOptions<DatabaseOptions> databaseOptions,
        IFileSystemService fileSystem,
        ILogger<SqlServerBackupHelper> logger)
    {
        _configuration = configuration;
        _databaseOptions = databaseOptions;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public async Task BackupAsync(string destinationFilePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationFilePath);

        if (!_fileSystem.IsValidWritablePath(destinationFilePath))
            throw new ArgumentException(
                "Choose a valid writable .bak path on a drive the SQL Server service can access.",
                nameof(destinationFilePath));

        if (!destinationFilePath.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Backup destination should use a .bak extension.", nameof(destinationFilePath));

        var connectionString = ResolveConnectionString();
        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;

        if (string.IsNullOrWhiteSpace(databaseName))
            throw new InvalidOperationException(
                "The connection string does not specify a database name (Initial Catalog / Database).");

        var directory = Path.GetDirectoryName(Path.GetFullPath(destinationFilePath));
        if (!string.IsNullOrWhiteSpace(directory))
            _fileSystem.EnsureDirectory(directory);

        // Log only safe metadata — never the raw connection string.
        _logger.LogInformation(
            "Starting SQL Server backup for database {Database} to {Path}. Connection: {Redacted}",
            databaseName,
            destinationFilePath,
            SerilogSetup.RedactConnectionString(connectionString));

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);

            // Bracket-escape database identifier; path is parameterized.
            var escapedDb = databaseName.Replace("]", "]]", StringComparison.Ordinal);
            await using var command = connection.CreateCommand();
            command.CommandText =
                $"""
                 BACKUP DATABASE [{escapedDb}]
                 TO DISK = @path
                 WITH COPY_ONLY, INIT, STATS = 10;
                 """;
            command.Parameters.AddWithValue("@path", Path.GetFullPath(destinationFilePath));
            command.CommandTimeout = 0;

            await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            _logger.LogInformation("SQL Server backup completed for database {Database}.", databaseName);
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "SQL Server backup failed for database {Database}. ErrorNumber={Number}",
                databaseName,
                ex.Number);

            throw new InvalidOperationException(BuildUserFacingError(ex), ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not ArgumentException)
        {
            _logger.LogError(ex, "Unexpected error during SQL Server backup for database {Database}.", databaseName);
            throw new InvalidOperationException(
                "Backup failed unexpectedly. Confirm SQL Server is reachable and the path is valid.",
                ex);
        }
    }

    private string ResolveConnectionString()
    {
        var fromConfig = _configuration.GetConnectionString("DevDesk");
        if (!string.IsNullOrWhiteSpace(fromConfig))
            return fromConfig;

        if (!string.IsNullOrWhiteSpace(_databaseOptions.Value.ConnectionString))
            return _databaseOptions.Value.ConnectionString;

        throw new InvalidOperationException(
            "No SQL Server connection string was found. Set ConnectionStrings:DevDesk in configuration.");
    }

    private static string BuildUserFacingError(SqlException ex)
    {
        // Common permission / path failures — keep messages clear and credential-free.
        return ex.Number switch
        {
            262 or 297 =>
                "Backup failed: the current SQL login does not have permission to BACKUP DATABASE. " +
                "Ask a DBA to grant BACKUP DATABASE (or use a privileged account for backups).",
            3201 or 3 =>
                "Backup failed: SQL Server could not write to the destination path. " +
                "Use a folder the SQL Server service account can write to (often a server-local path).",
            2 or 53 =>
                "Backup failed: could not connect to SQL Server. Check that the server is running and reachable.",
            4060 =>
                "Backup failed: the configured database was not found.",
            _ =>
                $"Backup failed: {ex.Message}"
        };
    }
}
