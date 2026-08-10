using DevDesk.Application.Dtos;
using DevDesk.Application.Events;
using DevDesk.Domain.Enums;
using DevDesk.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DevDesk.Tests;

public class TaskWorkflowSyncTests
{
    private static readonly DateTime Start = new(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Create_persists_and_publishes_TaskCreated_visible_in_queries()
    {
        var (db, _, service, events) = TestDbFactory.CreateTaskService();
        await using var _ = db;
        AppEvent? published = null;
        events.Published += (_, e) => published = e;

        var created = await service.CreateAsync(new CreateTaskRequest
        {
            Title = "Ship Focus Command Center",
            Priority = TaskPriority.High,
            DueDate = Start.Date
        });

        created.Id.Should().NotBeEmpty();
        (await service.GetByIdAsync(created.Id)).Should().NotBeNull();
        (await service.GetMyDayTasksAsync()).Should().Contain(t => t.Id == created.Id);
        published.Should().NotBeNull();
        published!.Kind.Should().Be(AppEventKind.TaskCreated);
        published.EntityId.Should().Be(created.Id);
    }

    [Fact]
    public async Task Edit_persists_and_publishes_TaskUpdated()
    {
        var (db, _, service, events) = TestDbFactory.CreateTaskService();
        await using var _ = db;
        var created = await service.CreateAsync(new CreateTaskRequest { Title = "Old", DueDate = Start.Date });
        AppEvent? published = null;
        events.Published += (_, e) => published = e;

        var updated = await service.UpdateAsync(created.Id, new UpdateTaskRequest
        {
            Title = "New title",
            Priority = TaskPriority.Critical,
            Status = WorkTaskStatus.Todo,
            DueDate = Start.Date,
            ProjectId = null
        });

        updated.Title.Should().Be("New title");
        updated.Priority.Should().Be(TaskPriority.Critical);
        (await service.GetByIdAsync(created.Id))!.Title.Should().Be("New title");
        published!.Kind.Should().Be(AppEventKind.TaskUpdated);
    }

    [Fact]
    public async Task Delete_removes_and_publishes_TaskDeleted()
    {
        var (db, _, service, events) = TestDbFactory.CreateTaskService();
        await using var _ = db;
        var created = await service.CreateAsync(new CreateTaskRequest { Title = "Temp", DueDate = Start.Date });
        AppEvent? published = null;
        events.Published += (_, e) => published = e;

        await service.DeleteAsync(created.Id);

        (await service.GetByIdAsync(created.Id)).Should().BeNull();
        (await service.GetMyDayTasksAsync()).Should().NotContain(t => t.Id == created.Id);
        published!.Kind.Should().Be(AppEventKind.TaskDeleted);
        published.EntityId.Should().Be(created.Id);
    }

    [Fact]
    public async Task Complete_updates_state_and_publishes_TaskCompleted()
    {
        var (db, _, service, events) = TestDbFactory.CreateTaskService();
        await using var _ = db;
        var created = await service.CreateAsync(new CreateTaskRequest { Title = "Done me", DueDate = Start.Date });
        AppEvent? published = null;
        events.Published += (_, e) => published = e;

        var completed = await service.CompleteAsync(created.Id);

        completed.Status.Should().Be(WorkTaskStatus.Done);
        published!.Kind.Should().Be(AppEventKind.TaskCompleted);
        var board = await service.GetMyDayTasksAsync();
        board.Should().Contain(t => t.Id == created.Id && t.Status == WorkTaskStatus.Done);
    }

    [Fact]
    public async Task Create_while_focus_active_does_not_stop_session()
    {
        var (db, clock, focus) = TestDbFactory.CreateFocusService();
        await using var _ = db;
        var events = new AppEventBus();
        var tasks = new Application.Services.TaskService(db, clock, events);

        var taskA = await tasks.CreateAsync(new CreateTaskRequest { Title = "A", DueDate = Start.Date });
        var session = await focus.StartAsync(new StartFocusRequest { TaskId = taskA.Id });
        session.IsActive.Should().BeTrue();

        var taskB = await tasks.CreateAsync(new CreateTaskRequest { Title = "B", DueDate = Start.Date });
        var active = await focus.GetActiveAsync();
        active.Should().NotBeNull();
        active!.Id.Should().Be(session.Id);
        active.IsActive.Should().BeTrue();

        (await tasks.GetMyDayTasksAsync(session.TaskId)).Should().Contain(t => t.Id == taskB.Id);
    }

    [Fact]
    public async Task Edit_while_focus_active_keeps_timer_session()
    {
        var (db, clock, focus) = TestDbFactory.CreateFocusService();
        await using var _ = db;
        var events = new AppEventBus();
        var tasks = new Application.Services.TaskService(db, clock, events);

        var task = await tasks.CreateAsync(new CreateTaskRequest { Title = "Focus task", DueDate = Start.Date });
        var session = await focus.StartAsync(new StartFocusRequest { TaskId = task.Id });

        await tasks.UpdateAsync(task.Id, new UpdateTaskRequest
        {
            Title = "Renamed focus task",
            Status = WorkTaskStatus.InProgress,
            Priority = TaskPriority.High,
            DueDate = Start.Date
        });

        var active = await focus.GetActiveAsync();
        active.Should().NotBeNull();
        active!.Id.Should().Be(session.Id);
        active.IsActive.Should().BeTrue();
        (await tasks.GetByIdAsync(task.Id))!.Title.Should().Be("Renamed focus task");
    }

    [Fact]
    public async Task Complete_focus_task_keeps_session_valid()
    {
        var (db, clock, focus) = TestDbFactory.CreateFocusService();
        await using var _ = db;
        var events = new AppEventBus();
        var tasks = new Application.Services.TaskService(db, clock, events);

        var task = await tasks.CreateAsync(new CreateTaskRequest { Title = "Active", DueDate = Start.Date });
        var session = await focus.StartAsync(new StartFocusRequest { TaskId = task.Id });
        await tasks.CompleteAsync(task.Id);

        var active = await focus.GetActiveAsync();
        active.Should().NotBeNull();
        active!.Id.Should().Be(session.Id);
        active.IsActive.Should().BeTrue();
        (await tasks.GetByIdAsync(task.Id))!.Status.Should().Be(WorkTaskStatus.Done);
    }

    [Fact]
    public async Task Switch_focus_allows_only_one_active_session()
    {
        var (db, clock, focus) = TestDbFactory.CreateFocusService();
        await using var _ = db;
        var events = new AppEventBus();
        var tasks = new Application.Services.TaskService(db, clock, events);

        var a = await tasks.CreateAsync(new CreateTaskRequest { Title = "A", DueDate = Start.Date });
        var b = await tasks.CreateAsync(new CreateTaskRequest { Title = "B", DueDate = Start.Date });
        var first = await focus.StartAsync(new StartFocusRequest { TaskId = a.Id });

        await focus.StopAsync(first.Id);
        var second = await focus.StartAsync(new StartFocusRequest { TaskId = b.Id });

        var active = await focus.GetActiveAsync();
        active.Should().NotBeNull();
        active!.Id.Should().Be(second.Id);
        active.TaskId.Should().Be(b.Id);
        db.FocusSessions.Count(s => s.EndedAt == null).Should().Be(1);
    }

    [Fact]
    public async Task GetMyDayTasks_sorts_active_focus_first_then_priority()
    {
        var (db, _, service, _) = TestDbFactory.CreateTaskService();
        await using var _ = db;

        var low = await service.CreateAsync(new CreateTaskRequest { Title = "Low", Priority = TaskPriority.Low, DueDate = Start.Date });
        var high = await service.CreateAsync(new CreateTaskRequest { Title = "High", Priority = TaskPriority.High, DueDate = Start.Date });
        var focusTask = await service.CreateAsync(new CreateTaskRequest { Title = "Focus", Priority = TaskPriority.Medium, DueDate = Start.Date });

        var board = await service.GetMyDayTasksAsync(focusTask.Id);
        board[0].Id.Should().Be(focusTask.Id);
        board.Select(t => t.Id).Should().Contain([low.Id, high.Id]);
    }

    [Fact]
    public async Task Focus_start_pause_resume_stop_publish_events()
    {
        var bus = new AppEventBus();
        var (db, _, _) = TestDbFactory.CreateFocusService();
        await using var _ = db;
        // recreate with shared bus
        var clock = new FakeClock(Start);
        var focus = new Application.Services.FocusService(
            db, clock,
            Microsoft.Extensions.Options.Options.Create(new Application.Options.PomodoroOptions()),
            Microsoft.Extensions.Options.Options.Create(new Application.Options.FocusOptions()),
            bus);

        var kinds = new List<AppEventKind>();
        bus.Published += (_, e) => kinds.Add(e.Kind);

        var session = await focus.StartAsync(new StartFocusRequest());
        await focus.PauseAsync(session.Id);
        await focus.ResumeAsync(session.Id);
        await focus.StopAsync(session.Id);

        kinds.Should().ContainInOrder(
            AppEventKind.FocusStarted,
            AppEventKind.FocusPaused,
            AppEventKind.FocusResumed,
            AppEventKind.FocusStopped);
    }
}
