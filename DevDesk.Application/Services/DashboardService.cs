using DevDesk.Application.Abstractions;
using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Application.Mapping;
using DevDesk.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DevDesk.Application.Services;

public sealed class DashboardService(
    IDevDeskDbContext db,
    IClock clock,
    IAnalyticsService analytics,
    IFocusService focusService,
    IDailyPlanService dailyPlanService,
    IDailyReviewService dailyReviewService,
    ISettingsService settingsService) : IDashboardService
{
    public async Task<DashboardDto> GetAsync(CancellationToken ct = default)
    {
        var today = clock.Today;
        var now = clock.UtcNow;
        var preferences = await settingsService.GetPreferencesAsync(ct);
        var score = await analytics.GetProductivityScoreAsync(today, ct);
        var activeFocus = await focusService.GetActiveAsync(ct);
        var plan = await dailyPlanService.GetOrCreateAsync(today, ct);
        var review = await dailyReviewService.GetOrCreateAsync(today, ct);

        var openTasks = await db.Tasks.AsNoTracking()
            .Include(t => t.Project)
            .Include(t => t.ChecklistItems)
            .Where(t => t.Status != WorkTaskStatus.Done && t.Status != WorkTaskStatus.Cancelled)
            .ToListAsync(ct);

        var todayTasks = openTasks
            .Where(t => t.DueDate.HasValue && DateOnly.FromDateTime(t.DueDate.Value) == today)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .Select(t => t.ToListItemDto(now, today))
            .ToList();

        var overdueTasks = openTasks
            .Where(t => t.DueDate.HasValue && DateOnly.FromDateTime(t.DueDate.Value) < today)
            .OrderBy(t => t.DueDate)
            .Select(t => t.ToListItemDto(now, today))
            .ToList();

        var starredTasks = openTasks
            .Where(t => t.IsStarred)
            .OrderByDescending(t => t.Priority)
            .Select(t => t.ToListItemDto(now, today))
            .Take(10)
            .ToList();

        var from = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to = today.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Utc);

        var focusMinutesToday = await db.FocusSessions.AsNoTracking()
            .Where(s => s.EndedAt != null && s.StartedAt >= from && s.StartedAt <= to)
            .SumAsync(s => s.DurationMinutes, ct);

        var completedToday = await db.Tasks.AsNoTracking()
            .CountAsync(t => t.Status == WorkTaskStatus.Done && t.CompletedAt != null && t.CompletedAt >= from && t.CompletedAt <= to, ct);

        var activeProjects = await db.Projects.AsNoTracking().CountAsync(p => !p.IsArchived, ct);

        return new DashboardDto
        {
            Greeting = BuildGreeting(now, preferences.DisplayName),
            UserDisplayName = preferences.DisplayName,
            Today = today,
            ProductivityScore = score,
            TodayTasks = todayTasks,
            OverdueTasks = overdueTasks,
            StarredTasks = starredTasks,
            ActiveFocusSession = activeFocus,
            DailyPlan = plan,
            DailyReview = review,
            FocusMinutesToday = focusMinutesToday,
            CompletedTasksToday = completedToday,
            OpenTaskCount = openTasks.Count,
            ActiveProjectCount = activeProjects
        };
    }

    private static string BuildGreeting(DateTime _, string displayName)
    {
        var hour = DateTime.Now.Hour;
        var part = hour switch
        {
            < 12 => "Good morning",
            < 17 => "Good afternoon",
            _ => "Good evening"
        };
        return $"{part}, {displayName}";
    }
}
