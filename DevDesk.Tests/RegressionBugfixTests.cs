using DevDesk.Application.Dtos;
using DevDesk.Application.Events;
using DevDesk.Application.Services;
using DevDesk.Domain.Entities;
using DevDesk.Domain.Enums;
using DevDesk.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevDesk.Tests;

public class RegressionBugfixTests
{
    private static readonly DateTime Start = new(2026, 8, 9, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CompletePomodoro_then_CompleteAgain_does_not_double_count_ActualMinutes()
    {
        var (db, clock, service) = TestDbFactory.CreateFocusService();
        await using var _ = db;
        clock.Set(Start);

        var taskId = Guid.NewGuid();
        db.Tasks.Add(new WorkTask
        {
            Id = taskId,
            Title = "Timed task",
            Status = WorkTaskStatus.Todo,
            CreatedAt = Start,
            UpdatedAt = Start
        });
        await db.SaveChangesAsync();

        var started = await service.StartPomodoroAsync(new StartPomodoroRequest { TaskId = taskId });
        clock.Advance(TimeSpan.FromMinutes(25));
        await service.CompletePomodoroAsync(started.Id);

        var afterFirst = await db.Tasks.AsNoTracking().FirstAsync(t => t.Id == taskId);
        afterFirst.ActualMinutes.Should().Be(25);

        await service.CompletePomodoroAsync(started.Id);

        var afterSecond = await db.Tasks.AsNoTracking().FirstAsync(t => t.Id == taskId);
        afterSecond.ActualMinutes.Should().Be(25);
    }

    [Fact]
    public async Task Stop_then_CompletePomodoro_does_not_double_count_ActualMinutes()
    {
        var (db, clock, service) = TestDbFactory.CreateFocusService();
        await using var _ = db;
        clock.Set(Start);

        var taskId = Guid.NewGuid();
        db.Tasks.Add(new WorkTask
        {
            Id = taskId,
            Title = "Timed task",
            Status = WorkTaskStatus.Todo,
            CreatedAt = Start,
            UpdatedAt = Start
        });
        await db.SaveChangesAsync();

        var started = await service.StartPomodoroAsync(new StartPomodoroRequest { TaskId = taskId });
        clock.Advance(TimeSpan.FromMinutes(20));
        await service.StopAsync(started.Id);
        await service.CompletePomodoroAsync(started.Id);

        var task = await db.Tasks.AsNoTracking().FirstAsync(t => t.Id == taskId);
        task.ActualMinutes.Should().Be(20);
    }

    [Fact]
    public async Task GetAllAsync_returns_tasks_when_search_empty_would_not()
    {
        var db = TestDbFactory.CreateDbContext();
        await using var _ = db;
        var clock = new FakeClock(Start);
        var service = new TaskService(db, clock, new AppEventBus());

        db.Tasks.Add(new WorkTask
        {
            Id = Guid.NewGuid(),
            Title = "Visible task",
            Status = WorkTaskStatus.Todo,
            CreatedAt = Start,
            UpdatedAt = Start
        });
        await db.SaveChangesAsync();

        (await service.SearchAsync("")).Should().BeEmpty();
        (await service.GetAllAsync()).Should().HaveCount(1);
    }

    [Fact]
    public async Task Recover_when_disabled_stops_orphan_session_so_start_is_unblocked()
    {
        var (db, clock, service) = TestDbFactory.CreateFocusService(
            focus: new Application.Options.FocusOptions { RecoverActiveSessionOnStartup = false });
        await using var _ = db;
        clock.Set(Start);

        await service.StartAsync(new StartFocusRequest());
        var recovered = await service.RecoverActiveOnStartupAsync();
        recovered.Should().BeNull();

        var act = async () => await service.StartAsync(new StartFocusRequest());
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void SystemClock_Today_uses_local_calendar_day()
    {
        var clock = new SystemClock();
        clock.Today.Should().Be(DateOnly.FromDateTime(DateTime.Now));
    }

    [Fact]
    public void FocusSessionDto_exposes_pause_fields_for_client_timer()
    {
        var session = new FocusSession
        {
            Id = Guid.NewGuid(),
            StartedAt = Start,
            IsPaused = true,
            PausedAt = Start.AddMinutes(10),
            PausedAccumulatedSeconds = 30
        };

        var dto = Application.Mapping.MappingExtensions.ToDto(session, Start.AddMinutes(12));
        dto.PausedAt.Should().Be(session.PausedAt);
        dto.PausedAccumulatedSeconds.Should().Be(30);
        dto.ElapsedSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Productivity_score_uses_preferences_target_focus_minutes()
    {
        var db = TestDbFactory.CreateDbContext();
        await using var _ = db;
        var clock = new FakeClock(Start);
        var settings = new SettingsService(db);
        await settings.SavePreferencesAsync(new AppPreferencesDto { TargetFocusMinutesPerDay = 60 });

        var analytics = new AnalyticsService(
            db,
            Options.Create(new Application.Options.AppOptions { TargetFocusMinutesPerDay = 120 }),
            settings);

        // 60 focus minutes vs target 60 → full focus component (35)
        db.FocusSessions.Add(new FocusSession
        {
            Id = Guid.NewGuid(),
            StartedAt = Start,
            EndedAt = Start.AddMinutes(60),
            DurationMinutes = 60,
            SessionType = FocusSessionType.Focus
        });
        await db.SaveChangesAsync();

        var score = await analytics.GetProductivityScoreAsync(DateOnly.FromDateTime(Start));
        score.FocusScore.Should().Be(35);
    }
}
