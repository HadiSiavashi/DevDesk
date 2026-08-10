namespace DevDesk.Application.Options;

public sealed class PomodoroOptions
{
    public const string SectionName = "Pomodoro";

    public int WorkMinutes { get; set; } = 25;
    public int ShortBreakMinutes { get; set; } = 5;
    public int LongBreakMinutes { get; set; } = 15;
    public int SessionsUntilLongBreak { get; set; } = 4;
}
