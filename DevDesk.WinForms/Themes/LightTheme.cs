namespace DevDesk.WinForms.Themes;

public static class LightTheme
{
    public static AppColors Create() => new(
        Background: Color.FromArgb(0xE8, 0xEC, 0xF1),
        Surface: Color.FromArgb(0xF4, 0xF6, 0xF9),
        SurfaceAlt: Color.FromArgb(0xDC, 0xE2, 0xEA),
        Border: Color.FromArgb(0xC4, 0xCD, 0xD8),
        TextPrimary: Color.FromArgb(0x1A, 0x22, 0x2E),
        TextSecondary: Color.FromArgb(0x4A, 0x56, 0x66),
        TextMuted: Color.FromArgb(0x6B, 0x78, 0x88),
        Accent: Color.FromArgb(0x3B, 0x6F, 0xE0),
        AccentHover: Color.FromArgb(0x2E, 0x5C, 0xC4),
        Success: Color.FromArgb(0x2A, 0xA8, 0x5A),
        Warning: Color.FromArgb(0xD4, 0x9A, 0x1A),
        Error: Color.FromArgb(0xD4, 0x3F, 0x52),
        SidebarBg: Color.FromArgb(0xDC, 0xE2, 0xEA),
        TopBarBg: Color.FromArgb(0xF4, 0xF6, 0xF9),
        InputBg: Color.White,
        HoverBg: Color.FromArgb(0xDC, 0xE2, 0xEA),
        SelectedBg: Color.FromArgb(0xC8, 0xD8, 0xF0));
}
