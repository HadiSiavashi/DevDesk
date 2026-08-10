namespace DevDesk.Domain.Enums;

/// <summary>
/// Status of a work task. Named WorkTaskStatus to avoid collision with System.Threading.Tasks.TaskStatus.
/// </summary>
public enum WorkTaskStatus
{
    Backlog = 0,
    Todo = 1,
    InProgress = 2,
    Blocked = 3,
    Review = 4,
    Done = 5,
    Cancelled = 6
}
