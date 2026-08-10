namespace DevDesk.Application.Dtos;

public sealed class AttachmentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long Size { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? NoteId { get; set; }
    public DateTime CreatedAt { get; set; }
}
