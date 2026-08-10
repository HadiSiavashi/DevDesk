namespace DevDesk.Domain.Entities;

public class Attachment
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long Size { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? NoteId { get; set; }
    public DateTime CreatedAt { get; set; }

    public WorkTask? Task { get; set; }
    public Note? Note { get; set; }
}
