using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Domain.Enums;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Dialogs;

public sealed class TaskEditorForm : ModalForm
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Guid? _taskId;
    private Guid? _projectId;
    private bool _isStarred;
    private int? _existingEstimate;

    private readonly TextBox _title = new();
    private readonly ComboBox _project = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _priority = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _hasDue = new() { Text = "Due date", AutoSize = true };
    private readonly DateTimePicker _due = new() { Format = DateTimePickerFormat.Short, Enabled = false };
    private readonly NumericUpDown _estimate = new() { Minimum = 0, Maximum = 24 * 60, Increment = 5 };
    private readonly TextBox _tags = new();
    private readonly Label _error = new() { AutoSize = false, Height = 36, Visible = false };
    private readonly ModernButton _save = new();
    private readonly ModernButton _cancel = new() { IsPrimary = false };
    private readonly Label _heading = new() { Font = UiMetrics.SectionTitle, AutoSize = true };

    public WorkTaskDto? ResultTask { get; private set; }

    public static TaskEditorForm ForCreate(IServiceScopeFactory scopeFactory, DateTime? defaultDue = null)
        => new(scopeFactory, null, defaultDue);

    public static TaskEditorForm ForEdit(IServiceScopeFactory scopeFactory, Guid taskId)
        => new(scopeFactory, taskId, null);

    private TaskEditorForm(IServiceScopeFactory scopeFactory, Guid? taskId, DateTime? defaultDue)
    {
        _scopeFactory = scopeFactory;
        _taskId = taskId;

        Text = taskId is null ? T("tasks.new") : T("tasks.edit");
        ClientSize = new Size(420, 360);
        Padding = new Padding(UiMetrics.Space16);

        foreach (TaskPriority p in Enum.GetValues(typeof(TaskPriority)))
            _priority.Items.Add(p);
        _priority.SelectedItem = TaskPriority.Medium;

        _hasDue.CheckedChanged += (_, _) => _due.Enabled = _hasDue.Checked;
        if (defaultDue.HasValue)
        {
            _hasDue.Checked = true;
            _due.Value = defaultDue.Value;
        }
        else if (taskId is null)
        {
            _hasDue.Checked = true;
            _due.Value = DateTime.Today;
        }

        _heading.Text = taskId is null ? T("tasks.new") : T("tasks.edit");
        _save.Text = taskId is null ? T("tasks.create") : T("tasks.saveChanges");
        _cancel.Text = T("common.cancel");
        _save.Height = UiMetrics.ButtonHeight;
        _cancel.Height = UiMetrics.ButtonHeight;
        _save.Width = 120;
        _cancel.Width = 90;
        _save.Click += async (_, _) => await SaveAsync();
        _cancel.Click += async (_, _) =>
        {
            if (!IsBusy) await AnimateOutAndCloseAsync(DialogResult.Cancel);
        };

        AcceptButton = _save;
        CancelButton = _cancel;

        BuildLayout();
        Load += async (_, _) => await LoadDataAsync();
    }

    private void BuildLayout()
    {
        var y = UiMetrics.Space16;
        void Place(Control c, int height = UiMetrics.InputHeight)
        {
            c.Left = UiMetrics.Space16;
            c.Top = y;
            c.Width = ClientSize.Width - UiMetrics.Space32;
            c.Height = height;
            Controls.Add(c);
            y += height + UiMetrics.Space8;
        }

        Place(_heading, 24);
        Place(MakeCaption(T("tasks.title")), 18);
        Place(_title);
        Place(MakeCaption(T("tasks.project")), 18);
        Place(_project);
        Place(MakeCaption(T("tasks.priority")), 18);
        Place(_priority);

        var dueRow = new Panel { Height = UiMetrics.InputHeight, Tag = "no-theme" };
        _hasDue.Left = 0;
        _hasDue.Top = 6;
        _due.Left = 100;
        _due.Width = 160;
        _due.Top = 0;
        dueRow.Controls.Add(_hasDue);
        dueRow.Controls.Add(_due);
        Place(dueRow);

        Place(MakeCaption(T("tasks.estimate")), 18);
        Place(_estimate);
        Place(MakeCaption(T("tasks.tags")), 18);
        Place(_tags);

        _error.ForeColor = ThemeManager.Instance.Current.Error;
        Place(_error, 36);

        _cancel.Left = ClientSize.Width - UiMetrics.Space16 - _cancel.Width;
        _cancel.Top = y + UiMetrics.Space8;
        _save.Left = _cancel.Left - UiMetrics.Space8 - _save.Width;
        _save.Top = _cancel.Top;
        Controls.Add(_save);
        Controls.Add(_cancel);

        ClientSize = new Size(420, _save.Bottom + UiMetrics.Space16);
    }

    private static Label MakeCaption(string text) => new()
    {
        Text = text,
        Font = UiMetrics.Caption,
        AutoSize = false,
        ForeColor = ThemeManager.Instance.Current.TextMuted
    };

    private async Task LoadDataAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var projects = await scope.ServiceProvider.GetRequiredService<IProjectService>().GetAllAsync();
        _project.Items.Clear();
        _project.Items.Add(new ProjectItem(null, "(None)"));
        foreach (var p in projects)
            _project.Items.Add(new ProjectItem(p.Id, p.Name));
        _project.SelectedIndex = 0;

        if (_taskId is null) return;

        var task = await scope.ServiceProvider.GetRequiredService<ITaskService>().GetByIdAsync(_taskId.Value);
        if (task is null) return;

        _title.Text = task.Title;
        _priority.SelectedItem = task.Priority;
        _projectId = task.ProjectId;
        _isStarred = task.IsStarred;
        _existingEstimate = task.EstimatedMinutes;
        if (task.EstimatedMinutes is int m)
            _estimate.Value = m;
        if (task.DueDate.HasValue)
        {
            _hasDue.Checked = true;
            _due.Value = task.DueDate.Value;
        }
        else
        {
            _hasDue.Checked = false;
        }

        for (var i = 0; i < _project.Items.Count; i++)
        {
            if (_project.Items[i] is ProjectItem pi && pi.Id == task.ProjectId)
            {
                _project.SelectedIndex = i;
                break;
            }
        }

        if (task.Tags.Count > 0)
            _tags.Text = string.Join(", ", task.Tags.Select(t => t.Name));
    }

    private async Task SaveAsync()
    {
        if (IsBusy) return;
        var title = _title.Text.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            ShowError(T("tasks.titleRequired"));
            _title.Focus();
            return;
        }

        IsBusy = true;
        _save.Enabled = false;
        _cancel.Enabled = false;
        _save.Text = _taskId is null ? T("tasks.creating") : T("tasks.saving");
        _error.Visible = false;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ITaskService>();
            var projectId = (_project.SelectedItem as ProjectItem)?.Id;
            var estimate = _estimate.Value > 0 ? (int?)_estimate.Value : null;
            var tags = _tags.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (_taskId is Guid id)
            {
                var current = await svc.GetByIdAsync(id)
                    ?? throw new KeyNotFoundException("Task was not found.");
                ResultTask = await svc.UpdateAsync(id, new UpdateTaskRequest
                {
                    Title = title,
                    Description = current.Description,
                    ProjectId = projectId,
                    Priority = (TaskPriority)(_priority.SelectedItem ?? TaskPriority.Medium),
                    DueDate = _hasDue.Checked ? _due.Value.Date : null,
                    EstimatedMinutes = estimate ?? current.EstimatedMinutes,
                    IsStarred = current.IsStarred,
                    Status = current.Status
                });
            }
            else
            {
                ResultTask = await svc.CreateAsync(new CreateTaskRequest
                {
                    Title = title,
                    ProjectId = projectId,
                    Priority = (TaskPriority)(_priority.SelectedItem ?? TaskPriority.Medium),
                    DueDate = _hasDue.Checked ? _due.Value.Date : null,
                    EstimatedMinutes = estimate,
                    TagNames = tags.Length > 0 ? tags : null
                });
            }

            await AnimateOutAndCloseAsync(DialogResult.OK);
        }
        catch (Exception ex)
        {
            ShowError(_taskId is null
                ? $"{T("tasks.createFailed")}\n{ex.Message}"
                : $"{T("tasks.updateFailed")}\n{ex.Message}");
            IsBusy = false;
            _save.Enabled = true;
            _cancel.Enabled = true;
            _save.Text = _taskId is null ? T("tasks.create") : T("tasks.saveChanges");
        }
    }

    private void ShowError(string message)
    {
        _error.Text = message;
        _error.Visible = true;
        _error.ForeColor = ThemeManager.Instance.Current.Error;
    }

    private sealed record ProjectItem(Guid? Id, string Name)
    {
        public override string ToString() => Name;
    }
}
