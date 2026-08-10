namespace DevDesk.Domain.Entities;

public class TaskTag
{
    public Guid TaskId { get; set; }
    public Guid TagId { get; set; }

    public WorkTask Task { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
