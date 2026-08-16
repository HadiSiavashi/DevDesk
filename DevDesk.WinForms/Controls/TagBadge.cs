using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class TagBadge : Control
{
    private string _name = "";

    public TagBadge()
    {
        Height = 18;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        ThemeManager.Instance.ThemeChanged += (_, _) => Invalidate();
    }

    public void SetTag(string name, string? colorHex = null)
    {
        _name = name;
        Width = TextRenderer.MeasureText(_name, UiMetrics.Meta).Width + 12;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var c = ThemeManager.Instance.Current;
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var bg = new SolidBrush(c.SurfaceAlt);
        DrawingUtil.FillRounded(g, bg, new Rectangle(0, 0, Width - 1, Height - 1), 3);
        TextRenderer.DrawText(g, _name, UiMetrics.Meta, ClientRectangle, c.TextMuted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}
