using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public class CardPanel : Panel
{
    public CardPanel()
    {
        Tag = "no-theme";
        Padding = new Padding(UiMetrics.Space16);
        DrawingUtil.EnableDoubleBuffer(this);
        ThemeManager.Instance.ThemeChanged += (_, _) => ApplyTheme();
        ApplyTheme();
    }

    public int CornerRadius { get; set; } = UiMetrics.RadiusMd;
    public bool AccentLeft { get; set; }
    public Color? AccentColor { get; set; }

    private void ApplyTheme()
    {
        BackColor = ThemeManager.Instance.Current.Surface;
        ForeColor = ThemeManager.Instance.Current.TextPrimary;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var c = ThemeManager.Instance.Current;
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var bg = new SolidBrush(c.Surface))
            DrawingUtil.FillRounded(g, bg, rect, CornerRadius);
        using (var pen = new Pen(c.Border))
            DrawingUtil.DrawRounded(g, pen, rect, CornerRadius);
        if (AccentLeft)
        {
            using var accent = new SolidBrush(AccentColor ?? c.Tertiary);
            g.FillRectangle(accent, 0, 8, 2, Height - 16);
        }
        base.OnPaint(e);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? ThemeManager.Instance.Current.Background);
    }
}
