namespace DevDesk.Domain.Entities;

public class Note
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public bool IsPinned { get; set; }
    public bool IsKnowledgeBase { get; set; }
    public string? KnowledgeCategory { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Project? Project { get; set; }
    public ICollection<NoteTag> NoteTags { get; set; } = new List<NoteTag>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
