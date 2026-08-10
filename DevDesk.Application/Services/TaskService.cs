using DevDesk.Application.Abstractions;
using DevDesk.Application.Dtos;
using DevDesk.Application.Events;
using DevDesk.Application.Interfaces;
using DevDesk.Application.Mapping;
using DevDesk.Application.Parsing;
using DevDesk.Domain.Entities;
using DevDesk.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DevDesk.Application.Services;

public sealed class TaskService(IDevDeskDbContext db, IClock clock, IAppEventBus events) : ITaskService
{
    public async Task<WorkTaskDto> CreateAsync(CreateTaskRequest request, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var task = new WorkTask
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description,
            ProjectId = request.ProjectId,
            Status = request.Status,
            Priority = request.Priority,
            DueDate = request.DueDate,
            EstimatedMinutes = request.EstimatedMinutes,
            IsStarred = request.IsStarred,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (request.ChecklistTitles is { Count: > 0 })
        {
            var order = 0;
            foreach (var title in request.ChecklistTitles.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                task.ChecklistItems.Add(new TaskChecklistItem
                {
                    Id = Guid.NewGuid(),
                    TaskId = task.Id,
                    Title = title.Trim(),
                    OrderNo = order++
                });
            }
        }

        if (request.TagNames is { Count: > 0 })
            await AttachTagsAsync(task, request.TagNames, ct);

        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);
        var dto = await GetRequiredAsync(task.Id, ct);
        events.Publish(AppEventKind.TaskCreated, dto.Id, dto);
        return dto;
    }

    public async Task<WorkTaskDto> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct = default)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new KeyNotFoundException($"Task {id} was not found.");

        var now = clock.UtcNow;
        task.Title = request.Title.Trim();
        task.Description = request.Description;
        task.ProjectId = request.ProjectId;
        task.Priority = request.Priority;
        task.DueDate = request.DueDate;
        task.EstimatedMinutes = request.EstimatedMinutes;
        task.IsStarred = request.IsStarred;
        task.UpdatedAt = now;

        if (task.Status != request.Status)
            ApplyStatus(task, request.Status, now);

        await db.SaveChangesAsync(ct);
        var dto = await GetRequiredAsync(id, ct);
        events.Publish(AppEventKind.TaskUpdated, dto.Id, dto);
        return dto;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new KeyNotFoundException($"Task {id} was not found.");
        db.Tasks.Remove(task);
        await db.SaveChangesAsync(ct);
        events.Publish(AppEventKind.TaskDeleted, id);
    }

    public async Task<WorkTaskDto> CompleteAsync(Guid id, CancellationToken ct = default)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new KeyNotFoundException($"Task {id} was not found.");
        task.Complete(clock.UtcNow);
        await db.SaveChangesAsync(ct);
        var dto = await GetRequiredAsync(id, ct);
        events.Publish(AppEventKind.TaskCompleted, dto.Id, dto);
        return dto;
    }

    public async Task<WorkTaskDto> ReopenAsync(Guid id, CancellationToken ct = default)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new KeyNotFoundException($"Task {id} was not found.");
        task.Reopen(clock.UtcNow);
        await db.SaveChangesAsync(ct);
        var dto = await GetRequiredAsync(id, ct);
        events.Publish(AppEventKind.TaskUpdated, dto.Id, dto);
        return dto;
    }

    public async Task<WorkTaskDto> ChangeStatusAsync(Guid id, WorkTaskStatus status, CancellationToken ct = default)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new KeyNotFoundException($"Task {id} was not found.");
        ApplyStatus(task, status, clock.UtcNow);
        await db.SaveChangesAsync(ct);
        var dto = await GetRequiredAsync(id, ct);
        events.Publish(status == WorkTaskStatus.Done ? AppEventKind.TaskCompleted : AppEventKind.TaskUpdated, dto.Id, dto);
        return dto;
    }

    public async Task<WorkTaskDto> ChangePriorityAsync(Guid id, TaskPriority priority, CancellationToken ct = default)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new KeyNotFoundException($"Task {id} was not found.");
        task.Priority = priority;
        task.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        var dto = await GetRequiredAsync(id, ct);
        events.Publish(AppEventKind.TaskUpdated, dto.Id, dto);
        return dto;
    }

    public async Task<WorkTaskDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var task = await QueryDetails().FirstOrDefaultAsync(t => t.Id == id, ct);
        return task?.ToDto(clock.UtcNow);
    }

    public async Task<IReadOnlyList<TaskListItemDto>> GetTodayAsync(CancellationToken ct = default)
    {
        var today = clock.Today;
        var now = clock.UtcNow;
        var items = await QueryList()
            .Where(t => t.Status != WorkTaskStatus.Done && t.Status != WorkTaskStatus.Cancelled && t.DueDate.HasValue)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .ToListAsync(ct);
        return items
            .Where(t => DateOnly.FromDateTime(t.DueDate!.Value) == today)
            .Select(t => t.ToListItemDto(now, clock.Today))
            .ToList();
    }

    public async Task<IReadOnlyList<TaskListItemDto>> GetMyDayTasksAsync(Guid? activeFocusTaskId = null, CancellationToken ct = default)
    {
        var today = clock.Today;
        var now = clock.UtcNow;
        var dayStart = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = today.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var items = await QueryList().ToListAsync(ct);

        static DateOnly? Due(WorkTask t) =>
            t.DueDate.HasValue ? DateOnly.FromDateTime(t.DueDate.Value) : null;

        var board = items
            .Where(t =>
            {
                if (t.Status == WorkTaskStatus.Cancelled) return false;
                if (activeFocusTaskId is Guid fid && t.Id == fid) return true;
                if (t.Status == WorkTaskStatus.Done)
                    return t.CompletedAt is DateTime c && c >= dayStart && c < dayEnd;
                if (t.Status == WorkTaskStatus.InProgress) return true;
                if (t.IsStarred) return true;
                var due = Due(t);
                return due is DateOnly d && d <= today;
            })
            .Select(t => t.ToListItemDto(now, today))
            .ToList();

        return board
            .OrderBy(t => SortKey(t, activeFocusTaskId))
            .ThenByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
            .ThenBy(t => t.Title)
            .ToList();
    }

    private static int SortKey(TaskListItemDto t, Guid? activeFocusTaskId)
    {
        if (activeFocusTaskId is Guid fid && t.Id == fid) return 0;
        if (t.Status == WorkTaskStatus.Done) return 6;
        if (t.Priority is TaskPriority.Critical or TaskPriority.High) return 1;
        if (t.IsOverdue) return 2;
        if (t.DueDate.HasValue) return 3;
        if (t.Status == WorkTaskStatus.InProgress) return 4;
        return 5;
    }

    public async Task<IReadOnlyList<TaskListItemDto>> GetUpcomingAsync(int days = 7, CancellationToken ct = default)
    {
        var today = clock.Today;
        var end = today.AddDays(Math.Max(1, days));
        var now = clock.UtcNow;
        var items = await QueryList()
            .Where(t => t.Status != WorkTaskStatus.Done && t.Status != WorkTaskStatus.Cancelled && t.DueDate.HasValue)
            .ToListAsync(ct);

        return items
            .Where(t =>
            {
                var d = DateOnly.FromDateTime(t.DueDate!.Value);
                return d > today && d <= end;
            })
            .OrderBy(t => t.DueDate)
            .ThenByDescending(t => t.Priority)
            .Select(t => t.ToListItemDto(now, clock.Today))
            .ToList();
    }

    public async Task<IReadOnlyList<TaskListItemDto>> GetOverdueAsync(CancellationToken ct = default)
    {
        var today = clock.Today;
        var now = clock.UtcNow;
        var items = await QueryList()
            .Where(t => t.Status != WorkTaskStatus.Done && t.Status != WorkTaskStatus.Cancelled && t.DueDate.HasValue)
            .ToListAsync(ct);

        return items
            .Where(t => DateOnly.FromDateTime(t.DueDate!.Value) < today)
            .OrderBy(t => t.DueDate)
            .Select(t => t.ToListItemDto(now, clock.Today))
            .ToList();
    }

    public async Task<IReadOnlyList<TaskListItemDto>> GetStarredAsync(CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var items = await QueryList()
            .Where(t => t.IsStarred && t.Status != WorkTaskStatus.Done && t.Status != WorkTaskStatus.Cancelled)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .ToListAsync(ct);
        return items.Select(t => t.ToListItemDto(now, clock.Today)).ToList();
    }

    public async Task<IReadOnlyList<TaskListItemDto>> GetCompletedAsync(int take = 100, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var items = await QueryList()
            .Where(t => t.Status == WorkTaskStatus.Done)
            .OrderByDescending(t => t.CompletedAt)
            .Take(Math.Clamp(take, 1, 500))
            .ToListAsync(ct);
        return items.Select(t => t.ToListItemDto(now, clock.Today)).ToList();
    }

    public async Task<IReadOnlyList<TaskListItemDto>> GetAllAsync(int take = 500, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var items = await QueryList()
            .OrderByDescending(t => t.UpdatedAt)
            .Take(Math.Clamp(take, 1, 2000))
            .ToListAsync(ct);
        return items.Select(t => t.ToListItemDto(now, clock.Today)).ToList();
    }

    public async Task<IReadOnlyList<TaskListItemDto>> GetByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var items = await QueryList()
            .Where(t => t.ProjectId == projectId)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .ToListAsync(ct);
        return items.Select(t => t.ToListItemDto(now, clock.Today)).ToList();
    }

    public async Task<IReadOnlyList<TaskListItemDto>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var term = query.Trim();
        var now = clock.UtcNow;
        var items = await QueryList()
            .Where(t => t.Title.Contains(term) || (t.Description != null && t.Description.Contains(term)))
            .OrderByDescending(t => t.UpdatedAt)
            .Take(100)
            .ToListAsync(ct);
        return items.Select(t => t.ToListItemDto(now, clock.Today)).ToList();
    }

    public async Task<WorkTaskDto> DuplicateAsync(Guid id, CancellationToken ct = default)
    {
        var source = await QueryDetails().FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new KeyNotFoundException($"Task {id} was not found.");

        var now = clock.UtcNow;
        var copy = new WorkTask
        {
            Id = Guid.NewGuid(),
            Title = $"{source.Title} (Copy)",
            Description = source.Description,
            ProjectId = source.ProjectId,
            Status = WorkTaskStatus.Todo,
            Priority = source.Priority,
            DueDate = source.DueDate,
            EstimatedMinutes = source.EstimatedMinutes,
            IsStarred = source.IsStarred,
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var item in source.ChecklistItems.OrderBy(c => c.OrderNo))
        {
            copy.ChecklistItems.Add(new TaskChecklistItem
            {
                Id = Guid.NewGuid(),
                TaskId = copy.Id,
                Title = item.Title,
                OrderNo = item.OrderNo
            });
        }

        foreach (var tag in source.TaskTags)
        {
            copy.TaskTags.Add(new TaskTag { TaskId = copy.Id, TagId = tag.TagId });
        }

        db.Tasks.Add(copy);
        await db.SaveChangesAsync(ct);
        var dto = await GetRequiredAsync(copy.Id, ct);
        events.Publish(AppEventKind.TaskCreated, dto.Id, dto);
        return dto;
    }

    public async Task<WorkTaskDto> CreateFromQuickAddAsync(string input, CancellationToken ct = default)
    {
        var parsed = QuickAddParser.Parse(input, clock.Today);
        Guid? projectId = null;
        if (!string.IsNullOrWhiteSpace(parsed.ProjectName))
        {
            var project = await db.Projects
                .FirstOrDefaultAsync(p => !p.IsArchived && p.Name == parsed.ProjectName, ct);

            if (project is null)
            {
                project = await db.Projects
                    .FirstOrDefaultAsync(p => !p.IsArchived && p.Name.ToLower() == parsed.ProjectName.ToLower(), ct);
            }

            projectId = project?.Id;
        }

        return await CreateAsync(new CreateTaskRequest
        {
            Title = parsed.Title,
            ProjectId = projectId,
            Priority = parsed.Priority ?? TaskPriority.Medium,
            DueDate = parsed.DueDate?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            EstimatedMinutes = parsed.EstimatedMinutes
        }, ct);
    }

    public async Task<ChecklistItemDto> AddChecklistItemAsync(Guid taskId, CreateChecklistItemRequest request, CancellationToken ct = default)
    {
        _ = await db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new KeyNotFoundException($"Task {taskId} was not found.");

        var maxOrder = await db.TaskChecklistItems
            .Where(c => c.TaskId == taskId)
            .Select(c => (int?)c.OrderNo)
            .MaxAsync(ct) ?? -1;

        var item = new TaskChecklistItem
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            Title = request.Title.Trim(),
            OrderNo = maxOrder + 1
        };
        db.TaskChecklistItems.Add(item);
        await db.SaveChangesAsync(ct);
        return item.ToDto();
    }

    public async Task<ChecklistItemDto> UpdateChecklistItemAsync(Guid taskId, Guid itemId, UpdateChecklistItemRequest request, CancellationToken ct = default)
    {
        var item = await db.TaskChecklistItems.FirstOrDefaultAsync(c => c.Id == itemId && c.TaskId == taskId, ct)
            ?? throw new KeyNotFoundException($"Checklist item {itemId} was not found.");

        item.Title = request.Title.Trim();
        item.IsCompleted = request.IsCompleted;
        item.OrderNo = request.OrderNo;
        await db.SaveChangesAsync(ct);
        return item.ToDto();
    }

    public async Task DeleteChecklistItemAsync(Guid taskId, Guid itemId, CancellationToken ct = default)
    {
        var item = await db.TaskChecklistItems.FirstOrDefaultAsync(c => c.Id == itemId && c.TaskId == taskId, ct)
            ?? throw new KeyNotFoundException($"Checklist item {itemId} was not found.");
        db.TaskChecklistItems.Remove(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task<ChecklistItemDto> ToggleChecklistItemAsync(Guid taskId, Guid itemId, CancellationToken ct = default)
    {
        var item = await db.TaskChecklistItems.FirstOrDefaultAsync(c => c.Id == itemId && c.TaskId == taskId, ct)
            ?? throw new KeyNotFoundException($"Checklist item {itemId} was not found.");
        item.IsCompleted = !item.IsCompleted;
        await db.SaveChangesAsync(ct);
        return item.ToDto();
    }

    private IQueryable<WorkTask> QueryList() =>
        db.Tasks
            .AsNoTracking()
            .Include(t => t.Project)
            .Include(t => t.ChecklistItems);

    private IQueryable<WorkTask> QueryDetails() =>
        db.Tasks
            .AsNoTracking()
            .Include(t => t.Project)
            .Include(t => t.ChecklistItems)
            .Include(t => t.TaskTags)
            .ThenInclude(tt => tt.Tag);

    private async Task<WorkTaskDto> GetRequiredAsync(Guid id, CancellationToken ct)
    {
        var task = await QueryDetails().FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new KeyNotFoundException($"Task {id} was not found.");
        return task.ToDto(clock.UtcNow);
    }

    private async Task AttachTagsAsync(WorkTask task, IReadOnlyList<string> tagNames, CancellationToken ct)
    {
        foreach (var name in tagNames.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var tag = await db.Tags.FirstOrDefaultAsync(t => t.Name == name, ct);
            if (tag is null)
            {
                tag = new Tag { Id = Guid.NewGuid(), Name = name };
                db.Tags.Add(tag);
            }

            task.TaskTags.Add(new TaskTag { TaskId = task.Id, TagId = tag.Id, Tag = tag });
        }
    }

    private static void ApplyStatus(WorkTask task, WorkTaskStatus status, DateTime utcNow)
    {
        if (status == WorkTaskStatus.Done)
        {
            task.Complete(utcNow);
            return;
        }

        if (task.Status == WorkTaskStatus.Done && status != WorkTaskStatus.Done)
        {
            task.Reopen(utcNow);
            task.Status = status;
            task.UpdatedAt = utcNow;
            return;
        }

        task.Status = status;
        task.UpdatedAt = utcNow;
        if (status != WorkTaskStatus.Done)
            task.CompletedAt = null;
    }
}
