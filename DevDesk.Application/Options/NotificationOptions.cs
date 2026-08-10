namespace DevDesk.Application.Options;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    public bool Enabled { get; set; } = true;
    public bool RemindOverdueTasks { get; set; } = true;
    public bool RemindDailyPlan { get; set; } = true;
    public bool RemindDailyReview { get; set; } = true;
    public int OverdueCheckIntervalMinutes { get; set; } = 30;

    /// <summary>Minutes before due time to fire TaskDueSoon.</summary>
    public int TaskDueSoonMinutes { get; set; } = 60;

    public bool TaskDueSoon { get; set; } = true;
    public bool OverdueTask { get; set; } = true;
    public bool PomodoroFinished { get; set; } = true;
    public bool FocusSessionFinished { get; set; } = true;
    public bool UpcomingCalendarEvent { get; set; } = true;
}
