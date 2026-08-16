using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public class IconButton : Button
{
    private bool _hover;
    private bool _pressed;

    public IconButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseOverBackColor = Color.Transparent;
        FlatAppearance.MouseDownBackColor = Color.Transparent;
        UseVisualStyleBackColor = false;
        Size = new Size(UiMetrics.IconButtonSize, UiMetrics.IconButtonSize);
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Opaque, true);
        UpdateStyles();
        ThemeManager.Instance.ThemeChanged += (_, _) => Invalidate();
    }

    public string Icon { get; set; } = "add";
    public bool ShowBadge { get; set; }
    public bool IsAccent { get; set; }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        pevent.Graphics.Clear(Parent?.BackColor ?? ThemeManager.Instance.Current.TopBarBg);
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var c = ThemeManager.Instance.Current;
        var g = pevent.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var fill = Parent?.BackColor ?? c.TopBarBg;
        g.Clear(fill);
        var rect = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));

        Color bg, fg, border;
        if (IsAccent)
        {
            bg = _hover || _pressed ? c.AccentHover : c.Accent;
            fg = c.OnPrimary;
            border = Color.Transparent;
        }
        else
        {
            bg = _pressed ? c.SelectedBg : (_hover ? c.HoverBg : Color.Transparent);
            fg = _hover ? c.TextPrimary : c.TextSecondary;
            border = _hover ? c.Border : Color.Transparent;
        }

        if (bg.A > 0)
        {
            using var brush = new SolidBrush(bg);
            DrawingUtil.FillRounded(g, brush, rect, UiMetrics.RadiusSm);
        }
        if (border.A > 0)
        {
            using var pen = new Pen(border);
            DrawingUtil.DrawRounded(g, pen, rect, UiMetrics.RadiusSm);
        }

        UiIcons.Draw(g, Icon, new Rectangle(6, 6, Width - 12, Height - 12), fg);

        if (ShowBadge)
        {
            using var b = new SolidBrush(c.Error);
            g.FillEllipse(b, Width - 10, 4, 7, 7);
        }
    }
}
