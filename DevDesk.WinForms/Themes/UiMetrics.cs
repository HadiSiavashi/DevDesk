namespace DevDesk.WinForms.Themes;

/// <summary>Centralized spacing, control heights, typography, and motion tokens.</summary>
public static class UiMetrics
{
    public static int Space4 => UiScale.Px(4);
    public static int Space8 => UiScale.Px(8);
    public static int Space12 => UiScale.Px(12);
    public static int Space16 => UiScale.Px(16);
    public static int Space20 => UiScale.Px(20);
    public static int Space24 => UiScale.Px(24);
    public static int Space32 => UiScale.Px(32);
    public static int Gutter => UiScale.Px(16);

    public static int ControlHeight => UiScale.Px(32);
    public static int ControlHeightCompact => UiScale.Px(28);
    public static int ButtonHeight => UiScale.Px(32);
    public static int InputHeight => UiScale.Px(32);
    public static int TaskRowHeight => UiScale.Px(48);
    public static int SidebarRowHeight => UiScale.Px(36);
    public static int TopBarHeight => UiScale.Px(48);
    public static int ToastWidth => UiScale.Px(320);
    public static int StatusBarHeight => UiScale.Px(32);

    public static int LineMeta => UiScale.Px(22);
    public static int LineBody => UiScale.Px(24);
    public static int LineTitle => UiScale.Px(32);
    public static int LinePage => UiScale.Px(40);
    public static int StatCardHeight => UiScale.Px(104);
    public static int ProgressHeight => UiScale.Px(8);

    public static int SidebarExpandedWidth => UiScale.Px(248);
    public static int SidebarCollapsedWidth => UiScale.Px(52);
    public static int SidebarAutoCollapseWidth => UiScale.Px(1000);
    public static int IconSize => UiScale.Px(18);
    public static int IconButtonSize => UiScale.Px(32);
    public static int ComboItemHeight => UiScale.Px(26);
    public static int SidebarHeaderHeight => UiScale.Px(64);
    public static int SidebarHeaderCollapsedHeight => UiScale.Px(48);
    public static int SettingsNavWidth => UiScale.Px(220);

    public static int DefaultWindowWidth => UiScale.Px(1280);
    public static int DefaultWindowHeight => UiScale.Px(800);
    public static int MinWindowWidth => UiScale.Px(960);
    public static int MinWindowHeight => UiScale.Px(640);

    public static int RadiusSm => UiScale.Px(4);
    public static int RadiusMd => UiScale.Px(6);
    public static int RadiusLg => UiScale.Px(8);

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
