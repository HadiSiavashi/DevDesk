namespace DevDesk.Domain.Entities;

public class NoteTag
{
    public Guid NoteId { get; set; }
    public Guid TagId { get; set; }

    public Note Note { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
