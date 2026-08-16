namespace DevDesk.WinForms.Themes;

/// <summary>Centralized spacing, control heights, typography, and motion tokens.</summary>
public static class UiMetrics
{
    public const int Space4 = 4;
    public const int Space8 = 8;
    public const int Space12 = 12;
    public const int Space16 = 16;
    public const int Space20 = 20;
    public const int Space24 = 24;
    public const int Space32 = 32;
    public const int Gutter = 16;

    public const int ControlHeight = 32;
    public const int ControlHeightCompact = 28;
    public const int ButtonHeight = 32;
    public const int InputHeight = 32;
    public const int TaskRowHeight = 48;
    public const int SidebarRowHeight = 36;
    public const int TopBarHeight = 48;
    public const int ToastWidth = 320;
    public const int StatusBarHeight = 32;

    public const int SidebarExpandedWidth = 200;
    public const int SidebarCollapsedWidth = 52;
    public const int SidebarAutoCollapseWidth = 1000;
    public const int IconSize = 18;
    public const int IconButtonSize = 32;

    public const int DefaultWindowWidth = 1280;
    public const int DefaultWindowHeight = 800;
    public const int MinWindowWidth = 960;
    public const int MinWindowHeight = 640;

    public const int RadiusSm = 4;
    public const int RadiusMd = 6;
    public const int RadiusLg = 8;

    public const int MicroMs = 140;
    public const int ModalMs = 200;
    public const int ListMs = 180;
    public const int ToastMs = 2800;

    public static Font PageTitle => UiFonts.PageTitle;
    public static Font SectionTitle => UiFonts.SectionTitle;
    public static Font TaskTitle => UiFonts.BodySemi;
    public static Font Body => UiFonts.Body;
    public static Font Meta => UiFonts.Meta;
    public static Font Caption => UiFonts.Caption;
    public static Font Timer => UiFonts.TimerGiant;
    public static Font TimerReady => UiFonts.TimerReady;
    public static Font Mono => UiFonts.Mono;
    public static Font MonoTimer => UiFonts.MonoTimer;
    public static Font StatValue => UiFonts.StatValue;
    public static Font Kbd => UiFonts.Kbd;
}
