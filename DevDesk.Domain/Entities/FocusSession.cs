using DevDesk.Domain.Enums;

namespace DevDesk.Domain.Entities;

public class FocusSession
{
    public Guid Id { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? ProjectId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int DurationMinutes { get; set; }
    public FocusSessionType SessionType { get; set; } = FocusSessionType.Focus;
    public string? Notes { get; set; }
    public bool IsPaused { get; set; }
    public DateTime? PausedAt { get; set; }
    public int PausedAccumulatedSeconds { get; set; }

    public WorkTask? Task { get; set; }
    public Project? Project { get; set; }
    public PomodoroSession? PomodoroSession { get; set; }

    public bool IsActive => EndedAt is null;

    public int CalculateElapsedMinutes(DateTime utcNow)
    {
        var end = EndedAt ?? utcNow;
        var totalSeconds = (int)(end - StartedAt).TotalSeconds - PausedAccumulatedSeconds;

        if (IsPaused && PausedAt.HasValue)
            totalSeconds -= (int)(utcNow - PausedAt.Value).TotalSeconds;

        return Math.Max(0, totalSeconds / 60);
    }

    public int CalculateElapsedSeconds(DateTime utcNow)
    {
        var end = EndedAt ?? utcNow;
        var totalSeconds = (int)(end - StartedAt).TotalSeconds - PausedAccumulatedSeconds;

        if (IsPaused && PausedAt.HasValue)
            totalSeconds -= (int)(utcNow - PausedAt.Value).TotalSeconds;

        return Math.Max(0, totalSeconds);
    }

    public void Pause(DateTime utcNow)
    {
        if (!IsActive || IsPaused)
            return;

        IsPaused = true;
        PausedAt = utcNow;
    }

    public void Resume(DateTime utcNow)
    {
        if (!IsActive || !IsPaused || PausedAt is null)
            return;

        PausedAccumulatedSeconds += (int)(utcNow - PausedAt.Value).TotalSeconds;
        IsPaused = false;
        PausedAt = null;
    }

    public void Stop(DateTime utcNow)
    {
        if (!IsActive)
            return;

        if (IsPaused)
            Resume(utcNow);

        EndedAt = utcNow;
        DurationMinutes = CalculateElapsedMinutes(utcNow);
        IsPaused = false;
        PausedAt = null;
    }
}
