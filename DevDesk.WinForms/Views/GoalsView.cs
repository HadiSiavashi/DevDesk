using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class GoalsView : ViewBase
{
    private readonly ListBox _list = new() { Dock = DockStyle.Fill };
    private readonly TrackBar _progress = new() { Dock = DockStyle.Bottom, Height = 40, Minimum = 0, Maximum = 100 };
    private readonly FlowLayoutPanel _toolbar = new() { Dock = DockStyle.Top, Height = 40, FlowDirection = FlowDirection.LeftToRight };
    private readonly ModernButton _add = new() { Height = 36, Text = "Add Goal" };
    private readonly ModernButton _delete = new() { Height = 36, IsPrimary = false };

    public GoalsView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        _delete.Text = T("common.delete");
        _add.Click += async (_, _) =>
        {
            var title = Dialogs.InputDialog.Show(T("common.create"), "Goal:");
            if (string.IsNullOrWhiteSpace(title)) return;
            using var scope = ScopeFactory.CreateScope();
            await GetService<IGoalService>(scope).CreateAsync(new Application.Dtos.CreateGoalRequest { Title = title });
            await LoadAsync();
        };
        _delete.Click += async (_, _) => await DeleteSelectedAsync();
        _toolbar.Controls.AddRange([_add, _delete]);
        _progress.ValueChanged += async (_, _) =>
        {
            if (_list.SelectedItem is not Application.Dtos.GoalDto g) return;
            using var scope = ScopeFactory.CreateScope();
            await GetService<IGoalService>(scope).SetProgressAsync(g.Id, _progress.Value);
        };
        _list.SelectedIndexChanged += (_, _) =>
        {
            if (_list.SelectedItem is Application.Dtos.GoalDto g) _progress.Value = g.Progress;
        };
        ContentPanel.Controls.Add(_list);
        ContentPanel.Controls.Add(_progress);
        ContentPanel.Controls.Add(_toolbar);
    }

    private async Task DeleteSelectedAsync()
    {
        if (_list.SelectedItem is not Application.Dtos.GoalDto goal) return;
        if (!Dialogs.ConfirmDialog.Show(T("common.confirm"), T("common.delete"))) return;
        using var scope = ScopeFactory.CreateScope();
        await GetService<IGoalService>(scope).DeleteAsync(goal.Id);
        await LoadAsync();
    }

    protected override async Task LoadAsync()
    {
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var goals = await GetService<IGoalService>(scope).GetAllAsync();
            _list.DataSource = goals.ToList();
            _list.DisplayMember = "Title";
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }
}
