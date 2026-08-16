using DevDesk.Domain.Enums;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class PriorityBadge : Control
{
    private TaskPriority _priority;

    public PriorityBadge()
    {
        Size = new Size(48, 16);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        ThemeManager.Instance.ThemeChanged += (_, _) => Invalidate();
    }

    public TaskPriority Priority
    {
        get => _priority;
        set { _priority = value; Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var c = ThemeManager.Instance.Current;
        var (fg, label) = _priority switch
        {
            TaskPriority.Critical => (c.Error, "CRIT"),
            TaskPriority.High => (c.Error, "HIGH"),
            TaskPriority.Medium => (c.Tertiary, "MED"),
            _ => (c.TextMuted, "LOW")
        };
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var bg = new SolidBrush(DrawingUtil.WithAlpha(fg, 40));
        DrawingUtil.FillRounded(g, bg, new Rectangle(0, 0, Width - 1, Height - 1), 3);
        TextRenderer.DrawText(g, label, UiMetrics.Kbd, ClientRectangle, fg,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}
