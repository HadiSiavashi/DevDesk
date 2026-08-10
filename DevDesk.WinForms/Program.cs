using DevDesk.Application;
using DevDesk.Application.Interfaces;
using DevDesk.Application.Options;
using DevDesk.Infrastructure;
using DevDesk.Infrastructure.Logging;
using DevDesk.Persistence;
using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Overlays;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using WinFormsApp = System.Windows.Forms.Application;

namespace DevDesk.WinForms;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var builder = Host.CreateApplicationBuilder();

        builder.Configuration
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();

        var services = builder.Services;
        services.AddDevDeskSerilog();
        services.AddLogging();
        services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.SectionName));
        services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<FocusOptions>(builder.Configuration.GetSection(FocusOptions.SectionName));
        services.Configure<PomodoroOptions>(builder.Configuration.GetSection(PomodoroOptions.SectionName));
        services.Configure<NotificationOptions>(builder.Configuration.GetSection(NotificationOptions.SectionName));

        services.AddApplication();
        services.AddPersistence(builder.Configuration);
        services.AddInfrastructure(builder.Configuration);

        services.AddSingleton<NavigationService>();
        services.AddSingleton<TrayIconService>();
        services.AddSingleton<MainForm>();

        var host = builder.Build();
        var provider = host.Services;

        WinFormsApp.ThreadException += (_, e) => HandleException(e.Exception, provider);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex) HandleException(ex, provider);
        };

        try
        {
            RunAsync(provider).GetAwaiter().GetResult();
        }
        finally
        {
            if (provider is IDisposable d) d.Dispose();
            Log.CloseAndFlush();
        }
    }

    private static async Task RunAsync(IServiceProvider provider)
    {
        var config = provider.GetRequiredService<IConfiguration>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("DevDesk.WinForms");

        var autoMigrate = config.GetValue("Database:AutoMigrate", false)
            || config.GetValue($"{DatabaseOptions.SectionName}:{nameof(DatabaseOptions.ApplyMigrationsOnStartup)}", true);
        var seedDemo = config.GetValue("Database:SeedDemoData", false)
            || config.GetValue($"{DatabaseOptions.SectionName}:{nameof(DatabaseOptions.SeedSampleData)}", false);

        using (var scope = provider.CreateScope())
        {
            var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
            await initializer.InitializeAsync(autoMigrate, seedDemo);
        }

        using (var scope = provider.CreateScope())
        {
            var focusOpts = config.GetSection(FocusOptions.SectionName).Get<FocusOptions>();
            if (focusOpts?.RecoverActiveSessionOnStartup == true)
            {
                var focus = scope.ServiceProvider.GetRequiredService<IFocusService>();
                await focus.RecoverActiveOnStartupAsync();
            }
        }

        await ApplyStartupPreferencesAsync(provider);

        using (var scope = provider.CreateScope())
        {
            var settings = scope.ServiceProvider.GetRequiredService<ISettingsService>();
            var onboarding = await settings.GetSettingAsync("OnboardingCompleted");
            if (!string.Equals(onboarding, "true", StringComparison.OrdinalIgnoreCase))
            {
                using var onboardingForm = new OnboardingForm(provider);
                if (onboardingForm.ShowDialog() != DialogResult.OK)
                    return;

                onboarding = await settings.GetSettingAsync("OnboardingCompleted");
                if (!string.Equals(onboarding, "true", StringComparison.OrdinalIgnoreCase))
                    return;
            }
        }

        var mainForm = provider.GetRequiredService<MainForm>();
        var nav = provider.GetRequiredService<NavigationService>();
        nav.RegisterViews(provider.GetRequiredService<IServiceScopeFactory>(), provider);

        WinFormsApp.Run(mainForm);
    }

    private static async Task ApplyStartupPreferencesAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        var prefs = await settings.GetPreferencesAsync();
        ThemeManager.Instance.SetMode(prefs.Theme);
        var culture = await settings.GetSettingAsync("Culture") ?? "en-US";
        LocalizationService.Instance.SetLanguage(culture);

        var alwaysOnTop = await settings.GetSettingAsync("AlwaysOnTop");
        var startMinimized = await settings.GetSettingAsync("StartMinimized");
        MainForm.ApplyAlwaysOnTop = string.Equals(alwaysOnTop, "true", StringComparison.OrdinalIgnoreCase);
        MainForm.ApplyStartMinimized = string.Equals(startMinimized, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static void HandleException(Exception ex, IServiceProvider provider)
    {
        try
        {
            var logger = provider.GetService<ILoggerFactory>()?.CreateLogger("DevDesk.WinForms");
            logger?.LogError(ex, "Unhandled UI exception");
        }
        catch { /* ignore logging failures */ }

        var details = ex.ToString();
        var result = MessageBox.Show(
            $"{LocalizationService.Instance.Get("common.error")}\n\n{ex.Message}\n\nCopy details to clipboard?",
            LocalizationService.Instance.Get("error.title"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Error);

        if (result == DialogResult.Yes)
        {
            ClipboardHelper.TrySetText(details);
        }
    }
}
