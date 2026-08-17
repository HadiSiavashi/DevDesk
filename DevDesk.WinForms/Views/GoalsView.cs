using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class GoalsView : ViewBase
{
    private readonly FlowLayoutPanel _list = new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly TrackBar _progress = new() { Dock = DockStyle.Bottom, Height = UiScale.Px(40), Minimum = 0, Maximum = 100 };
    private readonly ModernButton _add = new() { Text = "Add Goal", AutoFit = true };
    private readonly ModernButton _delete = new() { IsPrimary = false, AutoFit = true };
    private Application.Dtos.GoalDto? _selected;

    public GoalsView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        _delete.Text = T("common.delete");
        _add.FitToContents();
        _delete.FitToContents();
        _add.Click += async (_, _) =>
        {
            var title = Dialogs.InputDialog.Show(T("common.create"), "Goal:");
            if (string.IsNullOrWhiteSpace(title)) return;
            using var scope = ScopeFactory.CreateScope();
            await GetService<IGoalService>(scope).CreateAsync(new Application.Dtos.CreateGoalRequest { Title = title });
            await LoadAsync();
        };
        _delete.Click += async (_, _) => await DeleteSelectedAsync();
        _progress.ValueChanged += async (_, _) =>
        {
            if (_selected is null) return;
            using var scope = ScopeFactory.CreateScope();
            await GetService<IGoalService>(scope).SetProgressAsync(_selected.Id, _progress.Value);
        };
        var header = new PageHeader { TitleText = T("nav.goals") };
        header.Actions.Controls.Add(_add);
        header.Actions.Controls.Add(_delete);
        ContentPanel.Controls.Add(_list);
        ContentPanel.Controls.Add(_progress);
        ContentPanel.Controls.Add(header);
    }

    private async Task DeleteSelectedAsync()
    {
        if (_selected is null) return;
        if (!Dialogs.ConfirmDialog.Show(T("common.confirm"), T("common.delete"))) return;
        using var scope = ScopeFactory.CreateScope();
        await GetService<IGoalService>(scope).DeleteAsync(_selected.Id);
        await LoadAsync();
    }

    protected override async Task LoadAsync()
    {
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var goals = await GetService<IGoalService>(scope).GetAllAsync();
            _list.Controls.Clear();
            _selected = null;
            foreach (var g in goals)
            {
                var row = new InventoryRow { Width = Math.Max(280, _list.ClientSize.Width - 8), Margin = new Padding(0, 0, 0, 8) };
                row.Bind(g.Title, $"{g.Progress}%");
                row.Click += (_, _) =>
                {
                    _selected = g;
                    _progress.Value = g.Progress;
                };
                _list.Controls.Add(row);
            }
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }
}
