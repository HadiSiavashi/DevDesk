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
    private readonly Label _greeting = new() { Dock = DockStyle.Top, Height = 36, Font = UiMetrics.PageTitle };
    private readonly Label _summary = new() { Dock = DockStyle.Top, Height = 22, Font = UiMetrics.Meta };
    private readonly Panel _focusCard = new() { Dock = DockStyle.Top, Height = 72, Padding = new Padding(UiMetrics.Space12), Tag = "no-theme" };
    private readonly Label _focusCardTitle = new() { Dock = DockStyle.Top, Height = 22, Font = UiMetrics.SectionTitle };
    private readonly Label _focusCardBody = new() { Dock = DockStyle.Fill, Font = UiMetrics.Body };
    private readonly Label _scoreHeader = new() { Dock = DockStyle.Top, Height = 22, Font = UiMetrics.SectionTitle, Text = "Productivity Score" };
    private readonly Label _scoreBreakdown = new() { Dock = DockStyle.Top, Height = 56, Font = UiMetrics.Meta };
    private readonly Label _priorityHeader = new() { Dock = DockStyle.Top, Height = 24, Font = UiMetrics.SectionTitle, Text = "Priority Tasks" };
    private readonly Panel _tasksHost = new() { Dock = DockStyle.Fill, AutoScroll = true, Tag = "no-theme" };

    public DashboardView(IServiceScopeFactory scopeFactory, NavigationService navigation, IAppEventBus events)
        : base(scopeFactory, navigation)
    {
        _events = events;
        _focusCard.Controls.Add(_focusCardBody);
        _focusCard.Controls.Add(_focusCardTitle);
        _focusCard.Cursor = Cursors.Hand;
        _focusCard.Click += (_, _) => Navigation.Navigate("focus");
        _focusCardTitle.Click += (_, _) => Navigation.Navigate("focus");
        _focusCardBody.Click += (_, _) => Navigation.Navigate("focus");

        ContentPanel.Controls.Add(_tasksHost);
        ContentPanel.Controls.Add(_priorityHeader);
        ContentPanel.Controls.Add(_scoreBreakdown);
        ContentPanel.Controls.Add(_scoreHeader);
        ContentPanel.Controls.Add(_focusCard);
        ContentPanel.Controls.Add(_summary);
        ContentPanel.Controls.Add(_greeting);
        _tasksHost.Resize += (_, _) => LayoutTasks();

        _eventHandler = OnAppEvent;
        _events.Published += _eventHandler;
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
        if (InvokeRequired)
        {
            BeginInvoke(() => OnAppEvent(sender, e));
            return;
        }

        if (e.Kind is AppEventKind.TaskCreated or AppEventKind.TaskUpdated
            or AppEventKind.TaskDeleted or AppEventKind.TaskCompleted
            or AppEventKind.FocusStarted or AppEventKind.FocusStopped
            or AppEventKind.FocusPaused or AppEventKind.FocusResumed)
        {
            _ = LoadAsync();
        }
    }

    private void Bind(DashboardDto data)
    {
        var c = ThemeManager.Instance.Current;
        _greeting.Text = data.Greeting;
        _greeting.ForeColor = c.TextPrimary;
        _summary.Text = $"{data.Today:dddd, MMM d}  ·  {data.OpenTaskCount} tasks  ·  {data.FocusMinutesToday} min focus  ·  {data.ProductivityScore.Total}/100 score";
        _summary.ForeColor = c.TextSecondary;

        BindFocusCard(data, c);
        BindScore(data, c);
        BindPriorityTasks(data);
    }

    private void BindFocusCard(DashboardDto data, AppColors c)
    {
        _focusCard.BackColor = c.Surface;
        _focusCardTitle.ForeColor = c.TextPrimary;
        _focusCardBody.ForeColor = c.TextSecondary;

        if (data.ActiveFocusSession is { IsActive: true } session)
        {
            _focusCardTitle.Text = session.IsPaused ? "FOCUS — PAUSED" : "FOCUS — ACTIVE";
            var label = session.TaskTitle ?? session.ProjectName ?? "Session";
            _focusCardBody.Text = $"{label}  ·  {session.ElapsedMinutes} min elapsed";
        }
        else
        {
            _focusCardTitle.Text = "READY TO FOCUS";
            _focusCardBody.Text = "Start a focus session to track deep work time.";
        }
    }

    private void BindScore(DashboardDto data, AppColors c)
    {
        var ps = data.ProductivityScore;
        _scoreHeader.ForeColor = c.TextPrimary;
        _scoreBreakdown.ForeColor = c.TextMuted;
        _scoreBreakdown.Text =
            $"Total {ps.Total}/100  ·  Completion {ps.CompletionScore}  ·  Focus {ps.FocusScore}  ·  Planning {ps.PlanningScore}  ·  Review {ps.ReviewScore}\n{ps.Explanation}";
    }

    private void BindPriorityTasks(DashboardDto data)
    {
        _tasksHost.Controls.Clear();
        var priority = data.StarredTasks
            .Concat(data.TodayTasks.Where(t => t.Priority is TaskPriority.High or TaskPriority.Critical))
            .GroupBy(t => t.Id)
            .Select(g => g.First())
            .Take(8)
            .ToList();

        foreach (var task in priority)
        {
            var item = new TaskListItemControl
            {
                Width = Math.Max(200, _tasksHost.ClientSize.Width - 4),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            item.Bind(task);
            item.ItemClicked += (_, id) => Navigation.Navigate("task-detail", id);
            item.CompleteClicked += async (_, id) =>
            {
                using var s = ScopeFactory.CreateScope();
                await GetService<ITaskService>(s).CompleteAsync(id);
            };
            _tasksHost.Controls.Add(item);
        }
        LayoutTasks();
    }

    private void LayoutTasks()
    {
        for (var i = 0; i < _tasksHost.Controls.Count; i++)
        {
            if (_tasksHost.Controls[i] is TaskListItemControl item)
            {
                item.Width = Math.Max(200, _tasksHost.ClientSize.Width - 4);
                item.Location = new Point(0, i * (item.Height + 4));
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
