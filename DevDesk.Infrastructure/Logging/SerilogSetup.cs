using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using ILogger = Serilog.ILogger;

namespace DevDesk.Infrastructure.Logging;

/// <summary>
/// Configures Serilog with a rolling file sink. Never logs connection strings with credentials.
/// </summary>
public static partial class SerilogSetup
{
    public const string DefaultLogFileName = "devdesk-.log";

    /// <summary>
    /// Creates a configured Serilog logger writing to <c>logs/devdesk-.log</c>.
    /// </summary>
    public static ILogger CreateLogger(string? logDirectory = null, LogEventLevel minimumLevel = LogEventLevel.Information)
    {
        var logger = CreateLoggerConfiguration(logDirectory, minimumLevel).CreateLogger();
        Log.Logger = logger;
        return logger;
    }

    public static LoggerConfiguration CreateLoggerConfiguration(
        string? logDirectory = null,
        LogEventLevel minimumLevel = LogEventLevel.Information)
    {
        var directory = ResolveLogDirectory(logDirectory);
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, DefaultLogFileName);

        return new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("Application", "DevDesk")
            .Filter.With(new SensitiveDataLogEventFilter())
            .WriteTo.File(
                path: path,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 31,
                shared: true,
                outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}");
    }

    /// <summary>
    /// Registers Serilog as the logging provider and sets the static <see cref="Log.Logger"/>.
    /// </summary>
    public static IServiceCollection AddDevDeskSerilog(
        this IServiceCollection services,
        string? logDirectory = null,
        LogEventLevel minimumLevel = LogEventLevel.Information)
    {
        ArgumentNullException.ThrowIfNull(services);

        var logger = CreateLogger(logDirectory, minimumLevel);
        services.AddSingleton(logger);
        services.AddLogging(builder => builder.AddDevDeskSerilog(logger));
        return services;
    }

    /// <summary>
    /// Adds Serilog to an existing <see cref="ILoggingBuilder"/>.
    /// </summary>
    public static ILoggingBuilder AddDevDeskSerilog(
        this ILoggingBuilder builder,
        ILogger? logger = null,
        string? logDirectory = null,
        LogEventLevel minimumLevel = LogEventLevel.Information)
    {
        ArgumentNullException.ThrowIfNull(builder);

        logger ??= CreateLogger(logDirectory, minimumLevel);
        builder.ClearProviders();
        builder.AddSerilog(logger, dispose: false);
        return builder;
    }

    public static string ResolveLogDirectory(string? logDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(logDirectory))
            return Path.GetFullPath(logDirectory);

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "logs"));
    }

    /// <summary>
    /// Returns a redacted copy safe for diagnostics (password/pwd removed).
    /// </summary>
    public static string RedactConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return string.Empty;

        return CredentialRegex().Replace(connectionString, "$1=***");
    }

    [GeneratedRegex(
        @"(?i)(Password|Pwd|User\s*Id|UID|Account\s*Key|SharedAccessKey|ClientSecret)\s*=\s*[^;]*",
        RegexOptions.CultureInvariant)]
    private static partial Regex CredentialRegex();

    private sealed class SensitiveDataLogEventFilter : ILogEventFilter
    {
        private static readonly string[] SensitiveKeys =
        [
            "ConnectionString",
            "connectionString",
            "Password",
            "Pwd",
            "ClientSecret",
            "SharedAccessKey"
        ];

        public bool IsEnabled(LogEvent logEvent)
        {
            foreach (var (key, value) in logEvent.Properties)
            {
                if (SensitiveKeys.Any(k => key.Equals(k, StringComparison.OrdinalIgnoreCase)) &&
                    value is ScalarValue { Value: string text } &&
                    LooksLikeSecretOrConnectionString(text))
                {
                    return false;
                }

                if (value is ScalarValue { Value: string scalar } && LooksLikeSecretOrConnectionString(scalar))
                    return false;
            }

            return true;
        }

        private static bool LooksLikeSecretOrConnectionString(string value)
        {
            if (value.Length < 8)
                return false;

            return value.Contains("Password=", StringComparison.OrdinalIgnoreCase)
                   || value.Contains("Pwd=", StringComparison.OrdinalIgnoreCase)
                   || (value.Contains("Server=", StringComparison.OrdinalIgnoreCase)
                       && value.Contains("Database=", StringComparison.OrdinalIgnoreCase)
                       && value.Contains(';'));
        }
    }
}
