using DevDesk.Domain.Enums;

namespace DevDesk.Domain.Entities;

public class ProjectEnvironment
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public EnvironmentType EnvironmentType { get; set; }
    public string? BaseUrl { get; set; }
    public string? DatabaseServer { get; set; }
    public string? DatabaseName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Project Project { get; set; } = null!;
}
