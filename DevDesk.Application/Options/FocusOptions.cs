namespace DevDesk.Application.Options;

public sealed class FocusOptions
{
    public const string SectionName = "Focus";

    public int DefaultSessionMinutes { get; set; } = 60;
    public bool AutoSetTaskInProgressOnStart { get; set; } = true;
    public bool RecoverActiveSessionOnStartup { get; set; } = true;
}
