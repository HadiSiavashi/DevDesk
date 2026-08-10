namespace DevDesk.Domain.Entities;

public class PomodoroSession
{
    public Guid Id { get; set; }
    public Guid FocusSessionId { get; set; }
    public int WorkDurationMinutes { get; set; } = 25;
    public int BreakDurationMinutes { get; set; } = 5;
    public bool Completed { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int SessionNumber { get; set; } = 1;
    public bool IsBreak { get; set; }

    public FocusSession FocusSession { get; set; } = null!;
}
