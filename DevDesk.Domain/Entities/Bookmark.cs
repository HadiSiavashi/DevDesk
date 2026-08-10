namespace DevDesk.Domain.Entities;

public class Bookmark
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "Tools";
    public Guid? ProjectId { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime CreatedAt { get; set; }

    public Project? Project { get; set; }
}
