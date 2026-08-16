using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class AppSidebar : Panel
{
    public event EventHandler<string>? NavigateRequested;

    private readonly ToolTip _tip = new();
    private readonly List<NavItem> _items = [];
    private string? _active = "dashboard";
    private bool _collapsed;
    private int _hover = -1;

    private static readonly (string Key, string LabelKey, string Icon)[] Destinations =
    [
        ("dashboard", "nav.dashboard", "dashboard"),
        ("myday", "nav.myday", "sunny"),
        ("tasks", "nav.tasks", "check_circle"),
        ("focus", "nav.focus", "timer"),
        ("projects", "nav.projects", "account_tree"),
        ("calendar", "nav.calendar", "calendar_today"),
        ("notes", "nav.notes", "description"),
        ("goals", "nav.goals", "emoji_events"),
        ("habits", "nav.habits", "repeat"),
        ("snippets", "nav.snippets", "code"),
        ("bookmarks", "nav.bookmarks", "bookmark"),
        ("environments", "nav.environments", "dns"),
        ("knowledge", "nav.knowledge", "menu_book"),
        ("analytics", "nav.analytics", "analytics"),
        ("dailyplan", "nav.dailyplan", "event_note"),
        ("dailyreview", "nav.dailyreview", "rate_review")
    ];

    public AppSidebar()
    {
        Tag = "no-theme";
        Dock = DockStyle.Left;
        Width = UiMetrics.SidebarExpandedWidth;
        DrawingUtil.EnableDoubleBuffer(this);
        AutoScroll = true;
        Cursor = Cursors.Hand;
        ThemeManager.Instance.ThemeChanged += (_, _) => Invalidate();
        LocalizationService.Instance.LanguageChanged += (_, _) =>
        {
            Dock = LocalizationService.Instance.IsRtl ? DockStyle.Right : DockStyle.Left;
            Invalidate();
        };
        MouseMove += OnMove;
        MouseLeave += (_, _) => { _hover = -1; Invalidate(); };
        MouseClick += OnClick;
        ApplyTheme();
    }

    public bool Collapsed
    {
        get => _collapsed;
        set
        {
            _collapsed = value;
            Width = value ? UiMetrics.SidebarCollapsedWidth : UiMetrics.SidebarExpandedWidth;
            Invalidate();
        }
    }

    public void SetActive(string viewKey)
    {
        _active = viewKey switch
        {
            "task-detail" => "tasks",
            "project-detail" => "projects",
            "note-editor" => "notes",
            "snippet-editor" => "snippets",
            "productivity" or "reports" => "analytics",
            _ => viewKey.Split('-')[0]
        };
        Invalidate();
    }

    public void ApplyTheme()
    {
        BackColor = ThemeManager.Instance.Current.SidebarBg;
        Dock = LocalizationService.Instance.IsRtl ? DockStyle.Right : DockStyle.Left;
        Invalidate();
    }

    private int HeaderHeight => _collapsed ? 48 : 64;

    private Rectangle ItemRect(int index)
    {
        var y = HeaderHeight + 8 + index * (UiMetrics.SidebarRowHeight + 2);
        var x = 8;
        var w = Width - 16;
        return new Rectangle(x, y - VerticalScroll.Value, w, UiMetrics.SidebarRowHeight);
    }

    private Rectangle SettingsRect()
    {
        var y = Height - UiMetrics.SidebarRowHeight - 12;
        return new Rectangle(8, y, Width - 16, UiMetrics.SidebarRowHeight);
    }

    private void OnMove(object? sender, MouseEventArgs e)
    {
        var h = -1;
        for (var i = 0; i < Destinations.Length; i++)
            if (ItemRect(i).Contains(e.Location)) h = i;
        if (SettingsRect().Contains(e.Location)) h = Destinations.Length;
        if (h != _hover)
        {
            _hover = h;
            if (_collapsed && h >= 0)
            {
                var key = h == Destinations.Length ? "nav.settings" : Destinations[h].LabelKey;
                _tip.SetToolTip(this, LocalizationService.Instance.Get(key));
            }
            else _tip.SetToolTip(this, "");
            Invalidate();
        }
    }

    private void OnClick(object? sender, MouseEventArgs e)
    {
        for (var i = 0; i < Destinations.Length; i++)
            if (ItemRect(i).Contains(e.Location))
            {
                NavigateRequested?.Invoke(this, Destinations[i].Key);
                return;
            }
        if (SettingsRect().Contains(e.Location))
            NavigateRequested?.Invoke(this, "settings");
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var c = ThemeManager.Instance.Current;
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(c.SidebarBg);

        using (var pen = new Pen(c.Border))
        {
            if (LocalizationService.Instance.IsRtl)
                g.DrawLine(pen, 0, 0, 0, Height);
            else
                g.DrawLine(pen, Width - 1, 0, Width - 1, Height);
        }

        if (!_collapsed)
        {
            TextRenderer.DrawText(g, "DevDesk", UiMetrics.PageTitle, new Rectangle(16, 12, Width - 24, 28), c.TextPrimary);
            TextRenderer.DrawText(g, "Productivity Engine", UiMetrics.Meta, new Rectangle(16, 36, Width - 24, 18), c.TextMuted);
        }
        else
        {
            UiIcons.Draw(g, "terminal", new Rectangle(16, 16, 20, 20), c.Accent);
        }

        var rtl = LocalizationService.Instance.IsRtl;
        for (var i = 0; i < Destinations.Length; i++)
            PaintItem(g, c, ItemRect(i), Destinations[i].Icon, LocalizationService.Instance.Get(Destinations[i].LabelKey),
                Destinations[i].Key == _active, i == _hover, rtl);

        using (var pen = new Pen(DrawingUtil.WithAlpha(c.Border, 160)))
            g.DrawLine(pen, 12, SettingsRect().Top - 8, Width - 12, SettingsRect().Top - 8);
        PaintItem(g, c, SettingsRect(), "settings", LocalizationService.Instance.Get("nav.settings"),
            _active == "settings", _hover == Destinations.Length, rtl);
    }

    private void PaintItem(Graphics g, AppColors c, Rectangle r, string icon, string label, bool active, bool hover, bool rtl)
    {
        if (active)
        {
            using var bg = new SolidBrush(c.SelectedBg);
            DrawingUtil.FillRounded(g, bg, r, UiMetrics.RadiusSm);
            using var accent = new Pen(c.Accent, 2);
            if (rtl) g.DrawLine(accent, r.Right - 1, r.Top + 6, r.Right - 1, r.Bottom - 6);
            else g.DrawLine(accent, r.Left + 1, r.Top + 6, r.Left + 1, r.Bottom - 6);
        }
        else if (hover)
        {
            using var bg = new SolidBrush(c.HoverBg);
            DrawingUtil.FillRounded(g, bg, r, UiMetrics.RadiusSm);
        }

        var iconRect = new Rectangle(r.X + 10, r.Y + (r.Height - 18) / 2, 18, 18);
        UiIcons.Draw(g, icon, iconRect, active ? c.TextPrimary : c.TextSecondary);
        if (!_collapsed)
        {
            TextRenderer.DrawText(g, label, UiMetrics.SectionTitle, new Rectangle(r.X + 36, r.Y, r.Width - 44, r.Height),
                active ? c.TextPrimary : c.TextSecondary, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    private readonly record struct NavItem(string Key, string Label, string Icon);

    protected override void Dispose(bool disposing)
    {
        if (disposing) _tip.Dispose();
        base.Dispose(disposing);
    }
}
