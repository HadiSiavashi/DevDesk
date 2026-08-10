namespace DevDesk.Domain.Entities;

public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#3B82F6";
    public string? Icon { get; set; }
    public string? RepositoryUrl { get; set; }
    public string? LocalPath { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<WorkTask> Tasks { get; set; } = new List<WorkTask>();
    public ICollection<ProjectEnvironment> Environments { get; set; } = new List<ProjectEnvironment>();
    public ICollection<Note> Notes { get; set; } = new List<Note>();
    public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
    public ICollection<CodeSnippet> Snippets { get; set; } = new List<CodeSnippet>();
    public ICollection<FocusSession> FocusSessions { get; set; } = new List<FocusSession>();
    public ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();
}
