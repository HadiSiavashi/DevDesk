using System.Text.Json;
using DevDesk.Application.Abstractions;
using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Application.Mapping;
using DevDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevDesk.Application.Services;

public sealed class ImportExportService(
    IDevDeskDbContext db,
    IClock clock,
    ISettingsService settingsService) : IImportExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public async Task<ExportDataDto> ExportAsync(CancellationToken ct = default)
    {
        var projects = await db.Projects.AsNoTracking()
            .Include(p => p.Tasks)
            .Include(p => p.Milestones)
            .Include(p => p.Environments)
            .ToListAsync(ct);

        var tasks = await db.Tasks.AsNoTracking()
            .Include(t => t.Project)
            .Include(t => t.ChecklistItems)
            .Include(t => t.TaskTags).ThenInclude(tt => tt.Tag)
            .ToListAsync(ct);

        var notes = await db.Notes.AsNoTracking()
            .Include(n => n.Project)
            .Include(n => n.NoteTags).ThenInclude(nt => nt.Tag)
            .ToListAsync(ct);

        var goals = await db.Goals.AsNoTracking().Include(g => g.Milestones).ToListAsync(ct);
        var habits = await db.Habits.AsNoTracking().Include(h => h.Records).ToListAsync(ct);
        var bookmarks = await db.Bookmarks.AsNoTracking().Include(b => b.Project).ToListAsync(ct);
        var snippets = await db.CodeSnippets.AsNoTracking().Include(s => s.Project).ToListAsync(ct);
        var events = await db.CalendarEvents.AsNoTracking()
            .Include(e => e.Project)
            .Include(e => e.Task)
            .ToListAsync(ct);
        var tags = await db.Tags.AsNoTracking().ToListAsync(ct);
        var preferences = await settingsService.GetPreferencesAsync(ct);
        var today = clock.Today;

        return new ExportDataDto
        {
            Version = 1,
            ExportedAt = clock.UtcNow,
            Projects = projects.Select(p => p.ToDto()).ToList(),
            Tasks = tasks.Select(t => t.ToDto()).ToList(),
            Notes = notes.Select(n => n.ToDto()).ToList(),
            Goals = goals.Select(g => g.ToDto()).ToList(),
            Habits = habits.Select(h => h.ToDto(today)).ToList(),
            Bookmarks = bookmarks.Select(b => b.ToDto()).ToList(),
            Snippets = snippets.Select(s => s.ToDto()).ToList(),
            CalendarEvents = events.Select(e => e.ToDto()).ToList(),
            Tags = tags.Select(t => t.ToDto()).ToList(),
            Preferences = preferences
        };
    }

    public async Task<string> ExportJsonAsync(CancellationToken ct = default)
    {
        var data = await ExportAsync(ct);
        return JsonSerializer.Serialize(data, JsonOptions);
    }

    public async Task<ImportResultDto> ImportJsonAsync(string json, bool merge = true, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new ImportResultDto
            {
                Success = false,
                Message = "Import JSON is empty."
            };
        }

        ExportDataDto? data;
        try
        {
            data = JsonSerializer.Deserialize<ExportDataDto>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            return new ImportResultDto
            {
                Success = false,
                Message = $"Invalid JSON: {ex.Message}"
            };
        }

        if (data is null || data.Version < 1)
        {
            return new ImportResultDto
            {
                Success = false,
                Message = "Unsupported or missing export version."
            };
        }

        if (!merge)
        {
            return new ImportResultDto
            {
                Success = false,
                Message = "Blind overwrite is not supported. Use merge=true to import safely.",
                Warnings = ["Pass merge=true to import missing entities by Id without wiping existing data."]
            };
        }

        var warnings = new List<string>();
        var result = new ImportResultDto { Success = true };

        foreach (var tag in data.Tags)
        {
            if (await db.Tags.AnyAsync(t => t.Id == tag.Id || t.Name == tag.Name, ct))
                continue;

            db.Tags.Add(new Tag { Id = tag.Id == Guid.Empty ? Guid.NewGuid() : tag.Id, Name = tag.Name, Color = tag.Color });
        }

        foreach (var project in data.Projects)
        {
            if (await db.Projects.AnyAsync(p => p.Id == project.Id || p.Name == project.Name, ct))
            {
                warnings.Add($"Skipped existing project '{project.Name}'.");
                continue;
            }

            var now = clock.UtcNow;
            db.Projects.Add(new Project
            {
                Id = project.Id == Guid.Empty ? Guid.NewGuid() : project.Id,
                Name = project.Name,
                Description = project.Description,
                Color = project.Color,
                Icon = project.Icon,
                RepositoryUrl = project.RepositoryUrl,
                LocalPath = project.LocalPath,
                IsArchived = project.IsArchived,
                CreatedAt = project.CreatedAt == default ? now : project.CreatedAt,
                UpdatedAt = project.UpdatedAt == default ? now : project.UpdatedAt
            });
            result = result with { ProjectsImported = result.ProjectsImported + 1 };
        }

        await db.SaveChangesAsync(ct);

        foreach (var task in data.Tasks)
        {
            if (await db.Tasks.AnyAsync(t => t.Id == task.Id, ct))
            {
                warnings.Add($"Skipped existing task '{task.Title}'.");
                continue;
            }

            if (task.ProjectId is Guid pid && !await db.Projects.AnyAsync(p => p.Id == pid, ct))
            {
                warnings.Add($"Skipped task '{task.Title}' because project was missing.");
                continue;
            }

            var now = clock.UtcNow;
            db.Tasks.Add(new WorkTask
            {
                Id = task.Id == Guid.Empty ? Guid.NewGuid() : task.Id,
                Title = task.Title,
                Description = task.Description,
                ProjectId = task.ProjectId,
                Status = task.Status,
                Priority = task.Priority,
                DueDate = task.DueDate,
                EstimatedMinutes = task.EstimatedMinutes,
                ActualMinutes = task.ActualMinutes,
                IsStarred = task.IsStarred,
                CreatedAt = task.CreatedAt == default ? now : task.CreatedAt,
                UpdatedAt = task.UpdatedAt == default ? now : task.UpdatedAt,
                CompletedAt = task.CompletedAt
            });
            result = result with { TasksImported = result.TasksImported + 1 };
        }

        foreach (var note in data.Notes)
        {
            if (await db.Notes.AnyAsync(n => n.Id == note.Id, ct))
            {
                warnings.Add($"Skipped existing note '{note.Title}'.");
                continue;
            }

            var now = clock.UtcNow;
            db.Notes.Add(new Note
            {
                Id = note.Id == Guid.Empty ? Guid.NewGuid() : note.Id,
                Title = note.Title,
                Content = note.Content,
                ProjectId = note.ProjectId,
                IsPinned = note.IsPinned,
                IsKnowledgeBase = note.IsKnowledgeBase,
                KnowledgeCategory = note.KnowledgeCategory,
                CreatedAt = note.CreatedAt == default ? now : note.CreatedAt,
                UpdatedAt = note.UpdatedAt == default ? now : note.UpdatedAt
            });
            result = result with { NotesImported = result.NotesImported + 1 };
        }

        foreach (var goal in data.Goals)
        {
            if (await db.Goals.AnyAsync(g => g.Id == goal.Id, ct))
            {
                warnings.Add($"Skipped existing goal '{goal.Title}'.");
                continue;
            }

            var now = clock.UtcNow;
            db.Goals.Add(new Goal
            {
                Id = goal.Id == Guid.Empty ? Guid.NewGuid() : goal.Id,
                Title = goal.Title,
                Description = goal.Description,
                Category = goal.Category,
                TargetDate = goal.TargetDate,
                Progress = goal.Progress,
                IsCompleted = goal.IsCompleted,
                CreatedAt = goal.CreatedAt == default ? now : goal.CreatedAt,
                UpdatedAt = goal.UpdatedAt == default ? now : goal.UpdatedAt
            });
            result = result with { GoalsImported = result.GoalsImported + 1 };
        }

        foreach (var habit in data.Habits)
        {
            if (await db.Habits.AnyAsync(h => h.Id == habit.Id, ct))
            {
                warnings.Add($"Skipped existing habit '{habit.Name}'.");
                continue;
            }

            db.Habits.Add(new Habit
            {
                Id = habit.Id == Guid.Empty ? Guid.NewGuid() : habit.Id,
                Name = habit.Name,
                Description = habit.Description,
                Frequency = habit.Frequency,
                IsActive = habit.IsActive,
                CreatedAt = habit.CreatedAt == default ? clock.UtcNow : habit.CreatedAt
            });
            result = result with { HabitsImported = result.HabitsImported + 1 };
        }

        foreach (var bookmark in data.Bookmarks)
        {
            if (await db.Bookmarks.AnyAsync(b => b.Id == bookmark.Id, ct))
            {
                warnings.Add($"Skipped existing bookmark '{bookmark.Title}'.");
                continue;
            }

            db.Bookmarks.Add(new Bookmark
            {
                Id = bookmark.Id == Guid.Empty ? Guid.NewGuid() : bookmark.Id,
                Title = bookmark.Title,
                Url = bookmark.Url,
                Description = bookmark.Description,
                Category = bookmark.Category,
                ProjectId = bookmark.ProjectId,
                IsFavorite = bookmark.IsFavorite,
                CreatedAt = bookmark.CreatedAt == default ? clock.UtcNow : bookmark.CreatedAt
            });
            result = result with { BookmarksImported = result.BookmarksImported + 1 };
        }

        foreach (var snippet in data.Snippets)
        {
            if (await db.CodeSnippets.AnyAsync(s => s.Id == snippet.Id, ct))
            {
                warnings.Add($"Skipped existing snippet '{snippet.Title}'.");
                continue;
            }

            var now = clock.UtcNow;
            db.CodeSnippets.Add(new CodeSnippet
            {
                Id = snippet.Id == Guid.Empty ? Guid.NewGuid() : snippet.Id,
                Title = snippet.Title,
                Description = snippet.Description,
                Language = snippet.Language,
                Code = snippet.Code,
                ProjectId = snippet.ProjectId,
                IsFavorite = snippet.IsFavorite,
                CreatedAt = snippet.CreatedAt == default ? now : snippet.CreatedAt,
                UpdatedAt = snippet.UpdatedAt == default ? now : snippet.UpdatedAt
            });
            result = result with { SnippetsImported = result.SnippetsImported + 1 };
        }

        foreach (var calendarEvent in data.CalendarEvents)
        {
            if (await db.CalendarEvents.AnyAsync(e => e.Id == calendarEvent.Id, ct))
            {
                warnings.Add($"Skipped existing calendar event '{calendarEvent.Title}'.");
                continue;
            }

            db.CalendarEvents.Add(new CalendarEvent
            {
                Id = calendarEvent.Id == Guid.Empty ? Guid.NewGuid() : calendarEvent.Id,
                Title = calendarEvent.Title,
                Description = calendarEvent.Description,
                StartAt = calendarEvent.StartAt,
                EndAt = calendarEvent.EndAt,
                EventType = calendarEvent.EventType,
                ProjectId = calendarEvent.ProjectId,
                TaskId = calendarEvent.TaskId
            });
            result = result with { CalendarEventsImported = result.CalendarEventsImported + 1 };
        }

        if (data.Preferences is not null)
            await settingsService.SavePreferencesAsync(data.Preferences, ct);

        await db.SaveChangesAsync(ct);

        return result with
        {
            Message = "Import completed with merge (existing entities skipped).",
            Warnings = warnings
        };
    }
}
