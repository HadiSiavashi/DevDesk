using DevDesk.Application.Dtos;
using DevDesk.Application.Events;
using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Dialogs;
using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Overlays;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using DevDesk.WinForms.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms;

public sealed class MainForm : Form, INavigationHost
{
    private readonly IServiceProvider _services;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NavigationService _navigation;
    private readonly IAppEventBus _events;
    private readonly WindowStateStore _windowState = new();
    private readonly TrayIconService _tray;
    private readonly ToastHost _toast = new();
    private readonly UiNotifier _notifier;
    private readonly ToolTip _sidebarTip = new();

    private readonly Panel _topBar = new() { Dock = DockStyle.Top, Height = UiMetrics.TopBarHeight, Tag = "no-theme" };
    private readonly Panel _sidebar = new() { Dock = DockStyle.Left, Width = UiMetrics.SidebarExpandedWidth, Tag = "no-theme", AutoScroll = true };
    private readonly Panel _content = new() { Dock = DockStyle.Fill, Tag = "no-theme" };
    private readonly Label _title = new() { AutoSize = true, Left = 48, Top = 10, Font = UiMetrics.SectionTitle };
    private readonly IconButton _collapseBtn = new() { Text = "☰", Left = 8, Top = 4, Width = 28, Height = 28 };
    private readonly Label _focusIndicator = new()
    {
        AutoSize = false,
        Height = 28,
        Top = 6,
        Width = 260,
        Cursor = Cursors.Hand,
        Font = UiMetrics.Meta,
        TextAlign = ContentAlignment.MiddleLeft,
        Visible = false,
        Padding = new Padding(8, 0, 8, 0)
    };
    private Label? _searchHint;
    private IconButton? _quickBtn;

    private UserControl? _currentView;
    private bool _sidebarCollapsed;
    private WindowStateStore.SavedWindowState _savedState = new();
    private System.Windows.Forms.Timer? _notificationTimer;
    private readonly System.Windows.Forms.Timer _focusTick = new() { Interval = 1000 };
    private FocusSessionDto? _activeFocus;
    private EventHandler<AppEvent>? _eventHandler;

    public string? CurrentViewKey { get; private set; }

    public static bool ApplyAlwaysOnTop { get; set; }
    public static bool ApplyStartMinimized { get; set; }

