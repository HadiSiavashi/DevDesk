namespace DevDesk.Domain.Entities;

public class Milestone
{
    public Guid Id { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? GoalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public int OrderNo { get; set; }

    public Project? Project { get; set; }
    public Goal? Goal { get; set; }
}
