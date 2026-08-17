using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class ProjectDetailView : ViewBase
{
    private Guid _projectId;
    private readonly Label _overview = new() { Dock = DockStyle.Fill, AutoSize = false, Padding = new Padding(12) };
    private readonly ListBox _taskList = new() { Dock = DockStyle.Fill };
    private readonly ListBox _milestoneList = new() { Dock = DockStyle.Fill };
    private readonly ListBox _noteList = new() { Dock = DockStyle.Fill };
    private readonly ListBox _envList = new() { Dock = DockStyle.Fill };
    private readonly ListBox _snippetList = new() { Dock = DockStyle.Fill };
    private readonly ListBox _bookmarkList = new() { Dock = DockStyle.Fill };
    private readonly Label _analytics = new() { Dock = DockStyle.Fill, AutoSize = false, Padding = new Padding(12) };
    private readonly PageHeader _header = new();
    private readonly SegmentedTabs _tabs = new() { Dock = DockStyle.Top, UnderlineStyle = true };
    private readonly Panel[] _pages;

    public ProjectDetailView(IServiceScopeFactory scopeFactory, NavigationService navigation, object? parameter)
        : base(scopeFactory, navigation)
    {
        _projectId = parameter is Guid g ? g : Guid.Empty;
        _tabs.Items = ["Overview", "Tasks", "Milestones", "Notes", "Environments", "Snippets", "Bookmarks", "Analytics"];

        Panel Wrap(Control inner)
        {
            var p = new CardPanel { Dock = DockStyle.Fill, Visible = false };
            inner.Dock = DockStyle.Fill;
            p.Controls.Add(inner);
            return p;
        }

        _pages =
        [
            Wrap(_overview),
            Wrap(_taskList),
            Wrap(_milestoneList),
            Wrap(_noteList),
            Wrap(_envList),
            Wrap(_snippetList),
            Wrap(_bookmarkList),
            Wrap(_analytics)
        ];
        _pages[0].Visible = true;
        _tabs.SelectedIndexChanged += (_, i) =>
        {
            for (var n = 0; n < _pages.Length; n++)
                _pages[n].Visible = n == i;
            if (i >= 0 && i < _pages.Length)
                _pages[i].BringToFront();
        };

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

        var back = new ModernButton { Text = T("common.back"), Variant = ButtonVariant.Ghost, Width = 72 };
        back.Click += (_, _) => Navigation.NavigateBack();
        _header.Actions.Controls.Add(back);

        var host = new Panel { Dock = DockStyle.Fill, Tag = "no-theme" };
        foreach (var page in _pages)
            host.Controls.Add(page);

        ContentPanel.Controls.Add(host);
        ContentPanel.Controls.Add(_tabs);
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

            _header.TitleText = project.Name;
            _header.SubtitleText = $"{project.CompletedTasks}/{project.TotalTasks} tasks · {project.ProgressPercent:F0}%";
            _overview.Text =
                $"Description: {project.Description ?? "—"}\r\n" +
                $"Repository: {project.RepositoryUrl ?? "—"}\r\n" +
                $"Local path: {project.LocalPath ?? "—"}\r\n" +
                $"Progress: {project.ProgressPercent:F0}%\r\n" +
                $"Tasks: {project.CompletedTasks}/{project.TotalTasks} completed";
            _overview.Font = UiMetrics.Body;
            _overview.ForeColor = ThemeManager.Instance.Current.TextSecondary;

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
            _analytics.Font = UiMetrics.Body;
            _analytics.ForeColor = ThemeManager.Instance.Current.TextSecondary;

            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }
}
