using System.Text.Json;
using DevDesk.Application.Dtos;
using DevDesk.Domain.Entities;
using DevDesk.Domain.Enums;
using DevDesk.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DevDesk.Tests;

public class ImportExportTests
{
    [Fact]
    public async Task Import_rejects_empty_json()
    {
        var (db, _, service) = TestDbFactory.CreateImportExportService();
        await using var _ = db;

        var result = await service.ImportJsonAsync("  ");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("empty");
    }

    [Fact]
    public async Task Import_rejects_invalid_json()
    {
        var (db, _, service) = TestDbFactory.CreateImportExportService();
        await using var _ = db;

        var result = await service.ImportJsonAsync("{ not-json");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid JSON");
    }

    [Fact]
    public async Task Import_rejects_missing_or_unsupported_version()
    {
        var (db, _, service) = TestDbFactory.CreateImportExportService();
        await using var _ = db;

        var result = await service.ImportJsonAsync("""{"version":0,"projects":[]}""");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("version");
    }

    [Fact]
    public async Task Import_rejects_merge_false()
    {
        var (db, _, service) = TestDbFactory.CreateImportExportService();
        await using var _ = db;

        var payload = JsonSerializer.Serialize(new ExportDataDto { Version = 1 });
        var result = await service.ImportJsonAsync(payload, merge: false);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("overwrite");
        result.Warnings.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Export_import_round_trip_preserves_projects_and_tasks()
    {
        var dbName = Guid.NewGuid().ToString();
        var (sourceDb, clock, exportService) = TestDbFactory.CreateImportExportService(dbName);
        await using (sourceDb)
        {
            var projectId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            sourceDb.Projects.Add(new Project
            {
                Id = projectId,
                Name = "CRM",
                Description = "Customer CRM",
                Color = "#112233",
                CreatedAt = clock.UtcNow,
                UpdatedAt = clock.UtcNow
            });
            sourceDb.Tasks.Add(new WorkTask
            {
                Id = taskId,
                Title = "Fix payment API",
                Description = "Handle retries",
                ProjectId = projectId,
                Status = WorkTaskStatus.InProgress,
                Priority = TaskPriority.High,
                EstimatedMinutes = 120,
                ActualMinutes = 30,
                IsStarred = true,
                CreatedAt = clock.UtcNow,
                UpdatedAt = clock.UtcNow
            });
            sourceDb.Tags.Add(new Tag { Id = Guid.NewGuid(), Name = "backend", Color = "#00AA00" });
            await sourceDb.SaveChangesAsync();

            var json = await exportService.ExportJsonAsync();
            json.Should().Contain("Fix payment API");
            json.Should().Contain("CRM");

            // Import into a fresh InMemory database.
            var (targetDb, _, importService) = TestDbFactory.CreateImportExportService();
            await using (targetDb)
            {
                var importResult = await importService.ImportJsonAsync(json, merge: true);

                importResult.Success.Should().BeTrue();
                importResult.ProjectsImported.Should().Be(1);
                importResult.TasksImported.Should().Be(1);

                var project = await targetDb.Projects.AsNoTracking().SingleAsync();
                project.Id.Should().Be(projectId);
                project.Name.Should().Be("CRM");
                project.Color.Should().Be("#112233");

                var task = await targetDb.Tasks.AsNoTracking().SingleAsync();
                task.Id.Should().Be(taskId);
                task.Title.Should().Be("Fix payment API");
                task.ProjectId.Should().Be(projectId);
                task.Status.Should().Be(WorkTaskStatus.InProgress);
                task.Priority.Should().Be(TaskPriority.High);
                task.EstimatedMinutes.Should().Be(120);
                task.ActualMinutes.Should().Be(30);
                task.IsStarred.Should().BeTrue();

                var tags = await targetDb.Tags.AsNoTracking().ToListAsync();
                tags.Should().ContainSingle(t => t.Name == "backend");
            }
        }
    }

    [Fact]
    public async Task Import_skips_existing_entities_and_reports_warnings()
    {
        var (db, clock, service) = TestDbFactory.CreateImportExportService();
        await using var _ = db;

        var projectId = Guid.NewGuid();
        db.Projects.Add(new Project
        {
            Id = projectId,
            Name = "Existing",
            CreatedAt = clock.UtcNow,
            UpdatedAt = clock.UtcNow
        });
        await db.SaveChangesAsync();

        var payload = new ExportDataDto
        {
            Version = 1,
            Projects =
            [
                new ProjectDto
                {
                    Id = projectId,
                    Name = "Existing",
                    Color = "#000000",
                    CreatedAt = clock.UtcNow,
                    UpdatedAt = clock.UtcNow
                }
            ],
            Tasks =
            [
                new WorkTaskDto
                {
                    Id = Guid.NewGuid(),
                    Title = "Orphan",
                    ProjectId = Guid.NewGuid(),
                    Status = WorkTaskStatus.Todo,
                    Priority = TaskPriority.Medium,
                    CreatedAt = clock.UtcNow,
                    UpdatedAt = clock.UtcNow
                }
            ]
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var result = await service.ImportJsonAsync(json);

        result.Success.Should().BeTrue();
        result.ProjectsImported.Should().Be(0);
        result.TasksImported.Should().Be(0);
        result.Warnings.Should().Contain(w => w.Contains("Skipped existing project"));
        result.Warnings.Should().Contain(w => w.Contains("project was missing"));
        (await db.Projects.CountAsync()).Should().Be(1);
        (await db.Tasks.CountAsync()).Should().Be(0);
    }
}
