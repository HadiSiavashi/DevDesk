using DevDesk.Domain.Enums;

namespace DevDesk.Domain.Entities;

/// <summary>
/// A developer work item. Named WorkTask to avoid collision with System.Threading.Tasks.Task.
/// Maps to database table "Tasks".
/// </summary>
public class WorkTask
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ProjectId { get; set; }
    public WorkTaskStatus Status { get; set; } = WorkTaskStatus.Todo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? DueDate { get; set; }
    public int? EstimatedMinutes { get; set; }
    public int ActualMinutes { get; set; }
    public bool IsStarred { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Project? Project { get; set; }
    public ICollection<TaskChecklistItem> ChecklistItems { get; set; } = new List<TaskChecklistItem>();
    public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
    public ICollection<FocusSession> FocusSessions { get; set; } = new List<FocusSession>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();

    public bool IsCompleted => Status == WorkTaskStatus.Done;
    public bool IsOpen => Status is not WorkTaskStatus.Done and not WorkTaskStatus.Cancelled;

    public void Complete(DateTime utcNow)
    {
        Status = WorkTaskStatus.Done;
        CompletedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public void Reopen(DateTime utcNow)
    {
        Status = WorkTaskStatus.Todo;
        CompletedAt = null;
        UpdatedAt = utcNow;
    }

    public void StartFocus(DateTime utcNow)
    {
        if (Status is WorkTaskStatus.Done or WorkTaskStatus.Cancelled)
            throw new InvalidOperationException("Cannot start focus on a completed or cancelled task.");

        Status = WorkTaskStatus.InProgress;
        UpdatedAt = utcNow;
    }

    public void AddActualMinutes(int minutes, DateTime utcNow)
    {
        if (minutes < 0)
            throw new ArgumentOutOfRangeException(nameof(minutes));

        ActualMinutes += minutes;
        UpdatedAt = utcNow;
    }
}
