using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Localization;
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
    private readonly ModernButton _save = new() { AutoFit = true };

    public DailyPlanningView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        _save.Text = T("common.save");
        _save.FitToContents();
        _save.Click += async (_, _) => await SaveAsync();

        var isRtl = LocalizationService.Instance.IsRtl;
        var header = new PageHeader
        {
            TitleText = T("nav.dailyplan"),
            SubtitleText = DateFormatter.FormatLongDate(DateTime.Today)
        };
        header.Actions.Controls.Add(_save);

        var goals = new CardPanel { Dock = DockStyle.Top, Height = UiScale.Px(230), Margin = new Padding(0, 0, 0, UiMetrics.Space12) };
        var lblG3 = new Label { Text = T("dailyplan.goal3"), Dock = DockStyle.Top, Height = UiMetrics.LineMeta, Font = UiMetrics.Meta, TextAlign = isRtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft };
        var lblG2 = new Label { Text = T("dailyplan.goal2"), Dock = DockStyle.Top, Height = UiMetrics.LineMeta, Font = UiMetrics.Meta, TextAlign = isRtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft };
        var lblG1 = new Label { Text = T("dailyplan.goal1"), Dock = DockStyle.Top, Height = UiMetrics.LineMeta, Font = UiMetrics.Meta, TextAlign = isRtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft };
        var lblTitle = new Label { Text = T("dailyplan.goals"), Dock = DockStyle.Top, Height = UiMetrics.LineTitle, Font = UiMetrics.SectionTitle, TextAlign = isRtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft };

        goals.Controls.Add(_goal3);
        goals.Controls.Add(lblG3);
        goals.Controls.Add(_goal2);
        goals.Controls.Add(lblG2);
        goals.Controls.Add(_goal1);
        goals.Controls.Add(lblG1);
        goals.Controls.Add(lblTitle);

        var cap = new CardPanel { Dock = DockStyle.Top, Height = UiScale.Px(140), Margin = new Padding(0, 0, 0, UiMetrics.Space12) };
        _warning.TextAlign = isRtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
        var lblCap = new Label { Text = T("dailyplan.availableMinutes"), Dock = DockStyle.Top, Height = UiMetrics.LineMeta, Font = UiMetrics.Meta, TextAlign = isRtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft };

        cap.Controls.Add(_warning);
        cap.Controls.Add(_available);
        cap.Controls.Add(lblCap);

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
