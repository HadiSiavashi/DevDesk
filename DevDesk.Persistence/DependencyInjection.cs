using System.Text.RegularExpressions;
using DevDesk.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"];
        var connectionString = configuration.GetConnectionString("DevDesk");

        // Determine if SQLite should be used:
        // Default to SQLite if provider is "Sqlite", or if provider is not explicitly "SqlServer",
        // or if connection string is empty/null/points to a .db file.
        var isExplicitSqlServer = string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase);

        if (isExplicitSqlServer && !string.IsNullOrWhiteSpace(connectionString) && !connectionString.Contains(".db", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<DevDeskDbContext>(options =>
                options.UseSqlServer(connectionString));
        }
        else
        {
            // Portable SQLite Mode (Zero setup required for any user)
            if (string.IsNullOrWhiteSpace(connectionString) || !connectionString.Contains(".db", StringComparison.OrdinalIgnoreCase))
            {
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DevDesk");
                Directory.CreateDirectory(folder);
                connectionString = $"Data Source={Path.Combine(folder, "devdesk.db")}";
            }
            else
            {
                connectionString = Environment.ExpandEnvironmentVariables(connectionString);
                var match = Regex.Match(connectionString, @"Data Source=(?<path>[^;]+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var dbPath = match.Groups["path"].Value.Trim();
                    if (!string.Equals(dbPath, ":memory:", StringComparison.OrdinalIgnoreCase))
                    {
                        var dir = Path.GetDirectoryName(Path.GetFullPath(dbPath));
                        if (!string.IsNullOrEmpty(dir))
                            Directory.CreateDirectory(dir);
                    }
                }
            }

            services.AddDbContext<DevDeskDbContext>(options =>
                options.UseSqlite(connectionString));
        }

        services.AddScoped<IDevDeskDbContext>(sp => sp.GetRequiredService<DevDeskDbContext>());
        services.AddScoped<DatabaseInitializer>();

        return services;
    }
}
