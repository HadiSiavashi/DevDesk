namespace DevDesk.Domain.Entities;

public class TaskChecklistItem
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int OrderNo { get; set; }

    public WorkTask Task { get; set; } = null!;
}
