using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class ProgressBarControl : Control
{
    private float _value;

    public ProgressBarControl()
    {
        Height = 6;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        ThemeManager.Instance.ThemeChanged += (_, _) => Invalidate();
    }

    public float Value
    {
        get => _value;
        set { _value = Math.Clamp(value, 0, 1); Invalidate(); }
    }

    public Color? FillColor { get; set; }

    protected override void OnPaint(PaintEventArgs e)
    {
        var c = ThemeManager.Instance.Current;
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var track = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var bg = new SolidBrush(c.InputBg))
            DrawingUtil.FillRounded(g, bg, track, Height / 2);
        using (var pen = new Pen(c.Border))
            DrawingUtil.DrawRounded(g, pen, track, Height / 2);
        if (_value > 0)
        {
            var w = Math.Max(Height, (int)((Width - 1) * _value));
            var fill = new Rectangle(0, 0, w, Height - 1);
            using var fb = new SolidBrush(FillColor ?? c.Accent);
            DrawingUtil.FillRounded(g, fb, fill, Height / 2);
        }
    }
}
