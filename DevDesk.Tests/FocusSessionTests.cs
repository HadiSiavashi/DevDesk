using DevDesk.Domain.Entities;
using DevDesk.Domain.Enums;
using DevDesk.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DevDesk.Tests;

public class FocusSessionTests
{
    private static readonly DateTime Start = new(2026, 8, 9, 10, 0, 0, DateTimeKind.Utc);

    private static FocusSession CreateSession(DateTime? startedAt = null) => new()
    {
        Id = Guid.NewGuid(),
        StartedAt = startedAt ?? Start,
        SessionType = FocusSessionType.Focus
    };

    [Fact]
    public void CalculateElapsedMinutes_counts_running_time()
    {
        var session = CreateSession();
        var now = Start.AddMinutes(45);

        session.CalculateElapsedMinutes(now).Should().Be(45);
        session.CalculateElapsedSeconds(now).Should().Be(45 * 60);
    }

    [Fact]
    public void Pause_and_Resume_accumulate_paused_seconds()
    {
        var session = CreateSession();
        var pauseAt = Start.AddMinutes(20);
        var resumeAt = Start.AddMinutes(30);
        var stopAt = Start.AddMinutes(50);

        session.Pause(pauseAt);
        session.IsPaused.Should().BeTrue();
        session.PausedAt.Should().Be(pauseAt);

        // While paused, elapsed should freeze at ~20 minutes.
        session.CalculateElapsedMinutes(Start.AddMinutes(25)).Should().Be(20);

        session.Resume(resumeAt);
        session.IsPaused.Should().BeFalse();
        session.PausedAt.Should().BeNull();
        session.PausedAccumulatedSeconds.Should().Be(10 * 60);

        session.Stop(stopAt);

        // 50 minutes wall clock − 10 minutes pause = 40 minutes.
        session.DurationMinutes.Should().Be(40);
        session.EndedAt.Should().Be(stopAt);
        session.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Stop_while_paused_resumes_first_then_finalizes()
    {
        var session = CreateSession();
        session.Pause(Start.AddMinutes(15));

        session.Stop(Start.AddMinutes(25));

        session.IsPaused.Should().BeFalse();
        session.PausedAt.Should().BeNull();
        session.DurationMinutes.Should().Be(15);
        session.PausedAccumulatedSeconds.Should().Be(10 * 60);
    }

    [Fact]
    public void Stop_is_idempotent_when_already_ended()
    {
        var session = CreateSession();
        session.Stop(Start.AddMinutes(10));
        var endedAt = session.EndedAt;
        var duration = session.DurationMinutes;

        session.Stop(Start.AddMinutes(99));

        session.EndedAt.Should().Be(endedAt);
        session.DurationMinutes.Should().Be(duration);
    }

    [Fact]
    public void Pause_is_noop_when_already_paused_or_inactive()
    {
        var session = CreateSession();
        session.Pause(Start.AddMinutes(5));
        var pausedAt = session.PausedAt;

        session.Pause(Start.AddMinutes(6));
        session.PausedAt.Should().Be(pausedAt);

        session.Stop(Start.AddMinutes(10));
        session.Pause(Start.AddMinutes(11));
        session.IsPaused.Should().BeFalse();
    }

    [Fact]
    public async Task Recovery_recalculates_elapsed_from_persisted_timestamps()
    {
        var (db, clock, service) = TestDbFactory.CreateFocusService();
        await using var _ = db;

        clock.Set(Start);
        var started = await service.StartAsync(new Application.Dtos.StartFocusRequest());
        clock.Advance(TimeSpan.FromMinutes(12));

        // Simulate app restart: reload entity and recover.
        var recovered = await service.RecoverActiveOnStartupAsync();

        recovered.Should().NotBeNull();
        recovered!.Id.Should().Be(started.Id);
        recovered.IsActive.Should().BeTrue();
        recovered.ElapsedMinutes.Should().Be(12);

        // Persist pause state, then recover again with more wall time.
        await service.PauseAsync(started.Id);
        clock.Advance(TimeSpan.FromMinutes(8));

        var stillPaused = await service.RecoverActiveOnStartupAsync();
        stillPaused!.IsPaused.Should().BeTrue();
        stillPaused.ElapsedMinutes.Should().Be(12);

        await service.ResumeAsync(started.Id);
        clock.Advance(TimeSpan.FromMinutes(5));

        var afterResume = await service.GetActiveAsync();
        afterResume!.ElapsedMinutes.Should().Be(17);

        var entity = await db.FocusSessions.AsNoTracking().FirstAsync(s => s.Id == started.Id);
        entity.PausedAccumulatedSeconds.Should().Be(8 * 60);
        entity.CalculateElapsedMinutes(clock.UtcNow).Should().Be(17);
    }

    [Fact]
    public async Task Stop_adds_actual_minutes_to_linked_task()
    {
        var (db, clock, service) = TestDbFactory.CreateFocusService();
        await using var _ = db;

        var taskId = Guid.NewGuid();
        db.Tasks.Add(new WorkTask
        {
            Id = taskId,
            Title = "Focus me",
            Status = WorkTaskStatus.Todo,
            CreatedAt = clock.UtcNow,
            UpdatedAt = clock.UtcNow
        });
        await db.SaveChangesAsync();

        clock.Set(Start);
        var session = await service.StartAsync(new Application.Dtos.StartFocusRequest { TaskId = taskId });
        clock.Advance(TimeSpan.FromMinutes(30));
        await service.StopAsync(session.Id);

        var task = await db.Tasks.FirstAsync(t => t.Id == taskId);
        task.Status.Should().Be(WorkTaskStatus.InProgress);
        task.ActualMinutes.Should().Be(30);
    }
}
