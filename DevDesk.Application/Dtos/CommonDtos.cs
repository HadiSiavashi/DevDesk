using DevDesk.Domain.Enums;

namespace DevDesk.Application.Dtos;

public sealed class ChecklistItemDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int OrderNo { get; set; }
}

public sealed class TagDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#64748B";
}

public sealed class MilestoneDto
{
    public Guid Id { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? GoalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public int OrderNo { get; set; }
}

public sealed class UserDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class AppPreferencesDto
{
    public string DisplayName { get; set; } = "Developer";
    public ThemeMode Theme { get; set; } = ThemeMode.System;
    public int TargetFocusMinutesPerDay { get; set; } = 120;
    public int DefaultAvailableWorkMinutes { get; set; } = 480;
    public int PomodoroWorkMinutes { get; set; } = 25;
    public int PomodoroShortBreakMinutes { get; set; } = 5;
    public int PomodoroLongBreakMinutes { get; set; } = 15;
    public int SessionsUntilLongBreak { get; set; } = 4;
    public bool NotificationsEnabled { get; set; } = true;
    public bool RemindOverdueTasks { get; set; } = true;
    public bool RemindDailyPlan { get; set; } = true;
    public bool RemindDailyReview { get; set; } = true;
    public string? DefaultProjectId { get; set; }
}

public sealed class ProductivityScoreDto
{
    public int Total { get; set; }
    public int CompletionScore { get; set; }
    public int FocusScore { get; set; }
    public int PlanningScore { get; set; }
    public int ReviewScore { get; set; }
    public int OverduePenalty { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
}

public sealed class SearchResultDto
{
    public string EntityType { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? ProjectName { get; set; }
}

public sealed class ChartPointDto
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
}

public sealed class AnalyticsDto
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public IReadOnlyList<ChartPointDto> TasksCompletedPerDay { get; set; } = [];
    public IReadOnlyList<ChartPointDto> FocusMinutesPerDay { get; set; } = [];
    public IReadOnlyList<ChartPointDto> TasksByStatus { get; set; } = [];
    public IReadOnlyList<ChartPointDto> TasksByPriority { get; set; } = [];
    public IReadOnlyList<ChartPointDto> FocusByProject { get; set; } = [];
    public int TotalTasksCompleted { get; set; }
    public int TotalFocusMinutes { get; set; }
    public double AverageProductivityScore { get; set; }
    public int TotalEstimatedMinutes { get; set; }
    public int TotalActualMinutes { get; set; }
}

public sealed class DashboardDto
{
    public string Greeting { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;
    public DateOnly Today { get; set; }
    public ProductivityScoreDto ProductivityScore { get; set; } = new();
    public IReadOnlyList<TaskListItemDto> TodayTasks { get; set; } = [];
    public IReadOnlyList<TaskListItemDto> OverdueTasks { get; set; } = [];
    public IReadOnlyList<TaskListItemDto> StarredTasks { get; set; } = [];
    public FocusSessionDto? ActiveFocusSession { get; set; }
    public DailyPlanDto? DailyPlan { get; set; }
    public DailyReviewDto? DailyReview { get; set; }
    public int FocusMinutesToday { get; set; }
    public int CompletedTasksToday { get; set; }
    public int OpenTaskCount { get; set; }
    public int ActiveProjectCount { get; set; }
}

public sealed class ExportDataDto
{
    public int Version { get; set; } = 1;
    public DateTime ExportedAt { get; set; }
    public IReadOnlyList<ProjectDto> Projects { get; set; } = [];
    public IReadOnlyList<WorkTaskDto> Tasks { get; set; } = [];
    public IReadOnlyList<NoteDto> Notes { get; set; } = [];
    public IReadOnlyList<GoalDto> Goals { get; set; } = [];
    public IReadOnlyList<HabitDto> Habits { get; set; } = [];
    public IReadOnlyList<BookmarkDto> Bookmarks { get; set; } = [];
    public IReadOnlyList<CodeSnippetDto> Snippets { get; set; } = [];
    public IReadOnlyList<CalendarEventDto> CalendarEvents { get; set; } = [];
    public IReadOnlyList<TagDto> Tags { get; set; } = [];
    public AppPreferencesDto? Preferences { get; set; }
}

public sealed record ImportResultDto
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int ProjectsImported { get; init; }
    public int TasksImported { get; init; }
    public int NotesImported { get; init; }
    public int GoalsImported { get; init; }
    public int HabitsImported { get; init; }
    public int BookmarksImported { get; init; }
    public int SnippetsImported { get; init; }
    public int CalendarEventsImported { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed class NotificationDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public DateTime CreatedAt { get; set; }
}
