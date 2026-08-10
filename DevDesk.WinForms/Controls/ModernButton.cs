using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public class ModernButton : Button
{
    private bool _hover;

    public ModernButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        Height = UiMetrics.ButtonHeight;
        Padding = new Padding(12, 0, 12, 0);
        Font = UiMetrics.Body;
        ThemeManager.Instance.ThemeChanged += (_, _) => Invalidate();
    }

    public bool IsPrimary { get; set; } = true;

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var c = ThemeManager.Instance.Current;
        var bg = IsPrimary ? (_hover ? c.AccentHover : c.Accent) : (_hover ? c.HoverBg : c.Surface);
        var fg = IsPrimary ? Color.White : c.TextPrimary;
        using var brush = new SolidBrush(bg);
        pevent.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RoundedRect(rect, 6);
        pevent.Graphics.FillPath(brush, path);
        TextRenderer.DrawText(pevent.Graphics, Text, Font, ClientRectangle, fg,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        var d = radius * 2;
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
