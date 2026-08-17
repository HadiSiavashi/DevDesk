using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Domain.Enums;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class EnvironmentsView : ViewBase
{
    private readonly FlowLayoutPanel _list = new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly ThemedComboBox _projects = new() { Dock = DockStyle.Top };
    private readonly ModernButton _add = new() { Text = "Add Environment", AutoFit = true };

    public EnvironmentsView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        _add.FitToContents();
        _projects.SelectedIndexChanged += async (_, _) => await LoadEnvsAsync();
        _add.Click += async (_, _) =>
        {
            if (_projects.SelectedItem is not ProjectListItemDto p) return;
            var name = Dialogs.InputDialog.Show(T("common.create"), "Name:");
            if (string.IsNullOrWhiteSpace(name)) return;
            using var scope = ScopeFactory.CreateScope();
            await GetService<IEnvironmentService>(scope).CreateAsync(new CreateEnvironmentRequest
            {
                ProjectId = p.Id,
                Name = name,
                EnvironmentType = EnvironmentType.Development
            });
            await LoadEnvsAsync();
        };
        var header = new PageHeader { TitleText = T("nav.environments") };
        header.Actions.Controls.Add(_add);
        ContentPanel.Controls.Add(_list);
        ContentPanel.Controls.Add(_projects);
        ContentPanel.Controls.Add(header);
    }

    protected override async Task LoadAsync()
    {
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var projects = await GetService<IProjectService>(scope).GetAllAsync();
            _projects.DataSource = projects.ToList();
            _projects.DisplayMember = "Name";
            await LoadEnvsAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async Task LoadEnvsAsync()
    {
        if (_projects.SelectedItem is not ProjectListItemDto p) { ShowEmpty(); return; }
        using var scope = ScopeFactory.CreateScope();
        var envs = await GetService<IEnvironmentService>(scope).GetByProjectAsync(p.Id);
        _list.Controls.Clear();
        foreach (var e in envs)
        {
            var row = new InventoryRow { Width = Math.Max(280, _list.ClientSize.Width - 8), Margin = new Padding(0, 0, 0, 8) };
            row.Bind(e.Name, e.EnvironmentType.ToString());
            _list.Controls.Add(row);
        }
        ShowContent();
    }
}
