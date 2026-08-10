using DevDesk.Application.Dtos;

namespace DevDesk.Application.Interfaces;

public enum NotificationCategory
{
    TaskDueSoon,
    OverdueTask,
    PomodoroFinished,
    FocusSessionFinished,
    UpcomingCalendarEvent
}

public sealed class DesktopNotificationEventArgs : EventArgs
{
    public required NotificationCategory Category { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public string Severity { get; init; } = "Info";
}

public interface INotificationService
{
    /// <summary>
    /// Returns pending in-app notifications derived from application state.
    /// </summary>
    Task<IReadOnlyList<NotificationDto>> GetPendingAsync(CancellationToken ct = default);

    /// <summary>
    /// Requests a desktop toast / tray balloon for the given category.
    /// Respects enable/disable flags from notification options and preferences.
    /// WinForms can subscribe to <see cref="NotificationRequested"/> to present UI.
    /// </summary>
    Task ShowAsync(
        NotificationCategory category,
        string title,
        string message,
        CancellationToken ct = default);

    /// <summary>
    /// Shows all pending notifications that map to enabled desktop categories.
    /// </summary>
    Task ShowPendingDesktopAsync(CancellationToken ct = default);

    /// <summary>
    /// Raised when a desktop notification should be shown (MessageBox-free).
    /// </summary>
    event EventHandler<DesktopNotificationEventArgs>? NotificationRequested;
}
