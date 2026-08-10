namespace DevDesk.Domain.Entities;

public class DailyPlan
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public string? TopGoal1 { get; set; }
    public string? TopGoal2 { get; set; }
    public string? TopGoal3 { get; set; }
    public string? Notes { get; set; }
    public int AvailableWorkMinutes { get; set; } = 480;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public bool HasAllGoals =>
        !string.IsNullOrWhiteSpace(TopGoal1) &&
        !string.IsNullOrWhiteSpace(TopGoal2) &&
        !string.IsNullOrWhiteSpace(TopGoal3);
}
