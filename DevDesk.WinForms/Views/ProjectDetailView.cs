using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class ProjectDetailView : ViewBase
{
    private Guid _projectId;
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly Label _overview = new() { Dock = DockStyle.Fill, AutoSize = false, Padding = new Padding(12) };
    private readonly ListBox _taskList = new() { Dock = DockStyle.Fill };
    private readonly ListBox _milestoneList = new() { Dock = DockStyle.Fill };
    private readonly ListBox _noteList = new() { Dock = DockStyle.Fill };
    private readonly ListBox _envList = new() { Dock = DockStyle.Fill };
    private readonly ListBox _snippetList = new() { Dock = DockStyle.Fill };
    private readonly ListBox _bookmarkList = new() { Dock = DockStyle.Fill };
    private readonly Label _analytics = new() { Dock = DockStyle.Fill, AutoSize = false, Padding = new Padding(12) };
    private readonly Label _header = new() { Dock = DockStyle.Top, Height = 40, Font = new Font("Segoe UI Semibold", 14F) };

    public ProjectDetailView(IServiceScopeFactory scopeFactory, NavigationService navigation, object? parameter)
        : base(scopeFactory, navigation)
    {
        _projectId = parameter is Guid g ? g : Guid.Empty;
        _tabs.TabPages.Add(new TabPage("Overview") { Controls = { _overview } });
        _tabs.TabPages.Add(new TabPage("Tasks") { Controls = { _taskList } });
        _tabs.TabPages.Add(new TabPage("Milestones") { Controls = { _milestoneList } });
        _tabs.TabPages.Add(new TabPage("Notes") { Controls = { _noteList } });
        _tabs.TabPages.Add(new TabPage("Environments") { Controls = { _envList } });
        _tabs.TabPages.Add(new TabPage("Snippets") { Controls = { _snippetList } });
        _tabs.TabPages.Add(new TabPage("Bookmarks") { Controls = { _bookmarkList } });
        _tabs.TabPages.Add(new TabPage("Analytics") { Controls = { _analytics } });

        _taskList.DoubleClick += (_, _) =>
        {
            if (_taskList.SelectedItem is TaskListItemDto t)
                Navigation.Navigate("task-detail", t.Id);
        };
        _noteList.DoubleClick += (_, _) =>
        {
            if (_noteList.SelectedItem is NoteDto n)
                Navigation.Navigate("note-editor", n.Id);
        };
        _snippetList.DoubleClick += (_, _) =>
        {
            if (_snippetList.SelectedItem is CodeSnippetDto s)
                Navigation.Navigate("snippet-editor", s.Id);
        };

        var back = new ModernButton { Text = T("common.back"), Dock = DockStyle.Bottom, Height = 36, IsPrimary = false };
        back.Click += (_, _) => Navigation.NavigateBack();
        ContentPanel.Controls.Add(_tabs);
        ContentPanel.Controls.Add(back);
        ContentPanel.Controls.Add(_header);
    }

    protected override async Task LoadAsync()
    {
        if (_projectId == Guid.Empty) return;
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var project = await GetService<IProjectService>(scope).GetByIdAsync(_projectId);
            if (project is null) { ShowEmpty(); return; }

            _header.Text = project.Name;
            _overview.Text =
                $"Description: {project.Description ?? "—"}\r\n" +
                $"Repository: {project.RepositoryUrl ?? "—"}\r\n" +
                $"Local path: {project.LocalPath ?? "—"}\r\n" +
                $"Progress: {project.ProgressPercent:F0}%\r\n" +
                $"Tasks: {project.CompletedTasks}/{project.TotalTasks} completed";

            var tasks = await GetService<ITaskService>(scope).GetByProjectAsync(_projectId);
            _taskList.DataSource = tasks.ToList();
            _taskList.DisplayMember = "Title";

            _milestoneList.DataSource = project.Milestones.ToList();
            _milestoneList.DisplayMember = "Title";

            var notes = (await GetService<INoteService>(scope).GetAllAsync()).Where(n => n.ProjectId == _projectId).ToList();
            _noteList.DataSource = notes;
            _noteList.DisplayMember = "Title";

            var envs = await GetService<IEnvironmentService>(scope).GetByProjectAsync(_projectId);
            _envList.DataSource = envs.ToList();
            _envList.DisplayMember = "Name";

            var snippets = (await GetService<ISnippetService>(scope).GetAllAsync())
                .Where(s => s.ProjectId == _projectId).ToList();
            _snippetList.DataSource = snippets;
            _snippetList.DisplayMember = "Title";

            var bookmarks = (await GetService<IBookmarkService>(scope).GetAllAsync())
                .Where(b => b.ProjectId == _projectId).ToList();
            _bookmarkList.DataSource = bookmarks;
            _bookmarkList.DisplayMember = "Title";

            var openTasks = tasks.Count(t => t.Status != Domain.Enums.WorkTaskStatus.Done);
            var completedTasks = tasks.Count(t => t.Status == Domain.Enums.WorkTaskStatus.Done);
            _analytics.Text =
                $"Open tasks: {openTasks}\r\n" +
                $"Completed tasks: {completedTasks}\r\n" +
                $"Progress: {project.ProgressPercent:F0}%";

            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }
}
