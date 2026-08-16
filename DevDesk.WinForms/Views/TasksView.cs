using DevDesk.Application.Dtos;
using DevDesk.Application.Events;
using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Dialogs;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class TasksView : ViewBase
{
    private string _filter = "today";
    private TaskListItemControl? _selectedItem;
    private Guid? _highlightId;
    private readonly IAppEventBus _events;
    private EventHandler<AppEvent>? _eventHandler;
    private readonly PageHeader _header = new();
    private readonly SegmentedTabs _tabs = new() { Dock = DockStyle.Top, Height = 32, UnderlineStyle = true };
    private readonly Panel _listHost = new() { Dock = DockStyle.Fill, AutoScroll = true, Tag = "no-theme" };
    private readonly string[] _keys = ["today", "upcoming", "overdue", "starred", "completed", "all"];

    public TasksView(IServiceScopeFactory scopeFactory, NavigationService navigation, IAppEventBus events)
        : base(scopeFactory, navigation)
    {
        _events = events;
        _header.TitleText = T("nav.tasks");
        var newBtn = new ModernButton { Text = T("tasks.new"), Icon = "add", Shortcut = "N", Width = 120, Height = 32 };
        newBtn.Click += async (_, _) => await CreateTaskAsync();
        _header.Actions.Controls.Add(newBtn);
        _tabs.Items = _keys.Select(k => T($"tasks.filter.{k}")).ToArray();
        _tabs.SelectedIndexChanged += async (_, i) =>
        {
            _filter = _keys[Math.Clamp(i, 0, _keys.Length - 1)];
            await LoadAsync();
        };
        _listHost.Resize += (_, _) => LayoutListItems();
        ContentPanel.Controls.Add(_listHost);
        ContentPanel.Controls.Add(_tabs);
        ContentPanel.Controls.Add(_header);
        _eventHandler = OnAppEvent;
        _events.Published += _eventHandler;
    }

    protected override async Task LoadAsync()
    {
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var svc = GetService<ITaskService>(scope);
            IReadOnlyList<TaskListItemDto> tasks = _filter switch
            {
                "upcoming" => await svc.GetUpcomingAsync(),
                "overdue" => await svc.GetOverdueAsync(),
                "starred" => await svc.GetStarredAsync(),
                "completed" => await svc.GetCompletedAsync(),
                "all" => await svc.GetAllAsync(),
                _ => await svc.GetMyDayTasksAsync()
            };

            if (_filter == "today")
                tasks = tasks.Where(t => t.Status != Domain.Enums.WorkTaskStatus.Done || t.DueDate.HasValue).ToList();

            _listHost.Controls.Clear();
            _selectedItem = null;
            foreach (var task in tasks)
            {
                var item = new TaskListItemControl
                {
                    Width = Math.Max(200, _listHost.ClientSize.Width - 4),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                item.Bind(task);
                if (_highlightId == task.Id)
                    item.FlashHighlight();
                item.ItemClicked += (_, id) =>
                {
                    SelectItem(item);
                    Navigation.Navigate("task-detail", id);
                };
                item.EditClicked += (_, id) =>
                {
                    using var form = TaskEditorForm.ForEdit(ScopeFactory, id);
                    form.ShowDialog(FindForm());
                };
                item.CompleteClicked += async (_, id) =>
                {
                    using var s = ScopeFactory.CreateScope();
                    await GetService<ITaskService>(s).CompleteAsync(id);
                };
                _listHost.Controls.Add(item);
                LayoutItem(item, _listHost.Controls.Count - 1);
            }

            _highlightId = null;
            if (tasks.Count == 0) ShowEmpty(); else ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void OnAppEvent(object? sender, AppEvent e)
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(() => OnAppEvent(sender, e)); return; }
        if (e.Kind is AppEventKind.TaskCreated or AppEventKind.TaskUpdated
            or AppEventKind.TaskDeleted or AppEventKind.TaskCompleted)
        {
            _highlightId = e.EntityId;
            _ = LoadAsync();
        }
    }

    private void SelectItem(TaskListItemControl item)
    {
        if (_selectedItem is not null) _selectedItem.Selected = false;
        _selectedItem = item;
        _selectedItem.Selected = true;
    }

    private void LayoutListItems()
    {
        for (var i = 0; i < _listHost.Controls.Count; i++)
            if (_listHost.Controls[i] is TaskListItemControl item)
                LayoutItem(item, i);
    }

    private void LayoutItem(TaskListItemControl item, int index)
    {
        item.Width = Math.Max(200, _listHost.ClientSize.Width - 4);
        item.Location = new Point(0, index * (item.Height + 4));
    }

    public async Task CreateTaskAsync()
    {
        using var form = TaskEditorForm.ForCreate(ScopeFactory, DateTime.Today);
        if (form.ShowDialog(FindForm()) != DialogResult.OK) return;
        _highlightId = form.ResultTask?.Id;
        await LoadAsync();
    }

    public async Task CompleteSelectedAsync()
    {
        if (_selectedItem is null) return;
        using var scope = ScopeFactory.CreateScope();
        await GetService<ITaskService>(scope).CompleteAsync(_selectedItem.TaskId);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _eventHandler is not null)
            _events.Published -= _eventHandler;
        base.Dispose(disposing);
    }
}
