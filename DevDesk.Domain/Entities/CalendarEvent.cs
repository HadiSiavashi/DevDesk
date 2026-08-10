using DevDesk.Domain.Enums;

namespace DevDesk.Domain.Entities;

public class CalendarEvent
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public CalendarEventType EventType { get; set; } = CalendarEventType.Other;
    public Guid? ProjectId { get; set; }
    public Guid? TaskId { get; set; }

    public Project? Project { get; set; }
    public WorkTask? Task { get; set; }
}
