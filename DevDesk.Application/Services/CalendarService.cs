using DevDesk.Application.Abstractions;
using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Application.Mapping;
using DevDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevDesk.Application.Services;

public sealed class CalendarService(IDevDeskDbContext db) : ICalendarService
{
    public async Task<CalendarEventDto> CreateAsync(CreateCalendarEventRequest request, CancellationToken ct = default)
    {
        if (request.EndAt <= request.StartAt)
            throw new ArgumentException("EndAt must be after StartAt.");

        var calendarEvent = new CalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            EventType = request.EventType,
            ProjectId = request.ProjectId,
            TaskId = request.TaskId
        };
        db.CalendarEvents.Add(calendarEvent);
        await db.SaveChangesAsync(ct);
        return await GetRequiredAsync(calendarEvent.Id, ct);
    }

    public async Task<CalendarEventDto> UpdateAsync(Guid id, UpdateCalendarEventRequest request, CancellationToken ct = default)
    {
        if (request.EndAt <= request.StartAt)
            throw new ArgumentException("EndAt must be after StartAt.");

        var calendarEvent = await db.CalendarEvents.FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new KeyNotFoundException($"Calendar event {id} was not found.");

        calendarEvent.Title = request.Title.Trim();
        calendarEvent.Description = request.Description;
        calendarEvent.StartAt = request.StartAt;
        calendarEvent.EndAt = request.EndAt;
        calendarEvent.EventType = request.EventType;
        calendarEvent.ProjectId = request.ProjectId;
        calendarEvent.TaskId = request.TaskId;
        await db.SaveChangesAsync(ct);
        return await GetRequiredAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var calendarEvent = await db.CalendarEvents.FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new KeyNotFoundException($"Calendar event {id} was not found.");
        db.CalendarEvents.Remove(calendarEvent);
        await db.SaveChangesAsync(ct);
    }

    public async Task<CalendarEventDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var calendarEvent = await DetailQuery().FirstOrDefaultAsync(e => e.Id == id, ct);
        return calendarEvent?.ToDto();
    }

    public async Task<IReadOnlyList<CalendarEventDto>> GetRangeAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var items = await DetailQuery()
            .Where(e => e.StartAt <= to && e.EndAt >= from)
            .OrderBy(e => e.StartAt)
            .ToListAsync(ct);

        var events = items.Select(e => e.ToDto()).ToList();

        // Auto-include open tasks with due dates in range as Deadline calendar entries.
        var fromDate = DateOnly.FromDateTime(from);
        var toDate = DateOnly.FromDateTime(to);
        var dueTasks = await db.Tasks.AsNoTracking()
            .Include(t => t.Project)
            .Where(t =>
                t.DueDate.HasValue &&
                t.Status != Domain.Enums.WorkTaskStatus.Done &&
                t.Status != Domain.Enums.WorkTaskStatus.Cancelled)
            .ToListAsync(ct);

        foreach (var task in dueTasks)
        {
            var due = DateOnly.FromDateTime(task.DueDate!.Value);
            if (due < fromDate || due > toDate)
                continue;

            // Skip if an explicit calendar event already links this task.
            if (events.Any(e => e.TaskId == task.Id))
                continue;

            var start = task.DueDate.Value.Date;
            events.Add(new CalendarEventDto
            {
                Id = Guid.Empty, // synthetic
                Title = $"Deadline: {task.Title}",
                Description = task.Project?.Name,
                StartAt = start,
                EndAt = start.AddDays(1).AddTicks(-1),
                EventType = Domain.Enums.CalendarEventType.Deadline,
                ProjectId = task.ProjectId,
                ProjectName = task.Project?.Name,
                TaskId = task.Id,
                TaskTitle = task.Title
            });
        }

        return events.OrderBy(e => e.StartAt).ToList();
    }

    private IQueryable<CalendarEvent> DetailQuery() =>
        db.CalendarEvents.AsNoTracking()
            .Include(e => e.Project)
            .Include(e => e.Task);

    private async Task<CalendarEventDto> GetRequiredAsync(Guid id, CancellationToken ct)
    {
        var calendarEvent = await DetailQuery().FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new KeyNotFoundException($"Calendar event {id} was not found.");
        return calendarEvent.ToDto();
    }
}
