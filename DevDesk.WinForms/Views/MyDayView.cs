using DevDesk.Application.Dtos;
using DevDesk.Application.Events;
using DevDesk.Application.Interfaces;
using DevDesk.Domain.Enums;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Dialogs;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class MyDayView : ViewBase
{
    private readonly IAppEventBus _events;
    private EventHandler<AppEvent>? _eventHandler;
    private Guid? _highlightId;
    private readonly Label _goalsBody = new() { Dock = DockStyle.Fill, AutoSize = false };
    private readonly Label _capacityBody = new() { Dock = DockStyle.Fill, AutoSize = false };
    private readonly Panel _board = new() { Dock = DockStyle.Fill, AutoScroll = true, Tag = "no-theme" };
    private readonly ModernButton _addBtn = new() { Text = "+ Add task", Variant = ButtonVariant.Outline, Width = 120, Height = UiMetrics.ButtonHeight };

    public MyDayView(IServiceScopeFactory scopeFactory, NavigationService navigation, IAppEventBus events)
        : base(scopeFactory, navigation)
    {
        _events = events;
        _addBtn.Click += async (_, _) =>
        {
            using var form = TaskEditorForm.ForCreate(ScopeFactory, DateTime.Today);
            form.ShowDialog(FindForm());
        };
        var header = new PageHeader { TitleText = T("nav.myday"), SubtitleText = DateTime.Today.ToString("dddd, MMMM d") };
        header.Actions.Controls.Add(_addBtn);

        var goalsCard = new CardPanel { Dock = DockStyle.Top, Height = 140 };
        var goalsTitle = new Label { Text = "Top Goals for Today", Dock = DockStyle.Top, Height = 22, Font = UiMetrics.Meta };
        goalsCard.Controls.Add(_goalsBody);
        goalsCard.Controls.Add(goalsTitle);

        var capCard = new CardPanel { Dock = DockStyle.Top, Height = 100 };
        var capTitle = new Label { Text = "CAPACITY", Dock = DockStyle.Top, Height = 20, Font = UiMetrics.Meta };
        capCard.Controls.Add(_capacityBody);
        capCard.Controls.Add(capTitle);

        var left = new Panel { Dock = DockStyle.Left, Width = 280, Tag = "no-theme", Padding = new Padding(0, 0, 12, 0) };
        left.Controls.Add(capCard);
        left.Controls.Add(goalsCard);

        ContentPanel.Controls.Add(_board);
        ContentPanel.Controls.Add(left);
        ContentPanel.Controls.Add(header);
        _eventHandler = OnAppEvent;
        _events.Published += _eventHandler;
    }

    protected override async Task LoadAsync()
    {
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var taskSvc = GetService<ITaskService>(scope);
            var planSvc = GetService<IDailyPlanService>(scope);
            var tasks = await taskSvc.GetMyDayTasksAsync();
            var plan = await planSvc.GetOrCreateAsync(DateOnly.FromDateTime(DateTime.Today));
            var c = ThemeManager.Instance.Current;

            var goals = new[] { plan.TopGoal1, plan.TopGoal2, plan.TopGoal3 }
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Select((g, i) => $"{i + 1}. {g}")
                .DefaultIfEmpty("No goals set yet");
            _goalsBody.Text = string.Join("\n", goals);
            _goalsBody.ForeColor = c.TextSecondary;
            _goalsBody.Font = UiMetrics.Body;

            _capacityBody.Text = plan.WorkloadExceedsAvailable
                ? T("dailyplan.warning")
                : $"{plan.EstimatedWorkloadMinutes}/{plan.AvailableWorkMinutes} min planned";
            _capacityBody.ForeColor = plan.WorkloadExceedsAvailable ? c.Error : c.TextSecondary;
            _capacityBody.Font = UiMetrics.MonoTimer;

            var today = DateTime.Today;
            var overdue = tasks.Where(t => t.Status != WorkTaskStatus.Done && t.IsOverdue).ToList();
            var dueToday = tasks.Where(t => t.Status != WorkTaskStatus.Done && !t.IsOverdue
                && t.DueDate is DateTime d && d.Date <= today).ToList();
            var remaining = tasks.Where(t => t.Status != WorkTaskStatus.Done && !overdue.Contains(t) && !dueToday.Contains(t)).ToList();
            var done = tasks.Where(t => t.Status == WorkTaskStatus.Done).ToList();

            _board.Controls.Clear();
            var y = 0;
            y = AddGroup(_board, "Overdue", overdue, c.Error, y);
            y = AddGroup(_board, "Due Today", dueToday, c.TextPrimary, y);
            y = AddGroup(_board, "Later", remaining, c.TextPrimary, y);
            AddGroup(_board, "Completed Today", done, c.TextMuted, y);
            _highlightId = null;
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private int AddGroup(Panel host, string title, List<TaskListItemDto> items, Color titleColor, int y)
    {
        if (items.Count == 0) return y;
        var lbl = new Label
        {
            Text = $"{title}  ·  {items.Count}",
            Left = 0,
            Top = y,
            Width = Math.Max(200, host.ClientSize.Width - 8),
            Height = 24,
            Font = UiMetrics.SectionTitle,
            ForeColor = titleColor,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        host.Controls.Add(lbl);
        y += 28;
        foreach (var task in items)
        {
            var item = new TaskListItemControl
            {
                Width = Math.Max(200, host.ClientSize.Width - 4),
                Location = new Point(0, y),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            item.Bind(task);
            if (_highlightId == task.Id)
                item.FlashHighlight();
            item.CompleteClicked += async (_, id) =>
            {
                using var s = ScopeFactory.CreateScope();
                await GetService<ITaskService>(s).CompleteAsync(id);
            };
            item.ItemClicked += (_, id) => Navigation.Navigate("task-detail", id);
            host.Controls.Add(item);
            y += item.Height + 4;
        }
        return y + 12;
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
            or AppEventKind.FocusStopped)
        {
            _highlightId = e.EntityId;
            _ = LoadAsync();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _eventHandler is not null)
            _events.Published -= _eventHandler;
        base.Dispose(disposing);
    }
}
