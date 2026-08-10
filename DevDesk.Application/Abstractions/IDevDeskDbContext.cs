using DevDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevDesk.Application.Abstractions;

/// <summary>
/// Application-facing data access abstraction.
/// Persistence must implement this on DevDeskDbContext:
/// <c>public class DevDeskDbContext : DbContext, IDevDeskDbContext</c>
/// </summary>
public interface IDevDeskDbContext
{
    DbSet<WorkTask> Tasks { get; }
    DbSet<Project> Projects { get; }
    DbSet<TaskChecklistItem> TaskChecklistItems { get; }
    DbSet<Tag> Tags { get; }
    DbSet<TaskTag> TaskTags { get; }
    DbSet<NoteTag> NoteTags { get; }
    DbSet<FocusSession> FocusSessions { get; }
    DbSet<PomodoroSession> PomodoroSessions { get; }
    DbSet<DailyPlan> DailyPlans { get; }
    DbSet<DailyReview> DailyReviews { get; }
    DbSet<Note> Notes { get; }
    DbSet<Goal> Goals { get; }
    DbSet<Milestone> Milestones { get; }
    DbSet<Habit> Habits { get; }
    DbSet<HabitRecord> HabitRecords { get; }
    DbSet<Bookmark> Bookmarks { get; }
    DbSet<CodeSnippet> CodeSnippets { get; }
    DbSet<CalendarEvent> CalendarEvents { get; }
    DbSet<ProjectEnvironment> ProjectEnvironments { get; }
    DbSet<AppSetting> AppSettings { get; }
    DbSet<User> Users { get; }
    DbSet<Attachment> Attachments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
