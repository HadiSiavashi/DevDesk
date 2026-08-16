using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DevDesk.WinForms.Themes;

internal static class DrawingUtil
{
    public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        var d = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        if (d <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void FillRounded(Graphics g, Brush brush, Rectangle bounds, int radius)
    {
        using var path = RoundedRect(bounds, radius);
        g.FillPath(brush, path);
    }

    public static void DrawRounded(Graphics g, Pen pen, Rectangle bounds, int radius)
    {
        using var path = RoundedRect(bounds, radius);
        g.DrawPath(pen, path);
    }

    public static void EnableDoubleBuffer(Control control)
    {
        typeof(Control).InvokeMember(
            "DoubleBuffered",
            System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null,
            control,
            [true]);
    }

    public static Color WithAlpha(Color color, int alpha) =>
        Color.FromArgb(Math.Clamp(alpha, 0, 255), color);

    public static Color Blend(Color overlay, Color baseColor)
    {
        var a = overlay.A / 255f;
        return Color.FromArgb(
            255,
            (int)(overlay.R * a + baseColor.R * (1 - a)),
            (int)(overlay.G * a + baseColor.G * (1 - a)),
            (int)(overlay.B * a + baseColor.B * (1 - a)));
    }

    private static readonly ConditionalWeakTable<Control, object> ChromeWired = new();

    public static void ApplyWindowChrome(Control control)
    {
        void Apply()
        {
            if (!control.IsHandleCreated) return;
            SetWindowTheme(control.Handle, ThemeManager.Instance.IsDark ? "DarkMode_Explorer" : "Explorer", null);
            if (control is ComboBox cb)
                ApplyComboDropDownTheme(cb);
        }

        if (!ChromeWired.TryGetValue(control, out _))
        {
            ChromeWired.Add(control, new object());
            control.HandleCreated += (_, _) => Apply();
            if (control is ComboBox combo)
                combo.DropDown += (_, _) => ApplyComboDropDownTheme(combo);
        }

        if (control.IsHandleCreated) Apply();
    }

    public static void ApplyComboDropDownTheme(ComboBox combo)
    {
        if (!combo.IsHandleCreated) return;
        var info = new ComboBoxInfo { cbSize = Marshal.SizeOf<ComboBoxInfo>() };
        if (!GetComboBoxInfo(combo.Handle, ref info)) return;
        var theme = ThemeManager.Instance.IsDark ? "DarkMode_Explorer" : "Explorer";
        if (info.hwndList != IntPtr.Zero)
            SetWindowTheme(info.hwndList, theme, null);
        if (info.hwndItem != IntPtr.Zero)
            SetWindowTheme(info.hwndItem, theme, null);
    }

    [DllImport("gdi32.dll")]
    public static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, [In] ref uint pcFonts);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string? pszSubIdList);

    [DllImport("user32.dll")]
    private static extern bool GetComboBoxInfo(IntPtr hwndCombo, ref ComboBoxInfo pcbi);

    [StructLayout(LayoutKind.Sequential)]
    private struct ComboBoxInfo
    {
        public int cbSize;
        public RECT rcItem;
        public RECT rcButton;
        public int stateButton;
        public IntPtr hwndCombo;
        public IntPtr hwndItem;
        public IntPtr hwndList;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }
}
