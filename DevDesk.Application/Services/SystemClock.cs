using DevDesk.Application.Abstractions;

namespace DevDesk.Application.Services;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;

    /// <summary>
    /// Local calendar day for "today" / overdue / daily plan semantics.
    /// Timestamps still use <see cref="UtcNow"/>.
    /// </summary>
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}
