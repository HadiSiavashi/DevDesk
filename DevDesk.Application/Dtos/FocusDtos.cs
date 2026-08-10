using DevDesk.Domain.Enums;

namespace DevDesk.Application.Dtos;

public sealed class FocusSessionDto
{
    public Guid Id { get; set; }
    public Guid? TaskId { get; set; }
    public string? TaskTitle { get; set; }
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int DurationMinutes { get; set; }
    public int ElapsedMinutes { get; set; }
    public FocusSessionType SessionType { get; set; }
    public string? Notes { get; set; }
    public bool IsPaused { get; set; }
    public bool IsActive { get; set; }
    public DateTime? PausedAt { get; set; }
    public int PausedAccumulatedSeconds { get; set; }
    public int ElapsedSeconds { get; set; }
    public PomodoroSessionDto? Pomodoro { get; set; }
}

public sealed class PomodoroSessionDto
{
    public Guid Id { get; set; }
    public Guid FocusSessionId { get; set; }
    public int WorkDurationMinutes { get; set; }
    public int BreakDurationMinutes { get; set; }
    public bool Completed { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int SessionNumber { get; set; }
    public bool IsBreak { get; set; }
}

public sealed class StartFocusRequest
{
    public Guid? TaskId { get; set; }
    public Guid? ProjectId { get; set; }
    public FocusSessionType SessionType { get; set; } = FocusSessionType.Focus;
    public string? Notes { get; set; }
}

public sealed class StartPomodoroRequest
{
    public Guid? TaskId { get; set; }
    public Guid? ProjectId { get; set; }
    public int? WorkMinutes { get; set; }
    public int? BreakMinutes { get; set; }
    public string? Notes { get; set; }
}
