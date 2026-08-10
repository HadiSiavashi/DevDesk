using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevDesk.Persistence;

public class DatabaseInitializer
{
    private readonly DevDeskDbContext _context;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(DevDeskDbContext context, ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InitializeAsync(bool autoMigrate, bool seedDemoData, CancellationToken cancellationToken = default)
    {
        try
        {
            if (autoMigrate)
            {
                _logger.LogInformation("Applying database migrations...");
                await _context.Database.MigrateAsync(cancellationToken);
            }
            else
            {
                _logger.LogInformation("Verifying database connectivity...");
                if (!await _context.Database.CanConnectAsync(cancellationToken))
                {
                    throw new InvalidOperationException(
                        "Unable to connect to the DevDesk database. Check the connection string and ensure SQL Server is running.");
                }
            }

            if (seedDemoData)
            {
                _logger.LogInformation("Seeding demo data if database is empty...");
                await SeedData.SeedAsync(_context, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            if (ex is InvalidOperationException { Message: var message }
                && message.StartsWith("Unable to connect", StringComparison.Ordinal))
            {
                throw;
            }

            throw new InvalidOperationException(
                "Failed to initialize the DevDesk database. See inner exception for details.",
                ex);
        }
    }
}
