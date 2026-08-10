using DevDesk.Application.Events;
using DevDesk.Application.Interfaces;
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
    private readonly Label _planSummary = new() { Dock = DockStyle.Top, Height = 64, Font = UiMetrics.Body, Padding = new Padding(0, 0, 0, 8) };
    private readonly Panel _tasks = new() { Dock = DockStyle.Fill, AutoScroll = true, Tag = "no-theme" };
    private readonly ModernButton _addBtn = new() { Dock = DockStyle.Bottom, Height = UiMetrics.ButtonHeight, Text = "+ Add task", IsPrimary = false };

    public MyDayView(IServiceScopeFactory scopeFactory, NavigationService navigation, IAppEventBus events)
        : base(scopeFactory, navigation)
    {
        _events = events;
        _addBtn.Click += async (_, _) =>
        {
            using var form = TaskEditorForm.ForCreate(ScopeFactory, DateTime.Today);
            form.ShowDialog(FindForm());
        };
        _tasks.Resize += (_, _) => LayoutTasks();
        ContentPanel.Controls.Add(_tasks);
        ContentPanel.Controls.Add(_addBtn);
        ContentPanel.Controls.Add(_planSummary);
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
            _planSummary.Text = $"{T("dailyplan.goals")}: {plan.TopGoal1 ?? "-"} | {plan.TopGoal2 ?? "-"} | {plan.TopGoal3 ?? "-"}\n" +
                (plan.WorkloadExceedsAvailable ? T("dailyplan.warning") : $"{T("dailyplan.workload")}: {plan.EstimatedWorkloadMinutes}/{plan.AvailableWorkMinutes} min");
            _planSummary.ForeColor = ThemeManager.Instance.Current.TextSecondary;

            _tasks.Controls.Clear();
            foreach (var task in tasks)
            {
                var item = new TaskListItemControl
                {
                    Width = Math.Max(200, _tasks.ClientSize.Width - 4),
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
                _tasks.Controls.Add(item);
            }
            _highlightId = null;
            LayoutTasks();
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void LayoutTasks()
    {
        for (var i = 0; i < _tasks.Controls.Count; i++)
        {
            if (_tasks.Controls[i] is TaskListItemControl item)
            {
                item.Width = Math.Max(200, _tasks.ClientSize.Width - 4);
                item.Location = new Point(0, i * (item.Height + 4));
            }
        }
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
