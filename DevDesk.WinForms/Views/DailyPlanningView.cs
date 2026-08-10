using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class DailyPlanningView : ViewBase, ISaveableView
{
    private readonly TextBox _goal1 = new() { Dock = DockStyle.Top };
    private readonly TextBox _goal2 = new() { Dock = DockStyle.Top };
    private readonly TextBox _goal3 = new() { Dock = DockStyle.Top };
    private readonly NumericUpDown _available = new() { Minimum = 60, Maximum = 960, Value = 480, Dock = DockStyle.Top };
    private readonly Label _warning = new() { Dock = DockStyle.Top, Height = 40, ForeColor = Color.OrangeRed };
    private readonly ModernButton _save = new() { Dock = DockStyle.Bottom, Height = 36, Text = "Save" };

    public DailyPlanningView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        _save.Click += async (_, _) => await SaveAsync();
        ContentPanel.Controls.Add(_save);
        ContentPanel.Controls.Add(_warning);
        ContentPanel.Controls.Add(_available);
        ContentPanel.Controls.Add(new Label { Text = "Available minutes", Dock = DockStyle.Top, Height = 20 });
        ContentPanel.Controls.Add(_goal3);
        ContentPanel.Controls.Add(_goal2);
        ContentPanel.Controls.Add(_goal1);
        ContentPanel.Controls.Add(new Label { Text = T("dailyplan.goals"), Dock = DockStyle.Top, Height = 24, Font = new Font("Segoe UI Semibold", 11F) });
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
            _warning.Text = plan.WorkloadExceedsAvailable ? T("dailyplan.warning") : "";
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
