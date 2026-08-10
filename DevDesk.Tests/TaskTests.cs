using DevDesk.Application.Mapping;
using DevDesk.Domain.Entities;
using DevDesk.Domain.Enums;
using FluentAssertions;

namespace DevDesk.Tests;

public class TaskTests
{
    private static readonly DateTime Now = new(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);

    private static WorkTask CreateTask(
        WorkTaskStatus status = WorkTaskStatus.Todo,
        TaskPriority priority = TaskPriority.Medium,
        DateTime? dueDate = null)
    {
        return new WorkTask
        {
            Id = Guid.NewGuid(),
            Title = "Sample task",
            Status = status,
            Priority = priority,
            DueDate = dueDate,
            CreatedAt = Now,
            UpdatedAt = Now
        };
    }

    [Fact]
    public void Creation_defaults_to_Todo_and_Medium_priority()
    {
        var task = new WorkTask
        {
            Id = Guid.NewGuid(),
            Title = "New task",
            CreatedAt = Now,
            UpdatedAt = Now
        };

        task.Status.Should().Be(WorkTaskStatus.Todo);
        task.Priority.Should().Be(TaskPriority.Medium);
        task.ActualMinutes.Should().Be(0);
        task.IsCompleted.Should().BeFalse();
        task.IsOpen.Should().BeTrue();
    }

    [Fact]
    public void Complete_sets_Done_status_and_CompletedAt()
    {
        var task = CreateTask();

        task.Complete(Now);

        task.Status.Should().Be(WorkTaskStatus.Done);
        task.CompletedAt.Should().Be(Now);
        task.UpdatedAt.Should().Be(Now);
        task.IsCompleted.Should().BeTrue();
        task.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void Reopen_clears_completion_and_returns_to_Todo()
    {
        var task = CreateTask();
        task.Complete(Now);

        var reopenAt = Now.AddHours(1);
        task.Reopen(reopenAt);

        task.Status.Should().Be(WorkTaskStatus.Todo);
        task.CompletedAt.Should().BeNull();
        task.UpdatedAt.Should().Be(reopenAt);
        task.IsOpen.Should().BeTrue();
    }

    [Theory]
    [InlineData(WorkTaskStatus.Backlog)]
    [InlineData(WorkTaskStatus.Todo)]
    [InlineData(WorkTaskStatus.InProgress)]
    [InlineData(WorkTaskStatus.Blocked)]
    [InlineData(WorkTaskStatus.Review)]
    public void Status_transitions_preserve_open_until_Done_or_Cancelled(WorkTaskStatus status)
    {
        var task = CreateTask(status);

        task.IsOpen.Should().BeTrue();
        task.IsCompleted.Should().BeFalse();
    }

    [Theory]
    [InlineData(WorkTaskStatus.Done)]
    [InlineData(WorkTaskStatus.Cancelled)]
    public void Done_and_Cancelled_are_not_open(WorkTaskStatus status)
    {
        var task = CreateTask(status);

        task.IsOpen.Should().BeFalse();
    }

    [Theory]
    [InlineData(TaskPriority.Low)]
    [InlineData(TaskPriority.Medium)]
    [InlineData(TaskPriority.High)]
    [InlineData(TaskPriority.Critical)]
    public void Priority_can_be_set(TaskPriority priority)
    {
        var task = CreateTask(priority: priority);
        task.Priority.Should().Be(priority);
    }

    [Fact]
    public void Due_date_overdue_when_open_and_due_before_today()
    {
        var yesterday = Now.Date.AddDays(-1);
        var task = CreateTask(dueDate: yesterday);

        var dto = task.ToListItemDto(Now);

        dto.IsOverdue.Should().BeTrue();
    }

    [Fact]
    public void Due_date_not_overdue_when_due_today()
    {
        var task = CreateTask(dueDate: Now.Date);

        var dto = task.ToListItemDto(Now);

        dto.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public void Due_date_not_overdue_when_completed()
    {
        var task = CreateTask(dueDate: Now.Date.AddDays(-2));
        task.Complete(Now);

        var dto = task.ToListItemDto(Now);

        dto.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public void Due_date_not_overdue_when_no_due_date()
    {
        var task = CreateTask();

        var dto = task.ToListItemDto(Now);

        dto.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public void StartFocus_moves_task_to_InProgress()
    {
        var task = CreateTask(WorkTaskStatus.Todo);

        task.StartFocus(Now);

        task.Status.Should().Be(WorkTaskStatus.InProgress);
        task.UpdatedAt.Should().Be(Now);
    }

    [Fact]
    public void StartFocus_throws_for_completed_task()
    {
        var task = CreateTask();
        task.Complete(Now);

        var act = () => task.StartFocus(Now);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed or cancelled*");
    }

    [Fact]
    public void StartFocus_throws_for_cancelled_task()
    {
        var task = CreateTask(WorkTaskStatus.Cancelled);

        var act = () => task.StartFocus(Now);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddActualMinutes_accumulates_and_updates_timestamp()
    {
        var task = CreateTask();

        task.AddActualMinutes(25, Now);
        task.AddActualMinutes(10, Now.AddMinutes(5));

        task.ActualMinutes.Should().Be(35);
        task.UpdatedAt.Should().Be(Now.AddMinutes(5));
    }

    [Fact]
    public void AddActualMinutes_rejects_negative()
    {
        var task = CreateTask();

        var act = () => task.AddActualMinutes(-1, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
