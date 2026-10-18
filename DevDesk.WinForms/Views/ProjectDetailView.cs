using System.Diagnostics;
using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Domain.Enums;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Dialogs;
using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class ProjectDetailView : ViewBase
{
    private Guid _projectId;
    private ProjectDto? _project;

    // Overview components
    private readonly Panel _overviewContainer = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private readonly CardPanel _actionsCard = new() { Dock = DockStyle.Top, Height = 95, Margin = new Padding(0, 0, 0, 12) };
    private readonly CardPanel _infoCard = new() { Dock = DockStyle.Top, Height = 170, Margin = new Padding(0, 0, 0, 12) };
    private readonly CardPanel _statsCard = new() { Dock = DockStyle.Top, Height = 140, Margin = new Padding(0, 0, 0, 12) };

    private readonly Label _lblLocalPath = new() { AutoSize = true, Font = UiMetrics.Body };
    private readonly Label _lblRepo = new() { AutoSize = true, Font = UiMetrics.Body };
    private readonly Label _lblDesc = new() { AutoSize = true, Font = UiMetrics.Body };
    private readonly Label _lblStats = new() { AutoSize = true, Font = UiMetrics.Body };

    // Environments components
    private readonly Panel _envContainer = new() { Dock = DockStyle.Fill };
    private readonly Panel _envTopBar = new() { Dock = DockStyle.Top, Height = 44 };
    private readonly FlowLayoutPanel _envCards = new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly ModernButton _btnAddEnv = new() { Text = "+ Add Environment", Width = 150 };

    // Other tabs
    private readonly ListBox _taskList = new() { Dock = DockStyle.Fill };
    private readonly ListBox _milestoneList = new() { Dock = DockStyle.Fill };
    private readonly ListBox _noteList = new() { Dock = DockStyle.Fill };
    private readonly ListBox _snippetList = new() { Dock = DockStyle.Fill };
    private readonly ListBox _bookmarkList = new() { Dock = DockStyle.Fill };
    private readonly Label _analytics = new() { Dock = DockStyle.Fill, AutoSize = false, Padding = new Padding(16) };

    private readonly PageHeader _header = new();
    private readonly SegmentedTabs _tabs = new() { Dock = DockStyle.Top, UnderlineStyle = true };
    private readonly Panel[] _pages;

    // Developer action buttons in header
    private readonly ModernButton _btnVsCode = new() { Text = "VS Code", Variant = ButtonVariant.Ghost, Width = 84, Visible = false };
    private readonly ModernButton _btnTerminal = new() { Text = "Terminal", Variant = ButtonVariant.Ghost, Width = 84, Visible = false };
    private readonly ModernButton _btnExplorer = new() { Text = "Folder", Variant = ButtonVariant.Ghost, Width = 72, Visible = false };
    private readonly ModernButton _btnRepo = new() { Text = "Repo", Variant = ButtonVariant.Ghost, Width = 68, Visible = false };

    public ProjectDetailView(IServiceScopeFactory scopeFactory, NavigationService navigation, object? parameter)
        : base(scopeFactory, navigation)
    {
        _projectId = parameter is Guid g ? g : Guid.Empty;
        _tabs.Items = ["Overview", "Tasks", "Milestones", "Notes", "Environments", "Snippets", "Bookmarks", "Analytics"];

        SetupOverviewTab();
        SetupEnvironmentsTab();

        Panel Wrap(Control inner)
        {
            var p = new CardPanel { Dock = DockStyle.Fill, Visible = false };
            inner.Dock = DockStyle.Fill;
            p.Controls.Add(inner);
            return p;
        }

        _pages =
        [
            Wrap(_overviewContainer),
            Wrap(_taskList),
            Wrap(_milestoneList),
            Wrap(_noteList),
            Wrap(_envContainer),
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
        _bookmarkList.DoubleClick += (_, _) =>
        {
            if (_bookmarkList.SelectedItem is BookmarkDto b && !string.IsNullOrWhiteSpace(b.Url))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(b.Url) { UseShellExecute = true });
                }
                catch { /* ignore */ }
            }
        };

        SetupHeader();

        var host = new Panel { Dock = DockStyle.Fill, Tag = "no-theme" };
        foreach (var page in _pages)
            host.Controls.Add(page);

        ContentPanel.Controls.Add(host);
        ContentPanel.Controls.Add(_tabs);
        ContentPanel.Controls.Add(_header);
    }

    private void SetupHeader()
    {
        var back = new ModernButton { Text = T("common.back"), Variant = ButtonVariant.Ghost, Width = 72 };
        back.Click += (_, _) => Navigation.NavigateBack();

        _btnVsCode.Click += (_, _) => OpenVsCode();
        _btnTerminal.Click += (_, _) => OpenTerminal();
        _btnExplorer.Click += (_, _) => OpenExplorer();
        _btnRepo.Click += (_, _) => OpenRepo();

        _header.Actions.Controls.Add(back);
        _header.Actions.Controls.Add(_btnRepo);
        _header.Actions.Controls.Add(_btnExplorer);
        _header.Actions.Controls.Add(_btnTerminal);
        _header.Actions.Controls.Add(_btnVsCode);
    }

    private void SetupOverviewTab()
    {
        _overviewContainer.Padding = new Padding(12);

        // Actions Card
        var actionsTitle = new Label
        {
            Text = "Developer Quick Actions",
            Font = UiMetrics.SectionTitle,
            Dock = DockStyle.Top,
            Height = 26,
            ForeColor = ThemeManager.Instance.Current.TextPrimary
        };
        var actionsBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };

        var btnCode = new ModernButton { Text = "Open in VS Code", Width = 140, Height = 34 };
        btnCode.Click += (_, _) => OpenVsCode();

        var btnTerm = new ModernButton { Text = "Open Terminal", Width = 130, Height = 34, Variant = ButtonVariant.Ghost };
        btnTerm.Click += (_, _) => OpenTerminal();

        var btnExp = new ModernButton { Text = "Explore Files", Width = 120, Height = 34, Variant = ButtonVariant.Ghost };
        btnExp.Click += (_, _) => OpenExplorer();

        var btnGit = new ModernButton { Text = "Copy Clone Cmd", Width = 135, Height = 34, Variant = ButtonVariant.Ghost };
        btnGit.Click += (_, _) => CopyCloneCommand();

        actionsBar.Controls.AddRange([btnCode, btnTerm, btnExp, btnGit]);
        _actionsCard.Controls.Add(actionsBar);
        _actionsCard.Controls.Add(actionsTitle);

        // Info Card
        var infoTitle = new Label
        {
            Text = "Repository & Workspace",
            Font = UiMetrics.SectionTitle,
            Dock = DockStyle.Top,
            Height = 26,
            ForeColor = ThemeManager.Instance.Current.TextPrimary
        };
        var infoFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 0)
        };
        _lblLocalPath.ForeColor = ThemeManager.Instance.Current.TextSecondary;
        _lblRepo.ForeColor = ThemeManager.Instance.Current.TextSecondary;
        _lblDesc.ForeColor = ThemeManager.Instance.Current.TextPrimary;

        infoFlow.Controls.AddRange([_lblLocalPath, _lblRepo, _lblDesc]);
        _infoCard.Controls.Add(infoFlow);
        _infoCard.Controls.Add(infoTitle);

        // Stats Card
        var statsTitle = new Label
        {
            Text = "Project Progress & Health",
            Font = UiMetrics.SectionTitle,
            Dock = DockStyle.Top,
            Height = 26,
            ForeColor = ThemeManager.Instance.Current.TextPrimary
        };
        var statsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 0)
        };
        _lblStats.ForeColor = ThemeManager.Instance.Current.TextSecondary;
        statsFlow.Controls.Add(_lblStats);
        _statsCard.Controls.Add(statsFlow);
        _statsCard.Controls.Add(statsTitle);

        _overviewContainer.Controls.Add(_statsCard);
        _overviewContainer.Controls.Add(_infoCard);
        _overviewContainer.Controls.Add(_actionsCard);
    }

    private void SetupEnvironmentsTab()
    {
        _btnAddEnv.Height = UiMetrics.ButtonHeight;
        _btnAddEnv.Click += async (_, _) =>
        {
            if (EnvironmentEditorDialog.ShowCreate(this, ScopeFactory, _projectId))
                await ReloadEnvironmentsAsync();
        };

        _envTopBar.Padding = new Padding(12, 6, 12, 6);
        var lblEnvTitle = new Label
        {
            Text = "Configured Environments (Dev, Staging, Production)",
            Font = UiMetrics.SectionTitle,
            Dock = DockStyle.Left,
            AutoSize = true,
            Padding = new Padding(0, 6, 0, 0),
            ForeColor = ThemeManager.Instance.Current.TextPrimary
        };
        _btnAddEnv.Dock = DockStyle.Right;
        _envTopBar.Controls.Add(lblEnvTitle);
        _envTopBar.Controls.Add(_btnAddEnv);

        _envCards.Padding = new Padding(12);

        _envContainer.Controls.Add(_envCards);
        _envContainer.Controls.Add(_envTopBar);
    }

    private void OpenVsCode()
    {
        if (string.IsNullOrWhiteSpace(_project?.LocalPath)) return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "code",
                Arguments = $"\"{_project.LocalPath}\"",
                UseShellExecute = true,
                CreateNoWindow = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not launch VS Code: {ex.Message}", "DevDesk", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void OpenTerminal()
    {
        if (string.IsNullOrWhiteSpace(_project?.LocalPath)) return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoExit -Command Set-Location -LiteralPath '{_project.LocalPath.Replace("'", "''")}'",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open terminal: {ex.Message}", "DevDesk", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void OpenExplorer()
    {
        if (string.IsNullOrWhiteSpace(_project?.LocalPath)) return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{_project.LocalPath}\"",
                UseShellExecute = true
            });
        }
        catch { /* ignore */ }
    }

    private void OpenRepo()
    {
        if (string.IsNullOrWhiteSpace(_project?.RepositoryUrl)) return;
        try
        {
            Process.Start(new ProcessStartInfo(_project.RepositoryUrl) { UseShellExecute = true });
        }
        catch
        {
            Clipboard.SetText(_project.RepositoryUrl);
            MessageBox.Show("Repository URL copied to clipboard.", "DevDesk", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void CopyCloneCommand()
    {
        if (string.IsNullOrWhiteSpace(_project?.RepositoryUrl)) return;
        var cmd = $"git clone {_project.RepositoryUrl}";
        Clipboard.SetText(cmd);
        MessageBox.Show($"Copied to clipboard:\r\n{cmd}", "DevDesk", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    protected override async Task LoadAsync()
    {
        if (_projectId == Guid.Empty) return;
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            _project = await GetService<IProjectService>(scope).GetByIdAsync(_projectId);
            if (_project is null) { ShowEmpty(); return; }

            _header.TitleText = _project.Name;
            _header.SubtitleText = $"{_project.CompletedTasks}/{_project.TotalTasks} tasks · {_project.ProgressPercent:F0}%";

            var hasLocal = !string.IsNullOrWhiteSpace(_project.LocalPath);
            var hasRepo = !string.IsNullOrWhiteSpace(_project.RepositoryUrl);

            _btnVsCode.Visible = hasLocal;
            _btnTerminal.Visible = hasLocal;
            _btnExplorer.Visible = hasLocal;
            _btnRepo.Visible = hasRepo;

            _lblLocalPath.Text = $"Local Path: {(_project.LocalPath ?? "—")}";
            _lblRepo.Text = $"Repository: {(_project.RepositoryUrl ?? "—")}";
            _lblDesc.Text = $"Description: {(_project.Description ?? "No description provided.")}";

            var tasks = await GetService<ITaskService>(scope).GetByProjectAsync(_projectId);
            _taskList.DataSource = tasks.ToList();
            _taskList.DisplayMember = "Title";

            _milestoneList.DataSource = _project.Milestones.ToList();
            _milestoneList.DisplayMember = "Title";

            var notes = (await GetService<INoteService>(scope).GetAllAsync()).Where(n => n.ProjectId == _projectId).ToList();
            _noteList.DataSource = notes;
            _noteList.DisplayMember = "Title";

            var snippets = (await GetService<ISnippetService>(scope).GetAllAsync())
                .Where(s => s.ProjectId == _projectId).ToList();
            _snippetList.DataSource = snippets;
            _snippetList.DisplayMember = "Title";

            var bookmarks = (await GetService<IBookmarkService>(scope).GetAllAsync())
                .Where(b => b.ProjectId == _projectId).ToList();
            _bookmarkList.DataSource = bookmarks;
            _bookmarkList.DisplayMember = "Title";

            var openTasks = tasks.Count(t => t.Status != WorkTaskStatus.Done);
            var completedTasks = tasks.Count(t => t.Status == WorkTaskStatus.Done);

            _lblStats.Text =
                $"Tasks: {completedTasks} of {tasks.Count} completed ({_project.ProgressPercent:F0}%)\r\n" +
                $"Open Tasks: {openTasks} remaining\r\n" +
                $"Milestones: {_project.Milestones.Count} defined\r\n" +
                $"Notes: {notes.Count} | Snippets: {snippets.Count} | Bookmarks: {bookmarks.Count}";

            _analytics.Text =
                $"Project Analytics Overview\r\n\r\n" +
                $"Open tasks: {openTasks}\r\n" +
                $"Completed tasks: {completedTasks}\r\n" +
                $"Progress: {_project.ProgressPercent:F0}%\r\n" +
                $"Milestones: {_project.Milestones.Count}\r\n" +
                $"Notes & Docs: {notes.Count}\r\n" +
                $"Code Snippets: {snippets.Count}";
            _analytics.Font = UiMetrics.Body;
            _analytics.ForeColor = ThemeManager.Instance.Current.TextSecondary;

            await ReloadEnvironmentsAsync();

            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async Task ReloadEnvironmentsAsync()
    {
        using var scope = ScopeFactory.CreateScope();
        var envs = await GetService<IEnvironmentService>(scope).GetByProjectAsync(_projectId);

        _envCards.Controls.Clear();
        if (envs.Count == 0)
        {
            var emptyPanel = new CardPanel
            {
                Width = Math.Max(320, _envContainer.ClientSize.Width - 36),
                Height = 110,
                Margin = new Padding(0, 0, 0, 10)
            };
            var lblEmpty = new Label
            {
                Text = "No environments configured for this project yet.\r\nAdd Development, Staging, or Production connection details.",
                Font = UiMetrics.Body,
                ForeColor = ThemeManager.Instance.Current.TextSecondary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            emptyPanel.Controls.Add(lblEmpty);
            _envCards.Controls.Add(emptyPanel);
            return;
        }

        foreach (var env in envs)
        {
            var card = CreateEnvironmentCard(env);
            _envCards.Controls.Add(card);
        }
    }

    private CardPanel CreateEnvironmentCard(EnvironmentDto env)
    {
        var card = new CardPanel
        {
            Width = Math.Max(340, _envContainer.ClientSize.Width - 40),
            Height = 135,
            Margin = new Padding(0, 0, 0, 12),
            AccentLeft = true,
            AccentColor = env.EnvironmentType switch
            {
                EnvironmentType.Production => ThemeManager.Instance.Current.Error,
                EnvironmentType.Staging => ThemeManager.Instance.Current.Warning,
                EnvironmentType.Testing => Color.FromArgb(59, 130, 246),
                _ => ThemeManager.Instance.Current.Success
            }
        };

        // Header line: Name + Type Badge + Edit/Delete buttons
        var header = new Panel { Dock = DockStyle.Top, Height = 32 };
        var nameLabel = new Label
        {
            Text = $"{env.Name}  [{env.EnvironmentType}]",
            Font = UiMetrics.TaskTitle,
            ForeColor = ThemeManager.Instance.Current.TextPrimary,
            Dock = DockStyle.Left,
            AutoSize = true,
            Padding = new Padding(0, 4, 0, 0)
        };

        var btnDelete = new ModernButton { Text = T("common.delete"), Variant = ButtonVariant.Ghost, Width = 64, Height = 28, Dock = DockStyle.Right };
        var btnEdit = new ModernButton { Text = T("common.edit"), Variant = ButtonVariant.Ghost, Width = 56, Height = 28, Dock = DockStyle.Right };

        btnDelete.Click += async (_, _) =>
        {
            if (ConfirmDialog.Show("Delete Environment", $"Are you sure you want to delete '{env.Name}'?"))
            {
                using var scope = ScopeFactory.CreateScope();
                await GetService<IEnvironmentService>(scope).DeleteAsync(env.Id);
                await ReloadEnvironmentsAsync();
            }
        };

        btnEdit.Click += async (_, _) =>
        {
            if (EnvironmentEditorDialog.ShowEdit(this, ScopeFactory, env))
                await ReloadEnvironmentsAsync();
        };

        header.Controls.Add(nameLabel);
        header.Controls.Add(btnEdit);
        header.Controls.Add(btnDelete);

        // Body content
        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 0)
        };

        if (!string.IsNullOrWhiteSpace(env.BaseUrl))
        {
            var urlRow = new Panel { Width = card.Width - 32, Height = 26 };
            var urlLbl = new Label
            {
                Text = $"URL: {env.BaseUrl}",
                Font = UiMetrics.Body,
                ForeColor = ThemeManager.Instance.Current.Accent,
                Dock = DockStyle.Left,
                AutoSize = true,
                Padding = new Padding(0, 4, 0, 0),
                Cursor = Cursors.Hand
            };
            urlLbl.Click += (_, _) =>
            {
                try { Process.Start(new ProcessStartInfo(env.BaseUrl) { UseShellExecute = true }); } catch { }
            };

            var btnCopyUrl = new ModernButton { Text = "Copy", Variant = ButtonVariant.Ghost, Width = 52, Height = 24, Dock = DockStyle.Right };
            btnCopyUrl.Click += (_, _) => { Clipboard.SetText(env.BaseUrl); MessageBox.Show("URL copied to clipboard.", "DevDesk", MessageBoxButtons.OK, MessageBoxIcon.Information); };

            urlRow.Controls.Add(urlLbl);
            urlRow.Controls.Add(btnCopyUrl);
            body.Controls.Add(urlRow);
        }

        if (!string.IsNullOrWhiteSpace(env.DatabaseServer) || !string.IsNullOrWhiteSpace(env.DatabaseName))
        {
            var dbText = $"DB: {env.DatabaseServer ?? "—"} / {env.DatabaseName ?? "—"}";
            var dbRow = new Panel { Width = card.Width - 32, Height = 26 };
            var dbLbl = new Label
            {
                Text = dbText,
                Font = UiMetrics.Meta,
                ForeColor = ThemeManager.Instance.Current.TextSecondary,
                Dock = DockStyle.Left,
                AutoSize = true,
                Padding = new Padding(0, 4, 0, 0)
            };
            var btnCopyDb = new ModernButton { Text = "Copy", Variant = ButtonVariant.Ghost, Width = 52, Height = 24, Dock = DockStyle.Right };
            btnCopyDb.Click += (_, _) => { Clipboard.SetText(dbText); MessageBox.Show("Database info copied to clipboard.", "DevDesk", MessageBoxButtons.OK, MessageBoxIcon.Information); };

            dbRow.Controls.Add(dbLbl);
            dbRow.Controls.Add(btnCopyDb);
            body.Controls.Add(dbRow);
        }

        if (!string.IsNullOrWhiteSpace(env.Notes))
        {
            var notesLbl = new Label
            {
                Text = $"Note: {env.Notes}",
                Font = UiMetrics.Meta,
                ForeColor = ThemeManager.Instance.Current.TextSecondary,
                AutoSize = true
            };
            body.Controls.Add(notesLbl);
        }

        card.Controls.Add(body);
        card.Controls.Add(header);
        return card;
    }
}
