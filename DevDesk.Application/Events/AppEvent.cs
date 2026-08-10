namespace DevDesk.Application.Events;

public enum AppEventKind
{
    TaskCreated,
    TaskUpdated,
    TaskDeleted,
    TaskCompleted,
    FocusStarted,
    FocusPaused,
    FocusResumed,
    FocusStopped
}

public sealed class AppEvent
{
    public required AppEventKind Kind { get; init; }
    public Guid? EntityId { get; init; }
    public object? Payload { get; init; }
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
}
