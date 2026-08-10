using DevDesk.Application.Abstractions;
using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Application.Options;
using DevDesk.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevDesk.Application.Services;

public sealed class NotificationService(
    IDevDeskDbContext db,
    IClock clock,
    IOptions<NotificationOptions> options) : INotificationService
{
    /// <summary>
    /// Application-layer pending notifications do not raise desktop events.
    /// Infrastructure's <c>WindowsNotificationService</c> wraps this and owns the event.
    /// </summary>
    public event EventHandler<DesktopNotificationEventArgs>? NotificationRequested
    {
        add { }
        remove { }
    }

    public async Task<IReadOnlyList<NotificationDto>> GetPendingAsync(CancellationToken ct = default)
    {
        if (!options.Value.Enabled)
            return [];

        var now = clock.UtcNow;
        var today = clock.Today;
        var notifications = new List<NotificationDto>();

        if (options.Value.RemindOverdueTasks || options.Value.OverdueTask)
        {
            var open = await db.Tasks.AsNoTracking()
                .Where(t => t.DueDate.HasValue && t.Status != WorkTaskStatus.Done && t.Status != WorkTaskStatus.Cancelled)
                .ToListAsync(ct);

            var overdueCount = open.Count(t => DateOnly.FromDateTime(t.DueDate!.Value) < today);
            if (overdueCount > 0)
            {
                notifications.Add(new NotificationDto
                {
                    Id = "overdue-tasks",
                    Title = "Overdue tasks",
                    Message = $"You have {overdueCount} overdue task(s).",
                    Severity = "Warning",
                    CreatedAt = now
                });
            }
        }

        if (options.Value.TaskDueSoon)
        {
            var dueSoonWindow = now.AddMinutes(options.Value.TaskDueSoonMinutes);
            var dueSoon = await db.Tasks.AsNoTracking()
                .Where(t =>
                    t.DueDate.HasValue &&
                    t.Status != WorkTaskStatus.Done &&
                    t.Status != WorkTaskStatus.Cancelled &&
                    t.DueDate >= now &&
                    t.DueDate <= dueSoonWindow)
                .CountAsync(ct);

            if (dueSoon > 0)
            {
                notifications.Add(new NotificationDto
                {
                    Id = "tasks-due-soon",
                    Title = "Tasks due soon",
                    Message = $"You have {dueSoon} task(s) due within {options.Value.TaskDueSoonMinutes} minutes.",
                    Severity = "Info",
                    CreatedAt = now
                });
            }
        }

        if (options.Value.RemindDailyPlan)
        {
            var plan = await db.DailyPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Date == today, ct);
            if (plan is null || !plan.HasAllGoals)
            {
                notifications.Add(new NotificationDto
                {
                    Id = "daily-plan",
                    Title = "Daily plan",
                    Message = "Set your top 3 goals for today.",
                    Severity = "Info",
                    CreatedAt = now
                });
            }
        }

        if (options.Value.RemindDailyReview && now.Hour >= 17)
        {
            var review = await db.DailyReviews.AsNoTracking().FirstOrDefaultAsync(r => r.Date == today, ct);
            if (review is null || !review.IsComplete)
            {
                notifications.Add(new NotificationDto
                {
                    Id = "daily-review",
                    Title = "Daily review",
                    Message = "Capture today’s review before you wrap up.",
                    Severity = "Info",
                    CreatedAt = now
                });
            }
        }

        if (options.Value.UpcomingCalendarEvent)
        {
            var windowEnd = now.AddMinutes(30);
            var upcoming = await db.CalendarEvents.AsNoTracking()
                .Where(e => e.StartAt >= now && e.StartAt <= windowEnd)
                .CountAsync(ct);

            if (upcoming > 0)
            {
                notifications.Add(new NotificationDto
                {
                    Id = "upcoming-calendar",
                    Title = "Upcoming events",
                    Message = $"You have {upcoming} calendar event(s) starting within 30 minutes.",
                    Severity = "Info",
                    CreatedAt = now
                });
            }
        }

        return notifications;
    }

    public Task ShowAsync(
        NotificationCategory category,
        string title,
        string message,
        CancellationToken ct = default)
    {
        // Desktop delivery is provided by Infrastructure's WindowsNotificationService.
        return Task.CompletedTask;
    }

    public async Task ShowPendingDesktopAsync(CancellationToken ct = default)
    {
        var pending = await GetPendingAsync(ct).ConfigureAwait(false);
        foreach (var item in pending)
        {
            await ShowAsync(
                item.Severity == "Warning" ? NotificationCategory.OverdueTask : NotificationCategory.TaskDueSoon,
                item.Title,
                item.Message,
                ct).ConfigureAwait(false);
        }
    }
}
