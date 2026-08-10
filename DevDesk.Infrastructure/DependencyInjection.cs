using DevDesk.Application.Interfaces;
using DevDesk.Application.Options;
using DevDesk.Infrastructure.Backup;
using DevDesk.Infrastructure.Browser;
using DevDesk.Infrastructure.FileSystem;
using DevDesk.Infrastructure.Logging;
using DevDesk.Infrastructure.Notifications;
using DevDesk.Infrastructure.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;

namespace DevDesk.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers Infrastructure services. Call after <c>AddApplication</c> so
    /// <see cref="WindowsNotificationService"/> can decorate <c>NotificationService</c>.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<NotificationOptions>(configuration.GetSection(NotificationOptions.SectionName));
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));

        services.AddSingleton<IBrowserService, BrowserService>();
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddSingleton<IStartupRegistration, StartupRegistration>();
        services.AddScoped<IDatabaseBackupService, SqlServerBackupHelper>();

        // Singleton so WinForms can subscribe to NotificationRequested for the app lifetime.
        // NotificationService (concrete, scoped) remains registered by AddApplication.
        services.AddSingleton<WindowsNotificationService>();
        services.AddSingleton<INotificationService>(sp => sp.GetRequiredService<WindowsNotificationService>());

        return services;
    }

    /// <summary>
    /// Configures Serilog file logging. Prefer calling early during host/bootstrap setup.
    /// </summary>
    public static IServiceCollection AddDevDeskLogging(
        this IServiceCollection services,
        string? logDirectory = null,
        LogEventLevel minimumLevel = LogEventLevel.Information)
        => services.AddDevDeskSerilog(logDirectory, minimumLevel);
}
