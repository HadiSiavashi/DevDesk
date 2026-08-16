using DevDesk.Application.Dtos;
using DevDesk.Application.Events;
using DevDesk.Application.Interfaces;
using DevDesk.Domain.Enums;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class DashboardView : ViewBase
{
    private readonly IAppEventBus _events;
    private EventHandler<AppEvent>? _eventHandler;

    private readonly Label _greeting = new() { AutoSize = false, Height = 28 };
    private readonly Label _date = new() { AutoSize = false, Height = 20 };
    private readonly StatCard _completed = new();
    private readonly StatCard _focus = new();
    private readonly StatCard _open = new();
    private readonly StatCard _projects = new();
    private readonly CardPanel _priorityCard = new() { Padding = new Padding(0) };
    private readonly CardPanel _focusCard = new();
    private readonly CardPanel _scoreCard = new();
    private readonly Panel _taskList = new() { Dock = DockStyle.Fill, AutoScroll = true, Tag = "no-theme" };
    private readonly Label _focusTitle = new() { AutoSize = false, Height = 22, TextAlign = ContentAlignment.MiddleCenter };
    private readonly Label _focusBody = new() { AutoSize = false, Height = 18, TextAlign = ContentAlignment.MiddleCenter };
    private readonly ModernButton _startFocus = new() { Text = "Start focusing", Icon = "play_arrow", Width = 140, Height = 32 };
    private readonly Label _scoreValue = new() { AutoSize = false, Height = 32 };
    private readonly ProgressBarControl _completionBar = new() { Height = 6 };
    private readonly ProgressBarControl _focusBar = new() { Height = 6 };
    private readonly Label _completionLbl = new() { AutoSize = false, Height = 16 };
    private readonly Label _focusLbl = new() { AutoSize = false, Height = 16 };

    public DashboardView(IServiceScopeFactory scopeFactory, NavigationService navigation, IAppEventBus events)
        : base(scopeFactory, navigation)
    {
        _events = events;
        _startFocus.Click += (_, _) => Navigation.Navigate("focus");
        _focusCard.Click += (_, _) => Navigation.Navigate("focus");
        BuildLayout();
        _eventHandler = OnAppEvent;
        _events.Published += _eventHandler;
    }

    private void BuildLayout()
    {
        ContentPanel.Controls.Clear();
        var header = new Panel { Dock = DockStyle.Top, Height = 56, Tag = "no-theme" };
        _greeting.Dock = DockStyle.Top;
        _date.Dock = DockStyle.Top;
        header.Controls.Add(_date);
        header.Controls.Add(_greeting);

        var stats = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 80,
            ColumnCount = 4,
            RowCount = 1,
            Tag = "no-theme"
        };
        stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        foreach (var card in new[] { _completed, _focus, _open, _projects })
        {
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(0, 0, 8, 0);
        }
        _projects.Margin = new Padding(0);
        _projects.AccentLeft = true;
        stats.Controls.Add(_completed, 0, 0);
        stats.Controls.Add(_focus, 1, 0);
        stats.Controls.Add(_open, 2, 0);
        stats.Controls.Add(_projects, 3, 0);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Tag = "no-theme"
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        grid.Padding = new Padding(0, 12, 0, 0);

        var pHeader = new Panel { Dock = DockStyle.Top, Height = 40, Tag = "no-theme" };
        var pTitle = new Label { Text = "Priority Tasks", Dock = DockStyle.Left, Width = 200, TextAlign = ContentAlignment.MiddleLeft };
        var viewAll = new LinkLabel { Text = "View All", AutoSize = true, LinkColor = ThemeManager.Instance.Current.Accent, Dock = DockStyle.Right };
        viewAll.Click += (_, _) => Navigation.Navigate("tasks");
        pHeader.Controls.Add(viewAll);
        pHeader.Controls.Add(pTitle);
        _priorityCard.Controls.Add(_taskList);
        _priorityCard.Controls.Add(pHeader);
        pTitle.Font = UiMetrics.SectionTitle;
        pTitle.ForeColor = ThemeManager.Instance.Current.TextPrimary;

        var right = new Panel { Dock = DockStyle.Fill, Tag = "no-theme" };
        _focusCard.Dock = DockStyle.Top;
        _focusCard.Height = 180;
        _scoreCard.Dock = DockStyle.Fill;
        BuildFocusCard();
        BuildScoreCard();
        right.Controls.Add(_scoreCard);
        right.Controls.Add(_focusCard);
        _priorityCard.Dock = DockStyle.Fill;
        _priorityCard.Margin = new Padding(0, 0, 12, 0);
        grid.Controls.Add(_priorityCard, 0, 0);
        grid.Controls.Add(right, 1, 0);

        ContentPanel.Controls.Add(grid);
        ContentPanel.Controls.Add(stats);
        ContentPanel.Controls.Add(header);
        _taskList.Resize += (_, _) => LayoutTasks();
    }

    private void BuildFocusCard()
    {
        _focusCard.Controls.Clear();
        var wrap = new Panel { Dock = DockStyle.Fill, Tag = "no-theme" };
        _focusTitle.Dock = DockStyle.Top;
        _focusBody.Dock = DockStyle.Top;
        var btnHost = new Panel { Dock = DockStyle.Fill, Tag = "no-theme" };
        _startFocus.Anchor = AnchorStyles.None;
        btnHost.Controls.Add(_startFocus);
        btnHost.Resize += (_, _) =>
            _startFocus.Location = new Point((btnHost.Width - _startFocus.Width) / 2, 8);
        wrap.Controls.Add(btnHost);
        wrap.Controls.Add(_focusBody);
        wrap.Controls.Add(_focusTitle);
        _focusCard.Controls.Add(wrap);
    }

    private void BuildScoreCard()
    {
        _scoreCard.Controls.Clear();
        var top = new Panel { Dock = DockStyle.Top, Height = 40, Tag = "no-theme" };
        var title = new Label { Text = "Productivity Score", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = UiMetrics.SectionTitle };
        _scoreValue.Dock = DockStyle.Right;
        _scoreValue.Width = 90;
        _scoreValue.TextAlign = ContentAlignment.MiddleRight;
        _scoreValue.Font = UiMetrics.StatValue;
        top.Controls.Add(title);
        top.Controls.Add(_scoreValue);
        var bars = new Panel { Dock = DockStyle.Fill, Tag = "no-theme", Padding = new Padding(0, 8, 0, 0) };
        _completionLbl.Dock = DockStyle.Top;
        _completionBar.Dock = DockStyle.Top;
        _focusLbl.Dock = DockStyle.Top;
        _focusBar.Dock = DockStyle.Top;
        _focusBar.Margin = new Padding(0, 8, 0, 0);
        bars.Controls.Add(_focusBar);
        bars.Controls.Add(_focusLbl);
        bars.Controls.Add(_completionBar);
        bars.Controls.Add(_completionLbl);
        _scoreCard.Controls.Add(bars);
        _scoreCard.Controls.Add(top);
    }

    protected override async Task LoadAsync()
    {
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var data = await GetService<IDashboardService>(scope).GetAsync();
            Bind(data);
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void OnAppEvent(object? sender, AppEvent e)
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(() => OnAppEvent(sender, e)); return; }
        if (e.Kind is AppEventKind.TaskCreated or AppEventKind.TaskUpdated
            or AppEventKind.TaskDeleted or AppEventKind.TaskCompleted
            or AppEventKind.FocusStarted or AppEventKind.FocusStopped
            or AppEventKind.FocusPaused or AppEventKind.FocusResumed)
            _ = LoadAsync();
    }

    private void Bind(DashboardDto data)
    {
        var c = ThemeManager.Instance.Current;
        _greeting.Text = data.Greeting;
        _greeting.Font = UiMetrics.PageTitle;
        _greeting.ForeColor = c.TextPrimary;
        _date.Text = data.Today.ToDateTime(TimeOnly.MinValue).ToString("dddd, MMMM d, yyyy");
        _date.Font = UiMetrics.Body;
        _date.ForeColor = c.TextSecondary;

        _completed.Title = "Completed";
        _completed.Value = data.CompletedTasksToday.ToString();
        _focus.Title = "Focus Time";
        _focus.Value = $"{data.FocusMinutesToday} m";
        _open.Title = "Open Tasks";
        _open.Value = data.OpenTaskCount.ToString();
        _projects.Title = "Active Projects";
        _projects.Value = data.ActiveProjectCount.ToString();

        if (data.ActiveFocusSession is { IsActive: true } session)
        {
            _focusTitle.Text = session.IsPaused ? "Paused" : "Focusing";
            _focusBody.Text = session.TaskTitle ?? session.ProjectName ?? "Session";
            _startFocus.Text = "Open Focus";
        }
        else
        {
            _focusTitle.Text = "Ready to dive in?";
            _focusBody.Text = "No active focus session.";
            _startFocus.Text = "Start focusing";
        }
        _focusTitle.Font = UiMetrics.SectionTitle;
        _focusTitle.ForeColor = c.TextPrimary;
        _focusBody.Font = UiMetrics.Meta;
        _focusBody.ForeColor = c.TextMuted;

        var ps = data.ProductivityScore;
        _scoreValue.Text = $"{ps.Total}";
        _scoreValue.ForeColor = c.Accent;
        _completionLbl.Text = $"Completion    {ps.CompletionScore}%";
        _focusLbl.Text = $"Focus Quality    {ps.FocusScore}%";
        _completionLbl.Font = _focusLbl.Font = UiMetrics.Meta;
        _completionLbl.ForeColor = _focusLbl.ForeColor = c.TextMuted;
        _completionBar.Value = Math.Clamp(ps.CompletionScore / 100f, 0, 1);
        _focusBar.Value = Math.Clamp(ps.FocusScore / 100f, 0, 1);
        _focusBar.FillColor = c.Tertiary;

        _taskList.Controls.Clear();
        var priority = data.StarredTasks
            .Concat(data.TodayTasks.Where(t => t.Priority is TaskPriority.High or TaskPriority.Critical))
            .GroupBy(t => t.Id)
            .Select(g => g.First())
            .Take(8)
            .ToList();
        foreach (var task in priority)
        {
            var item = new TaskListItemControl { Width = Math.Max(180, _taskList.ClientSize.Width - 8) };
            item.Bind(task);
            item.ItemClicked += (_, id) => Navigation.Navigate("task-detail", id);
            item.CompleteClicked += async (_, id) =>
            {
                using var s = ScopeFactory.CreateScope();
                await GetService<ITaskService>(s).CompleteAsync(id);
            };
            _taskList.Controls.Add(item);
        }
        LayoutTasks();
    }

    private void LayoutTasks()
    {
        for (var i = 0; i < _taskList.Controls.Count; i++)
        {
            if (_taskList.Controls[i] is TaskListItemControl item)
            {
                item.Width = Math.Max(180, _taskList.ClientSize.Width - 8);
                item.Location = new Point(4, i * (item.Height + 4) + 4);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _eventHandler is not null)
            _events.Published -= _eventHandler;
        base.Dispose(disposing);
    }
}
