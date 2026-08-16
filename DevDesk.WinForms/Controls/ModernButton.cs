using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public enum ButtonVariant { Primary, Outline, Ghost }

public class ModernButton : Button
{
    private bool _hover;
    private bool _pressed;
    private ButtonVariant _variant = ButtonVariant.Primary;

    public ModernButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseOverBackColor = Color.Transparent;
        FlatAppearance.MouseDownBackColor = Color.Transparent;
        UseVisualStyleBackColor = false;
        Cursor = Cursors.Hand;
        Height = UiMetrics.ButtonHeight;
        Padding = new Padding(12, 0, 12, 0);
        Font = UiMetrics.Body;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Opaque, true);
        UpdateStyles();
        ThemeManager.Instance.ThemeChanged += (_, _) => Invalidate();
    }

    public bool IsPrimary
    {
        get => Variant == ButtonVariant.Primary;
        set => Variant = value ? ButtonVariant.Primary : ButtonVariant.Outline;
    }

    public ButtonVariant Variant
    {
        get => _variant;
        set { _variant = value; Invalidate(); }
    }

    public string? Icon { get; set; }
    public string? Shortcut { get; set; }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        var fill = Parent?.BackColor ?? ThemeManager.Instance.Current.Background;
        pevent.Graphics.Clear(fill);
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var c = ThemeManager.Instance.Current;
        var g = pevent.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var fill = Parent?.BackColor ?? c.Background;
        g.Clear(fill);
        var rect = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));

        Color bg, fg, border;
        switch (Variant)
        {
            case ButtonVariant.Ghost:
                bg = _pressed ? c.SelectedBg : (_hover ? c.HoverBg : fill);
                fg = _hover ? c.TextPrimary : c.TextSecondary;
                border = Color.Transparent;
                break;
            case ButtonVariant.Outline:
                bg = _pressed ? c.SelectedBg : (_hover ? c.HoverBg : c.Surface);
                fg = c.TextPrimary;
                border = _hover ? c.Accent : c.Border;
                break;
            default:
                bg = _pressed ? c.AccentHover : (_hover ? DrawingUtil.Blend(DrawingUtil.WithAlpha(Color.White, 28), c.Accent) : c.Accent);
                fg = c.OnPrimary;
                border = Color.Transparent;
                break;
        }

        using (var brush = new SolidBrush(bg))
            DrawingUtil.FillRounded(g, brush, rect, UiMetrics.RadiusSm);
        if (border.A > 0)
        {
            using var pen = new Pen(border);
            DrawingUtil.DrawRounded(g, pen, rect, UiMetrics.RadiusSm);
        }

        var text = Text ?? "";
        var iconPad = string.IsNullOrEmpty(Icon) ? 0 : 20;
        var kbdW = string.IsNullOrEmpty(Shortcut) ? 0 : 28;
        var content = new Rectangle(4 + iconPad, 0, Math.Max(8, Width - 8 - iconPad - kbdW), Height);

        if (!string.IsNullOrEmpty(Icon))
            UiIcons.Draw(g, Icon, new Rectangle(8, (Height - 16) / 2, 16, 16), fg);

        TextRenderer.DrawText(g, text, Font ?? UiMetrics.Body, content, fg,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

        if (!string.IsNullOrEmpty(Shortcut))
        {
            var kr = new Rectangle(Width - 30, (Height - 16) / 2, 22, 16);
            using var kb = new SolidBrush(DrawingUtil.WithAlpha(Color.Black, 40));
            DrawingUtil.FillRounded(g, kb, kr, 3);
            TextRenderer.DrawText(g, Shortcut, UiMetrics.Kbd, kr, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }
}
