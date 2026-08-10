using DevDesk.Domain.Enums;

namespace DevDesk.Application.Parsing;

public sealed class QuickAddResult
{
    public string Title { get; init; } = string.Empty;
    public string? ProjectName { get; init; }
    public TaskPriority? Priority { get; init; }
    public DateOnly? DueDate { get; init; }
    public int? EstimatedMinutes { get; init; }
}
