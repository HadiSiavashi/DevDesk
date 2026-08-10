namespace DevDesk.Application.Options;

public sealed class AppOptions
{
    public const string SectionName = "App";

    public string DisplayName { get; set; } = "DevDesk";
    public string DefaultUserName { get; set; } = "Developer";
    public string Culture { get; set; } = "en-US";
    public int TargetFocusMinutesPerDay { get; set; } = 120;
}
