using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
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
        ContentPanel.Controls.Add(_list);
        ContentPanel.Controls.Add(_add);
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
                var row = new Panel { Width = 480, Height = 48 };
                var lbl = new Label
                {
                    Text = $"{h.Name}  •  Streak: {h.CurrentStreak}  •  This month: {monthlyCount}",
                    Left = 8,
                    Top = 6,
                    AutoSize = true,
                    Font = new Font("Segoe UI Semibold", 9.5F)
                };
                var streak = new Label
                {
                    Text = $"🔥 {h.CurrentStreak}",
                    Left = 8,
                    Top = 26,
                    AutoSize = true
                };
                var chk = new CheckBox { Text = T("common.today"), Left = 360, Top = 12, Checked = h.CompletedToday };
                var del = new IconButton { Text = "×", Left = 430, Top = 8, Width = 28, Height = 28 };
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
