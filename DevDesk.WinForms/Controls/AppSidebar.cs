using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class AppSidebar : Panel
{
    public event EventHandler<string>? NavigateRequested;

    private readonly ToolTip _tip = new();
    private readonly Panel _header = new() { Dock = DockStyle.Top, Tag = "no-theme" };
    private readonly Panel _nav = new() { Dock = DockStyle.Fill, Tag = "no-theme", AutoScroll = true };
    private readonly Panel _settings = new() { Dock = DockStyle.Bottom, Tag = "no-theme" };
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
        DrawingUtil.EnableDoubleBuffer(_nav);
        DrawingUtil.EnableDoubleBuffer(_header);
        DrawingUtil.EnableDoubleBuffer(_settings);
        Cursor = Cursors.Hand;
        ApplyScale();

        _header.Paint += PaintHeader;
        _nav.Paint += PaintNav;
        _settings.Paint += PaintSettings;
        _nav.MouseMove += OnNavMove;
        _nav.MouseLeave += (_, _) => { if (_hover >= 0 && _hover < Destinations.Length) { _hover = -1; _nav.Invalidate(); } };
        _nav.MouseClick += OnNavClick;
        _settings.MouseMove += (_, _) =>
        {
            if (_hover != Destinations.Length) { _hover = Destinations.Length; _settings.Invalidate(); }
        };
        _settings.MouseLeave += (_, _) => { if (_hover == Destinations.Length) { _hover = -1; _settings.Invalidate(); } };
        _settings.MouseClick += (_, _) => NavigateRequested?.Invoke(this, "settings");
        _nav.Scroll += (_, _) => _nav.Invalidate();
        _nav.MouseWheel += (_, _) => _nav.Invalidate();
        _nav.Resize += (_, _) => UpdateNavScroll();

        Controls.Add(_nav);
        Controls.Add(_settings);
        Controls.Add(_header);

        ThemeManager.Instance.Attach(this, (_, _) => ApplyTheme());
        UiScale.Attach(this, (_, _) => ApplyScale());
        LocalizationService.Instance.LanguageChanged += (_, _) =>
        {
            Dock = LocalizationService.Instance.IsRtl ? DockStyle.Right : DockStyle.Left;
            ApplyTheme();
        };
        HandleCreated += (_, _) => DrawingUtil.ApplyWindowChrome(_nav);
        ApplyTheme();
    }

    public bool Collapsed
    {
        get => _collapsed;
        set
        {
            _collapsed = value;
            Width = value ? UiMetrics.SidebarCollapsedWidth : UiMetrics.SidebarExpandedWidth;
            ApplyScale();
            ApplyTheme();
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
        _nav.Invalidate();
        _settings.Invalidate();
    }

    public void ApplyScale()
    {
        Width = _collapsed ? UiMetrics.SidebarCollapsedWidth : UiMetrics.SidebarExpandedWidth;
        _header.Height = _collapsed ? UiMetrics.SidebarHeaderCollapsedHeight : UiMetrics.SidebarHeaderHeight;
        _settings.Height = UiMetrics.Space8 + UiMetrics.SidebarRowHeight + UiMetrics.Space8;
        UpdateNavScroll();
        _header.Invalidate();
        _nav.Invalidate();
        _settings.Invalidate();
    }

    public void ApplyTheme()
    {
        var c = ThemeManager.Instance.Current;
        BackColor = c.SidebarBg;
        _header.BackColor = c.SidebarBg;
        _nav.BackColor = c.SidebarBg;
        _settings.BackColor = c.SidebarBg;
        Dock = LocalizationService.Instance.IsRtl ? DockStyle.Right : DockStyle.Left;
        UpdateNavScroll();
        _header.Invalidate();
        _nav.Invalidate();
        _settings.Invalidate();
        if (IsHandleCreated) DrawingUtil.ApplyWindowChrome(_nav);
    }

    private void UpdateNavScroll()
    {
        var pad = UiMetrics.Space8;
        var contentH = pad + Destinations.Length * (UiMetrics.SidebarRowHeight + 2) + pad;
        _nav.AutoScrollMinSize = new Size(0, contentH);
    }

    private Rectangle ItemRect(int index)
    {
        var pad = UiMetrics.Space8;
        var y = pad + index * (UiMetrics.SidebarRowHeight + 2) - _nav.VerticalScroll.Value;
        return new Rectangle(pad, y, Math.Max(8, _nav.ClientSize.Width - pad * 2), UiMetrics.SidebarRowHeight);
    }

    private void OnNavMove(object? sender, MouseEventArgs e)
    {
        var h = -1;
        for (var i = 0; i < Destinations.Length; i++)
            if (ItemRect(i).Contains(e.Location)) h = i;
        if (h == _hover) return;
        _hover = h;
        if (_collapsed && h >= 0)
            _tip.SetToolTip(_nav, LocalizationService.Instance.Get(Destinations[h].LabelKey));
        else
            _tip.SetToolTip(_nav, "");
        _nav.Invalidate();
    }

    private void OnNavClick(object? sender, MouseEventArgs e)
    {
        for (var i = 0; i < Destinations.Length; i++)
            if (ItemRect(i).Contains(e.Location))
            {
                NavigateRequested?.Invoke(this, Destinations[i].Key);
                return;
            }
    }

    private void PaintHeader(object? sender, PaintEventArgs e)
    {
        var c = ThemeManager.Instance.Current;
        var g = e.Graphics;
        g.Clear(c.SidebarBg);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        if (!_collapsed)
        {
            TextRenderer.DrawText(g, "DevDesk", UiMetrics.PageTitle, new Rectangle(UiMetrics.Space16, UiMetrics.Space8, _header.Width - UiMetrics.Space24, UiScale.Px(26)), c.TextPrimary);
            TextRenderer.DrawText(g, "Productivity Engine", UiMetrics.Meta, new Rectangle(UiMetrics.Space16, UiScale.Px(34), _header.Width - UiMetrics.Space24, UiScale.Px(18)), c.TextMuted);
        }
        else
            UiIcons.Draw(g, "terminal", new Rectangle(UiMetrics.Space16, UiMetrics.Space12, UiMetrics.IconSize, UiMetrics.IconSize), c.Accent);
        using var pen = new Pen(c.Border);
        g.DrawLine(pen, 12, _header.Height - 1, _header.Width - 12, _header.Height - 1);
    }

    private void PaintNav(object? sender, PaintEventArgs e)
    {
        var c = ThemeManager.Instance.Current;
        var g = e.Graphics;
        g.Clear(c.SidebarBg);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var rtl = LocalizationService.Instance.IsRtl;
        for (var i = 0; i < Destinations.Length; i++)
            PaintItem(g, c, ItemRect(i), Destinations[i].Icon, LocalizationService.Instance.Get(Destinations[i].LabelKey),
                Destinations[i].Key == _active, i == _hover, rtl);
    }

    private void PaintSettings(object? sender, PaintEventArgs e)
    {
        var c = ThemeManager.Instance.Current;
        var g = e.Graphics;
        g.Clear(c.SidebarBg);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using (var pen = new Pen(c.Border))
            g.DrawLine(pen, 12, 0, _settings.Width - 12, 0);
        var r = new Rectangle(UiMetrics.Space8, UiMetrics.Space8, Math.Max(8, _settings.Width - UiMetrics.Space16), UiMetrics.SidebarRowHeight);
        PaintItem(g, c, r, "settings", LocalizationService.Instance.Get("nav.settings"),
            _active == "settings", _hover == Destinations.Length, LocalizationService.Instance.IsRtl);
    }

    private void PaintItem(Graphics g, AppColors c, Rectangle r, string icon, string label, bool active, bool hover, bool rtl)
    {
        if (r.Width <= 0 || r.Height <= 0) return;
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

        var iconSize = UiMetrics.IconSize;
        var iconRect = new Rectangle(r.X + 10, r.Y + (r.Height - iconSize) / 2, iconSize, iconSize);
        UiIcons.Draw(g, icon, iconRect, active || hover ? c.TextPrimary : c.TextSecondary);
        if (!_collapsed)
        {
            var textX = r.X + 10 + iconSize + UiMetrics.Space8;
            TextRenderer.DrawText(g, label, UiMetrics.Body, new Rectangle(textX, r.Y, Math.Max(8, r.Right - textX - UiMetrics.Space8), r.Height),
                active || hover ? c.TextPrimary : c.TextSecondary,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var c = ThemeManager.Instance.Current;
        e.Graphics.Clear(c.SidebarBg);
        using var pen = new Pen(c.Border);
        if (LocalizationService.Instance.IsRtl)
            e.Graphics.DrawLine(pen, 0, 0, 0, Height);
        else
            e.Graphics.DrawLine(pen, Width - 1, 0, Width - 1, Height);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _tip.Dispose();
        base.Dispose(disposing);
    }
}
