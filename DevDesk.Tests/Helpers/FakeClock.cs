using DevDesk.Application.Abstractions;

namespace DevDesk.Tests.Helpers;

public sealed class FakeClock : IClock
{
    public FakeClock(DateTime utcNow)
    {
        UtcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
    }

    public DateTime UtcNow { get; private set; }

    public DateOnly Today => DateOnly.FromDateTime(UtcNow);

    public void Advance(TimeSpan span) => UtcNow = UtcNow.Add(span);

    public void Set(DateTime utcNow) => UtcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
}
