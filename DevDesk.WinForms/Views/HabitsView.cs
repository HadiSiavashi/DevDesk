using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class HabitsView : ViewBase
{
    private readonly FlowLayoutPanel _list = new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly ModernButton _add = new() { Dock = DockStyle.Top, Height = 36, Text = "Add Habit" };

    public HabitsView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        _add.Click += async (_, _) =>
        {
            var name = Dialogs.InputDialog.Show(T("common.create"), "Habit:");
            if (string.IsNullOrWhiteSpace(name)) return;
            using var scope = ScopeFactory.CreateScope();
            await GetService<IHabitService>(scope).CreateAsync(new Application.Dtos.CreateHabitRequest { Name = name });
            await LoadAsync();
        };
        var header = new PageHeader { TitleText = T("nav.habits") };
        header.Actions.Controls.Add(_add);
        _add.Dock = DockStyle.None;
        _add.Width = 120;
        ContentPanel.Controls.Add(_list);
        ContentPanel.Controls.Add(header);
    }

    protected override async Task LoadAsync()
    {
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var habits = await GetService<IHabitService>(scope).GetAllAsync();
            var monthStart = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
            _list.Controls.Clear();
            foreach (var h in habits)
            {
                var monthlyCount = h.RecentRecords.Count(r => r.IsCompleted && r.Date >= monthStart);
                var row = new CardPanel { Width = Math.Max(480, _list.ClientSize.Width - 8), Height = 64, Margin = new Padding(0, 0, 0, 8) };
                var lbl = new Label
                {
                    Text = h.Name,
                    Left = 8,
                    Top = 8,
                    AutoSize = true,
                    Font = UiMetrics.SectionTitle
                };
                var streak = new Label
                {
                    Text = $"Streak {h.CurrentStreak}  ·  This month {monthlyCount}",
                    Left = 8,
                    Top = 32,
                    AutoSize = true,
                    Font = UiMetrics.Meta,
                    ForeColor = ThemeManager.Instance.Current.TextMuted
                };
                var chk = new CheckBox { Text = T("common.today"), Left = row.Width - 160, Top = 18, Checked = h.CompletedToday, Anchor = AnchorStyles.Top | AnchorStyles.Right };
                var del = new IconButton { Icon = "close", Left = row.Width - 44, Top = 16, Width = 28, Height = 28, Anchor = AnchorStyles.Top | AnchorStyles.Right };
                var id = h.Id;
                chk.CheckedChanged += async (_, _) =>
                {
                    using var s = ScopeFactory.CreateScope();
                    await GetService<IHabitService>(s).ToggleCompletionAsync(id, DateOnly.FromDateTime(DateTime.Today));
                    await LoadAsync();
                };
                del.Click += async (_, _) =>
                {
                    if (!Dialogs.ConfirmDialog.Show(T("common.confirm"), T("common.delete"))) return;
                    using var s = ScopeFactory.CreateScope();
                    await GetService<IHabitService>(s).DeleteAsync(id);
                    await LoadAsync();
                };
                row.Controls.Add(del);
                row.Controls.Add(chk);
                row.Controls.Add(streak);
                row.Controls.Add(lbl);
                _list.Controls.Add(row);
            }
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }
}
