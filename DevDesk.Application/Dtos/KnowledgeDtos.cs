using DevDesk.Domain.Enums;

namespace DevDesk.Application.Dtos;

public sealed class NoteDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public bool IsPinned { get; set; }
    public bool IsKnowledgeBase { get; set; }
    public string? KnowledgeCategory { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public IReadOnlyList<TagDto> Tags { get; set; } = [];
}

public sealed class CreateNoteRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public bool IsPinned { get; set; }
    public bool IsKnowledgeBase { get; set; }
    public string? KnowledgeCategory { get; set; }
    public IReadOnlyList<string>? TagNames { get; set; }
}

public sealed class UpdateNoteRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public bool IsPinned { get; set; }
    public bool IsKnowledgeBase { get; set; }
    public string? KnowledgeCategory { get; set; }
}

public sealed class GoalDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public GoalCategory Category { get; set; }
    public DateTime? TargetDate { get; set; }
    public int Progress { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public IReadOnlyList<MilestoneDto> Milestones { get; set; } = [];
}

public sealed class CreateGoalRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public GoalCategory Category { get; set; } = GoalCategory.Other;
    public DateTime? TargetDate { get; set; }
}

public sealed class UpdateGoalRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public GoalCategory Category { get; set; }
    public DateTime? TargetDate { get; set; }
    public int Progress { get; set; }
}

public sealed class HabitDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public HabitFrequency Frequency { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CurrentStreak { get; set; }
    public bool CompletedToday { get; set; }
    public IReadOnlyList<HabitRecordDto> RecentRecords { get; set; } = [];
}

public sealed class HabitRecordDto
{
    public Guid Id { get; set; }
    public Guid HabitId { get; set; }
    public DateOnly Date { get; set; }
    public bool IsCompleted { get; set; }
}

public sealed class CreateHabitRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public HabitFrequency Frequency { get; set; } = HabitFrequency.Daily;
}

public sealed class UpdateHabitRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public HabitFrequency Frequency { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class BookmarkDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "Tools";
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class CreateBookmarkRequest
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "Tools";
    public Guid? ProjectId { get; set; }
    public bool IsFavorite { get; set; }
}

public sealed class UpdateBookmarkRequest
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "Tools";
    public Guid? ProjectId { get; set; }
    public bool IsFavorite { get; set; }
}

public sealed class CodeSnippetDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Language { get; set; } = "C#";
    public string Code { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class CreateSnippetRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Language { get; set; } = "C#";
    public string Code { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public bool IsFavorite { get; set; }
}

public sealed class UpdateSnippetRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Language { get; set; } = "C#";
    public string Code { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public bool IsFavorite { get; set; }
}

public sealed class CalendarEventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public CalendarEventType EventType { get; set; }
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? TaskId { get; set; }
    public string? TaskTitle { get; set; }
}

public sealed class CreateCalendarEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public CalendarEventType EventType { get; set; } = CalendarEventType.Other;
    public Guid? ProjectId { get; set; }
    public Guid? TaskId { get; set; }
}

public sealed class UpdateCalendarEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public CalendarEventType EventType { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? TaskId { get; set; }
}

public sealed class EnvironmentDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public EnvironmentType EnvironmentType { get; set; }
    public string? BaseUrl { get; set; }
    public string? DatabaseServer { get; set; }
    public string? DatabaseName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class CreateEnvironmentRequest
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public EnvironmentType EnvironmentType { get; set; }
    public string? BaseUrl { get; set; }
    public string? DatabaseServer { get; set; }
    public string? DatabaseName { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpdateEnvironmentRequest
{
    public string Name { get; set; } = string.Empty;
    public EnvironmentType EnvironmentType { get; set; }
    public string? BaseUrl { get; set; }
    public string? DatabaseServer { get; set; }
    public string? DatabaseName { get; set; }
    public string? Notes { get; set; }
}
