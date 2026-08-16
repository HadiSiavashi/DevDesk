using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class IconGlyph : Control
{
    private string _icon = "check";
    private bool _hover;

    public IconGlyph()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        Size = new Size(UiMetrics.IconSize, UiMetrics.IconSize);
        BackColor = Color.Transparent;
        ThemeManager.Instance.ThemeChanged += (_, _) => Invalidate();
    }

    public string Icon
    {
        get => _icon;
        set { _icon = value; Invalidate(); }
    }

    public Color? IconColor { get; set; }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var c = ThemeManager.Instance.Current;
        var color = IconColor ?? (_hover && Cursor == Cursors.Hand ? c.TextPrimary : c.TextSecondary);
        UiIcons.Draw(e.Graphics, _icon, ClientRectangle, color);
    }
}
