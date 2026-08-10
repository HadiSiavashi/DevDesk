using DevDesk.Domain.Enums;

namespace DevDesk.Domain.Entities;

public class Goal
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public GoalCategory Category { get; set; } = GoalCategory.Other;
    public DateTime? TargetDate { get; set; }
    public int Progress { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();

    public void SetProgress(int progress, DateTime utcNow)
    {
        Progress = Math.Clamp(progress, 0, 100);
        IsCompleted = Progress >= 100;
        UpdatedAt = utcNow;
    }
}
