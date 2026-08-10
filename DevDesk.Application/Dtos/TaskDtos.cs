using DevDesk.Domain.Enums;

namespace DevDesk.Application.Dtos;

public sealed class WorkTaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public WorkTaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public int? EstimatedMinutes { get; set; }
    public int ActualMinutes { get; set; }
    public bool IsStarred { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public IReadOnlyList<ChecklistItemDto> ChecklistItems { get; set; } = [];
    public IReadOnlyList<TagDto> Tags { get; set; } = [];
}

public sealed class TaskListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public WorkTaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public int? EstimatedMinutes { get; set; }
    public int ActualMinutes { get; set; }
    public bool IsStarred { get; set; }
    public int ChecklistTotal { get; set; }
    public int ChecklistCompleted { get; set; }
    public bool IsOverdue { get; set; }
}

public sealed class CreateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ProjectId { get; set; }
    public WorkTaskStatus Status { get; set; } = WorkTaskStatus.Todo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? DueDate { get; set; }
    public int? EstimatedMinutes { get; set; }
    public bool IsStarred { get; set; }
    public IReadOnlyList<string>? ChecklistTitles { get; set; }
    public IReadOnlyList<string>? TagNames { get; set; }
}

public sealed class UpdateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ProjectId { get; set; }
    public WorkTaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public int? EstimatedMinutes { get; set; }
    public bool IsStarred { get; set; }
}

public sealed class CreateChecklistItemRequest
{
    public string Title { get; set; } = string.Empty;
}

public sealed class UpdateChecklistItemRequest
{
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int OrderNo { get; set; }
}
