using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

/// <summary>Single-select inner nav used by Settings. Owner-drawn so labels never double-paint.</summary>
public sealed class SettingsNavList : Control
{
    public event EventHandler<int>? SelectedIndexChanged;

    private string[] _items = [];
    private int _selected;
    private int _hover = -1;

    public SettingsNavList()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Opaque, true);
        Cursor = Cursors.Hand;
        ThemeManager.Instance.ThemeChanged += (_, _) => Invalidate();
    }

    public string[] Items
    {
        get => _items;
        set { _items = value; Invalidate(); }
    }

    public int SelectedIndex
    {
        get => _selected;
        set
        {
            if (_selected == value) return;
            _selected = value;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, _selected);
        }
    }

    private const int RowHeight = 36;

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var i = Hit(e.Y);
        if (i != _hover) { _hover = i; Invalidate(); }
        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = -1;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        var i = Hit(e.Y);
        if (i >= 0) SelectedIndex = i;
        base.OnMouseClick(e);
    }

    private int Hit(int y)
    {
        var i = y / RowHeight;
        return i >= 0 && i < _items.Length ? i : -1;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var c = ThemeManager.Instance.Current;
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? c.Background);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        for (var i = 0; i < _items.Length; i++)
        {
            var r = new Rectangle(4, i * RowHeight + 2, Width - 8, RowHeight - 4);
            if (i == _selected)
            {
                using var bg = new SolidBrush(c.SelectedBg);
                DrawingUtil.FillRounded(g, bg, r, UiMetrics.RadiusSm);
                using var accent = new Pen(c.Accent, 2);
                g.DrawLine(accent, r.Left + 1, r.Top + 6, r.Left + 1, r.Bottom - 6);
            }
            else if (i == _hover)
            {
                using var bg = new SolidBrush(c.HoverBg);
                DrawingUtil.FillRounded(g, bg, r, UiMetrics.RadiusSm);
            }

            TextRenderer.DrawText(g, _items[i], UiMetrics.Body, new Rectangle(r.X + 14, r.Y, r.Width - 20, r.Height),
                i == _selected ? c.TextPrimary : c.TextSecondary,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }
    }
}
