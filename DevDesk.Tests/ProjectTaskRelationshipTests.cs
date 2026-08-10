using DevDesk.Application.Mapping;
using DevDesk.Domain.Entities;
using DevDesk.Domain.Enums;
using DevDesk.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DevDesk.Tests;

public class ProjectTaskRelationshipTests
{
    private static readonly DateTime Now = new(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Project_with_tasks_calculates_progress_from_completed_ratio()
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "CRM",
            CreatedAt = Now,
            UpdatedAt = Now,
            Tasks =
            [
                new WorkTask { Id = Guid.NewGuid(), Title = "A", Status = WorkTaskStatus.Done, CreatedAt = Now, UpdatedAt = Now },
                new WorkTask { Id = Guid.NewGuid(), Title = "B", Status = WorkTaskStatus.Done, CreatedAt = Now, UpdatedAt = Now },
                new WorkTask { Id = Guid.NewGuid(), Title = "C", Status = WorkTaskStatus.Todo, CreatedAt = Now, UpdatedAt = Now },
                new WorkTask { Id = Guid.NewGuid(), Title = "D", Status = WorkTaskStatus.InProgress, CreatedAt = Now, UpdatedAt = Now }
            ]
        };

        var dto = project.ToListItemDto();

        dto.TotalTasks.Should().Be(4);
        dto.CompletedTasks.Should().Be(2);
        dto.ProgressPercent.Should().Be(50.0);
    }

    [Fact]
    public void Project_with_no_tasks_has_zero_progress()
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Empty",
            CreatedAt = Now,
            UpdatedAt = Now
        };

        var dto = project.ToDto();

        dto.TotalTasks.Should().Be(0);
        dto.CompletedTasks.Should().Be(0);
        dto.ProgressPercent.Should().Be(0);
    }

    [Fact]
    public async Task ProjectService_includes_task_progress_from_persisted_relationship()
    {
        var (db, clock, service) = TestDbFactory.CreateProjectService();
        await using var _ = db;

        var projectId = Guid.NewGuid();
        db.Projects.Add(new Project
        {
            Id = projectId,
            Name = "API",
            CreatedAt = clock.UtcNow,
            UpdatedAt = clock.UtcNow
        });
        db.Tasks.AddRange(
            new WorkTask
            {
                Id = Guid.NewGuid(),
                Title = "Done task",
                ProjectId = projectId,
                Status = WorkTaskStatus.Done,
                CreatedAt = clock.UtcNow,
                UpdatedAt = clock.UtcNow,
                CompletedAt = clock.UtcNow
            },
            new WorkTask
            {
                Id = Guid.NewGuid(),
                Title = "Open task",
                ProjectId = projectId,
                Status = WorkTaskStatus.Todo,
                CreatedAt = clock.UtcNow,
                UpdatedAt = clock.UtcNow
            },
            new WorkTask
            {
                Id = Guid.NewGuid(),
                Title = "Also done",
                ProjectId = projectId,
                Status = WorkTaskStatus.Done,
                CreatedAt = clock.UtcNow,
                UpdatedAt = clock.UtcNow,
                CompletedAt = clock.UtcNow
            });
        await db.SaveChangesAsync();

        var dto = await service.GetByIdAsync(projectId);

        dto.Should().NotBeNull();
        dto!.TotalTasks.Should().Be(3);
        dto.CompletedTasks.Should().Be(2);
        dto.ProgressPercent.Should().Be(66.7);
    }

    [Fact]
    public async Task Completing_task_updates_project_progress()
    {
        var (db, clock, service) = TestDbFactory.CreateProjectService();
        await using var _ = db;

        var projectId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        db.Projects.Add(new Project
        {
            Id = projectId,
            Name = "Ship",
            CreatedAt = clock.UtcNow,
            UpdatedAt = clock.UtcNow
        });
        db.Tasks.Add(new WorkTask
        {
            Id = taskId,
            Title = "Finish",
            ProjectId = projectId,
            Status = WorkTaskStatus.Todo,
            CreatedAt = clock.UtcNow,
            UpdatedAt = clock.UtcNow
        });
        await db.SaveChangesAsync();

        var before = await service.GetByIdAsync(projectId);
        before!.ProgressPercent.Should().Be(0);

        var task = await db.Tasks.FirstAsync(t => t.Id == taskId);
        task.Complete(clock.UtcNow);
        await db.SaveChangesAsync();

        var after = await service.GetByIdAsync(projectId);
        after!.CompletedTasks.Should().Be(1);
        after.ProgressPercent.Should().Be(100.0);
    }
}
