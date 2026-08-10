namespace DevDesk.Application.Dtos;

public sealed class ProjectDto
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
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public double ProgressPercent { get; set; }
    public IReadOnlyList<MilestoneDto> Milestones { get; set; } = [];
    public IReadOnlyList<EnvironmentDto> Environments { get; set; } = [];
}

public sealed class ProjectListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#3B82F6";
    public string? Icon { get; set; }
    public bool IsArchived { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public double ProgressPercent { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#3B82F6";
    public string? Icon { get; set; }
    public string? RepositoryUrl { get; set; }
    public string? LocalPath { get; set; }
}

public sealed class UpdateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#3B82F6";
    public string? Icon { get; set; }
    public string? RepositoryUrl { get; set; }
    public string? LocalPath { get; set; }
}
