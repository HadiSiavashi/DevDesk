using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class DailyPlanningView : ViewBase, ISaveableView
{
    private readonly TextField _goal1 = new() { Dock = DockStyle.Top };
    private readonly TextField _goal2 = new() { Dock = DockStyle.Top };
    private readonly TextField _goal3 = new() { Dock = DockStyle.Top };
    private readonly NumericUpDown _available = new() { Minimum = 60, Maximum = 960, Value = 480, Dock = DockStyle.Top, Height = UiMetrics.InputHeight };
    private readonly Label _warning = new() { Dock = DockStyle.Top, Height = UiMetrics.LineBody };
    private readonly ModernButton _save = new() { Text = "Save", AutoFit = true };

    public DailyPlanningView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        var header = new PageHeader { TitleText = T("nav.dailyplan"), SubtitleText = DateTime.Today.ToString("dddd, MMMM d") };
        header.Actions.Controls.Add(_save);
        _save.FitToContents();
        _save.Click += async (_, _) => await SaveAsync();

        var goals = new CardPanel { Dock = DockStyle.Top, Height = UiScale.Px(220) };
        goals.Controls.Add(_goal3);
        goals.Controls.Add(new Label { Text = "Goal 3", Dock = DockStyle.Top, Height = UiMetrics.LineMeta, Font = UiMetrics.Meta });
        goals.Controls.Add(_goal2);
        goals.Controls.Add(new Label { Text = "Goal 2", Dock = DockStyle.Top, Height = UiMetrics.LineMeta, Font = UiMetrics.Meta });
        goals.Controls.Add(_goal1);
        goals.Controls.Add(new Label { Text = T("dailyplan.goals"), Dock = DockStyle.Top, Height = UiMetrics.LineTitle, Font = UiMetrics.SectionTitle });

        var cap = new CardPanel { Dock = DockStyle.Top, Height = UiScale.Px(140) };
        cap.Controls.Add(_warning);
        cap.Controls.Add(_available);
        cap.Controls.Add(new Label { Text = "Available minutes", Dock = DockStyle.Top, Height = UiMetrics.LineMeta, Font = UiMetrics.Meta });

        ContentPanel.Controls.Add(cap);
        ContentPanel.Controls.Add(goals);
        ContentPanel.Controls.Add(header);
    }

    protected override async Task LoadAsync()
    {
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var plan = await GetService<IDailyPlanService>(scope).GetOrCreateAsync(DateOnly.FromDateTime(DateTime.Today));
            _goal1.Text = plan.TopGoal1 ?? "";
            _goal2.Text = plan.TopGoal2 ?? "";
            _goal3.Text = plan.TopGoal3 ?? "";
            _available.Value = plan.AvailableWorkMinutes;
            _warning.Text = plan.WorkloadExceedsAvailable ? T("dailyplan.warning") : $"{T("dailyplan.workload")}: {plan.EstimatedWorkloadMinutes}/{plan.AvailableWorkMinutes} min";
            _warning.ForeColor = plan.WorkloadExceedsAvailable
                ? ThemeManager.Instance.Current.Error
                : ThemeManager.Instance.Current.TextSecondary;
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    public async Task SaveAsync()
    {
        using var scope = ScopeFactory.CreateScope();
        await GetService<IDailyPlanService>(scope).UpdateAsync(DateOnly.FromDateTime(DateTime.Today), new Application.Dtos.UpdateDailyPlanRequest
        {
            TopGoal1 = _goal1.Text,
            TopGoal2 = _goal2.Text,
            TopGoal3 = _goal3.Text,
            AvailableWorkMinutes = (int)_available.Value
        });
        await LoadAsync();
    }
}
