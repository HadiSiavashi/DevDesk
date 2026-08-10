using DevDesk.Application.Abstractions;
using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Application.Options;
using DevDesk.Application.Productivity;
using DevDesk.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevDesk.Application.Services;

public sealed class AnalyticsService(
    IDevDeskDbContext db,
    IOptions<AppOptions> appOptions,
    ISettingsService settingsService) : IAnalyticsService
{
    public async Task<AnalyticsDto> GetAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (to < from)
            (from, to) = (to, from);

        var fromDt = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDt = to.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Utc);

        var completedTasks = await db.Tasks.AsNoTracking()
            .Where(t => t.Status == WorkTaskStatus.Done && t.CompletedAt != null && t.CompletedAt >= fromDt && t.CompletedAt <= toDt)
            .Select(t => t.CompletedAt!.Value)
            .ToListAsync(ct);

        var focusSessions = await db.FocusSessions.AsNoTracking()
            .Include(s => s.Project)
            .Where(s => s.EndedAt != null && s.StartedAt >= fromDt && s.StartedAt <= toDt)
            .ToListAsync(ct);

        var allTasks = await db.Tasks.AsNoTracking().ToListAsync(ct);

        var tasksCompletedPerDay = new List<ChartPointDto>();
        var focusMinutesPerDay = new List<ChartPointDto>();
        var scores = new List<int>();

        for (var day = from; day <= to; day = day.AddDays(1))
        {
            var completed = completedTasks.Count(d => DateOnly.FromDateTime(d) == day);
            var focus = focusSessions
                .Where(s => DateOnly.FromDateTime(s.StartedAt) == day)
                .Sum(s => s.DurationMinutes);

            tasksCompletedPerDay.Add(new ChartPointDto { Label = day.ToString("MM-dd"), Value = completed });
            focusMinutesPerDay.Add(new ChartPointDto { Label = day.ToString("MM-dd"), Value = focus });

            var score = await GetProductivityScoreAsync(day, ct);
            scores.Add(score.Total);
        }

        var byStatus = allTasks
            .GroupBy(t => t.Status)
            .Select(g => new ChartPointDto { Label = g.Key.ToString(), Value = g.Count() })
            .OrderByDescending(p => p.Value)
            .ToList();

        var byPriority = allTasks
            .GroupBy(t => t.Priority)
            .Select(g => new ChartPointDto { Label = g.Key.ToString(), Value = g.Count() })
            .OrderByDescending(p => p.Value)
            .ToList();

        var byProject = focusSessions
            .GroupBy(s => s.Project?.Name ?? "No project")
            .Select(g => new ChartPointDto { Label = g.Key, Value = g.Sum(s => s.DurationMinutes) })
            .OrderByDescending(p => p.Value)
            .Take(10)
            .ToList();

        var estimatedSum = allTasks.Where(t => t.EstimatedMinutes.HasValue).Sum(t => t.EstimatedMinutes!.Value);
        var actualSum = allTasks.Sum(t => t.ActualMinutes);

        return new AnalyticsDto
        {
            From = from,
            To = to,
            TasksCompletedPerDay = tasksCompletedPerDay,
            FocusMinutesPerDay = focusMinutesPerDay,
            TasksByStatus = byStatus,
            TasksByPriority = byPriority,
            FocusByProject = byProject,
            TotalTasksCompleted = completedTasks.Count,
            TotalFocusMinutes = focusSessions.Sum(s => s.DurationMinutes),
            AverageProductivityScore = scores.Count == 0 ? 0 : Math.Round(scores.Average(), 1),
            TotalEstimatedMinutes = estimatedSum,
            TotalActualMinutes = actualSum
        };
    }

    public async Task<ProductivityScoreDto> GetProductivityScoreAsync(DateOnly date, CancellationToken ct = default)
    {
        var from = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to = date.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Utc);

        var completedToday = await db.Tasks.AsNoTracking()
            .CountAsync(t => t.Status == WorkTaskStatus.Done && t.CompletedAt != null && t.CompletedAt >= from && t.CompletedAt <= to, ct);

        var plannedCount = await db.Tasks.AsNoTracking()
            .CountAsync(t =>
                t.DueDate.HasValue &&
                t.Status != WorkTaskStatus.Cancelled &&
                DateOnly.FromDateTime(t.DueDate.Value) == date, ct);

        var focusMinutes = await db.FocusSessions.AsNoTracking()
            .Where(s => s.EndedAt != null && s.StartedAt >= from && s.StartedAt <= to)
            .SumAsync(s => s.DurationMinutes, ct);

        var overdue = await db.Tasks.AsNoTracking()
            .CountAsync(t =>
                t.DueDate.HasValue &&
                t.Status != WorkTaskStatus.Done &&
                t.Status != WorkTaskStatus.Cancelled &&
                DateOnly.FromDateTime(t.DueDate.Value) < date, ct);

        var hasPlan = await db.DailyPlans.AsNoTracking()
            .AnyAsync(p => p.Date == date && p.TopGoal1 != null && p.TopGoal2 != null && p.TopGoal3 != null, ct);

        var hasReview = await db.DailyReviews.AsNoTracking()
            .AnyAsync(r => r.Date == date && (
                (r.WhatWentWell != null && r.WhatWentWell != "") ||
                (r.WhatDidNotGoWell != null && r.WhatDidNotGoWell != "") ||
                (r.LessonsLearned != null && r.LessonsLearned != "") ||
                (r.TomorrowPlan != null && r.TomorrowPlan != "")), ct);

        var prefs = await settingsService.GetPreferencesAsync(ct);
        var targetFocus = prefs.TargetFocusMinutesPerDay > 0
            ? prefs.TargetFocusMinutesPerDay
            : appOptions.Value.TargetFocusMinutesPerDay;

        return ProductivityHelpers.Calculate(
            date,
            completedToday,
            plannedCount,
            focusMinutes,
            targetFocus,
            overdue,
            hasPlan,
            hasReview);
    }
}
