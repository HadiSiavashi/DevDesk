using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class StatusBadge : Control
{
    private string _text = "";
    private Color _fg;
    private Color _bg;

    public StatusBadge()
    {
        Size = new Size(64, 18);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        ThemeManager.Instance.ThemeChanged += (_, _) => Invalidate();
    }

    public void Set(string text, Color? foreground = null, Color? background = null)
    {
        _text = text;
        var c = ThemeManager.Instance.Current;
        _fg = foreground ?? c.TextMuted;
        _bg = background ?? c.SurfaceAlt;
        Width = TextRenderer.MeasureText(_text, UiMetrics.Kbd).Width + 10;
        Invalidate();
    }

    public DevDesk.Domain.Enums.WorkTaskStatus Status
    {
        set
        {
            var c = ThemeManager.Instance.Current;
            Set(value.ToString(), value == DevDesk.Domain.Enums.WorkTaskStatus.Done ? c.Success : c.TextMuted, c.SurfaceAlt);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var b = new SolidBrush(_bg);
        DrawingUtil.FillRounded(g, b, new Rectangle(0, 0, Width - 1, Height - 1), 3);
        TextRenderer.DrawText(g, _text, UiMetrics.Kbd, ClientRectangle, _fg,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}