    public MainForm(IServiceProvider services, NavigationService navigation, TrayIconService tray, IAppEventBus events)
    {
        _services = services;
        _scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        _navigation = navigation;
        _events = events;
        _tray = tray;
        _navigation.Host = this;
        _notifier = new UiNotifier(_toast, this, events);

        Text = LocalizationService.Instance.Get("app.title");
        MinimumSize = new Size(UiMetrics.MinWindowWidth, UiMetrics.MinWindowHeight);
        KeyPreview = true;

        _savedState = _windowState.Load();
        _windowState.Apply(this, _savedState);
        _sidebarCollapsed = _savedState.SidebarCollapsed;
        ApplySidebarWidth();

        BuildTopBar();
        BuildSidebar();
        Controls.Add(_content);
        Controls.Add(_sidebar);
        Controls.Add(_topBar);
        Controls.Add(_toast);

        ApplyThemeAndLocale();
        ThemeManager.Instance.ThemeChanged += (_, _) => ApplyThemeAndLocale();
        LocalizationService.Instance.LanguageChanged += (_, _) => { ApplyThemeAndLocale(); BuildSidebar(); };

        FormClosing += OnFormClosing;
        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized) MaybeMinimizeToTray();
            if (ClientSize.Width < UiMetrics.SidebarAutoCollapseWidth && !_sidebarCollapsed)
                SetSidebarCollapsed(true);
            else if (ClientSize.Width >= UiMetrics.SidebarAutoCollapseWidth + 80 && _sidebarCollapsed && !_savedState.SidebarCollapsed)
                SetSidebarCollapsed(false);
            LayoutTopBar();
        };

        RegisterShortcuts();
        _tray.Attach(this);
        WireTrayActions();

        _focusTick.Tick += (_, _) => UpdateFocusIndicator();
        _eventHandler = OnAppEvent;
        _events.Published += _eventHandler;
        _notifier.Attach();
    }

    private void WireTrayActions()
    {
        _tray.StartFocusRequested = () => _ = StartFocusFromTrayAsync();
        _tray.PauseFocusRequested = () => _ = PauseFocusFromTrayAsync();
        _tray.StopFocusRequested = () => _ = StopFocusFromTrayAsync();
        _tray.QuickAddRequested = ShowQuickAdd;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        TopMost = ApplyAlwaysOnTop;
        _tray.SetVisible(true);

        if (ApplyStartMinimized)
            WindowState = FormWindowState.Minimized;

        var notifications = _services.GetRequiredService<INotificationService>();
        notifications.NotificationRequested += OnNotificationRequested;
        StartNotificationPolling();
        _ = RefreshActiveFocusAsync();
        _focusTick.Start();

        Navigate("dashboard");
    }

    private void OnAppEvent(object? sender, AppEvent e)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke(() => OnAppEvent(sender, e));
            return;
        }

        switch (e.Kind)
        {
            case AppEventKind.FocusStarted:
            case AppEventKind.FocusPaused:
            case AppEventKind.FocusResumed:
                if (e.Payload is FocusSessionDto s)
                    _activeFocus = s;
                else
                    _ = RefreshActiveFocusAsync();
                UpdateFocusIndicator();
                break;
            case AppEventKind.FocusStopped:
                _activeFocus = null;
                UpdateFocusIndicator();
                break;
        }
    }

    private async Task RefreshActiveFocusAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            _activeFocus = await scope.ServiceProvider.GetRequiredService<IFocusService>().GetActiveAsync();
            if (InvokeRequired) BeginInvoke(UpdateFocusIndicator);
            else UpdateFocusIndicator();
        }
        catch { /* ignore */ }
    }

    private void UpdateFocusIndicator()
    {
        if (_focusIndicator.IsDisposed) return;
        if (_activeFocus is not { IsActive: true })
        {
            _focusIndicator.Visible = false;
            LayoutTopBar();
            return;
        }

        var now = DateTime.UtcNow;
        var end = _activeFocus.EndedAt ?? now;
        var secs = (int)(end - _activeFocus.StartedAt).TotalSeconds - _activeFocus.PausedAccumulatedSeconds;
        if (_activeFocus.IsPaused && _activeFocus.PausedAt is DateTime p)
            secs -= (int)(now - p).TotalSeconds;
        secs = Math.Max(0, secs);
        var ts = TimeSpan.FromSeconds(secs);
        var time = ts.TotalHours >= 1 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"mm\:ss");
        var name = _activeFocus.TaskTitle ?? _activeFocus.ProjectName ?? "Focus";
        if (name.Length > 28) name = name[..27] + "…";
        var state = _activeFocus.IsPaused ? "PAUSED" : "FOCUS";
        _focusIndicator.Text = $"● {state}  {time}  {name}";
        _focusIndicator.Visible = true;
        var c = ThemeManager.Instance.Current;
        _focusIndicator.ForeColor = _activeFocus.IsPaused ? c.Warning : c.Accent;
        _focusIndicator.BackColor = c.SurfaceAlt;
        LayoutTopBar();
    }

    private void OnNotificationRequested(object? sender, DesktopNotificationEventArgs e)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke(() => OnNotificationRequested(sender, e));
            return;
        }

        _tray.SetVisible(true);
        _tray.ShowBalloon(e.Title, e.Message,
            e.Severity == "Warning" ? ToolTipIcon.Warning : ToolTipIcon.Info);
    }

    private void StartNotificationPolling()
    {
        _notificationTimer = new System.Windows.Forms.Timer { Interval = 5 * 60 * 1000 };
        _notificationTimer.Tick += async (_, _) => await PollNotificationsAsync();
        _notificationTimer.Start();
        _ = PollNotificationsAsync();
    }

    private async Task PollNotificationsAsync()
    {
        try
        {
            var notifications = _services.GetRequiredService<INotificationService>();
            await notifications.ShowPendingDesktopAsync();
        }
        catch { /* best effort */ }
    }

    private async Task StartFocusFromTrayAsync()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        Navigate("focus");
        using var scope = _scopeFactory.CreateScope();
        var focus = scope.ServiceProvider.GetRequiredService<IFocusService>();
        var active = await focus.GetActiveAsync();
        if (active is null || !active.IsActive)
            await focus.StartAsync(new StartFocusRequest());
    }

    private async Task PauseFocusFromTrayAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var focus = scope.ServiceProvider.GetRequiredService<IFocusService>();
        var active = await focus.GetActiveAsync();
        if (active is { IsActive: true, IsPaused: false })
            await focus.PauseAsync(active.Id);
    }

    private async Task StopFocusFromTrayAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var focus = scope.ServiceProvider.GetRequiredService<IFocusService>();
        var active = await focus.GetActiveAsync();
        if (active is { IsActive: true })
            await focus.StopAsync(active.Id);
    }

    public void ApplyAlwaysOnTopSetting(bool enabled) => TopMost = enabled;

    public void ShowToast(string message, bool isError = false) => _toast.ShowToast(message, isError);

    public void Navigate(string viewKey, object? parameter = null)
    {
        CurrentViewKey = viewKey;
        _currentView?.Dispose();
        _content.Controls.Clear();
        _currentView = _navigation.CreateView(viewKey, parameter);
        _currentView.Dock = DockStyle.Fill;
        _content.Controls.Add(_currentView);
        _title.Text = LocalizationService.Instance.Get($"nav.{viewKey.Replace("-", "")}") is var t && !t.StartsWith("nav.")
            ? t
            : viewKey;
        HighlightSidebar(viewKey);
    }

    public void NavigateBack() => _navigation.NavigateBack();

    private void BuildTopBar()
    {
        _collapseBtn.Click += (_, _) => SetSidebarCollapsed(!_sidebarCollapsed);
        _collapseBtn.AccessibleName = "Toggle sidebar";
        _focusIndicator.Click += (_, _) => _navigation.Navigate("focus");
        _topBar.Controls.Add(_collapseBtn);
        _topBar.Controls.Add(_title);
        _topBar.Controls.Add(_focusIndicator);

        _searchHint = new Label
        {
            Text = "Ctrl+K  Search…",
            Height = UiMetrics.ControlHeightCompact,
            Top = 6,
            Width = 220,
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand,
            Font = UiMetrics.Meta,
            Padding = new Padding(10, 0, 10, 0),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _searchHint.Click += (_, _) => ShowGlobalSearch();
        _searchHint.Paint += (_, e) =>
        {
            var c = ThemeManager.Instance.Current;
            using var bg = new SolidBrush(c.InputBg);
            using var border = new Pen(c.Border);
            var r = new Rectangle(0, 0, _searchHint.Width - 1, _searchHint.Height - 1);
            e.Graphics.FillRectangle(bg, r);
            e.Graphics.DrawRectangle(border, r);
            TextRenderer.DrawText(e.Graphics, _searchHint.Text, _searchHint.Font, _searchHint.ClientRectangle, c.TextMuted,
                TextFormatFlags.VerticalCenter | TextFormatFlags.LeftAndRightPadding);
        };

        _quickBtn = new IconButton
        {
            Text = "+",
            Width = 28,
            Height = 28,
            Top = 6,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            AccessibleName = "Quick Add"
        };
        _sidebarTip.SetToolTip(_quickBtn, "Quick Add (Ctrl+Shift+Space)");
        _quickBtn.Click += (_, _) => ShowQuickAdd();

        _topBar.Controls.Add(_searchHint);
        _topBar.Controls.Add(_quickBtn);
        LayoutTopBar();
        _topBar.Resize += (_, _) => LayoutTopBar();
    }

    private void LayoutTopBar()
    {
        if (_quickBtn is null || _searchHint is null) return;
        _quickBtn.Left = Math.Max(160, _topBar.Width - 40);
        _searchHint.Left = Math.Max(200, _topBar.Width - 280);
        if (_focusIndicator.Visible)
        {
            _focusIndicator.Left = Math.Max(_title.Right + 16, _searchHint.Left - _focusIndicator.Width - 12);
            _focusIndicator.Width = Math.Min(280, Math.Max(160, _searchHint.Left - _focusIndicator.Left - 8));
        }
    }

    private void BuildSidebar()
    {
        _sidebarTip.RemoveAll();
        _sidebar.Controls.Clear();
        var loc = LocalizationService.Instance;
        var sections = new (string? header, (string key, string labelKey, string icon)[] items)[]
        {
            (null, new[]
            {
                ("dashboard", "nav.dashboard", "⌂"), ("myday", "nav.myday", "☀"), ("tasks", "nav.tasks", "☑"),
                ("projects", "nav.projects", "▦"), ("calendar", "nav.calendar", "▦"), ("focus", "nav.focus", "▶"),
                ("notes", "nav.notes", "✎"), ("goals", "nav.goals", "★"), ("habits", "nav.habits", "↻")
            }),
            ("nav.devtools", new[]
            {
                ("snippets", "nav.snippets", "</>"), ("bookmarks", "nav.bookmarks", "¦"),
                ("environments", "nav.environments", "⚙"), ("knowledge", "nav.knowledge", "≡")
            }),
            ("nav.analyticsSection", new[]
            {
                ("analytics", "nav.analytics", "▥"), ("productivity", "nav.productivity", "▦"),
                ("reports", "nav.reports", "▧"), ("dailyplan", "nav.dailyplan", "☰"),
                ("dailyreview", "nav.dailyreview", "✎")
            }),
            ("nav.system", new[]
            {
                ("settings", "nav.settings", "⚙")
            })
        };

        var y = UiMetrics.Space8;
        foreach (var (header, items) in sections)
        {
            if (header is not null && !_sidebarCollapsed)
            {
                var lbl = new Label
                {
                    Text = loc.Get(header),
                    Left = 12,
                    Top = y,
                    Width = UiMetrics.SidebarExpandedWidth - 24,
                    Height = 18,
                    ForeColor = ThemeManager.Instance.Current.TextMuted,
                    Font = UiMetrics.Caption
                };
                _sidebar.Controls.Add(lbl);
                y += 22;
            }

            foreach (var (key, labelKey, icon) in items)
            {
                var label = loc.Get(labelKey);
                var text = _sidebarCollapsed ? icon : $"  {icon}   {label}";
                var btn = new Button
                {
                    Text = text,
                    Tag = key,
                    FlatStyle = FlatStyle.Flat,
                    Width = _sidebarCollapsed ? 40 : UiMetrics.SidebarExpandedWidth - 16,
                    Height = UiMetrics.SidebarRowHeight,
                    Left = 8,
                    Top = y,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = UiMetrics.Body,
                    AccessibleName = label,
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (_, _) => _navigation.Navigate(key);
                if (_sidebarCollapsed)
                    _sidebarTip.SetToolTip(btn, label);
                _sidebar.Controls.Add(btn);
                y += UiMetrics.SidebarRowHeight + 2;
            }

            y += UiMetrics.Space8;
        }
        StyleSidebar();
        if (CurrentViewKey is not null) HighlightSidebar(CurrentViewKey);
    }

    private void SetSidebarCollapsed(bool collapsed)
    {
        _sidebarCollapsed = collapsed;
        var target = collapsed ? UiMetrics.SidebarCollapsedWidth : UiMetrics.SidebarExpandedWidth;
        var start = _sidebar.Width;
        AnimationScheduler.Instance.Animate(UiMetrics.MicroMs, t =>
        {
            if (!IsDisposed)
                _sidebar.Width = start + (int)((target - start) * t);
        }, () =>
        {
            if (!IsDisposed)
            {
                ApplySidebarWidth();
                BuildSidebar();
            }
        });
    }

    private void ApplySidebarWidth() =>
        _sidebar.Width = _sidebarCollapsed ? UiMetrics.SidebarCollapsedWidth : UiMetrics.SidebarExpandedWidth;

    private static string SidebarKeyFor(string viewKey) => viewKey switch
    {
        "task-detail" => "tasks",
        "project-detail" => "projects",
        "note-editor" => "notes",
        "snippet-editor" => "snippets",
        _ => viewKey.Split('-')[0]
    };

    private void HighlightSidebar(string viewKey)
    {
        var highlight = SidebarKeyFor(viewKey);
        foreach (Control c in _sidebar.Controls)
        {
            if (c is Button b)
            {
                var tag = b.Tag?.ToString();
                var selected = tag == highlight || tag == viewKey;
                b.BackColor = selected ? ThemeManager.Instance.Current.SelectedBg : Color.Transparent;
            }
        }
    }

    private void StyleSidebar()
    {
        var c = ThemeManager.Instance.Current;
        _sidebar.BackColor = c.SidebarBg;
        _topBar.BackColor = c.TopBarBg;
        _content.BackColor = c.Background;
        foreach (Control ctrl in _sidebar.Controls)
        {
            if (ctrl is Button b)
            {
                b.ForeColor = c.TextPrimary;
                b.BackColor = Color.Transparent;
            }
        }
        _title.ForeColor = c.TextPrimary;
    }

    private void ApplyThemeAndLocale()
    {
        StyleSidebar();
        LocalizationService.Instance.ApplyRtl(this);
        _tray.RebuildMenu();
        UpdateFocusIndicator();
    }

    private void RegisterShortcuts()
    {
        KeyDown += async (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.K) { ShowGlobalSearch(); e.Handled = true; }
            else if (e.Control && e.Shift && e.KeyCode == Keys.Space) { ShowQuickAdd(); e.Handled = true; }
            else if (e.Control && e.Shift && e.KeyCode == Keys.F) { _navigation.Navigate("focus"); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.S) { await SaveCurrentViewAsync(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.N) { await NewTaskShortcut(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.Oemcomma) { _navigation.Navigate("settings"); e.Handled = true; }
            else if (e.KeyCode == Keys.F1) { ShowShortcutHelp(); e.Handled = true; }
            else if (e.KeyCode == Keys.Space && _currentView is TasksView tv) { await tv.CompleteSelectedAsync(); e.Handled = true; }
        };
    }

    private async Task SaveCurrentViewAsync()
    {
        if (_currentView is ISaveableView saveable)
            await saveable.SaveAsync();
    }

    private void ShowGlobalSearch()
    {
        using var form = new GlobalSearchForm(_scopeFactory, _navigation);
        form.ShowDialog(this);
    }

    private void ShowQuickAdd()
    {
        using var form = new QuickAddForm(_scopeFactory);
        form.ShowDialog(this);
        // Toast + list sync via AppEventBus
    }

    private void ShowShortcutHelp()
    {
        using var form = new ShortcutHelpForm();
        form.ShowDialog(this);
    }

    private async Task NewTaskShortcut()
    {
        if (_currentView is TasksView tv)
            await tv.CreateTaskAsync();
        else if (_currentView is FocusView)
        {
            using var form = TaskEditorForm.ForCreate(_scopeFactory, DateTime.Today);
            form.ShowDialog(this);
        }
        else
        {
            using var form = TaskEditorForm.ForCreate(_scopeFactory, DateTime.Today);
            form.ShowDialog(this);
        }
    }

    private async void MaybeMinimizeToTray()
    {
        using var scope = _scopeFactory.CreateScope();
        var minimize = await scope.ServiceProvider.GetRequiredService<ISettingsService>().GetSettingAsync("MinimizeToTray");
        if (minimize == "true")
        {
            Hide();
            _tray.SetVisible(true);
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            using var scope = _scopeFactory.CreateScope();
            var setting = scope.ServiceProvider.GetRequiredService<ISettingsService>().GetSettingAsync("MinimizeToTray").GetAwaiter().GetResult();
            if (setting == "true")
            {
                e.Cancel = true;
                Hide();
                _tray.SetVisible(true);
                return;
            }
        }
        _windowState.Save(this, _sidebarCollapsed);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_eventHandler is not null)
                _events.Published -= _eventHandler;
            _notifier.Detach();
            _notificationTimer?.Stop();
            _notificationTimer?.Dispose();
            _focusTick.Stop();
            _focusTick.Dispose();
            _currentView?.Dispose();
            _sidebarTip.Dispose();
        }
        base.Dispose(disposing);
    }
}
