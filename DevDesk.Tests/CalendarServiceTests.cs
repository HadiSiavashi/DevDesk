using DevDesk.Application.Dtos;
using DevDesk.Domain.Entities;
using DevDesk.Domain.Enums;
using DevDesk.Tests.Helpers;
using FluentAssertions;

namespace DevDesk.Tests;

public sealed class CalendarServiceTests
{
    [Fact]
    public async Task GetRangeAsync_includes_open_tasks_with_due_dates_as_synthetic_deadlines()
    {
        var (db, clock, service) = TestDbFactory.CreateCalendarService();
        var due = clock.UtcNow.Date.AddDays(2);
        db.Tasks.Add(new WorkTask
        {
            Id = Guid.NewGuid(),
            Title = "Ship release",
            Status = WorkTaskStatus.Todo,
            Priority = TaskPriority.High,
            DueDate = due,
            CreatedAt = clock.UtcNow,
            UpdatedAt = clock.UtcNow
        });
        await db.SaveChangesAsync();

        var from = due.Date;
        var to = due.Date.AddDays(1).AddTicks(-1);
        var events = await service.GetRangeAsync(from, to);

        events.Should().ContainSingle(e =>
            e.Id == Guid.Empty &&
            e.EventType == CalendarEventType.Deadline &&
            e.Title.Contains("Ship release"));
    }

    [Fact]
    public async Task GetRangeAsync_excludes_done_tasks_from_synthetic_deadlines()
    {
        var (db, clock, service) = TestDbFactory.CreateCalendarService();
        var due = clock.UtcNow.Date.AddDays(1);
        db.Tasks.Add(new WorkTask
        {
            Id = Guid.NewGuid(),
            Title = "Already done",
            Status = WorkTaskStatus.Done,
            Priority = TaskPriority.Low,
            DueDate = due,
            CompletedAt = clock.UtcNow,
            CreatedAt = clock.UtcNow,
            UpdatedAt = clock.UtcNow
        });
        await db.SaveChangesAsync();

        var events = await service.GetRangeAsync(due.Date, due.Date.AddDays(1));
        events.Should().NotContain(e => e.Title.Contains("Already done"));
    }
}
