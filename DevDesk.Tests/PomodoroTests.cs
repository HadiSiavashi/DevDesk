using DevDesk.Application.Dtos;
using DevDesk.Domain.Entities;
using DevDesk.Domain.Enums;
using DevDesk.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DevDesk.Tests;

public class PomodoroTests
{
    private static readonly DateTime Start = new(2026, 8, 9, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task StartPomodoro_creates_focus_session_with_pomodoro_metadata()
    {
        var (db, clock, service) = TestDbFactory.CreateFocusService();
        await using var _ = db;
        clock.Set(Start);

        var dto = await service.StartPomodoroAsync(new StartPomodoroRequest
        {
            WorkMinutes = 25,
            BreakMinutes = 5
        });

        dto.SessionType.Should().Be(FocusSessionType.Pomodoro);
        dto.IsActive.Should().BeTrue();
        dto.Pomodoro.Should().NotBeNull();
        dto.Pomodoro!.WorkDurationMinutes.Should().Be(25);
        dto.Pomodoro.BreakDurationMinutes.Should().Be(5);
        dto.Pomodoro.Completed.Should().BeFalse();
        dto.Pomodoro.IsBreak.Should().BeFalse();
        dto.Pomodoro.SessionNumber.Should().Be(1);
    }

    [Fact]
    public async Task CompletePomodoro_stops_session_and_marks_pomodoro_completed()
    {
        var (db, clock, service) = TestDbFactory.CreateFocusService();
        await using var _ = db;
        clock.Set(Start);

        var started = await service.StartPomodoroAsync(new StartPomodoroRequest());
        clock.Advance(TimeSpan.FromMinutes(25));

        var completed = await service.CompletePomodoroAsync(started.Id);

        completed.IsActive.Should().BeFalse();
        completed.DurationMinutes.Should().Be(25);
        completed.Pomodoro.Should().NotBeNull();
        completed.Pomodoro!.Completed.Should().BeTrue();
        completed.Pomodoro.EndedAt.Should().Be(clock.UtcNow);

        var entity = await db.PomodoroSessions.AsNoTracking().FirstAsync(p => p.FocusSessionId == started.Id);
        entity.Completed.Should().BeTrue();
    }

    [Fact]
    public async Task StartPomodoro_moves_linked_task_to_InProgress()
    {
        var (db, clock, service) = TestDbFactory.CreateFocusService();
        await using var _ = db;

        var taskId = Guid.NewGuid();
        db.Tasks.Add(new WorkTask
        {
            Id = taskId,
            Title = "Pomodoro task",
            Status = WorkTaskStatus.Todo,
            CreatedAt = clock.UtcNow,
            UpdatedAt = clock.UtcNow
        });
        await db.SaveChangesAsync();

        await service.StartPomodoroAsync(new StartPomodoroRequest { TaskId = taskId });

        var task = await db.Tasks.FirstAsync(t => t.Id == taskId);
        task.Status.Should().Be(WorkTaskStatus.InProgress);
    }

    [Fact]
    public async Task Second_pomodoro_increments_session_number_after_completed_one()
    {
        var (db, clock, service) = TestDbFactory.CreateFocusService();
        await using var _ = db;
        clock.Set(Start);

        var first = await service.StartPomodoroAsync(new StartPomodoroRequest());
        clock.Advance(TimeSpan.FromMinutes(25));
        await service.CompletePomodoroAsync(first.Id);

        clock.Advance(TimeSpan.FromMinutes(5));
        var second = await service.StartPomodoroAsync(new StartPomodoroRequest());

        second.Pomodoro!.SessionNumber.Should().Be(2);
    }

    [Fact]
    public async Task Cannot_start_pomodoro_while_another_session_is_active()
    {
        var (db, clock, service) = TestDbFactory.CreateFocusService();
        await using var _ = db;

        await service.StartAsync(new StartFocusRequest());

        var act = async () => await service.StartPomodoroAsync(new StartPomodoroRequest());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*active focus session*");
    }
}
