namespace DevDesk.Domain.Entities;

public class Tag
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#64748B";

    public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
    public ICollection<NoteTag> NoteTags { get; set; } = new List<NoteTag>();
}
