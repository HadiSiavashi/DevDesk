using DevDesk.Application.Abstractions;
using DevDesk.Application.Dtos;
using DevDesk.Application.Events;
using DevDesk.Application.Interfaces;
using DevDesk.Application.Mapping;
using DevDesk.Application.Options;
using DevDesk.Domain.Entities;
using DevDesk.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevDesk.Application.Services;

public sealed class FocusService(
    IDevDeskDbContext db,
    IClock clock,
    IOptions<PomodoroOptions> pomodoroOptions,
    IOptions<FocusOptions> focusOptions,
    IAppEventBus events) : IFocusService
{
    private readonly PomodoroOptions _pomodoro = pomodoroOptions.Value;
    private readonly FocusOptions _focus = focusOptions.Value;

    public async Task<FocusSessionDto> StartAsync(StartFocusRequest request, CancellationToken ct = default)
    {
        await EnsureNoActiveSessionAsync(ct);

        var now = clock.UtcNow;
        var session = new FocusSession
        {
            Id = Guid.NewGuid(),
            TaskId = request.TaskId,
            ProjectId = request.ProjectId,
            StartedAt = now,
            SessionType = request.SessionType,
            Notes = request.Notes
        };

        if (_focus.AutoSetTaskInProgressOnStart && request.TaskId is Guid taskId)
        {
            var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
            task?.StartFocus(now);
            if (session.ProjectId is null && task?.ProjectId is Guid projectId)
                session.ProjectId = projectId;
        }

        db.FocusSessions.Add(session);
        await db.SaveChangesAsync(ct);
        var dto = await GetRequiredAsync(session.Id, ct);
        events.Publish(AppEventKind.FocusStarted, dto.Id, dto);
        return dto;
    }

    public async Task<FocusSessionDto> PauseAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await GetActiveEntityAsync(sessionId, ct);
        session.Pause(clock.UtcNow);
        await db.SaveChangesAsync(ct);
        var dto = await GetRequiredAsync(sessionId, ct);
        events.Publish(AppEventKind.FocusPaused, dto.Id, dto);
        return dto;
    }

    public async Task<FocusSessionDto> ResumeAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await GetActiveEntityAsync(sessionId, ct);
        session.Resume(clock.UtcNow);
        await db.SaveChangesAsync(ct);
        var dto = await GetRequiredAsync(sessionId, ct);
        events.Publish(AppEventKind.FocusResumed, dto.Id, dto);
        return dto;
    }

    public async Task<FocusSessionDto> StopAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await db.FocusSessions
            .Include(s => s.Task)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct)
            ?? throw new KeyNotFoundException($"Focus session {sessionId} was not found.");

        if (!session.IsActive)
            return await GetRequiredAsync(sessionId, ct);

        var now = clock.UtcNow;
        session.Stop(now);

        if (session.Task is not null && session.DurationMinutes > 0)
            session.Task.AddActualMinutes(session.DurationMinutes, now);

        await db.SaveChangesAsync(ct);
        var dto = await GetRequiredAsync(sessionId, ct);
        events.Publish(AppEventKind.FocusStopped, dto.Id, dto);
        return dto;
    }

    public async Task<FocusSessionDto?> GetActiveAsync(CancellationToken ct = default)
    {
        var session = await DetailQuery().FirstOrDefaultAsync(s => s.EndedAt == null, ct);
        return session?.ToDto(clock.UtcNow);
    }

    public async Task<FocusSessionDto> StartPomodoroAsync(StartPomodoroRequest request, CancellationToken ct = default)
    {
        await EnsureNoActiveSessionAsync(ct);

        var now = clock.UtcNow;
        var work = request.WorkMinutes ?? _pomodoro.WorkMinutes;
        var brk = request.BreakMinutes ?? _pomodoro.ShortBreakMinutes;

        var session = new FocusSession
        {
            Id = Guid.NewGuid(),
            TaskId = request.TaskId,
            ProjectId = request.ProjectId,
            StartedAt = now,
            SessionType = FocusSessionType.Pomodoro,
            Notes = request.Notes
        };

        if (_focus.AutoSetTaskInProgressOnStart && request.TaskId is Guid taskId)
        {
            var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
            task?.StartFocus(now);
            if (session.ProjectId is null && task?.ProjectId is Guid projectId)
                session.ProjectId = projectId;
        }

        var completedCount = await db.PomodoroSessions.CountAsync(p => p.Completed, ct);
        var pomodoro = new PomodoroSession
        {
            Id = Guid.NewGuid(),
            FocusSessionId = session.Id,
            WorkDurationMinutes = work,
            BreakDurationMinutes = brk,
            StartedAt = now,
            SessionNumber = completedCount + 1,
            IsBreak = false
        };

        session.PomodoroSession = pomodoro;
        db.FocusSessions.Add(session);
        db.PomodoroSessions.Add(pomodoro);
        await db.SaveChangesAsync(ct);
        var dto = await GetRequiredAsync(session.Id, ct);
        events.Publish(AppEventKind.FocusStarted, dto.Id, dto);
        return dto;
    }

    public async Task<FocusSessionDto> CompletePomodoroAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await db.FocusSessions
            .Include(s => s.PomodoroSession)
            .Include(s => s.Task)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct)
            ?? throw new KeyNotFoundException($"Focus session {sessionId} was not found.");

        if (session.PomodoroSession?.Completed == true && !session.IsActive)
            return await GetRequiredAsync(sessionId, ct);

        var now = clock.UtcNow;
        var wasActive = session.IsActive;
        if (wasActive)
            session.Stop(now);

        if (session.PomodoroSession is not null)
        {
            session.PomodoroSession.Completed = true;
            session.PomodoroSession.EndedAt ??= now;
        }

        // Credit ActualMinutes only when this call stops an active session (StopAsync path).
        // Avoid double-counting when Stop was already called separately.
        if (wasActive && session.Task is not null && session.DurationMinutes > 0)
            session.Task.AddActualMinutes(session.DurationMinutes, now);

        await db.SaveChangesAsync(ct);
        var dto = await GetRequiredAsync(sessionId, ct);
        if (wasActive)
            events.Publish(AppEventKind.FocusStopped, dto.Id, dto);
        return dto;
    }

    public async Task<FocusSessionDto?> RecoverActiveOnStartupAsync(CancellationToken ct = default)
    {
        var active = await db.FocusSessions
            .Include(s => s.Task)
            .FirstOrDefaultAsync(s => s.EndedAt == null, ct);

        if (active is null)
            return null;

        if (!_focus.RecoverActiveSessionOnStartup)
        {
            // Orphaned active session when recovery is disabled — stop safely so Start isn't blocked.
            var now = clock.UtcNow;
            active.Stop(now);
            if (active.Task is not null && active.DurationMinutes > 0)
                active.Task.AddActualMinutes(active.DurationMinutes, now);
            await db.SaveChangesAsync(ct);
            return null;
        }

        return await GetRequiredAsync(active.Id, ct);
    }

    public async Task<IReadOnlyList<FocusSessionDto>> GetHistoryAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default)
    {
        var start = from ?? clock.Today.AddDays(-14);
        var end = to ?? clock.Today;
        var fromDt = start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDt = end.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Utc);
        var now = clock.UtcNow;

        var items = await DetailQuery()
            .Where(s => s.StartedAt >= fromDt && s.StartedAt <= toDt)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(ct);

        return items.Select(s => s.ToDto(now)).ToList();
    }

    private async Task EnsureNoActiveSessionAsync(CancellationToken ct)
    {
        var active = await db.FocusSessions.AnyAsync(s => s.EndedAt == null, ct);
        if (active)
            throw new InvalidOperationException("An active focus session already exists. Stop or complete it first.");
    }

    private async Task<FocusSession> GetActiveEntityAsync(Guid sessionId, CancellationToken ct)
    {
        var session = await db.FocusSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct)
            ?? throw new KeyNotFoundException($"Focus session {sessionId} was not found.");
        if (!session.IsActive)
            throw new InvalidOperationException("Focus session is not active.");
        return session;
    }

    private IQueryable<FocusSession> DetailQuery() =>
        db.FocusSessions.AsNoTracking()
            .Include(s => s.Task)
            .Include(s => s.Project)
            .Include(s => s.PomodoroSession);

    private async Task<FocusSessionDto> GetRequiredAsync(Guid id, CancellationToken ct)
    {
        var session = await DetailQuery().FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new KeyNotFoundException($"Focus session {id} was not found.");
        return session.ToDto(clock.UtcNow);
    }
}
