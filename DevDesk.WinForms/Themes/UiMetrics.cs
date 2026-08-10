namespace DevDesk.WinForms.Themes;

/// <summary>Centralized spacing, control heights, typography, and motion tokens.</summary>
public static class UiMetrics
{
    // Spacing scale
    public const int Space4 = 4;
    public const int Space8 = 8;
    public const int Space12 = 12;
    public const int Space16 = 16;
    public const int Space20 = 20;
    public const int Space24 = 24;
    public const int Space32 = 32;

    // Control heights
    public const int ControlHeight = 32;
    public const int ControlHeightCompact = 28;
    public const int ButtonHeight = 32;
    public const int InputHeight = 32;
    public const int TaskRowHeight = 52;
    public const int SidebarRowHeight = 36;
    public const int TopBarHeight = 40;
    public const int ToastHeight = 36;

    // Sidebar / shell
    public const int SidebarExpandedWidth = 200;
    public const int SidebarCollapsedWidth = 52;
    public const int SidebarAutoCollapseWidth = 1000;
    public const int IconSize = 16;

    // Window
    public const int DefaultWindowWidth = 1180;
    public const int DefaultWindowHeight = 740;
    public const int MinWindowWidth = 960;
    public const int MinWindowHeight = 640;

    // Radius
    public const int RadiusSm = 4;
    public const int RadiusMd = 6;
    public const int RadiusLg = 8;

    // Motion (ms)
    public const int MicroMs = 140;
    public const int ModalMs = 200;
    public const int ListMs = 180;
    public const int ToastMs = 2200;

    // Typography
    public static Font PageTitle => new("Segoe UI Semibold", 14F);
    public static Font SectionTitle => new("Segoe UI Semibold", 11F);
    public static Font TaskTitle => new("Segoe UI Semibold", 9.5F);
    public static Font Body => new("Segoe UI", 9F);
    public static Font Meta => new("Segoe UI", 8F);
    public static Font Caption => new("Segoe UI", 7.5F);
    public static Font Timer => new("Consolas", 36F, FontStyle.Bold);
    public static Font TimerReady => new("Consolas", 28F, FontStyle.Bold);
}
