using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class HabitsView : ViewBase
{
    private readonly FlowLayoutPanel _list = new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly ModernButton _add = new() { Height = UiMetrics.ButtonHeight, AutoFit = true };

    public HabitsView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        _add.Text = T("habits.add");
        _add.FitToContents();
        _add.Click += async (_, _) =>
        {
            var name = Dialogs.InputDialog.Show(T("common.create"), $"{T("habits.habit")}:");
            if (string.IsNullOrWhiteSpace(name)) return;
            using var scope = ScopeFactory.CreateScope();
            await GetService<IHabitService>(scope).CreateAsync(new Application.Dtos.CreateHabitRequest { Name = name });
            await LoadAsync();
        };

        var header = new PageHeader { TitleText = T("nav.habits") };
        header.Actions.Controls.Add(_add);
        ContentPanel.Controls.Add(_list);
        ContentPanel.Controls.Add(header);

        _list.Resize += (_, _) =>
        {
            var targetW = Math.Max(UiScale.Px(320), _list.ClientSize.Width - UiScale.Px(16));
            foreach (Control c in _list.Controls)
                c.Width = targetW;
        };
    }

    protected override async Task LoadAsync()
    {
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var habits = await GetService<IHabitService>(scope).GetAllAsync();
            var monthStart = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
            var isRtl = LocalizationService.Instance.IsRtl;
            _list.Controls.Clear();

            if (habits.Count == 0)
            {
                var empty = new EmptyStatePanel
                {
                    TitleText = T("habits.empty"),
                    Icon = "repeat",
                    Width = Math.Max(UiScale.Px(320), _list.ClientSize.Width - UiScale.Px(16)),
                    Height = UiScale.Px(200)
                };
                _list.Controls.Add(empty);
            }
            else
            {
                var targetW = Math.Max(UiScale.Px(320), _list.ClientSize.Width - UiScale.Px(16));
                foreach (var h in habits)
                {
                    var monthlyCount = h.RecentRecords.Count(r => r.IsCompleted && r.Date >= monthStart);
                    var row = new CardPanel
                    {
                        Width = targetW,
                        Height = UiScale.Px(76),
                        Margin = new Padding(0, 0, 0, UiMetrics.Space8),
                        Padding = new Padding(UiMetrics.Space12)
                    };

                    var actionsPanel = new Panel
                    {
                        Dock = isRtl ? DockStyle.Left : DockStyle.Right,
                        Width = UiScale.Px(160),
                        Tag = "no-theme"
                    };

                    var del = new IconButton
                    {
                        Icon = "close",
                        Size = new Size(UiMetrics.IconButtonSize, UiMetrics.IconButtonSize),
                        Dock = isRtl ? DockStyle.Left : DockStyle.Right
                    };

                    var chk = new CheckBox
                    {
                        Text = T("common.today"),
                        Dock = DockStyle.Fill,
                        Checked = h.CompletedToday,
                        Font = UiMetrics.Body,
                        ForeColor = ThemeManager.Instance.Current.TextPrimary,
                        CheckAlign = isRtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft,
                        TextAlign = isRtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft
                    };

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

                    actionsPanel.Controls.Add(chk);
                    actionsPanel.Controls.Add(del);

                    var infoPanel = new Panel
                    {
                        Dock = DockStyle.Fill,
                        Tag = "no-theme"
                    };

                    var lbl = new Label
                    {
                        Text = h.Name,
                        Dock = DockStyle.Top,
                        Height = UiMetrics.LineTitle,
                        Font = UiMetrics.SectionTitle,
                        ForeColor = ThemeManager.Instance.Current.TextPrimary,
                        TextAlign = isRtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft
                    };

                    var streak = new Label
                    {
                        Text = $"{T("habits.streak")}: {h.CurrentStreak}  ·  {T("habits.thisMonth")}: {monthlyCount}",
                        Dock = DockStyle.Top,
                        Height = UiMetrics.LineMeta,
                        Font = UiMetrics.Meta,
                        ForeColor = ThemeManager.Instance.Current.TextMuted,
                        TextAlign = isRtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft
                    };

                    infoPanel.Controls.Add(streak);
                    infoPanel.Controls.Add(lbl);

                    row.Controls.Add(infoPanel);
                    row.Controls.Add(actionsPanel);
                    _list.Controls.Add(row);
                }
            }

            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }
}
