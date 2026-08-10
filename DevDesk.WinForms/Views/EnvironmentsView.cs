using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using DevDesk.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class EnvironmentsView : ViewBase
{
    private readonly ListBox _list = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _projects = new() { Dock = DockStyle.Top };
    private readonly ModernButton _add = new() { Dock = DockStyle.Top, Height = 36, Text = "Add Environment" };

    public EnvironmentsView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        _projects.SelectedIndexChanged += async (_, _) => await LoadEnvsAsync();
        _add.Click += async (_, _) =>
        {
            if (_projects.SelectedItem is not Application.Dtos.ProjectListItemDto p) return;
            var name = Dialogs.InputDialog.Show(T("common.create"), "Name:");
            if (string.IsNullOrWhiteSpace(name)) return;
            using var scope = ScopeFactory.CreateScope();
            await GetService<IEnvironmentService>(scope).CreateAsync(new Application.Dtos.CreateEnvironmentRequest
            {
                ProjectId = p.Id,
                Name = name,
                EnvironmentType = EnvironmentType.Development
            });
            await LoadEnvsAsync();
        };
        ContentPanel.Controls.Add(_list);
        ContentPanel.Controls.Add(_add);
        ContentPanel.Controls.Add(_projects);
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
        if (_projects.SelectedItem is not Application.Dtos.ProjectListItemDto p) { ShowEmpty(); return; }
        using var scope = ScopeFactory.CreateScope();
        var envs = await GetService<IEnvironmentService>(scope).GetByProjectAsync(p.Id);
        _list.DataSource = envs.ToList();
        _list.DisplayMember = "Name";
        ShowContent();
    }
}
