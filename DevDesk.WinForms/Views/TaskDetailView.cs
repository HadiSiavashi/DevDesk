using DevDesk.Application.Dtos;
using DevDesk.Application.Events;
using DevDesk.Application.Interfaces;
using DevDesk.Domain.Enums;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class TaskDetailView : ViewBase, ISaveableView
{
    private Guid _taskId;
    private Guid? _projectId;
    private bool _isStarred;
    private List<ChecklistItemDto> _checklistItems = [];
    private bool _bindingChecklist;
    private readonly IAppEventBus _events;
    private EventHandler<AppEvent>? _eventHandler;
    private readonly TextBox _title = new() { Dock = DockStyle.Top };
    private readonly TextBox _desc = new() { Dock = DockStyle.Top, Height = 80, Multiline = true };
    private readonly ComboBox _status = new() { Dock = DockStyle.Top };
    private readonly ComboBox _priority = new() { Dock = DockStyle.Top };
    private readonly CheckBox _hasDueDate = new() { Dock = DockStyle.Top, Text = "Has due date" };
    private readonly DateTimePicker _due = new() { Dock = DockStyle.Top, Format = DateTimePickerFormat.Short, Enabled = false };
    private readonly Label _timeLabel = new() { Dock = DockStyle.Top, Height = 20 };
    private readonly CheckedListBox _checklist = new() { Dock = DockStyle.Top, Height = 120 };
    private readonly ListBox _attachments = new() { Dock = DockStyle.Top, Height = 80 };
    private readonly FlowLayoutPanel _attachmentActions = new() { Dock = DockStyle.Top, Height = 36, FlowDirection = FlowDirection.LeftToRight };
    private readonly FlowLayoutPanel _actions = new() { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.LeftToRight };

    public TaskDetailView(IServiceScopeFactory scopeFactory, NavigationService navigation, object? parameter, IAppEventBus events)
        : base(scopeFactory, navigation)
    {
        _events = events;
        _taskId = parameter is Guid g ? g : Guid.Empty;
        foreach (WorkTaskStatus s in Enum.GetValues(typeof(WorkTaskStatus))) _status.Items.Add(s);
        foreach (TaskPriority p in Enum.GetValues(typeof(TaskPriority))) _priority.Items.Add(p);

        _hasDueDate.CheckedChanged += (_, _) => _due.Enabled = _hasDueDate.Checked;
        _checklist.ItemCheck += async (_, e) => await OnChecklistItemCheckAsync(e);

        var addAttachment = new ModernButton { Text = T("common.add"), IsPrimary = false };
        addAttachment.Click += async (_, _) => await AddAttachmentAsync();
        var openAttachment = new ModernButton { Text = T("common.open"), IsPrimary = false };
        openAttachment.Click += (_, _) => OpenSelectedAttachment();
        var deleteAttachment = new ModernButton { Text = T("common.delete"), IsPrimary = false };
        deleteAttachment.Click += async (_, _) => await DeleteAttachmentAsync();
        _attachmentActions.Controls.AddRange([addAttachment, openAttachment, deleteAttachment]);

        var save = new ModernButton { Text = T("common.save") };
        save.Click += async (_, _) => await SaveAsync();
        var complete = new ModernButton { Text = T("common.complete"), IsPrimary = false };
        complete.Click += async (_, _) => { using var s = ScopeFactory.CreateScope(); await GetService<ITaskService>(s).CompleteAsync(_taskId); await LoadAsync(); };
        var focus = new ModernButton { Text = T("tasks.startFocus"), IsPrimary = false };
        focus.Click += async (_, _) =>
        {
            using var s = ScopeFactory.CreateScope();
            await GetService<IFocusService>(s).StartAsync(new StartFocusRequest { TaskId = _taskId });
            Navigation.Navigate("focus");
        };
        var dup = new ModernButton { Text = T("common.duplicate"), IsPrimary = false };
        dup.Click += async (_, _) =>
        {
            using var s = ScopeFactory.CreateScope();
            var t = await GetService<ITaskService>(s).DuplicateAsync(_taskId);
            Navigation.Navigate("task-detail", t.Id);
        };
        var del = new ModernButton { Text = T("common.delete"), IsPrimary = false };
        del.Click += async (_, _) =>
        {
            if (!Dialogs.ConfirmDialog.Show(T("common.confirm"), T("common.delete"))) return;
            using var s = ScopeFactory.CreateScope();
            await GetService<ITaskService>(s).DeleteAsync(_taskId);
            Navigation.NavigateBack();
        };
        var back = new ModernButton { Text = T("common.back"), IsPrimary = false };
        back.Click += (_, _) => Navigation.NavigateBack();
        _actions.Controls.AddRange([save, complete, focus, dup, del, back]);

        ContentPanel.Controls.Add(_actions);
        ContentPanel.Controls.Add(_attachmentActions);
        ContentPanel.Controls.Add(_attachments);
        ContentPanel.Controls.Add(new Label { Text = "Attachments", Dock = DockStyle.Top, Height = 20 });
        ContentPanel.Controls.Add(_checklist);
        ContentPanel.Controls.Add(new Label { Text = T("tasks.checklist"), Dock = DockStyle.Top, Height = 20 });
        ContentPanel.Controls.Add(_timeLabel);
        ContentPanel.Controls.Add(_due);
        ContentPanel.Controls.Add(_hasDueDate);
        ContentPanel.Controls.Add(_priority);
        ContentPanel.Controls.Add(new Label { Text = T("tasks.priority"), Dock = DockStyle.Top, Height = 20 });
        ContentPanel.Controls.Add(_status);
        ContentPanel.Controls.Add(new Label { Text = T("tasks.status"), Dock = DockStyle.Top, Height = 20 });
        ContentPanel.Controls.Add(_desc);
        ContentPanel.Controls.Add(new Label { Text = T("tasks.description"), Dock = DockStyle.Top, Height = 20 });
        ContentPanel.Controls.Add(_title);
        ContentPanel.Controls.Add(new Label { Text = T("tasks.title"), Dock = DockStyle.Top, Height = 20 });

        _eventHandler = OnAppEvent;
        _events.Published += _eventHandler;
    }

    private void OnAppEvent(object? sender, AppEvent e)
    {
        if (IsDisposed || e.EntityId != _taskId) return;
        if (InvokeRequired)
        {
            BeginInvoke(() => OnAppEvent(sender, e));
            return;
        }

        if (e.Kind is AppEventKind.TaskUpdated or AppEventKind.TaskCompleted)
            _ = LoadAsync();
        else if (e.Kind == AppEventKind.TaskDeleted)
            Navigation.NavigateBack();
    }

    protected override async Task LoadAsync()
    {
        if (_taskId == Guid.Empty) { ShowEmpty(); return; }
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var task = await GetService<ITaskService>(scope).GetByIdAsync(_taskId);
            if (task is null) { ShowEmpty(); return; }
            _title.Text = task.Title;
            _desc.Text = task.Description ?? "";
            _status.SelectedItem = task.Status;
            _priority.SelectedItem = task.Priority;
            _projectId = task.ProjectId;
            _isStarred = task.IsStarred;
            _hasDueDate.Checked = task.DueDate.HasValue;
            _due.Enabled = task.DueDate.HasValue;
            if (task.DueDate.HasValue) _due.Value = task.DueDate.Value;
            _timeLabel.Text = $"Estimate: {task.EstimatedMinutes?.ToString() ?? "—"} min | Actual: {task.ActualMinutes} min";

            _checklistItems = task.ChecklistItems.ToList();
            _bindingChecklist = true;
            _checklist.Items.Clear();
            foreach (var c in _checklistItems)
                _checklist.Items.Add(c.Title, c.IsCompleted);
            _bindingChecklist = false;

            var attachments = await GetService<IAttachmentService>(scope).GetForTaskAsync(_taskId);
            _attachments.DataSource = attachments.ToList();
            _attachments.DisplayMember = "FileName";

            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async Task OnChecklistItemCheckAsync(ItemCheckEventArgs e)
    {
        if (_bindingChecklist || e.Index < 0 || e.Index >= _checklistItems.Count)
            return;

        var item = _checklistItems[e.Index];
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var updated = await GetService<ITaskService>(scope).ToggleChecklistItemAsync(_taskId, item.Id);
            _checklistItems[e.Index] = updated;
        }
        catch (Exception ex)
        {
            _bindingChecklist = true;
            _checklist.SetItemChecked(e.Index, item.IsCompleted);
            _bindingChecklist = false;
            MessageBox.Show(ex.Message, T("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task AddAttachmentAsync()
    {
        using var dlg = new OpenFileDialog { Title = "Add attachment", Multiselect = false };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        try
        {
            using var scope = ScopeFactory.CreateScope();
            await GetService<IAttachmentService>(scope).AddForTaskAsync(_taskId, dlg.FileName);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, T("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void OpenSelectedAttachment()
    {
        if (_attachments.SelectedItem is not AttachmentDto a) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = a.FilePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, T("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task DeleteAttachmentAsync()
    {
        if (_attachments.SelectedItem is not AttachmentDto a) return;
        if (!Dialogs.ConfirmDialog.Show(T("common.confirm"), T("common.delete"))) return;
        using var scope = ScopeFactory.CreateScope();
        await GetService<IAttachmentService>(scope).DeleteAsync(a.Id);
        await LoadAsync();
    }

    public async Task SaveAsync()
    {
        using var scope = ScopeFactory.CreateScope();
        var task = await GetService<ITaskService>(scope).GetByIdAsync(_taskId);
        await GetService<ITaskService>(scope).UpdateAsync(_taskId, new UpdateTaskRequest
        {
            Title = _title.Text,
            Description = _desc.Text,
            ProjectId = _projectId ?? task?.ProjectId,
            Status = (WorkTaskStatus)(_status.SelectedItem ?? WorkTaskStatus.Todo),
            Priority = (TaskPriority)(_priority.SelectedItem ?? TaskPriority.Medium),
            DueDate = _hasDueDate.Checked ? _due.Value : null,
            EstimatedMinutes = task?.EstimatedMinutes,
            IsStarred = _isStarred || (task?.IsStarred ?? false)
        });
        await LoadAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _eventHandler is not null)
            _events.Published -= _eventHandler;
        base.Dispose(disposing);
    }
}
