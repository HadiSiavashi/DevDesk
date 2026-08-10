using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Domain.Enums;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class CalendarView : ViewBase
{
    private enum ViewMode { Month, Week, Day }

    private DateTime _selected = DateTime.Today;
    private ViewMode _mode = ViewMode.Month;
    private readonly MonthCalendar _calendar = new() { Dock = DockStyle.Left, Width = 260 };
    private readonly ListBox _events = new() { Dock = DockStyle.Fill };
    private readonly FlowLayoutPanel _modeBar = new() { Dock = DockStyle.Top, Height = 36, FlowDirection = FlowDirection.LeftToRight };
    private readonly FlowLayoutPanel _actions = new() { Dock = DockStyle.Bottom, Height = 40 };

    public CalendarView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        _calendar.DateSelected += async (_, e) => { _selected = e.Start; await LoadEventsAsync(); };
        _events.DoubleClick += (_, _) => OpenSelectedEvent();

        foreach (var (mode, label) in new[] { (ViewMode.Month, "Month"), (ViewMode.Week, "Week"), (ViewMode.Day, "Day") })
        {
            var rb = new RadioButton { Text = label, AutoSize = true, Tag = mode, Checked = mode == ViewMode.Month };
            rb.CheckedChanged += async (_, _) =>
            {
                if (!rb.Checked) return;
                _mode = mode;
                await LoadEventsAsync();
            };
            _modeBar.Controls.Add(rb);
        }

        var add = new ModernButton { Text = T("common.add") };
        add.Click += async (_, _) => await AddEventAsync();
        var edit = new ModernButton { Text = T("common.edit"), IsPrimary = false };
        edit.Click += async (_, _) => await EditEventAsync();
        var del = new ModernButton { Text = T("common.delete"), IsPrimary = false };
        del.Click += async (_, _) => await DeleteEventAsync();
        _actions.Controls.AddRange([add, edit, del]);

        ContentPanel.Controls.Add(_events);
        ContentPanel.Controls.Add(_actions);
        ContentPanel.Controls.Add(_modeBar);
        ContentPanel.Controls.Add(_calendar);
    }

    protected override async Task LoadAsync() => await LoadEventsAsync();

    private (DateTime From, DateTime To) GetRange()
    {
        var day = _selected.Date;
        return _mode switch
        {
            ViewMode.Week =>
            (
                day.AddDays(-(int)day.DayOfWeek),
                day.AddDays(-(int)day.DayOfWeek + 6).AddDays(1).AddTicks(-1)
            ),
            ViewMode.Day => (day, day.AddDays(1).AddTicks(-1)),
            _ => (day, day.AddDays(1).AddTicks(-1))
        };
    }

    private async Task LoadEventsAsync()
    {
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var (from, to) = GetRange();
            var events = await GetService<ICalendarService>(scope).GetRangeAsync(from, to);
            _events.DataSource = events.Select(e => new CalendarEventListItem(e)).ToList();
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void OpenSelectedEvent()
    {
        if (_events.SelectedItem is not CalendarEventListItem item) return;
        if (item.Event.EventType == CalendarEventType.Deadline && item.Event.TaskId is Guid taskId)
            Navigation.Navigate("task-detail", taskId);
    }

    private static bool IsSynthetic(CalendarEventDto ev) => ev.Id == Guid.Empty;

    private async Task AddEventAsync()
    {
        var title = Dialogs.InputDialog.Show(T("common.create"), "Event title:");
        if (string.IsNullOrWhiteSpace(title)) return;
        using var scope = ScopeFactory.CreateScope();
        await GetService<ICalendarService>(scope).CreateAsync(new CreateCalendarEventRequest
        {
            Title = title,
            StartAt = _selected.Date.AddHours(9),
            EndAt = _selected.Date.AddHours(10),
            EventType = CalendarEventType.Meeting
        });
        await LoadEventsAsync();
    }

    private async Task EditEventAsync()
    {
        if (_events.SelectedItem is not CalendarEventListItem { Event: var ev }) return;
        if (IsSynthetic(ev))
        {
            MessageBox.Show("Task deadlines cannot be edited here. Open the task instead.", T("common.edit"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var title = Dialogs.InputDialog.Show(T("common.edit"), "Title:", ev.Title);
        if (string.IsNullOrWhiteSpace(title)) return;
        using var scope = ScopeFactory.CreateScope();
        await GetService<ICalendarService>(scope).UpdateAsync(ev.Id, new UpdateCalendarEventRequest
        {
            Title = title,
            StartAt = ev.StartAt,
            EndAt = ev.EndAt,
            EventType = ev.EventType
        });
        await LoadEventsAsync();
    }

    private async Task DeleteEventAsync()
    {
        if (_events.SelectedItem is not CalendarEventListItem { Event: var ev }) return;
        if (IsSynthetic(ev))
        {
            MessageBox.Show("Task deadlines cannot be deleted here. Edit the task due date instead.", T("common.delete"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!Dialogs.ConfirmDialog.Show(T("common.confirm"), T("common.delete"))) return;
        using var scope = ScopeFactory.CreateScope();
        await GetService<ICalendarService>(scope).DeleteAsync(ev.Id);
        await LoadEventsAsync();
    }

    private sealed class CalendarEventListItem
    {
        public CalendarEventDto Event { get; }

        public CalendarEventListItem(CalendarEventDto ev) => Event = ev;

        public override string ToString() => $"{Event.EventType}: {Event.Title}";
    }
}
