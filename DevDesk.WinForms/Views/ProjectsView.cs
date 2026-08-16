using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Dialogs;
using DevDesk.WinForms.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class ProjectsView : ViewBase
{
    private readonly FlowLayoutPanel _cards = new() { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true };
    private Guid? _selectedProjectId;

    public ProjectsView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        var header = new PageHeader { TitleText = T("nav.projects") };
        var add = new ModernButton { Text = "New Project", Icon = "add", Width = 130, Height = 32 };
        add.Click += async (_, _) =>
        {
            var name = InputDialog.Show(T("common.create"), "Name:");
            if (string.IsNullOrWhiteSpace(name)) return;
            using var scope = ScopeFactory.CreateScope();
            await GetService<IProjectService>(scope).CreateAsync(new CreateProjectRequest { Name = name });
            await LoadAsync();
        };
        header.Actions.Controls.Add(add);
        ContentPanel.Controls.Add(_cards);
        ContentPanel.Controls.Add(header);
    }

    protected override async Task LoadAsync()
    {
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var projects = await GetService<IProjectService>(scope).GetAllAsync();
            _cards.Controls.Clear();
            foreach (var p in projects)
            {
                var card = new ProjectCard();
                card.Bind(p);
                card.OpenRequested += (_, id) =>
                {
                    _selectedProjectId = id;
                    Navigation.Navigate("project-detail", id);
                };
                card.DeleteRequested += async (_, id) =>
                {
                    _selectedProjectId = id;
                    if (!ConfirmDialog.Show(T("common.confirm"), T("common.delete"))) return;
                    using var s = ScopeFactory.CreateScope();
                    await GetService<IProjectService>(s).DeleteAsync(id);
                    await LoadAsync();
                };
                _cards.Controls.Add(card);
            }
            if (projects.Count == 0) ShowEmpty(); else ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }
}
