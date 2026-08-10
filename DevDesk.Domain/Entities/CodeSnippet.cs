namespace DevDesk.Domain.Entities;

public class CodeSnippet
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Language { get; set; } = "C#";
    public string Code { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Project? Project { get; set; }
}
