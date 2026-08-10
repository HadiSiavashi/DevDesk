namespace DevDesk.Domain.Entities;

public class HabitRecord
{
    public Guid Id { get; set; }
    public Guid HabitId { get; set; }
    public DateOnly Date { get; set; }
    public bool IsCompleted { get; set; }

    public Habit Habit { get; set; } = null!;
}
