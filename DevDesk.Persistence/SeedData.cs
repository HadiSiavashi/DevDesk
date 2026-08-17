using DevDesk.Domain.Entities;
using DevDesk.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DevDesk.Persistence;

public static class SeedData
{
    public static async Task SeedAsync(DevDeskDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.Users.AnyAsync(cancellationToken))
            return;

        var now = DateTime.UtcNow;

        var user = new User
        {
            Id = Guid.NewGuid(),
            DisplayName = "Developer",
            Email = "developer@devdesk.local",
            CreatedAt = now,
            UpdatedAt = now
        };

        var tags = new Dictionary<string, Tag>(StringComparer.OrdinalIgnoreCase)
        {
            ["Backend"] = CreateTag("Backend", "#2563EB"),
            ["Frontend"] = CreateTag("Frontend", "#DB2777"),
            ["Bug"] = CreateTag("Bug", "#DC2626"),
            ["Feature"] = CreateTag("Feature", "#059669"),
            ["DevOps"] = CreateTag("DevOps", "#D97706"),
            ["Urgent"] = CreateTag("Urgent", "#EA580C"),
            ["Learning"] = CreateTag("Learning", "#7C3AED")
        };

        var crm = CreateProject("CRM", "Customer relationship management system", "#2563EB", now);
        var apiGateway = CreateProject("API Gateway", "Internal API gateway and developer portal", "#059669", now);
        var personal = CreateProject("Personal", "Personal projects and learning", "#7C3AED", now);

        var authBug = CreateTask(
            "Fix authentication bug",
            "Investigate and fix login failures in CRM.",
            crm.Id,
            TaskPriority.High,
            WorkTaskStatus.Todo,
            now);

        var reviewApi = CreateTask(
            "Review API",
            "Review CRM API endpoints and contracts.",
            crm.Id,
            TaskPriority.Medium,
            WorkTaskStatus.Todo,
            now);

        var updateDocs = CreateTask(
            "Update documentation",
            "Refresh API Gateway developer documentation.",
            apiGateway.Id,
            TaskPriority.Medium,
            WorkTaskStatus.Todo,
            now);

        var deployProd = CreateTask(
            "Deploy production",
            "Deploy CRM release to production.",
            crm.Id,
            TaskPriority.Critical,
            WorkTaskStatus.Todo,
            now);

        var taskTags = new List<TaskTag>
        {
            new() { TaskId = authBug.Id, TagId = tags["Bug"].Id },
            new() { TaskId = deployProd.Id, TagId = tags["DevOps"].Id }
        };

        var bookmarks = new[]
        {
            CreateBookmark("Microsoft Docs", "https://learn.microsoft.com/", "Documentation", now),
            CreateBookmark("GitHub", "https://github.com/", "GitHub", now),
            CreateBookmark("Stack Overflow", "https://stackoverflow.com/", "Learning", now),
            CreateBookmark("Docker Docs", "https://docs.docker.com/", "DevOps", now)
        };

        var note = new Note
        {
            Id = Guid.NewGuid(),
            Title = "Getting started with DevDesk",
            Content = "Use DevDesk to track projects, focus sessions, habits, and daily plans in one place.",
            ProjectId = personal.Id,
            IsPinned = true,
            IsKnowledgeBase = true,
            KnowledgeCategory = "Onboarding",
            CreatedAt = now,
            UpdatedAt = now
        };

        var habit = new Habit
        {
            Id = Guid.NewGuid(),
            Name = "Daily coding",
            Description = "Write or review code every day.",
            Frequency = HabitFrequency.Daily,
            IsActive = true,
            CreatedAt = now
        };

        var goal = new Goal
        {
            Id = Guid.NewGuid(),
            Title = "Ship DevDesk MVP",
            Description = "Deliver a usable first version of DevDesk for daily developer workflow.",
            Category = GoalCategory.Projects,
            TargetDate = now.Date.AddMonths(2),
            Progress = 10,
            IsCompleted = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        var settings = new[]
        {
            new AppSetting { Key = "OnboardingCompleted", Value = "false" },
            new AppSetting { Key = "Culture", Value = "en-US" },
            new AppSetting { Key = "MinimizeToTray", Value = "false" },
            new AppSetting { Key = "AutoMigrate", Value = "true" },
            new AppSetting { Key = "SeedDemoData", Value = "true" },
            new AppSetting { Key = "DefaultFocusMinutes", Value = "25" },
            new AppSetting { Key = "PomodoroWorkMinutes", Value = "25" },
            new AppSetting { Key = "PomodoroBreakMinutes", Value = "5" }
        };

        context.Users.Add(user);
        context.Tags.AddRange(tags.Values);
        context.Projects.AddRange(crm, apiGateway, personal);
        context.Tasks.AddRange(authBug, reviewApi, updateDocs, deployProd);
        context.TaskTags.AddRange(taskTags);
        context.Bookmarks.AddRange(bookmarks);
        context.Notes.Add(note);
        context.Habits.Add(habit);
        context.Goals.Add(goal);
        context.AppSettings.AddRange(settings);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static Tag CreateTag(string name, string color) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Color = color
    };

    private static Project CreateProject(string name, string description, string color, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Description = description,
        Color = color,
        CreatedAt = now,
        UpdatedAt = now
    };

    private static WorkTask CreateTask(
        string title,
        string description,
        Guid projectId,
        TaskPriority priority,
        WorkTaskStatus status,
        DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Description = description,
        ProjectId = projectId,
        Priority = priority,
        Status = status,
        CreatedAt = now,
        UpdatedAt = now
    };

    private static Bookmark CreateBookmark(string title, string url, string category, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Url = url,
        Category = category,
        IsFavorite = true,
        CreatedAt = now
    };
}
