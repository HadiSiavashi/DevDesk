using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class ProjectsView : ViewBase
{
    private readonly FlowLayoutPanel _cards = new() { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true };
    private readonly FlowLayoutPanel _toolbar = new() { Dock = DockStyle.Top, Height = 40, FlowDirection = FlowDirection.LeftToRight };
    private readonly ModernButton _add = new() { Height = 36, Text = "Add Project" };
    private readonly ModernButton _delete = new() { Height = 36, IsPrimary = false };
    private Guid? _selectedProjectId;

    public ProjectsView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        _delete.Text = T("common.delete");
        _add.Click += async (_, _) =>
        {
            var name = Dialogs.InputDialog.Show(T("common.create"), "Name:");
            if (string.IsNullOrWhiteSpace(name)) return;
            using var scope = ScopeFactory.CreateScope();
            await GetService<IProjectService>(scope).CreateAsync(new CreateProjectRequest { Name = name });
            await LoadAsync();
        };
        _delete.Click += async (_, _) => await DeleteSelectedAsync();
        _toolbar.Controls.AddRange([_add, _delete]);
        ContentPanel.Controls.Add(_cards);
        ContentPanel.Controls.Add(_toolbar);
    }

    private async Task DeleteSelectedAsync()
    {
        if (_selectedProjectId is not Guid id) return;
        if (!Dialogs.ConfirmDialog.Show(T("common.confirm"), T("common.delete"))) return;
        using var scope = ScopeFactory.CreateScope();
        await GetService<IProjectService>(scope).DeleteAsync(id);
        _selectedProjectId = null;
        await LoadAsync();
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
                var card = new Panel { Width = 220, Height = 120, Margin = new Padding(8), Cursor = Cursors.Hand };
                var theme = ThemeManager.Instance.Current;
                card.BackColor = theme.Surface;
                var name = new Label { Text = p.Name, Dock = DockStyle.Top, Height = 28, Font = new Font("Segoe UI Semibold", 10F), ForeColor = theme.TextPrimary };
                var prog = new Label { Text = $"{p.CompletedTasks}/{p.TotalTasks} ({p.ProgressPercent:F0}%)", Dock = DockStyle.Top, Height = 24, ForeColor = theme.TextMuted };
                card.Controls.Add(prog);
                card.Controls.Add(name);
                void SelectProject(object? _, EventArgs __) => _selectedProjectId = p.Id;
                void OpenDetail(object? _, EventArgs __)
                {
                    _selectedProjectId = p.Id;
                    Navigation.Navigate("project-detail", p.Id);
                }
                card.Click += OpenDetail;
                card.Click += SelectProject;
                name.Click += OpenDetail;
                name.Click += SelectProject;
                prog.Click += OpenDetail;
                prog.Click += SelectProject;
                _cards.Controls.Add(card);
            }
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }
}
