using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class SegmentedTabs : Control
{
    public event EventHandler<int>? SelectedIndexChanged;

    private string[] _items = [];
    private int _selected;
    private int _hover = -1;

    public SegmentedTabs()
    {
        Height = UiMetrics.ControlHeightCompact;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        ThemeManager.Instance.Attach(this, (_, _) => Invalidate());
        UiScale.Attach(this, (_, _) => { Height = UiMetrics.ControlHeightCompact; Invalidate(); });
        Cursor = Cursors.Hand;
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

    public bool UnderlineStyle { get; set; }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var i = Hit(e.X);
        if (i != _hover) { _hover = i; Invalidate(); }
        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e) { _hover = -1; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseClick(MouseEventArgs e)
    {
        var i = Hit(e.X);
        if (i >= 0) SelectedIndex = i;
        base.OnMouseClick(e);
    }

    private int Hit(int x)
    {
        if (_items.Length == 0) return -1;
        if (UnderlineStyle)
        {
            var pos = 0;
            for (var i = 0; i < _items.Length; i++)
            {
                var w = TextRenderer.MeasureText(_items[i], UiMetrics.SectionTitle).Width + UiMetrics.Space16;
                if (x >= pos && x < pos + w) return i;
                pos += w + 8;
            }
            return -1;
        }
        return Math.Clamp(x / Math.Max(1, Width / _items.Length), 0, _items.Length - 1);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var c = ThemeManager.Instance.Current;
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        if (UnderlineStyle)
        {
            var x = 0;
            for (var i = 0; i < _items.Length; i++)
            {
                var w = TextRenderer.MeasureText(_items[i], UiMetrics.SectionTitle).Width + UiMetrics.Space16;
                var fg = i == _selected ? c.TextPrimary : c.TextMuted;
                TextRenderer.DrawText(g, _items[i], UiMetrics.SectionTitle, new Rectangle(x, 0, w, Height - 3), fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                if (i == _selected)
                {
                    using var pen = new Pen(c.Accent, 2);
                    g.DrawLine(pen, x + 4, Height - 2, x + w - 4, Height - 2);
                }
                x += w + 8;
            }
            return;
        }

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var bg = new SolidBrush(c.Surface))
            DrawingUtil.FillRounded(g, bg, rect, UiMetrics.RadiusSm);
        using (var border = new Pen(c.Border))
            DrawingUtil.DrawRounded(g, border, rect, UiMetrics.RadiusSm);

        if (_items.Length == 0) return;
        var slot = Width / _items.Length;
        for (var i = 0; i < _items.Length; i++)
        {
            var ir = new Rectangle(i * slot + 2, 2, slot - 4, Height - 5);
            if (i == _selected)
            {
                using var sb = new SolidBrush(c.SelectedBg);
                DrawingUtil.FillRounded(g, sb, ir, 3);
                using var sp = new Pen(c.Accent);
                DrawingUtil.DrawRounded(g, sp, ir, 3);
            }
            var fg = i == _selected ? c.AccentSoft : c.TextMuted;
            TextRenderer.DrawText(g, _items[i], UiMetrics.Meta, ir, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}
