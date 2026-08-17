using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Domain.Enums;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Dialogs;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class CalendarView : ViewBase
{
    private enum ViewMode { Month, Week, Day }

    private DateTime _cursor = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _selected = DateTime.Today;
    private ViewMode _mode = ViewMode.Month;
    private IReadOnlyList<CalendarEventDto> _events = [];
    private readonly Panel _grid = new() { Dock = DockStyle.Fill, Tag = "no-theme" };
    private readonly ListBox _dayList = new() { Dock = DockStyle.Fill, IntegralHeight = false, BorderStyle = BorderStyle.None };
    private readonly PageHeader _header = new();
    private readonly SegmentedTabs _modes = new() { Width = UiScale.Px(220) };

    public CalendarView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        _header.TitleText = T("nav.calendar");
        var prev = new IconButton { Icon = "chevron_left" };
        var next = new IconButton { Icon = "chevron_right" };
        var today = new ModernButton { Text = "Today", Variant = ButtonVariant.Outline, AutoFit = true };
        var add = new ModernButton { Text = "New Event", Icon = "add", Shortcut = "N", AutoFit = true };
        today.FitToContents();
        add.FitToContents();
        prev.Click += async (_, _) => { Shift(-1); await LoadEventsAsync(); };
        next.Click += async (_, _) => { Shift(1); await LoadEventsAsync(); };
        today.Click += async (_, _) => { _cursor = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); _selected = DateTime.Today; await LoadEventsAsync(); };
        add.Click += async (_, _) => await AddEventAsync();
        _modes.Items = ["Month", "Week", "Day"];
        _modes.SelectedIndexChanged += async (_, i) =>
        {
            _mode = (ViewMode)i;
            await LoadEventsAsync();
        };
        _header.Actions.Controls.Add(prev);
        _header.Actions.Controls.Add(next);
        _header.Actions.Controls.Add(today);
        _header.Actions.Controls.Add(_modes);
        _header.Actions.Controls.Add(add);

        _grid.Paint += PaintGrid;
        _grid.MouseClick += OnGridClick;
        DrawingUtil.EnableDoubleBuffer(_grid);
        _dayList.DoubleClick += (_, _) => OpenSelectedEvent();
        var c = ThemeManager.Instance.Current;
        _dayList.BackColor = c.Surface;
        _dayList.ForeColor = c.TextPrimary;
        _dayList.Font = UiMetrics.Body;

        var edit = new ModernButton { Text = T("common.edit"), Variant = ButtonVariant.Outline, AutoFit = true };
        var del = new ModernButton { Text = T("common.delete"), Variant = ButtonVariant.Ghost, AutoFit = true };
        edit.FitToContents();
        del.FitToContents();
        edit.Click += async (_, _) => await EditEventAsync();
        del.Click += async (_, _) => await DeleteEventAsync();
        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = UiMetrics.ButtonHeight + UiMetrics.Space16,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(8, 6, 8, 6),
            Tag = "no-theme"
        };
        footer.Controls.Add(edit);
        footer.Controls.Add(del);
        var side = new Panel { Dock = DockStyle.Right, Width = 280, Tag = "no-theme", Padding = new Padding(8, 0, 0, 0) };
        _dayList.Dock = DockStyle.Fill;
        side.Controls.Add(_dayList);
        side.Controls.Add(footer);

        ContentPanel.Controls.Add(_grid);
        ContentPanel.Controls.Add(side);
        ContentPanel.Controls.Add(_header);
        ThemeManager.Instance.ThemeChanged += (_, _) =>
        {
            _grid.Invalidate();
            var colors = ThemeManager.Instance.Current;
            _dayList.BackColor = colors.Surface;
            _dayList.ForeColor = colors.TextPrimary;
        };
    }

    private void Shift(int dir)
    {
        _cursor = _mode switch
        {
            ViewMode.Week => _cursor.AddDays(7 * dir),
            ViewMode.Day => _cursor.AddDays(dir),
            _ => _cursor.AddMonths(dir)
        };
        _selected = _cursor;
    }

    protected override async Task LoadAsync() => await LoadEventsAsync();

    private (DateTime From, DateTime To) GetRange()
    {
        if (_mode == ViewMode.Day)
            return (_selected.Date, _selected.Date.AddDays(1).AddTicks(-1));
        if (_mode == ViewMode.Week)
        {
            var start = _selected.Date.AddDays(-(int)_selected.DayOfWeek);
            return (start, start.AddDays(7).AddTicks(-1));
        }
        var monthStart = new DateTime(_cursor.Year, _cursor.Month, 1);
        var gridStart = monthStart.AddDays(-(int)monthStart.DayOfWeek);
        return (gridStart, gridStart.AddDays(42).AddTicks(-1));
    }

    private async Task LoadEventsAsync()
    {
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var (from, to) = GetRange();
            _events = await GetService<ICalendarService>(scope).GetRangeAsync(from, to);
            _header.SubtitleText = _mode switch
            {
                ViewMode.Week => $"{_selected.Date.AddDays(-(int)_selected.DayOfWeek):MMM d} – {_selected.Date.AddDays(6 - (int)_selected.DayOfWeek):MMM d, yyyy}",
                ViewMode.Day => _selected.ToString("dddd, MMMM d"),
                _ => _cursor.ToString("MMMM yyyy")
            };
            RefreshDayList();
            _grid.Invalidate();
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void RefreshDayList()
    {
        var dayEvents = _events.Where(e => e.StartAt.Date == _selected.Date).ToList();
        _dayList.DataSource = dayEvents.Select(e => new CalendarEventListItem(e)).ToList();
    }

    private void OnGridClick(object? sender, MouseEventArgs e)
    {
        if (_mode == ViewMode.Day)
        {
            _selected = _cursor.Date;
            RefreshDayList();
            _grid.Invalidate();
            return;
        }

        var colW = _grid.Width / 7f;
        var col = Math.Clamp((int)(e.X / Math.Max(1, colW)), 0, 6);
        if (_mode == ViewMode.Week)
        {
            var start = _selected.Date.AddDays(-(int)_selected.DayOfWeek);
            _selected = start.AddDays(col);
            RefreshDayList();
            _grid.Invalidate();
            return;
        }

        var monthStart = new DateTime(_cursor.Year, _cursor.Month, 1);
        var gridStart = monthStart.AddDays(-(int)monthStart.DayOfWeek);
        var rowH = (_grid.Height - 24) / 6f;
        var row = Math.Clamp((int)((e.Y - 24) / Math.Max(1, rowH)), 0, 5);
        _selected = gridStart.AddDays(row * 7 + col);
        RefreshDayList();
        _grid.Invalidate();
    }

    private void PaintGrid(object? sender, PaintEventArgs e)
    {
        if (_mode == ViewMode.Week) { PaintWeek(e); return; }
        if (_mode == ViewMode.Day) { PaintDay(e); return; }
        PaintMonth(e);
    }

    private void PaintMonth(PaintEventArgs e)
    {
        var c = ThemeManager.Instance.Current;
        var g = e.Graphics;
        g.Clear(c.Background);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var names = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        var colW = _grid.Width / 7f;
        for (var i = 0; i < 7; i++)
            TextRenderer.DrawText(g, names[i], UiMetrics.Meta, new Rectangle((int)(i * colW), 0, (int)colW, 24), c.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        var monthStart = new DateTime(_cursor.Year, _cursor.Month, 1);
        var gridStart = monthStart.AddDays(-(int)monthStart.DayOfWeek);
        var rowH = (_grid.Height - 24) / 6f;
        for (var r = 0; r < 6; r++)
        for (var col = 0; col < 7; col++)
        {
            var day = gridStart.AddDays(r * 7 + col);
            var rect = new Rectangle((int)(col * colW) + 1, 24 + (int)(r * rowH) + 1, (int)colW - 2, (int)rowH - 2);
            var inMonth = day.Month == _cursor.Month;
            var bg = day.Date == _selected.Date ? c.SelectedBg : c.Surface;
            using (var b = new SolidBrush(bg))
                DrawingUtil.FillRounded(g, b, rect, 4);
            using (var p = new Pen(c.Border))
                DrawingUtil.DrawRounded(g, p, rect, 4);
            var fg = day.Date == DateTime.Today ? c.Accent : inMonth ? c.TextPrimary : c.TextMuted;
            TextRenderer.DrawText(g, day.Day.ToString(), UiMetrics.Meta, new Rectangle(rect.X + 4, rect.Y + 2, 28, 16), fg);
            var dayEvents = _events.Where(ev => ev.StartAt.Date == day.Date).Take(3).ToList();
            var y = rect.Y + 20;
            foreach (var ev in dayEvents)
            {
                using var chip = new SolidBrush(c.SurfaceAlt);
                var cr = new Rectangle(rect.X + 4, y, rect.Width - 8, 14);
                DrawingUtil.FillRounded(g, chip, cr, 3);
                TextRenderer.DrawText(g, ev.Title, UiMetrics.Kbd, cr, c.AccentSoft, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
                y += 16;
            }
        }
    }

    private void PaintWeek(PaintEventArgs e)
    {
        var c = ThemeManager.Instance.Current;
        var g = e.Graphics;
        g.Clear(c.Background);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var start = _selected.Date.AddDays(-(int)_selected.DayOfWeek);
        var colW = _grid.Width / 7f;
        var hours = 12;
        var hourH = (_grid.Height - 28) / (float)hours;
        for (var i = 0; i < 7; i++)
        {
            var day = start.AddDays(i);
            var header = new Rectangle((int)(i * colW), 0, (int)colW, 28);
            TextRenderer.DrawText(g, day.ToString("ddd d"), UiMetrics.Meta, header,
                day.Date == DateTime.Today ? c.Accent : c.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            var colRect = new Rectangle((int)(i * colW) + 1, 28, (int)colW - 2, _grid.Height - 30);
            using (var b = new SolidBrush(day.Date == _selected.Date ? c.SelectedBg : c.Surface))
                DrawingUtil.FillRounded(g, b, colRect, 4);
            using (var p = new Pen(c.Border))
                DrawingUtil.DrawRounded(g, p, colRect, 4);
            var dayEvents = _events.Where(ev => ev.StartAt.Date == day.Date).Take(hours).ToList();
            var y = 32;
            foreach (var ev in dayEvents)
            {
                var cr = new Rectangle(colRect.X + 4, y, colRect.Width - 8, 18);
                using var chip = new SolidBrush(c.SurfaceAlt);
                DrawingUtil.FillRounded(g, chip, cr, 3);
                TextRenderer.DrawText(g, $"{ev.StartAt:HH:mm} {ev.Title}", UiMetrics.Kbd, cr, c.AccentSoft,
                    TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
                y += 20;
            }
        }
        _ = hourH;
    }

    private void PaintDay(PaintEventArgs e)
    {
        var c = ThemeManager.Instance.Current;
        var g = e.Graphics;
        g.Clear(c.Background);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var startHour = 7;
        var hours = 14;
        var rowH = Math.Max(28, (_grid.Height - 8) / hours);
        for (var i = 0; i < hours; i++)
        {
            var hour = startHour + i;
            var y = i * rowH;
            TextRenderer.DrawText(g, $"{hour:00}:00", UiMetrics.Mono, new Rectangle(0, y, 56, rowH), c.TextMuted,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            var rect = new Rectangle(64, y + 2, _grid.Width - 72, rowH - 4);
            using (var b = new SolidBrush(c.Surface))
                DrawingUtil.FillRounded(g, b, rect, 4);
            using (var p = new Pen(c.Border))
                DrawingUtil.DrawRounded(g, p, rect, 4);
            foreach (var ev in _events.Where(ev => ev.StartAt.Date == _selected.Date && ev.StartAt.Hour == hour))
            {
                TextRenderer.DrawText(g, $"{ev.StartAt:HH:mm}  {ev.Title}", UiMetrics.Body, rect, c.TextPrimary,
                    TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
            }
        }
    }

    private void OpenSelectedEvent()
    {
        if (_dayList.SelectedItem is not CalendarEventListItem item) return;
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
        if (_dayList.SelectedItem is not CalendarEventListItem { Event: var ev }) return;
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
            Title = title, StartAt = ev.StartAt, EndAt = ev.EndAt, EventType = ev.EventType
        });
        await LoadEventsAsync();
    }

    private async Task DeleteEventAsync()
    {
        if (_dayList.SelectedItem is not CalendarEventListItem { Event: var ev }) return;
        if (IsSynthetic(ev))
        {
            MessageBox.Show("Task deadlines cannot be deleted here.", T("common.delete"),
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
        public override string ToString() => $"{Event.StartAt:HH:mm}  {Event.Title}";
    }
}
