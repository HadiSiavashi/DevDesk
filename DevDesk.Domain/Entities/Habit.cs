using DevDesk.Domain.Enums;

namespace DevDesk.Domain.Entities;

public class Habit
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public HabitFrequency Frequency { get; set; } = HabitFrequency.Daily;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public ICollection<HabitRecord> Records { get; set; } = new List<HabitRecord>();
}
