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
    private readonly AppSidebar _sidebar = new();
    private readonly AppTopBar _topBar = new();
    private readonly Panel _content = new() { Dock = DockStyle.Fill, Tag = "no-theme" };

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
        BackColor = ThemeManager.Instance.Current.Background;

        _savedState = _windowState.Load();
        _windowState.Apply(this, _savedState);
        _sidebarCollapsed = _savedState.SidebarCollapsed;
        _sidebar.Collapsed = _sidebarCollapsed;

        _sidebar.NavigateRequested += (_, key) => _navigation.Navigate(key);
        _topBar.CollapseRequested += (_, _) => SetSidebarCollapsed(!_sidebarCollapsed);
        _topBar.SearchRequested += (_, _) => ShowGlobalSearch();
        _topBar.QuickAddRequested += (_, _) => ShowQuickAdd();
        _topBar.FocusRequested += (_, _) => _navigation.Navigate("focus");
        _topBar.StopFocusRequested += (_, _) => _ = StopFocusFromTrayAsync();

        Controls.Add(_content);
        Controls.Add(_sidebar);
        Controls.Add(_topBar);
        Controls.Add(_toast);

        ApplyThemeAndLocale();
        ThemeManager.Instance.ThemeChanged += (_, _) => ApplyThemeAndLocale();
        LocalizationService.Instance.LanguageChanged += (_, _) => ApplyThemeAndLocale();

        FormClosing += OnFormClosing;
        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized) MaybeMinimizeToTray();
            if (ClientSize.Width < UiMetrics.SidebarAutoCollapseWidth && !_sidebarCollapsed)
                SetSidebarCollapsed(true);
            else if (ClientSize.Width >= UiMetrics.SidebarAutoCollapseWidth + 80 && _sidebarCollapsed && !_savedState.SidebarCollapsed)
                SetSidebarCollapsed(false);
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
        if (IsDisposed) return;
        _topBar.SetFocusSession(_activeFocus is { IsActive: true } ? _activeFocus : null);
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
        _topBar.SetNotificationBadge(true);
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
        _sidebar.SetActive(viewKey);
        _toast.BringToFront();
    }

    public void NavigateBack() => _navigation.NavigateBack();

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
                _sidebar.Collapsed = collapsed;
        });
    }

    private void ApplyThemeAndLocale()
    {
        var c = ThemeManager.Instance.Current;
        BackColor = c.Background;
        _content.BackColor = c.Background;
        _sidebar.ApplyTheme();
        _topBar.ApplyTheme();
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
        }
        base.Dispose(disposing);
    }
}
