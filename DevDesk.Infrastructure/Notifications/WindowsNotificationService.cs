using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Application.Options;
using DevDesk.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevDesk.Infrastructure.Notifications;

/// <summary>
/// Desktop notification implementation. Raises <see cref="NotificationRequested"/> so WinForms
/// can show a tray balloon / toast without MessageBox. Wraps Application pending-notification logic.
/// Registered as a singleton so UI can subscribe once for the app lifetime.
/// </summary>
public sealed class WindowsNotificationService : INotificationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<NotificationOptions> _options;
    private readonly ILogger<WindowsNotificationService> _logger;

    public WindowsNotificationService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<NotificationOptions> options,
        ILogger<WindowsNotificationService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    public event EventHandler<DesktopNotificationEventArgs>? NotificationRequested;

    public async Task<IReadOnlyList<NotificationDto>> GetPendingAsync(CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var pending = scope.ServiceProvider.GetRequiredService<NotificationService>();
        return await pending.GetPendingAsync(ct).ConfigureAwait(false);
    }

    public async Task ShowAsync(
        NotificationCategory category,
        string title,
        string message,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (!await IsCategoryEnabledAsync(category, ct).ConfigureAwait(false))
        {
            _logger.LogDebug("Notification suppressed for category {Category}.", category);
            return;
        }

        var severity = category is NotificationCategory.OverdueTask ? "Warning" : "Info";
        var args = new DesktopNotificationEventArgs
        {
            Category = category,
            Title = title.Trim(),
            Message = message.Trim(),
            Severity = severity
        };

        _logger.LogInformation("Desktop notification [{Category}]: {Title}", category, args.Title);
        NotificationRequested?.Invoke(this, args);
    }

    /// <summary>
    /// Convenience helper: show pending items that map to desktop categories.
    /// </summary>
    public async Task ShowPendingDesktopAsync(CancellationToken ct = default)
    {
        var pending = await GetPendingAsync(ct).ConfigureAwait(false);
        foreach (var item in pending)
        {
            var category = MapCategory(item.Id);
            if (category is null)
                continue;

            await ShowAsync(category.Value, item.Title, item.Message, ct).ConfigureAwait(false);
        }
    }

    private async Task<bool> IsCategoryEnabledAsync(NotificationCategory category, CancellationToken ct)
    {
        var options = _options.CurrentValue;
        if (!options.Enabled)
            return false;

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var settings = scope.ServiceProvider.GetRequiredService<ISettingsService>();
            var preferences = await settings.GetPreferencesAsync(ct).ConfigureAwait(false);
            if (!preferences.NotificationsEnabled)
                return false;

            if (category is NotificationCategory.OverdueTask && !preferences.RemindOverdueTasks)
                return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not load preferences for notification gating; using options only.");
        }

        return category switch
        {
            NotificationCategory.TaskDueSoon => options.TaskDueSoon,
            NotificationCategory.OverdueTask => options.OverdueTask || options.RemindOverdueTasks,
            NotificationCategory.PomodoroFinished => options.PomodoroFinished,
            NotificationCategory.FocusSessionFinished => options.FocusSessionFinished,
            NotificationCategory.UpcomingCalendarEvent => options.UpcomingCalendarEvent,
            _ => options.Enabled
        };
    }

    private static NotificationCategory? MapCategory(string id) => id switch
    {
        "overdue-tasks" => NotificationCategory.OverdueTask,
        "tasks-due-soon" => NotificationCategory.TaskDueSoon,
        "upcoming-calendar" => NotificationCategory.UpcomingCalendarEvent,
        _ => null
    };
}
