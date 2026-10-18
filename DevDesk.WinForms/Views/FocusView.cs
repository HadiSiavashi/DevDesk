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

/// <summary>Focus Command Center — current session + today's tasks + daily summary.</summary>
public sealed class FocusView : ViewBase
{
    private FocusSessionDto? _session;
    private Guid? _highlightTaskId;
    private bool _pomodoroCompletedNotified;
    private readonly IAppEventBus _events;
    private EventHandler<AppEvent>? _eventHandler;

    private readonly SplitContainer _split = new()
    {
        Dock = DockStyle.Fill,
        Orientation = Orientation.Vertical,
        SplitterWidth = 6
    };

    private readonly Panel _left = new() { Dock = DockStyle.Fill, Tag = "no-theme" };
    private readonly FlowLayoutPanel _leftFlow = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        Tag = "no-theme",
        Padding = new Padding(UiMetrics.Space16)
    };
    private readonly Panel _right = new() { Dock = DockStyle.Fill, Padding = new Padding(UiMetrics.Space12), Tag = "no-theme" };
    private readonly Panel _summary = new() { Dock = DockStyle.Bottom, Height = UiMetrics.StatusBarHeight, Padding = new Padding(UiMetrics.Space16, 0, UiMetrics.Space16, 0), Tag = "no-theme" };

    private readonly Label _modeLabel = new() { Font = UiMetrics.SectionTitle, AutoSize = true, Margin = new Padding(0, 0, 0, UiMetrics.Space4) };
    private readonly Label _taskTitle = new() { Font = UiMetrics.PageTitle, AutoSize = true, Margin = new Padding(0, 0, 0, UiMetrics.Space4) };
    private readonly Label _taskMeta = new() { Font = UiMetrics.Meta, AutoSize = true, Margin = new Padding(0, 0, 0, UiMetrics.Space16) };
    private readonly TimerDisplay _timer = new() { Height = UiScale.Px(90), Font = UiMetrics.Timer, Margin = new Padding(0, 0, 0, UiMetrics.Space16) };
    private readonly FlowLayoutPanel _actions = new() { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0) };

    private readonly Label _headerRight = new() { Text = "Today's Task Board", Font = UiMetrics.SectionTitle, Dock = DockStyle.Top, Height = UiMetrics.LineTitle };
    private readonly Panel _taskList = new() { Dock = DockStyle.Fill, AutoScroll = true, Tag = "no-theme" };
    private readonly ModernButton _addTask = new() { Text = "+ Add task", IsPrimary = false, Height = UiMetrics.ButtonHeight, Dock = DockStyle.Bottom };

    private readonly Label _summaryLabel = new() { Dock = DockStyle.Fill, Font = UiMetrics.Body, TextAlign = ContentAlignment.MiddleLeft };
    private readonly System.Windows.Forms.Timer _tick = new() { Interval = 1000 };

    private readonly ModernButton _btnStart = new() { Text = "Start Focus", Icon = "play_arrow", Width = UiScale.Px(260), Height = UiMetrics.ButtonHeight, AutoFit = false };
    private readonly ModernButton _btnPomodoro = new() { Text = "Start Pomodoro", Icon = "timelapse", Shortcut = "25m", Variant = ButtonVariant.Outline, Width = UiScale.Px(260), Height = UiMetrics.ButtonHeight, AutoFit = false };
    private readonly ModernButton _btnPause = new() { Text = "Pause", Variant = ButtonVariant.Outline, Width = UiScale.Px(260), Height = UiMetrics.ButtonHeight, AutoFit = false };
    private readonly ModernButton _btnResume = new() { Text = "Resume", Variant = ButtonVariant.Outline, Width = UiScale.Px(260), Height = UiMetrics.ButtonHeight, AutoFit = false };
    private readonly ModernButton _btnStop = new() { Text = "Stop", Variant = ButtonVariant.Ghost, Width = UiScale.Px(260), Height = UiMetrics.ButtonHeight, AutoFit = false };

    private Guid? _selectedTaskId;

    public FocusView(IServiceScopeFactory scopeFactory, NavigationService navigation, IAppEventBus events)
        : base(scopeFactory, navigation)
    {
        _events = events;
        _tick.Tick += (_, _) => UpdateTimer();

        _modeLabel.Text = $"●  {T("focus.ready").ToUpperInvariant()}";
        _taskTitle.Text = T("focus.readyToFocus");
        _taskMeta.Text = T("focus.startPrompt");
        _headerRight.Text = T("focus.todayTasks");
        _addTask.Text = T("focus.addTask");
        _btnStart.Text = T("focus.startFocus");
        _btnPomodoro.Text = T("focus.startPomodoro");
        _btnPause.Text = T("focus.pause");
        _btnResume.Text = T("focus.resume");
        _btnStop.Text = T("focus.stop");

        _btnStart.Click += async (_, _) => await StartFocusAsync(false);
        _btnPomodoro.Click += async (_, _) => await StartFocusAsync(true);
        _btnPause.Click += async (_, _) => await PauseAsync();
        _btnResume.Click += async (_, _) => await ResumeAsync();
        _btnStop.Click += async (_, _) => await StopAsync();
        _addTask.Click += async (_, _) => await AddTaskAsync();
        _actions.Controls.AddRange([_btnStart, _btnPomodoro, _btnPause, _btnResume, _btnStop]);

        BuildLeft();
        BuildRight();
        _summary.Controls.Add(_summaryLabel);

        var leftCard = new CardPanel { Dock = DockStyle.Fill };
        _left.Dock = DockStyle.Fill;
        leftCard.Controls.Add(_left);
        _split.Panel1.Controls.Add(leftCard);
        var rightCard = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(UiMetrics.Space12) };
        _right.Dock = DockStyle.Fill;
        rightCard.Controls.Add(_right);
        _split.Panel2.Controls.Add(rightCard);
        ContentPanel.Controls.Add(_split);
        ContentPanel.Controls.Add(_summary);

        _eventHandler = OnAppEvent;
        _events.Published += _eventHandler;

        _split.SizeChanged += (_, _) => ApplySplitLayout();
        HandleCreated += (_, _) => ApplySplitLayout();
        Resize += (_, _) => ApplySplitLayout();
    }

    private void ApplySplitLayout()
    {
        if (_split.IsDisposed || !_split.IsHandleCreated)
            return;

        var width = _split.ClientSize.Width;
        if (width < 120)
            return;

        // Clear mins first so WinForms won't throw while recalculating SplitterDistance.
        _split.Panel1MinSize = 0;
        _split.Panel2MinSize = 0;

        var splitter = Math.Max(1, _split.SplitterWidth);
        var panel1Min = Math.Min(220, Math.Max(120, width / 5));
        var panel2Min = Math.Min(240, Math.Max(140, width / 5));
        if (panel1Min + panel2Min + splitter >= width)
        {
            panel1Min = Math.Max(80, (width - splitter) / 3);
            panel2Min = Math.Max(80, (width - splitter) - panel1Min);
        }

        var maxDistance = width - panel2Min - splitter;
        var minDistance = panel1Min;
        if (maxDistance < minDistance)
            return;

        var desired = (int)(width * 0.60);
        try
        {
            _split.SplitterDistance = Math.Clamp(desired, minDistance, maxDistance);
            _split.Panel1MinSize = panel1Min;
            _split.Panel2MinSize = panel2Min;
        }
        catch (InvalidOperationException)
        {
            // Layout can race during dock/resize; next SizeChanged will retry.
        }
    }

    private void BuildLeft()
    {
        foreach (ModernButton btn in new[] { _btnStart, _btnPomodoro, _btnPause, _btnResume, _btnStop })
        {
            btn.Margin = new Padding(0, 0, 0, UiMetrics.Space8);
            btn.Width = UiScale.Px(260);
            btn.Height = UiMetrics.ButtonHeight;
        }

        _leftFlow.Controls.Add(_modeLabel);
        _leftFlow.Controls.Add(_taskTitle);
        _leftFlow.Controls.Add(_taskMeta);
        _leftFlow.Controls.Add(_timer);
        _leftFlow.Controls.Add(_actions);

        _left.Controls.Add(_leftFlow);
        _left.Resize += (_, _) =>
        {
            var availW = Math.Max(UiScale.Px(200), _leftFlow.ClientSize.Width - UiScale.Px(32));
            _taskTitle.MaximumSize = new Size(availW, 0);
            _taskMeta.MaximumSize = new Size(availW, 0);
            _timer.Width = availW;
        };
    }

    private void BuildRight()
    {
        _right.Controls.Add(_taskList);
        _right.Controls.Add(_addTask);
        _right.Controls.Add(_headerRight);
        _taskList.Resize += (_, _) => LayoutTaskRows();
    }

    protected override async Task LoadAsync()
    {
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var focusSvc = GetService<IFocusService>(scope);
            try
            {
                _session = await focusSvc.GetActiveAsync()
                    ?? await focusSvc.RecoverActiveOnStartupAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, T("focus.start"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            _pomodoroCompletedNotified = _session?.Pomodoro?.Completed == true;
            await ReloadTasksAsync();
            await ReloadSummaryAsync();
            UpdateTimer();
            _tick.Start();
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async Task ReloadTasksAsync()
    {
        using var scope = ScopeFactory.CreateScope();
        var tasks = await GetService<ITaskService>(scope).GetMyDayTasksAsync(_session?.TaskId);
        _taskList.Controls.Clear();
        if (tasks.Count == 0)
        {
            var empty = new EmptyStatePanel
            {
                TitleText = T("focus.noTasksToday"),
                Icon = "check_circle_outline",
                Dock = DockStyle.Fill
            };
            _taskList.Controls.Add(empty);
            return;
        }

        foreach (var task in tasks)
        {
            var item = CreateTaskRow(task);
            _taskList.Controls.Add(item);
        }
        LayoutTaskRows();
    }

    private TaskListItemControl CreateTaskRow(TaskListItemDto task)
    {
        var item = new TaskListItemControl
        {
            Width = Math.Max(200, _taskList.ClientSize.Width - 8),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        item.Bind(task);
        if (_highlightTaskId == task.Id)
            item.FlashHighlight();
        if (_selectedTaskId == task.Id)
            item.Selected = true;

        item.ItemClicked += (_, id) =>
        {
            _selectedTaskId = id;
            foreach (Control c in _taskList.Controls)
            {
                if (c is TaskListItemControl row)
                    row.Selected = row.TaskId == id;
            }
        };
        item.CompleteClicked += async (_, id) => await CompleteTaskAsync(id);
        item.DoubleClick += async (_, _) => await EditTaskAsync(task.Id);
        item.ContextMenuStrip = BuildTaskMenu(task);
        return item;
    }

    private ContextMenuStrip BuildTaskMenu(TaskListItemDto task)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Start Focus", null, async (_, _) =>
        {
            _selectedTaskId = task.Id;
            await StartFocusAsync(false);
        });
        menu.Items.Add("Edit", null, async (_, _) => await EditTaskAsync(task.Id));
        menu.Items.Add("Complete", null, async (_, _) => await CompleteTaskAsync(task.Id));
        var priority = new ToolStripMenuItem("Priority");
        foreach (TaskPriority p in Enum.GetValues(typeof(TaskPriority)))
        {
            var captured = p;
            priority.DropDownItems.Add(p.ToString(), null, async (_, _) =>
            {
                using var s = ScopeFactory.CreateScope();
                await GetService<ITaskService>(s).ChangePriorityAsync(task.Id, captured);
            });
        }
        menu.Items.Add(priority);
        var status = new ToolStripMenuItem("Status");
        foreach (WorkTaskStatus st in Enum.GetValues(typeof(WorkTaskStatus)))
        {
            if (st == WorkTaskStatus.Cancelled) continue;
            var captured = st;
            status.DropDownItems.Add(st.ToString(), null, async (_, _) =>
            {
                using var s = ScopeFactory.CreateScope();
                await GetService<ITaskService>(s).ChangeStatusAsync(task.Id, captured);
            });
        }
        menu.Items.Add(status);
        menu.Items.Add("Delete", null, async (_, _) => await DeleteTaskAsync(task.Id));
        return menu;
    }

    private void LayoutTaskRows()
    {
        for (var i = 0; i < _taskList.Controls.Count; i++)
        {
            if (_taskList.Controls[i] is TaskListItemControl item)
            {
                item.Width = Math.Max(200, _taskList.ClientSize.Width - 8);
                item.Location = new Point(0, i * (item.Height + 4));
            }
        }
    }

    private async Task ReloadSummaryAsync()
    {
        using var scope = ScopeFactory.CreateScope();
        var tasks = await GetService<ITaskService>(scope).GetMyDayTasksAsync(_session?.TaskId);
        var focusHistory = await GetService<IFocusService>(scope).GetHistoryAsync(DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today));
        var focusedMin = focusHistory.Sum(s => s.DurationMinutes > 0 ? s.DurationMinutes : s.ElapsedMinutes);
        if (_session is { IsActive: true })
            focusedMin += _session.ElapsedMinutes;

        var open = tasks.Count(t => t.Status != WorkTaskStatus.Done);
        var done = tasks.Count(t => t.Status == WorkTaskStatus.Done);
        var total = open + done;
        var pct = total == 0 ? 0 : (int)Math.Round(100.0 * done / total);
        _summaryLabel.Text = $"Focused {FormatMinutes(focusedMin)}     Completed {done}/{total}     Remaining {open}     {pct}%";
        _summary.BackColor = ThemeManager.Instance.Current.Surface;
        _summaryLabel.ForeColor = ThemeManager.Instance.Current.TextSecondary;
    }

    private static string FormatMinutes(int minutes)
    {
        var h = minutes / 60;
        var m = minutes % 60;
        return h > 0 ? $"{h}h{m:D2}m" : $"{m}m";
    }

    private void UpdateTimer()
    {
        var c = ThemeManager.Instance.Current;
        if (_session is null || !_session.IsActive)
        {
            _modeLabel.Text = $"●  {T("focus.ready").ToUpperInvariant()}";
            _modeLabel.ForeColor = c.TextMuted;
            _timer.SetTime(TimeSpan.Zero);
            _taskTitle.Text = _selectedTaskId is null ? T("focus.readyToFocus") : T("focus.readyToStart");
            _taskMeta.Text = T("focus.startPrompt");
            _taskTitle.ForeColor = c.TextPrimary;
            _taskMeta.ForeColor = c.TextMuted;
            UpdateActionButtons();
            return;
        }

        var secs = ComputeElapsedSeconds(_session);
        _timer.SetTime(TimeSpan.FromSeconds(secs));
        _taskTitle.Text = _session.TaskTitle ?? _session.ProjectName ?? T("nav.focus");
        _taskMeta.Text = _session.SessionType == FocusSessionType.Pomodoro ? T("focus.pomodoro") : T("focus.deepWork");
        _taskTitle.ForeColor = c.TextPrimary;
        _taskMeta.ForeColor = c.TextSecondary;

        if (_session.Pomodoro?.IsBreak == true)
        {
            _modeLabel.Text = $"●  {T("focus.break").ToUpperInvariant()}";
            _modeLabel.ForeColor = c.Tertiary;
        }
        else if (_session.IsPaused)
        {
            _modeLabel.Text = $"●  {T("focus.paused").ToUpperInvariant()}";
            _modeLabel.ForeColor = c.Warning;
        }
        else
        {
            _modeLabel.Text = $"●  {T("focus.focusing").ToUpperInvariant()}";
            _modeLabel.ForeColor = c.Accent;
        }

        UpdateActionButtons();
        _ = TryCompletePomodoroAsync(secs);
    }

    private void UpdateActionButtons()
    {
        var ready = _session is null || !_session.IsActive;
        var paused = _session is { IsActive: true, IsPaused: true };
        var onBreak = _session?.Pomodoro?.IsBreak == true;
        _btnStart.Visible = ready;
        _btnPomodoro.Visible = ready;
        _btnPause.Visible = !ready && !paused && !onBreak;
        _btnResume.Visible = paused;
        _btnStop.Visible = !ready;
    }

    private static int ComputeElapsedSeconds(FocusSessionDto session)
    {
        var now = DateTime.UtcNow;
        var end = session.EndedAt ?? now;
        var secs = (int)(end - session.StartedAt).TotalSeconds - session.PausedAccumulatedSeconds;
        if (session.IsPaused && session.PausedAt is DateTime p)
            secs -= (int)(now - p).TotalSeconds;
        return Math.Max(0, secs);
    }

    private async Task TryCompletePomodoroAsync(int elapsedSeconds)
    {
        if (_session is null || _pomodoroCompletedNotified) return;
        var pomodoro = _session.Pomodoro;
        if (_session.SessionType != FocusSessionType.Pomodoro || pomodoro is null || pomodoro.IsBreak) return;
        if (elapsedSeconds < pomodoro.WorkDurationMinutes * 60 || _session.IsPaused) return;

        _pomodoroCompletedNotified = true;
        try
        {
            using var scope = ScopeFactory.CreateScope();
            _session = await GetService<IFocusService>(scope).CompletePomodoroAsync(_session.Id);
            System.Media.SystemSounds.Asterisk.Play();
            await GetService<INotificationService>(scope).ShowAsync(
                NotificationCategory.PomodoroFinished,
                "Pomodoro complete",
                "Great work! Your pomodoro session is finished.");
            UpdateTimer();
            await ReloadSummaryAsync();
        }
        catch
        {
            _pomodoroCompletedNotified = false;
        }
    }

    private async Task StartFocusAsync(bool pomodoro)
    {
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var focus = GetService<IFocusService>(scope);
            var active = await focus.GetActiveAsync();
            var targetTaskId = _selectedTaskId ?? _session?.TaskId;

            if (active is { IsActive: true })
            {
                // Same deep-work session on the same task — ignore duplicate Start.
                if (!pomodoro && targetTaskId is Guid tid && active.TaskId == tid)
                    return;

                // Already in a pomodoro on the same task — ignore.
                if (pomodoro && active.SessionType == FocusSessionType.Pomodoro
                    && targetTaskId is Guid ptid && active.TaskId == ptid && active.IsActive)
                    return;

                var currentLabel = active.TaskTitle ?? "Current session";
                var newLabel = "New session";
                if (targetTaskId is Guid nid)
                {
                    var t = await GetService<ITaskService>(scope).GetByIdAsync(nid);
                    newLabel = t?.Title ?? newLabel;
                }
                else if (pomodoro && active.TaskId is null)
                {
                    // Switching deep work → pomodoro without a task still needs confirmation.
                    newLabel = "Pomodoro";
                }

                if (!ConfirmDialog.ShowDetailed(
                        "Switch focus?",
                        "An active focus session is running.",
                        $"Current: {currentLabel}",
                        $"New: {newLabel}"))
                    return;

                await focus.StopAsync(active.Id);
                _session = null;
            }

            _pomodoroCompletedNotified = false;
            _session = pomodoro
                ? await focus.StartPomodoroAsync(new StartPomodoroRequest { TaskId = targetTaskId })
                : await focus.StartAsync(new StartFocusRequest { TaskId = targetTaskId });
            UpdateTimer();
            await ReloadTasksAsync();
            await ReloadSummaryAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, T("focus.start"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task PauseAsync()
    {
        if (_session is null) return;
        using var scope = ScopeFactory.CreateScope();
        _session = await GetService<IFocusService>(scope).PauseAsync(_session.Id);
        UpdateTimer();
    }

    private async Task ResumeAsync()
    {
        if (_session is null) return;
        using var scope = ScopeFactory.CreateScope();
        _session = await GetService<IFocusService>(scope).ResumeAsync(_session.Id);
        UpdateTimer();
    }

    private async Task StopAsync()
    {
        if (_session is null) return;
        using var scope = ScopeFactory.CreateScope();
        await GetService<IFocusService>(scope).StopAsync(_session.Id);
        _session = null;
        _pomodoroCompletedNotified = false;
        UpdateTimer();
        await ReloadSummaryAsync();
        await ReloadTasksAsync();
    }

    private async Task AddTaskAsync()
    {
        using var form = TaskEditorForm.ForCreate(ScopeFactory, DateTime.Today);
        if (form.ShowDialog(FindForm()) != DialogResult.OK || form.ResultTask is null)
            return;
        _highlightTaskId = form.ResultTask.Id;
        // Event bus will also refresh; ensure local highlight
        await ReloadTasksAsync();
        await ReloadSummaryAsync();
        UpdateTimer(); // timer must stay unaffected
    }

    private async Task EditTaskAsync(Guid taskId)
    {
        using var form = TaskEditorForm.ForEdit(ScopeFactory, taskId);
        if (form.ShowDialog(FindForm()) != DialogResult.OK)
            return;
        _highlightTaskId = taskId;
        await ReloadTasksAsync();
        UpdateTimer();
    }

    private async Task CompleteTaskAsync(Guid taskId)
    {
        using var scope = ScopeFactory.CreateScope();
        await GetService<ITaskService>(scope).CompleteAsync(taskId);
        // Keep focus session if completing the active task
        await ReloadTasksAsync();
        await ReloadSummaryAsync();
        UpdateTimer();
    }

    private async Task DeleteTaskAsync(Guid taskId)
    {
        if (!ConfirmDialog.Show(T("common.confirm"), T("common.delete"))) return;
        using var scope = ScopeFactory.CreateScope();
        await GetService<ITaskService>(scope).DeleteAsync(taskId);
        if (_selectedTaskId == taskId) _selectedTaskId = null;
        await ReloadTasksAsync();
        await ReloadSummaryAsync();
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
            case AppEventKind.TaskCreated:
            case AppEventKind.TaskUpdated:
            case AppEventKind.TaskDeleted:
            case AppEventKind.TaskCompleted:
                _highlightTaskId = e.EntityId;
                _ = RefreshBoardQuietAsync();
                break;
            case AppEventKind.FocusStarted:
            case AppEventKind.FocusPaused:
            case AppEventKind.FocusResumed:
                if (e.Payload is FocusSessionDto session)
                    _session = session;
                else
                    _ = RefreshSessionAsync();
                UpdateTimer();
                break;
            case AppEventKind.FocusStopped:
                _session = null;
                UpdateTimer();
                _ = ReloadSummaryAsync();
                break;
        }
    }

    private async Task RefreshBoardQuietAsync()
    {
        try
        {
            await ReloadTasksAsync();
            await ReloadSummaryAsync();
        }
        catch { /* ignore */ }
    }

    private async Task RefreshSessionAsync()
    {
        try
        {
            using var scope = ScopeFactory.CreateScope();
            _session = await GetService<IFocusService>(scope).GetActiveAsync();
            UpdateTimer();
        }
        catch { /* ignore */ }
    }

    protected override void OnThemeChanged()
    {
        base.OnThemeChanged();
        var c = ThemeManager.Instance.Current;
        _left.BackColor = c.Surface;
        _leftFlow.BackColor = c.Surface;
        _right.BackColor = c.Background;
        _summary.BackColor = c.Surface;
        _headerRight.ForeColor = c.TextMuted;
        _taskTitle.ForeColor = c.TextPrimary;
        UpdateTimer();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_eventHandler is not null)
                _events.Published -= _eventHandler;
            _tick.Stop();
            _tick.Dispose();
        }
        base.Dispose(disposing);
    }
}
