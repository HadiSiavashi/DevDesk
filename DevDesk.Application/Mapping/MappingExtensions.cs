using DevDesk.Application.Dtos;
using DevDesk.Domain.Entities;
using DevDesk.Domain.Enums;

namespace DevDesk.Application.Mapping;

public static class MappingExtensions
{
    public static ChecklistItemDto ToDto(this TaskChecklistItem item) => new()
    {
        Id = item.Id,
        TaskId = item.TaskId,
        Title = item.Title,
        IsCompleted = item.IsCompleted,
        OrderNo = item.OrderNo
    };

    public static TagDto ToDto(this Tag tag) => new()
    {
        Id = tag.Id,
        Name = tag.Name,
        Color = tag.Color
    };

    public static MilestoneDto ToDto(this Milestone milestone) => new()
    {
        Id = milestone.Id,
        ProjectId = milestone.ProjectId,
        GoalId = milestone.GoalId,
        Title = milestone.Title,
        Description = milestone.Description,
        DueDate = milestone.DueDate,
        IsCompleted = milestone.IsCompleted,
        OrderNo = milestone.OrderNo
    };

    public static UserDto ToDto(this User user) => new()
    {
        Id = user.Id,
        DisplayName = user.DisplayName,
        Email = user.Email,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };

    public static WorkTaskDto ToDto(this WorkTask task, DateTime? utcNow = null)
    {
        _ = utcNow;
        return new WorkTaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            ProjectId = task.ProjectId,
            ProjectName = task.Project?.Name,
            Status = task.Status,
            Priority = task.Priority,
            DueDate = task.DueDate,
            EstimatedMinutes = task.EstimatedMinutes,
            ActualMinutes = task.ActualMinutes,
            IsStarred = task.IsStarred,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            CompletedAt = task.CompletedAt,
            ChecklistItems = task.ChecklistItems.OrderBy(c => c.OrderNo).Select(c => c.ToDto()).ToList(),
            Tags = task.TaskTags.Select(tt => tt.Tag.ToDto()).ToList()
        };
    }

    public static TaskListItemDto ToListItemDto(this WorkTask task, DateTime utcNow, DateOnly? today = null)
    {
        var dueDateOnly = task.DueDate.HasValue ? DateOnly.FromDateTime(task.DueDate.Value) : (DateOnly?)null;
        var day = today ?? DateOnly.FromDateTime(DateTime.Now);
        return new TaskListItemDto
        {
            Id = task.Id,
            Title = task.Title,
            ProjectId = task.ProjectId,
            ProjectName = task.Project?.Name,
            Status = task.Status,
            Priority = task.Priority,
            DueDate = task.DueDate,
            EstimatedMinutes = task.EstimatedMinutes,
            ActualMinutes = task.ActualMinutes,
            IsStarred = task.IsStarred,
            ChecklistTotal = task.ChecklistItems.Count,
            ChecklistCompleted = task.ChecklistItems.Count(c => c.IsCompleted),
            IsOverdue = dueDateOnly.HasValue && dueDateOnly.Value < day && task.IsOpen
        };
    }

    public static ProjectListItemDto ToListItemDto(this Project project)
    {
        var total = project.Tasks.Count;
        var completed = project.Tasks.Count(t => t.Status == WorkTaskStatus.Done);
        return new ProjectListItemDto
        {
            Id = project.Id,
            Name = project.Name,
            Color = project.Color,
            Icon = project.Icon,
            IsArchived = project.IsArchived,
            TotalTasks = total,
            CompletedTasks = completed,
            ProgressPercent = total == 0 ? 0 : Math.Round(100.0 * completed / total, 1),
            UpdatedAt = project.UpdatedAt
        };
    }

    public static ProjectDto ToDto(this Project project)
    {
        var total = project.Tasks.Count;
        var completed = project.Tasks.Count(t => t.Status == WorkTaskStatus.Done);
        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Color = project.Color,
            Icon = project.Icon,
            RepositoryUrl = project.RepositoryUrl,
            LocalPath = project.LocalPath,
            IsArchived = project.IsArchived,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            TotalTasks = total,
            CompletedTasks = completed,
            ProgressPercent = total == 0 ? 0 : Math.Round(100.0 * completed / total, 1),
            Milestones = project.Milestones.OrderBy(m => m.OrderNo).Select(m => m.ToDto()).ToList(),
            Environments = project.Environments.Select(e => e.ToDto()).ToList()
        };
    }

    public static FocusSessionDto ToDto(this FocusSession session, DateTime utcNow) => new()
    {
        Id = session.Id,
        TaskId = session.TaskId,
        TaskTitle = session.Task?.Title,
        ProjectId = session.ProjectId,
        ProjectName = session.Project?.Name,
        StartedAt = session.StartedAt,
        EndedAt = session.EndedAt,
        DurationMinutes = session.DurationMinutes,
        ElapsedMinutes = session.CalculateElapsedMinutes(utcNow),
        ElapsedSeconds = session.CalculateElapsedSeconds(utcNow),
        SessionType = session.SessionType,
        Notes = session.Notes,
        IsPaused = session.IsPaused,
        IsActive = session.IsActive,
        PausedAt = session.PausedAt,
        PausedAccumulatedSeconds = session.PausedAccumulatedSeconds,
        Pomodoro = session.PomodoroSession?.ToDto()
    };

    public static PomodoroSessionDto ToDto(this PomodoroSession pomodoro) => new()
    {
        Id = pomodoro.Id,
        FocusSessionId = pomodoro.FocusSessionId,
        WorkDurationMinutes = pomodoro.WorkDurationMinutes,
        BreakDurationMinutes = pomodoro.BreakDurationMinutes,
        Completed = pomodoro.Completed,
        StartedAt = pomodoro.StartedAt,
        EndedAt = pomodoro.EndedAt,
        SessionNumber = pomodoro.SessionNumber,
        IsBreak = pomodoro.IsBreak
    };

    public static DailyPlanDto ToDto(this DailyPlan plan, int estimatedWorkloadMinutes = 0) => new()
    {
        Id = plan.Id,
        Date = plan.Date,
        TopGoal1 = plan.TopGoal1,
        TopGoal2 = plan.TopGoal2,
        TopGoal3 = plan.TopGoal3,
        Notes = plan.Notes,
        AvailableWorkMinutes = plan.AvailableWorkMinutes,
        EstimatedWorkloadMinutes = estimatedWorkloadMinutes,
        WorkloadExceedsAvailable = estimatedWorkloadMinutes > plan.AvailableWorkMinutes,
        HasAllGoals = plan.HasAllGoals,
        CreatedAt = plan.CreatedAt,
        UpdatedAt = plan.UpdatedAt
    };

    public static DailyReviewDto ToDto(this DailyReview review) => new()
    {
        Id = review.Id,
        Date = review.Date,
        WhatWentWell = review.WhatWentWell,
        WhatDidNotGoWell = review.WhatDidNotGoWell,
        LessonsLearned = review.LessonsLearned,
        TomorrowPlan = review.TomorrowPlan,
        FocusMinutes = review.FocusMinutes,
        CompletedTaskCount = review.CompletedTaskCount,
        IsComplete = review.IsComplete,
        CreatedAt = review.CreatedAt,
        UpdatedAt = review.UpdatedAt
    };

    public static NoteDto ToDto(this Note note) => new()
    {
        Id = note.Id,
        Title = note.Title,
        Content = note.Content,
        ProjectId = note.ProjectId,
        ProjectName = note.Project?.Name,
        IsPinned = note.IsPinned,
        IsKnowledgeBase = note.IsKnowledgeBase,
        KnowledgeCategory = note.KnowledgeCategory,
        CreatedAt = note.CreatedAt,
        UpdatedAt = note.UpdatedAt,
        Tags = note.NoteTags.Select(nt => nt.Tag.ToDto()).ToList()
    };

    public static GoalDto ToDto(this Goal goal) => new()
    {
        Id = goal.Id,
        Title = goal.Title,
        Description = goal.Description,
        Category = goal.Category,
        TargetDate = goal.TargetDate,
        Progress = goal.Progress,
        IsCompleted = goal.IsCompleted,
        CreatedAt = goal.CreatedAt,
        UpdatedAt = goal.UpdatedAt,
        Milestones = goal.Milestones.OrderBy(m => m.OrderNo).Select(m => m.ToDto()).ToList()
    };

    public static HabitRecordDto ToDto(this HabitRecord record) => new()
    {
        Id = record.Id,
        HabitId = record.HabitId,
        Date = record.Date,
        IsCompleted = record.IsCompleted
    };

    public static HabitDto ToDto(this Habit habit, DateOnly today)
    {
        var records = habit.Records.OrderByDescending(r => r.Date).Take(30).Select(r => r.ToDto()).ToList();
        return new HabitDto
        {
            Id = habit.Id,
            Name = habit.Name,
            Description = habit.Description,
            Frequency = habit.Frequency,
            IsActive = habit.IsActive,
            CreatedAt = habit.CreatedAt,
            CurrentStreak = CalculateStreak(habit, today),
            CompletedToday = habit.Records.Any(r => r.Date == today && r.IsCompleted),
            RecentRecords = records
        };
    }

    public static BookmarkDto ToDto(this Bookmark bookmark) => new()
    {
        Id = bookmark.Id,
        Title = bookmark.Title,
        Url = bookmark.Url,
        Description = bookmark.Description,
        Category = bookmark.Category,
        ProjectId = bookmark.ProjectId,
        ProjectName = bookmark.Project?.Name,
        IsFavorite = bookmark.IsFavorite,
        CreatedAt = bookmark.CreatedAt
    };

    public static CodeSnippetDto ToDto(this CodeSnippet snippet) => new()
    {
        Id = snippet.Id,
        Title = snippet.Title,
        Description = snippet.Description,
        Language = snippet.Language,
        Code = snippet.Code,
        ProjectId = snippet.ProjectId,
        ProjectName = snippet.Project?.Name,
        IsFavorite = snippet.IsFavorite,
        CreatedAt = snippet.CreatedAt,
        UpdatedAt = snippet.UpdatedAt
    };

    public static CalendarEventDto ToDto(this CalendarEvent calendarEvent) => new()
    {
        Id = calendarEvent.Id,
        Title = calendarEvent.Title,
        Description = calendarEvent.Description,
        StartAt = calendarEvent.StartAt,
        EndAt = calendarEvent.EndAt,
        EventType = calendarEvent.EventType,
        ProjectId = calendarEvent.ProjectId,
        ProjectName = calendarEvent.Project?.Name,
        TaskId = calendarEvent.TaskId,
        TaskTitle = calendarEvent.Task?.Title
    };

    public static EnvironmentDto ToDto(this ProjectEnvironment environment) => new()
    {
        Id = environment.Id,
        ProjectId = environment.ProjectId,
        ProjectName = environment.Project?.Name ?? string.Empty,
        Name = environment.Name,
        EnvironmentType = environment.EnvironmentType,
        BaseUrl = environment.BaseUrl,
        DatabaseServer = environment.DatabaseServer,
        DatabaseName = environment.DatabaseName,
        Notes = environment.Notes,
        CreatedAt = environment.CreatedAt,
        UpdatedAt = environment.UpdatedAt
    };

    private static int CalculateStreak(Habit habit, DateOnly today)
    {
        var completedDates = habit.Records
            .Where(r => r.IsCompleted)
            .Select(r => r.Date)
            .ToHashSet();

        if (completedDates.Count == 0)
            return 0;

        var streak = 0;
        var cursor = completedDates.Contains(today) ? today : today.AddDays(-1);
        while (completedDates.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }
}
